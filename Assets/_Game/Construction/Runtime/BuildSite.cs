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

    public int ActiveWorkersCount = 0;

    public event Action OnUIChanged;
    public event Action<float> OnStageProgressChanged;

    // Текущий этап
    ConstructionStage Stage => (Plan != null && Plan.Stages != null && CurrentStageIndex >= 0 && CurrentStageIndex < Plan.Stages.Count)
        ? Plan.Stages[CurrentStageIndex]
        : null;

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

    // дефицит по ресурсу для текущего этапа (учитывает буфер + "в пути")
    public int GetDeficit(ResourceDef res)
    {
        if (Stage == null || res == null) return 0;

        int required = RequiredAmountFor(res);
        if (required <= 0) return 0;

        int inBuffer  = Buffer ? Buffer.Get(res) : 0;
        int inTransit = JobManager.Instance ? JobManager.Instance.GetInTransit(this, res) : 0;

        return Mathf.Max(0, required - inBuffer - inTransit);
    }

    // Быстрая утилита: сколько требуется по ресурсу на текущем этапе
    int RequiredAmountFor(ResourceDef res)
    {
        if (Stage == null || res == null || Stage.Requirements == null) return 0;
        var req = Stage.Requirements.FirstOrDefault(r => r.Resource == res);
        return req != null ? req.Amount : 0;
    }

    // Карта дефицита на тик
    Dictionary<ResourceDef, int> _need = new();

    void RecomputeNeeds()
    {
        _need.Clear();
        if (Stage == null || Stage.Requirements == null) return;

        foreach (var req in Stage.Requirements)
        {
            if (req?.Resource == null) continue;
            int lack = GetDeficit(req.Resource);   // учитываем буфер + в пути
            _need[req.Resource] = lack;
        }

        // Блокировка по ресурсам для UI/диспетчера
        bool allOkFlow = Stage.Mode == BuildMode.Flow
            ? Stage.Requirements.All(r => Buffer.Get(r.Resource) > 0)
            : true;

        bool fullSetBatch = Stage.Mode == BuildMode.Batch
            ? Stage.Requirements.All(r => Buffer.Get(r.Resource) >= r.Amount)
            : true;

        IsBlockedByLack = !( (Stage.Mode == BuildMode.Flow && allOkFlow) || (Stage.Mode == BuildMode.Batch && fullSetBatch) );
    }

    void TryDispatchJobs()
    {
        if (Stage == null) return;

        // Доставки по актуальному дефициту
        foreach (var kvp in _need)
        {
            var res = kvp.Key;
            int lack = kvp.Value;
            if (lack > 0)
                JobManager.Instance.EnsureHaulJob(this, res, lack, chunk: 15);
            else
                JobManager.Instance.RemoveHaulJob(this, res);
        }

        // Строительная задача
        if (CanBuildNow()) JobManager.Instance.EnsureBuildJob(this);
        else               JobManager.Instance.RemoveBuildJob(this);
    }

    public bool CanBuildNow()
    {
        if (IsPaused || Stage == null) return false;

        // Flow: на площадке должен быть хотя бы 1 ед. каждого требуемого ресурса
        bool allRequirementsOk =
            Stage.Requirements != null &&
            Stage.Requirements.All(r => Buffer.Get(r.Resource) > 0);

        // Batch: на площадке должен быть полный комплект по количеству
        bool fullSet =
            Stage.Requirements != null &&
            Stage.Requirements.All(r => Buffer.Get(r.Resource) >= r.Amount);

        bool can =
            (Stage.Mode == BuildMode.Flow  && allRequirementsOk) ||
            (Stage.Mode == BuildMode.Batch && fullSet);

        IsBlockedByLack = !can;
        return can;
    }

    public void Pause(bool pause)
    {
        IsPaused = pause;
        OnUIChanged?.Invoke();
    }

    void TryBuildTick(float dt)
    {
        if (!CanBuildNow() || ActiveWorkersCount <= 0) return;

        float buildPts = ActiveWorkersCount * BaseBuildSpeedPerWorker * dt * (Stage?.TimeMultiplier ?? 1f);
        if (buildPts <= 0) return;

        if (Stage.Mode == BuildMode.Flow)
        {
            // каждый тик тратим по 1 ед. каждого ресурса, только если все присутствуют
            foreach (var req in Stage.Requirements)
                if (Buffer.Get(req.Resource) <= 0) return;

            foreach (var req in Stage.Requirements)
                Buffer.Remove(req.Resource, 1);
        }
        else // Batch
        {
            // пропорционально "работе" тратим ресурсы, но не больше наличия
            foreach (var req in Stage.Requirements)
            {
                // Сколько “единиц” ресурса нужно в секунду, если весь WorkAmount делается ActiveWorkersCount'ом за Stage.WorkAmount/скорость
                float denom = Mathf.Max(1f, Stage.WorkAmount / (ActiveWorkersCount * BaseBuildSpeedPerWorker));
                float perSec = req.Amount / denom;
                int toRemove = Mathf.Clamp(Mathf.CeilToInt(perSec * dt), 0, Buffer.Get(req.Resource));
                if (toRemove > 0) Buffer.Remove(req.Resource, toRemove);
            }
        }

        StageProgress += buildPts;
        OnStageProgressChanged?.Invoke(StageProgress);

        if (StageProgress >= (Stage?.WorkAmount ?? 0f))
            CompleteStage();
    }

    void CompleteStage()
    {
        StageProgress = 0f;
        CurrentStageIndex++;
        OnUIChanged?.Invoke();

        if (Stage == null)
        {
            // стройка завершена
            JobManager.Instance.RemoveBuildJob(this);
            // снимем все оставшиеся доставки
            foreach (var res in _need.Keys) JobManager.Instance.RemoveHaulJob(this, res);
            _need.Clear();
            return;
        }

        if (Plan != null && Plan.AutoStartNext)
        {
            RecomputeNeeds();
            TryDispatchJobs();
        }
    }

    // Для UI панели: сколько нужно, сколько на площадке (буфер)
    public IEnumerable<(ResourceDef res, int required, int onSite, int inTransit)> GetStageResourceInfo()
    {
        if (Stage == null) yield break;
        foreach (var req in Stage.Requirements)
        {
            int onSite = Buffer.Get(req.Resource);
            int inTransit = JobManager.Instance ? JobManager.Instance.GetInTransit(this, req.Resource) : 0;
            yield return (req.Resource, req.Amount, onSite, inTransit);
        }
    }
}
