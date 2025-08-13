using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public class BuildSite : MonoBehaviour
{
    [Header("План стройки")]
    public BuildPlan Plan;                  // последовательность этапов
    public int CurrentStageIndex = 0;

    [Header("Инвентари")]
    public InventoryProviderAdapter Storage;   // адаптер на главный Склад (общий)
    public InventoryProviderAdapter Buffer;    // локальный буфер площадки

    [Header("Параметры")]
    public int Priority = 0;
    public float DistanceToStorage = 10f;

    [Header("Скорость строительства")]
    public float BaseBuildSpeedPerWorker = 1f;

    // Runtime
    public float StageProgress { get; private set; }
    public bool IsPaused { get; private set; }
    public bool IsBlockedByLack { get; private set; }

    public event Action OnUIChanged;
    public event Action<float> OnStageProgressChanged;

    ConstructionStage Stage => (Plan != null && CurrentStageIndex < Plan.Stages.Count) ? Plan.Stages[CurrentStageIndex] : null;

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

    void OnInventoryChanged(ResourceDef r)
    {
        RecomputeNeeds();
        OnUIChanged?.Invoke();
    }

    void TickSite()
    {
        RecomputeNeeds();
        TryDispatchJobs();
        TryBuildTick(0.5f);
    }

    Dictionary<ResourceDef, int> _need = new();

    void RecomputeNeeds()
    {
        _need.Clear();
        if (Stage == null) return;

        foreach (var req in Stage.Requirements)
        {
            int haveOnSite = Buffer.Get(req.Resource);
            int required = req.Amount;
            int lack = Mathf.Max(0, required - haveOnSite);
            _need[req.Resource] = lack;
        }
    }

    void TryDispatchJobs()
    {
        if (Stage == null) return;

        foreach (var kvp in _need)
        {
            var res = kvp.Key;
            int lack = kvp.Value;
            if (lack > 0)
            {
                JobManager.Instance.EnsureHaulJob(this, res, lack, chunk: 15);
            }
            else
            {
                JobManager.Instance.RemoveHaulJob(this, res);
            }
        }

        if (CanBuildNow()) JobManager.Instance.EnsureBuildJob(this);
        else JobManager.Instance.RemoveBuildJob(this);
    }

    public bool CanBuildNow()
    {
        if (IsPaused || Stage == null) return false;

        bool allRequirementsOk =
            Stage.Requirements.All(r => Buffer.Get(r.Resource) > 0);

        bool fullSet =
            Stage.Requirements.All(r => Buffer.Get(r.Resource) >= r.Amount);

        bool can =
            (Stage.Mode == BuildMode.Flow && allRequirementsOk) ||
            (Stage.Mode == BuildMode.Batch && fullSet);

        IsBlockedByLack = !can;
        return can;
    }

    public void Pause(bool pause)
    {
        IsPaused = pause;
        OnUIChanged?.Invoke();
    }

    public int ActiveWorkersCount = 0;

    void TryBuildTick(float dt)
    {
        if (!CanBuildNow() || ActiveWorkersCount <= 0) return;

        float buildPts = ActiveWorkersCount * BaseBuildSpeedPerWorker * dt * (Stage?.TimeMultiplier ?? 1f);
        if (buildPts <= 0) return;

        if (Stage.Mode == BuildMode.Flow)
        {
            foreach (var req in Stage.Requirements)
            {
                if (Buffer.Get(req.Resource) <= 0)
                    return;
            }
            foreach (var req in Stage.Requirements) Buffer.Remove(req.Resource, 1);
        }
        else
        {
            foreach (var req in Stage.Requirements)
            {
                float perSec = req.Amount / Mathf.Max(1f, Stage.WorkAmount / (ActiveWorkersCount * BaseBuildSpeedPerWorker));
                int toRemove = Mathf.Clamp(Mathf.CeilToInt(perSec * dt), 0, Buffer.Get(req.Resource));
                Buffer.Remove(req.Resource, toRemove);
            }
        }

        StageProgress += buildPts;
        OnStageProgressChanged?.Invoke(StageProgress);

        if (StageProgress >= (Stage?.WorkAmount ?? 0f))
        {
            CompleteStage();
        }
    }

    void CompleteStage()
    {
        StageProgress = 0f;
        CurrentStageIndex++;
        OnUIChanged?.Invoke();

        if (Stage == null)
        {
            JobManager.Instance.RemoveBuildJob(this);
            foreach (var res in _need.Keys) JobManager.Instance.RemoveHaulJob(this, res);
            return;
        }

        if (Plan != null && Plan.AutoStartNext)
        {
            RecomputeNeeds();
            TryDispatchJobs();
        }
    }

    public IEnumerable<(ResourceDef res, int required, int onSite)> GetStageResourceInfo()
    {
        if (Stage == null) yield break;
        foreach (var req in Stage.Requirements)
            yield return (req.Resource, req.Amount, Buffer.Get(req.Resource));
    }
}
