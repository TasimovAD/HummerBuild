using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

public class BuildSite : MonoBehaviour
{
    [Header("План стройки")]
    public BuildPlan Plan;
    public int CurrentStageIndex = 0;

    [Header("Инвентари (через адаптеры)")]
    public InventoryProviderAdapter Storage;
    public InventoryProviderAdapter Buffer;

    [Header("Параметры диспетчера")]
    public int Priority = 0;
    public float DistanceToStorage = 10f;

    [Header("Скорость строительства")]
    public float BaseBuildSpeedPerWorker = 1f;

    [Header("Ограничение строителей")]
    public int BuilderSlots = 8;

    [Header("Визуальные компоненты")]
    public Transform DropRoot;
    public PalletGroupManager buildPalletGroup;
    public List<PickupPoints> resourceDropPoints;

    // Статус
    public float StageProgress { get; private set; }
    public bool IsPaused { get; private set; }
    public bool IsBlockedByLack { get; private set; }
    public int ActiveWorkersCount = 0;

    public bool HasFreeBuilderSlot() => ActiveWorkersCount < Mathf.Max(1, BuilderSlots);
    public bool IsReserveLocked => _reserveLocked && Stage?.Mode == BuildMode.Batch;

    public ConstructionStage Stage =>
        (Plan != null && CurrentStageIndex < Plan.Stages.Count) ? Plan.Stages[CurrentStageIndex] : null;

    public event Action OnUIChanged;
    public event Action<float> OnStageProgressChanged;

    readonly Dictionary<ResourceDef, int> _reserved = new();
    readonly Dictionary<ResourceDef, int> _deliveredUISnapshot = new();
    Dictionary<ResourceDef, int> _need = new();
    bool _reserveLocked = false;

    void OnEnable()
    {
        if (Storage) Storage.OnChanged += OnInventoryChanged;
        if (Buffer) Buffer.OnChanged += OnInventoryChanged;
        InvokeRepeating(nameof(TickSite), 0.5f, 0.5f);
    }

    void OnDisable()
    {
        if (Storage) Storage.OnChanged -= OnInventoryChanged;
        if (Buffer) Buffer.OnChanged -= OnInventoryChanged;
        CancelInvoke(nameof(TickSite));
    }

    public void Pause(bool pause)
    {
        IsPaused = pause;
        OnUIChanged?.Invoke();
    }

    void OnInventoryChanged(ResourceDef r)
    {
        RecomputeNeeds();
        OnUIChanged?.Invoke();
    }

    void TickSite()
    {
        RecomputeNeeds();

        if (Stage != null && Stage.Mode == BuildMode.Batch && !_reserveLocked && IsFullSetOnSiteOrReserved())
        {
            LockFromBufferToReserve();
            _reserveLocked = true;
            OnUIChanged?.Invoke();

            if (JobManager.Instance != null && Stage.Requirements != null)
                foreach (var req in Stage.Requirements)
                    JobManager.Instance.RemoveHaulJob(this, req.Resource);

            RecomputeNeeds();
        }

        TryDispatchJobs();
        TryBuildTick(0.5f);
    }

    public int GetDeficit(ResourceDef res)
    {
        if (Stage == null || res == null) return 0;
        if (_reserveLocked && Stage.Mode == BuildMode.Batch) return 0;

        int required = RequiredAmountFor(res);
        int reserved = _reserved.TryGetValue(res, out var rsv) ? rsv : 0;
        int inBuffer = Buffer ? Buffer.Get(res) : 0;
        int inTransit = JobManager.Instance ? JobManager.Instance.GetInTransit(this, res) : 0;

        return Mathf.Max(0, required - reserved - inBuffer - inTransit);
    }

    public int GetDropCap(ResourceDef res)
    {
        if (Stage == null || res == null) return 0;

        int required = RequiredAmountFor(res);
        int reserved = _reserved.TryGetValue(res, out var rsv) ? rsv : 0;
        int inBuffer = Buffer ? Buffer.Get(res) : 0;

        return Mathf.Max(0, required - reserved - inBuffer);
    }

    void RecomputeNeeds()
    {
        _need.Clear();
        if (Stage == null || Stage.Requirements == null) return;

        foreach (var req in Stage.Requirements)
        {
            if (req?.Resource == null) continue;
            int lack = GetDeficit(req.Resource);
            _need[req.Resource] = lack;
        }

        bool flowOk = Stage.Mode == BuildMode.Flow &&
                      Stage.Requirements.All(r => Buffer.Get(r.Resource) + (_reserved.TryGetValue(r.Resource, out var v) ? v : 0) > 0);

        bool batchOk = Stage.Mode == BuildMode.Batch &&
                       Stage.Requirements.All(r => Buffer.Get(r.Resource) + (_reserved.TryGetValue(r.Resource, out var v) ? v : 0) >= r.Amount);

        IsBlockedByLack = !((Stage.Mode == BuildMode.Flow && flowOk) || (Stage.Mode == BuildMode.Batch && batchOk));
    }

    public bool CanBuildNow()
    {
        if (IsPaused || Stage == null) return false;

        bool flowOk = Stage.Mode == BuildMode.Flow &&
                      Stage.Requirements.All(r => Buffer.Get(r.Resource) + (_reserved.TryGetValue(r.Resource, out var v) ? v : 0) > 0);

        bool batchOk = Stage.Mode == BuildMode.Batch &&
                      (_reserveLocked || Stage.Requirements.All(r => Buffer.Get(r.Resource) + (_reserved.TryGetValue(r.Resource, out var v) ? v : 0) >= r.Amount));

        return (Stage.Mode == BuildMode.Flow && flowOk) || (Stage.Mode == BuildMode.Batch && batchOk);
    }

    void TryDispatchJobs()
    {
        if (Stage == null || JobManager.Instance == null) return;

        foreach (var kvp in _need)
        {
            var res = kvp.Key;
            int lack = kvp.Value;

            if (lack > 0)
                JobManager.Instance.EnsureHaulJob(this, res, lack, 15);
            else
                JobManager.Instance.RemoveHaulJob(this, res);
        }

        if (CanBuildNow())
            JobManager.Instance.EnsureBuildJob(this);
        else
            JobManager.Instance.RemoveBuildJob(this);
    }

    void TryBuildTick(float dt)
    {
        if (!CanBuildNow() || ActiveWorkersCount <= 0) return;

        if (Stage.Mode == BuildMode.Flow)
            TopUpReserveFromBuffer();
        else if (Stage.Mode == BuildMode.Batch && !_reserveLocked)
        {
            if (IsFullSetOnSiteOrReserved())
            {
                LockFromBufferToReserve();
                _reserveLocked = true;
                OnUIChanged?.Invoke();
            }
            else
            {
                TopUpReserveFromBuffer();
            }
        }

        float buildPts = ActiveWorkersCount * BaseBuildSpeedPerWorker * dt * (Stage?.TimeMultiplier ?? 1f);
        if (buildPts <= 0) return;

        if (Stage.Mode == BuildMode.Flow)
        {
            foreach (var req in Stage.Requirements)
                if ((_reserved.TryGetValue(req.Resource, out var v) ? v : 0) <= 0)
                    return;

            foreach (var req in Stage.Requirements)
                _reserved[req.Resource]--;
        }
        else
        {
            foreach (var req in Stage.Requirements)
            {
                float denom = Mathf.Max(1f, Stage.WorkAmount / (ActiveWorkersCount * BaseBuildSpeedPerWorker));
                float perSec = req.Amount / denom;
                int toRemove = Mathf.CeilToInt(perSec * dt);

                int rsv = _reserved.TryGetValue(req.Resource, out var have) ? have : 0;
                int take = Mathf.Clamp(toRemove, 0, rsv);
                if (take > 0) _reserved[req.Resource] = rsv - take;
            }
        }

        StageProgress += buildPts;
        OnStageProgressChanged?.Invoke(StageProgress);

        if (StageProgress >= (Stage?.WorkAmount ?? 0f))
            CompleteStage();
    }

    bool IsFullSetOnSiteOrReserved()
    {
        if (Stage == null || Stage.Requirements == null) return false;

        foreach (var req in Stage.Requirements)
        {
            int buf = Buffer ? Buffer.Get(req.Resource) : 0;
            int rsv = _reserved.TryGetValue(req.Resource, out var v) ? v : 0;
            if (buf + rsv < req.Amount) return false;
        }

        return true;
    }

    void LockFromBufferToReserve()
    {
        if (Stage == null) return;

        foreach (var req in Stage.Requirements)
        {
            int required = req.Amount;
            int already = _reserved.TryGetValue(req.Resource, out var cur) ? cur : 0;
            int need = Mathf.Max(0, required - already);
            if (need <= 0) continue;

            int haveBuf = Buffer ? Buffer.Get(req.Resource) : 0;
            int take = Mathf.Clamp(need, 0, haveBuf);
            if (take > 0)
            {
                Buffer.Remove(req.Resource, take);
                _reserved[req.Resource] = already + take;
            }
        }

        if (Stage.Mode == BuildMode.Batch)
        {
            foreach (var req in Stage.Requirements)
            {
                int reserved = _reserved.TryGetValue(req.Resource, out var rsv) ? rsv : 0;
                _deliveredUISnapshot[req.Resource] = Mathf.Min(req.Amount, reserved);
            }
        }
    }

    void TopUpReserveFromBuffer()
    {
        if (Stage == null) return;

        foreach (var req in Stage.Requirements)
        {
            int required = req.Amount;
            int already = _reserved.TryGetValue(req.Resource, out var cur) ? cur : 0;
            int need = Mathf.Max(0, required - already);
            if (need <= 0) continue;

            int haveBuf = Buffer ? Buffer.Get(req.Resource) : 0;
            int take = Mathf.Clamp(need, 0, haveBuf);
            if (take > 0)
            {
                Buffer.Remove(req.Resource, take);
                _reserved[req.Resource] = already + take;
            }
        }
    }

    void CompleteStage()
    {
        _reserved.Clear();
        _deliveredUISnapshot.Clear();
        _reserveLocked = false;
        StageProgress = 0f;
        CurrentStageIndex++;
        OnUIChanged?.Invoke();

        if (Stage == null)
        {
            if (JobManager.Instance != null)
            {
                JobManager.Instance.RemoveBuildJob(this);
                foreach (var res in _need.Keys)
                    JobManager.Instance.RemoveHaulJob(this, res);
            }
            _need.Clear();
            return;
        }

        if (Plan != null && Plan.AutoStartNext)
        {
            RecomputeNeeds();
            TryDispatchJobs();
        }
    }

    int RequiredAmountFor(ResourceDef res)
    {
        var req = Stage?.Requirements?.FirstOrDefault(r => r.Resource == res);
        return req != null ? req.Amount : 0;
    }

    public struct StageResourceUI
    {
        public ResourceDef res;
        public int required;
        public int deliveredUI;
        public int inTransit;
    }

    public IEnumerable<StageResourceUI> GetStageUIRows()
    {
        if (Stage == null || Stage.Requirements == null) yield break;
        bool lockedBatch = IsReserveLocked;

        foreach (var req in Stage.Requirements)
        {
            int required = req.Amount;
            int buf = Buffer ? Buffer.Get(req.Resource) : 0;
            int rsv = _reserved.TryGetValue(req.Resource, out var v) ? v : 0;
            int inTransit = JobManager.Instance ? JobManager.Instance.GetInTransit(this, req.Resource) : 0;

            int delivered = lockedBatch
                ? _deliveredUISnapshot.TryGetValue(req.Resource, out var snap) ? Mathf.Min(required, snap) : Mathf.Min(required, rsv)
                : Mathf.Min(required, buf + rsv);

            yield return new StageResourceUI
            {
                res = req.Resource,
                required = required,
                deliveredUI = delivered,
                inTransit = inTransit
            };
        }
    }
}
