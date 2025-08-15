// Assets/_Game/Construction/Runtime/BuildSite.cs
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

public class BuildSite : MonoBehaviour
{
    [Header("План стройки")]
    public BuildPlan Plan;                  // последовательность этапов
    public int CurrentStageIndex = 0;

    [Header("Инвентари (через адаптеры)")]
    public InventoryProviderAdapter Storage;   // главный склад
    public InventoryProviderAdapter Buffer;    // локальный буфер площадки

    [Header("Параметры диспетчера")]
    public int Priority = 0;
    public float DistanceToStorage = 10f;

    [Header("Скорость строительства")]
    public float BaseBuildSpeedPerWorker = 1f;

    // Runtime
    public float StageProgress { get; private set; }
    public bool IsPaused { get; private set; }
    public bool IsBlockedByLack { get; private set; }
    public int ActiveWorkersCount = 0;

    public event Action OnUIChanged;
    public event Action<float> OnStageProgressChanged;

    // Текущий этап
    ConstructionStage Stage => (Plan != null && Plan.Stages != null &&
                                CurrentStageIndex >= 0 && CurrentStageIndex < Plan.Stages.Count)
        ? Plan.Stages[CurrentStageIndex]
        : null;

    // === РЕЗЕРВ ДЛЯ ЭТАПА ===
    readonly Dictionary<ResourceDef, int> _reserved = new();   // забронировано на этап
    bool _reserveLocked = false;                                // фикс резерва (Batch)

    // Снимок «сколько доставлено» для UI после фиксации (чтобы не убывало)
    readonly Dictionary<ResourceDef, int> _deliveredUISnapshot = new();
    public bool IsReserveLocked => _reserveLocked && Stage?.Mode == BuildMode.Batch;

    // Карта дефицита на тик
    Dictionary<ResourceDef, int> _need = new();

    void OnEnable()
    {
        if (Storage) Storage.OnChanged += OnInventoryChanged;
        if (Buffer)  Buffer.OnChanged  += OnInventoryChanged;
        InvokeRepeating(nameof(TickSite), 0.5f, 0.5f);
    }

    void OnDisable()
    {
        if (Storage) Storage.OnChanged -= OnInventoryChanged;
        if (Buffer)  Buffer.OnChanged  -= OnInventoryChanged;
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

        // ▶ РАННЯЯ БРОНЬ ДЛЯ BATCH: как только полный комплект собран (buffer+reserved)
        if (Stage != null && Stage.Mode == BuildMode.Batch && !_reserveLocked && IsFullSetOnSiteOrReserved())
        {
            LockFromBufferToReserve();   // уводим комплект в резерв
            _reserveLocked = true;
            OnUIChanged?.Invoke();
            RecomputeNeeds();            // после брони дефицит должен стать 0
        }

        TryDispatchJobs();
        TryBuildTick(0.5f);
    }

    // ======= ДЕФИЦИТ / ТРЕБОВАНИЯ =======

    int RequiredAmountFor(ResourceDef res)
    {
        if (Stage == null || res == null || Stage.Requirements == null) return 0;
        var req = Stage.Requirements.FirstOrDefault(r => r.Resource == res);
        return req != null ? req.Amount : 0;
    }

    public int GetDeficit(ResourceDef res)
    {
        if (Stage == null || res == null) return 0;

        // Для Batch после фиксации — довозы отключаем
        if (_reserveLocked && Stage.Mode == BuildMode.Batch)
            return 0;

        int required  = RequiredAmountFor(res);
        int reserved  = _reserved.TryGetValue(res, out var rsv) ? rsv : 0;
        int inBuffer  = Buffer ? Buffer.Get(res) : 0;
        int inTransit = JobManager.Instance ? JobManager.Instance.GetInTransit(this, res) : 0;

        return Mathf.Max(0, required - reserved - inBuffer - inTransit);
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

        // Блокировка/готовность для UI
        bool allOkFlow = Stage.Mode == BuildMode.Flow
            ? Stage.Requirements.All(r =>
            {
                int buf = Buffer.Get(r.Resource);
                int rsv = _reserved.TryGetValue(r.Resource, out var v) ? v : 0;
                return (buf + rsv) > 0;
            })
            : true;

        bool fullSetBatch = Stage.Mode == BuildMode.Batch
            ? Stage.Requirements.All(r =>
            {
                int buf = Buffer.Get(r.Resource);
                int rsv = _reserved.TryGetValue(r.Resource, out var v) ? v : 0;
                return (buf + rsv) >= r.Amount;
            })
            : true;

        IsBlockedByLack = !( (Stage.Mode == BuildMode.Flow  && allOkFlow) ||
                             (Stage.Mode == BuildMode.Batch && fullSetBatch) );
    }

    // Полный комплект на площадке (буфер + резерв) — для старта Batch
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

    // ======= ДИСПЕТЧЕР ЗАДАЧ =======

    void TryDispatchJobs()
    {
        if (Stage == null || JobManager.Instance == null) return;

        // Доставки по актуальному дефициту
        foreach (var kvp in _need)
        {
            var res = kvp.Key;
            int lack = kvp.Value;
            if (lack > 0) JobManager.Instance.EnsureHaulJob(this, res, lack, chunk: 15);
            else          JobManager.Instance.RemoveHaulJob(this, res);
        }

        // Строительная задача
        if (CanBuildNow()) JobManager.Instance.EnsureBuildJob(this);
        else               JobManager.Instance.RemoveBuildJob(this);
    }

    public bool CanBuildNow()
    {
        if (IsPaused || Stage == null) return false;

        bool allRequirementsOk =
            Stage.Requirements != null &&
            Stage.Requirements.All(r =>
            {
                int buf = Buffer.Get(r.Resource);
                int rsv = _reserved.TryGetValue(r.Resource, out var v) ? v : 0;
                return (buf + rsv) > 0;
            });

        bool fullSet =
            Stage.Requirements != null &&
            Stage.Requirements.All(r =>
            {
                int buf = Buffer.Get(r.Resource);
                int rsv = _reserved.TryGetValue(r.Resource, out var v) ? v : 0;
                return (buf + rsv) >= r.Amount;
            });

        bool can =
            (Stage.Mode == BuildMode.Flow  && allRequirementsOk) ||
            (Stage.Mode == BuildMode.Batch && fullSet);

        IsBlockedByLack = !can;
        return can;
    }

    // ======= СТРОИТЕЛЬНЫЙ ТИК =======

    void TryBuildTick(float dt)
    {
        if (!CanBuildNow() || ActiveWorkersCount <= 0) return;

        // Flow — можно подливать резерв из буфера;
        // Batch — после фиксации резерв НЕ пополняем; до фиксации можно добирать до комплекта
        if (Stage.Mode == BuildMode.Flow)
        {
            TopUpReserveFromBuffer();
        }
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
                TopUpReserveFromBuffer(); // добираем до комплекта до фиксации
            }
        }

        float buildPts = ActiveWorkersCount * BaseBuildSpeedPerWorker * dt * (Stage?.TimeMultiplier ?? 1f);
        if (buildPts <= 0) return;

        // Расходуем ИМЕННО резерв (а не буфер)
        if (Stage.Mode == BuildMode.Flow)
        {
            foreach (var req in Stage.Requirements)
            {
                int rsv = _reserved.TryGetValue(req.Resource, out var v) ? v : 0;
                if (rsv <= 0) return; // ждём дозаполнения
            }
            foreach (var req in Stage.Requirements)
                _reserved[req.Resource] -= 1;
        }
        else // Batch
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

    // Перенести как можно ближе к требованиям из буфера в резерв + сделать снимок для UI
    void LockFromBufferToReserve()
    {
        if (Stage == null) return;

        foreach (var req in Stage.Requirements)
        {
            int required = req.Amount;
            int already  = _reserved.TryGetValue(req.Resource, out var cur) ? cur : 0;
            int need     = Mathf.Max(0, required - already);
            if (need <= 0) continue;

            int haveBuf = Buffer ? Buffer.Get(req.Resource) : 0;
            int take = Mathf.Clamp(need, 0, haveBuf);
            if (take > 0)
            {
                Buffer.Remove(req.Resource, take);
                _reserved[req.Resource] = already + take;
            }
        }

        // === Снимок для UI (после фиксации Batch) ===
        if (Stage.Mode == BuildMode.Batch)
        {
            foreach (var req in Stage.Requirements)
            {
                int required = req.Amount;
                int reserved = _reserved.TryGetValue(req.Resource, out var rsv) ? rsv : 0;
                _deliveredUISnapshot[req.Resource] = Mathf.Min(required, reserved);
            }
        }
    }

    // Пополнить резерв из буфера (используется для Flow и для Batch до фиксации)
    void TopUpReserveFromBuffer()
    {
        if (Stage == null) return;
        foreach (var req in Stage.Requirements)
        {
            int required = req.Amount;
            int already  = _reserved.TryGetValue(req.Resource, out var cur) ? cur : 0;
            int need     = Mathf.Max(0, required - already);
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
                foreach (var res in _need.Keys) JobManager.Instance.RemoveHaulJob(this, res);
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

    // === ДАННЫЕ ДЛЯ UI ===
    public struct StageResourceUI
    {
        public ResourceDef res;
        public int required;     // сколько нужно по плану
        public int deliveredUI;  // сколько показывать в панели (не убывает после фиксации Batch)
        public int inTransit;    // в пути
    }

    public IEnumerable<StageResourceUI> GetStageUIRows()
    {
        if (Stage == null || Stage.Requirements == null) yield break;

        bool lockedBatch = IsReserveLocked;

        foreach (var req in Stage.Requirements)
        {
            int required  = req.Amount;
            int buf       = Buffer ? Buffer.Get(req.Resource) : 0;
            int rsv       = _reserved.TryGetValue(req.Resource, out var v) ? v : 0;
            int inTransit = JobManager.Instance ? JobManager.Instance.GetInTransit(this, req.Resource) : 0;

            int delivered;
            if (lockedBatch)
            {
                delivered = _deliveredUISnapshot.TryGetValue(req.Resource, out var snap)
                    ? Mathf.Min(required, snap)
                    : Mathf.Min(required, rsv);
            }
            else
            {
                delivered = Mathf.Min(required, buf + rsv);
            }

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
