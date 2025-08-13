// Assets/_HummerBuild/Construction/Runtime/JobManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Централизованный менеджер задач: доставка ресурсов (Haul) и строительство (Build)
/// Держи один экземпляр на сцене (BuildSystem)
/// </summary>
public class JobManager : MonoBehaviour
{
    public static JobManager Instance; // удобно, но можешь убрать синглтон и лочить ссылкой

    private readonly List<HaulJob> haulJobs = new();
    private readonly List<BuildJob> buildJobs = new();

    void Awake()
    {
        Instance = this;
    }

    // === API для BuildSite ===
    public void EnsureHaulJob(BuildSite site, ResourceDef res, int needAmount, int chunk)
    {
        if (needAmount <= 0) return;
        var job = haulJobs.FirstOrDefault(j => j.Site == site && j.Resource == res);
        if (job == null)
        {
            job = new HaulJob(site, res);
            haulJobs.Add(job);
        }
        job.RequestedAmount = needAmount;
        job.ChunkSize = Mathf.Max(1, chunk);
    }

    public void RemoveHaulJob(BuildSite site, ResourceDef res)
    {
        haulJobs.RemoveAll(j => j.Site == site && j.Resource == res);
    }

    public void EnsureBuildJob(BuildSite site)
    {
        if (!buildJobs.Any(j => j.Site == site))
            buildJobs.Add(new BuildJob(site));
    }

    public void RemoveBuildJob(BuildSite site)
    {
        buildJobs.RemoveAll(j => j.Site == site);
    }

    // === Выдача задач рабочим ===
    public bool TryGetNextHaulJob(WorkerAgent worker, out HaulJob job)
    {
        // Простая приоритезация: сначала те, где стройка стоит из‑за дефицита
        job = haulJobs
            .Where(j => j.RequestedAmount > 0)
            .OrderByDescending(j => j.Site.IsBlockedByLack) // критично
            .ThenBy(j => j.Site.Priority)                   // приоритет площадки
            .ThenBy(j => j.Site.DistanceToStorage)          // близость
            .FirstOrDefault();
        return job != null;
    }

    public bool TryGetBuildJob(WorkerAgent worker, out BuildJob job)
    {
        job = buildJobs
            .Where(j => j.Site.CanBuildNow())
            .OrderByDescending(j => j.Site.Priority)
            .FirstOrDefault();
        return job != null;
    }
}

// === Модели задач ===
public class HaulJob
{
    public BuildSite Site;
    public ResourceDef Resource;
    public int RequestedAmount; // общий дефицит
    public int InTransit;       // уже кто-то несёт
    public int ChunkSize = 10;

    public HaulJob(BuildSite s, ResourceDef r) { Site = s; Resource = r; }

    public int ReserveChunk(int capacity)
    {
        if (RequestedAmount <= 0) return 0;
        int chunk = Mathf.Min(ChunkSize, RequestedAmount);
        chunk = Mathf.Min(chunk, capacity);
        if (chunk <= 0) return 0;
        RequestedAmount -= chunk;
        InTransit += chunk;
        return chunk;
    }

    public void CompleteChunk(int amountDelivered)
    {
        InTransit = Mathf.Max(0, InTransit - amountDelivered);
        // Остаток дефицита пересчитается на следующем тике BuildSite
    }
}

public class BuildJob
{
    public BuildSite Site;
    public BuildJob(BuildSite s) { Site = s; }
}
