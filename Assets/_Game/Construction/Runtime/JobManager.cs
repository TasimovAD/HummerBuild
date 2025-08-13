// Assets/_Game/Construction/Runtime/JobManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Централизованный менеджер задач: доставка (Haul) и строительство (Build).
/// Должен быть один активный экземпляр на сцене (Singleton).
/// </summary>
[DefaultExecutionOrder(-10000)] // Инициализируйся раньше воркеров/прочих систем
public class JobManager : MonoBehaviour
{
    // ===== Singleton =====
    private static JobManager _instance;
    public static JobManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Фолбэк — найдём объект в сцене (на случай сброса статики)
                _instance = FindObjectOfType<JobManager>();
            }
            return _instance;
        }
    }

    // Обнуление статики при входе в Play Mode, если включён "Enter Play Mode (No Domain Reload)"
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Prewarm()
    {
        _instance = null;
    }

    // Автосоздание, если забыли положить JobManager в сцену
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance == null)
        {
            var go = new GameObject("BuildSystem(JobManager_Auto)");
            _instance = go.AddComponent<JobManager>();
            DontDestroyOnLoad(go);
            Debug.Log($"[JobManager] Auto-created (id={_instance.GetInstanceID()})");
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning($"[JobManager] Duplicate detected. Keep id={_instance.GetInstanceID()}, destroy id={GetInstanceID()} on '{name}'");
            Destroy(gameObject);
            return;
        }
        _instance = this;
        Debug.Log($"[JobManager] Ready (id={GetInstanceID()}, scene='{gameObject.scene.name}')");
    }

    // ===== Хранилища задач =====
    private readonly List<HaulJob> haulJobs = new();
    private readonly List<BuildJob> buildJobs = new();

    // ===== API для BuildSite =====

    /// <summary>Создать/обновить доставку ресурса на площадку.</summary>
    public void EnsureHaulJob(BuildSite site, ResourceDef res, int needAmount, int chunk)
    {
        if (!site || !res || needAmount <= 0) return;

        var job = haulJobs.FirstOrDefault(j => j.Site == site && j.Resource == res);
        if (job == null)
        {
            job = new HaulJob(site, res);
            haulJobs.Add(job);
            // Debug.Log($"[JobManager] HaulJob added: {res.DisplayName} → {site.name}");
        }

        job.RequestedAmount = needAmount;
        job.ChunkSize = Mathf.Max(1, chunk);
    }

    /// <summary>Удалить доставку ресурса на площадку.</summary>
    public void RemoveHaulJob(BuildSite site, ResourceDef res)
    {
        if (!site || !res) return;
        haulJobs.RemoveAll(j => j.Site == site && j.Resource == res);
    }

    /// <summary>Гарантировать наличие строительной задачи для площадки.</summary>
    public void EnsureBuildJob(BuildSite site)
    {
        if (!site) return;
        if (!buildJobs.Any(j => j.Site == site))
            buildJobs.Add(new BuildJob(site));
    }

    /// <summary>Удалить строительную задачу площадки.</summary>
    public void RemoveBuildJob(BuildSite site)
    {
        if (!site) return;
        buildJobs.RemoveAll(j => j.Site == site);
    }

    // ===== API для WorkerAgent =====

    /// <summary>Выдать воркеру следующую задачу доставки.</summary>
    public bool TryGetNextHaulJob(WorkerAgent worker, out HaulJob job)
{
    job = null;
    if (haulJobs.Count == 0) return false;

    // 1) сначала только те, по которым есть ресурс на складе
    var available = haulJobs
        .Where(j => j != null && j.RequestedAmount > 0 && j.Site != null && j.Resource != null)
        .Where(j => j.Site.Storage != null && j.Site.Storage.Get(j.Resource) > 0) // <-- фильтр
        .OrderByDescending(j => j.Site.IsBlockedByLack)
        .ThenByDescending(j => j.Site.Priority)
        .ThenBy(j => j.Site.DistanceToStorage)
        .FirstOrDefault();

    if (available != null) { job = available; return true; }

    // 2) если на складе пусто по всем — оставляем старую эвристику (пусть кто-то хотя бы проверит)
    job = haulJobs
        .Where(j => j != null && j.RequestedAmount > 0 && j.Site != null && j.Resource != null)
        .OrderByDescending(j => j.Site.IsBlockedByLack)
        .ThenByDescending(j => j.Site.Priority)
        .ThenBy(j => j.Site.DistanceToStorage)
        .FirstOrDefault();

    return job != null;
}


    /// <summary>Выдать воркеру задачу строительства.</summary>
    public bool TryGetBuildJob(WorkerAgent worker, out BuildJob job)
    {
        job = null;
        if (buildJobs.Count == 0) return false;

        job = buildJobs
            .Where(j => j != null && j.Site != null && j.Site.CanBuildNow())
            .OrderByDescending(j => j.Site.Priority)
            .FirstOrDefault();

        return job != null;
    }

    // ===== Диагностика/утилиты (по желанию для UI) =====

    public int GetActiveHaulJobsCount() =>
        haulJobs.Count(j => j != null && j.RequestedAmount > 0 && j.Site != null && j.Resource != null);

    public int GetActiveBuildJobsCount() =>
        buildJobs.Count(j => j != null && j.Site != null && j.Site.CanBuildNow());

    /// <summary>Сколько по ресурсу сейчас числится "в пути" (для UI).</summary>
    public int GetInTransit(BuildSite site, ResourceDef res)
    {
        var j = haulJobs.FirstOrDefault(x => x.Site == site && x.Resource == res);
        return j != null ? j.InTransit : 0;
    }
}

// ====================== МОДЕЛИ ЗАДАЧ ======================

public class HaulJob
{
    public BuildSite Site;
    public ResourceDef Resource;

    /// <summary>Сколько ещё требуется доставить (уменьшается ReserveChunk'ом).</summary>
    public int RequestedAmount;

    /// <summary>Сколько уже несут рабочие (для UI “в пути”).</summary>
    public int InTransit;

    /// <summary>Желаемый размер одной “порции” на рейс.</summary>
    public int ChunkSize = 10;

    public HaulJob(BuildSite s, ResourceDef r)
    {
        Site = s;
        Resource = r;
    }

    /// <summary>Зарезервировать порцию под грузоподъёмность воркера.</summary>
    public int ReserveChunk(int capacity)
    {
        if (Site == null || Resource == null) return 0;
        if (RequestedAmount <= 0) return 0;

        int chunk = Mathf.Min(ChunkSize, RequestedAmount);
        chunk = Mathf.Min(chunk, Mathf.Max(1, capacity));
        if (chunk <= 0) return 0;

        RequestedAmount -= chunk;
        InTransit += chunk;
        return chunk;
    }

    /// <summary>Отметить доставку (или провал, если amountDelivered=0).</summary>
    public void CompleteChunk(int amountDelivered)
    {
        InTransit = Mathf.Max(0, InTransit - Mathf.Max(0, amountDelivered));
        // Остаток дефицита BuildSite пересчитает на следующем тике.
    }
}

public class BuildJob
{
    public BuildSite Site;
    public BuildJob(BuildSite s) { Site = s; }
}
