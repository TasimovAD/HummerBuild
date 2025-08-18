using System.Collections.Generic;
using System.Linq;
using UnityEngine;





/// <summary>
/// Диспетчер задач: доставка ресурсов (Haul) и строительство (Build).
/// </summary>
[DefaultExecutionOrder(-1000)] // 🔑 запускаем раньше остальных
public class JobManager : MonoBehaviour
{
    public static JobManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // 🔑 переживаем смену сцен
        Debug.Log("[JobManager] Ready.");
    }

    // ---------- МОДЕЛИ ЗАДАЧ ----------

    public class HaulJob
    {
        public BuildSite Site;
        public ResourceDef Resource;
        public int RequestedAmount;  // сколько ещё нужно довезти (без inTransit)
        public int InTransit;        // уже зарезервировано/в пути
        public int ChunkSize = 15;

        /// <summary>Резервирует порцию для текущего рабочего (увеличивает InTransit, уменьшает RequestedAmount).</summary>
        public int ReserveChunk(int maxByCapacity)
        {
            int remain = Mathf.Max(0, RequestedAmount);
            if (remain <= 0) return 0;

            int take = Mathf.Min(remain, Mathf.Max(1, Mathf.Min(ChunkSize, maxByCapacity)));
            RequestedAmount -= take;
            InTransit += take;
            return take;
        }

        /// <summary>Подтвердить доставку части груза: уменьшает InTransit.</summary>
        public void CompleteChunk(int delivered)
        {
            if (delivered <= 0) return;
            InTransit = Mathf.Max(0, InTransit - delivered);
        }

        /// <summary>Отменить "зависший" в пути груз (например, рабочий вернул остаток на склад).</summary>
        public void CancelInTransit(int amount)
        {
            if (amount <= 0) return;
            InTransit = Mathf.Max(0, InTransit - amount);
        }
    }

    public class BuildJob
    {
        public BuildSite Site;
    }

    // ---------- ХРАНИЛИЩЕ ЗАДАЧ ----------

    // ключ: (Site, Resource)
    private readonly Dictionary<(BuildSite, ResourceDef), HaulJob> _haul = new();

    // множество площадок с потенциальной Build‑работой
    private readonly HashSet<BuildSite> _buildSites = new();

    // ---------- ПУБЛИЧНОЕ API ----------

    /// <summary>Гарантировать наличие Haul‑задачи. amount = сколько ещё довезти.</summary>
    public void EnsureHaulJob(BuildSite site, ResourceDef res, int amount, int chunk = 15)
    {
        if (!site || !res) return;
        var key = (site, res);

        if (!_haul.TryGetValue(key, out var job))
        {
            job = new HaulJob { Site = site, Resource = res, RequestedAmount = 0, InTransit = 0, ChunkSize = chunk };
            _haul[key] = job;
        }

        // amount — это «актуальный дефицит» на тик; держим RequestedAmount равным ему (но не меньше нуля)
        job.ChunkSize = chunk;
        job.RequestedAmount = Mathf.Max(0, amount);
    }

    /// <summary>Снять Haul‑задачу полностью.</summary>
    public void RemoveHaulJob(BuildSite site, ResourceDef res)
    {
        if (!site || !res) return;
        var key = (site, res);
        _haul.Remove(key);
    }

    /// <summary>Сколько единиц данного ресурса для площадки сейчас в пути (inTransit).</summary>
    public int GetInTransit(BuildSite site, ResourceDef res)
    {
        if (!site || !res) return 0;
        var key = (site, res);
        return _haul.TryGetValue(key, out var job) ? Mathf.Max(0, job.InTransit) : 0;
    }

    /// <summary>Получить следующую задачу доставки для рабочего.</summary>
    public bool TryGetNextHaulJob(WorkerAgent worker, out HaulJob job)
    {
        // Простая эвристика: сортируем по приоритету площадки и величине остатка
        var cand = _haul.Values
            .Where(j => j != null && j.Site != null && j.RequestedAmount > 0)
            .OrderByDescending(j => j.Site.Priority)
            .ThenBy(j => j.Site.DistanceToStorage)
            .ThenByDescending(j => j.RequestedAmount)
            .FirstOrDefault();

        if (cand != null)
        {
            job = cand;
            return true;
        }

        job = null;
        return false;
    }

    /// <summary>Зарегистрировать, что по площадке есть потенциальная Build‑работа.</summary>
    public void EnsureBuildJob(BuildSite site)
    {
        if (!site) return;
        _buildSites.Add(site);
    }

    /// <summary>Снять Build‑работу.</summary>
    public void RemoveBuildJob(BuildSite site)
    {
        if (!site) return;
        _buildSites.Remove(site);
    }

    /// <summary>Есть ли площадки, готовые строить, со свободными слотами.</summary>
    public bool HasReadyBuildSites()
    {
        foreach (var s in _buildSites)
        {
            if (!s) continue;
            if (s.CanBuildNow() && s.HasFreeBuilderSlot()) return true;
        }
        return false;
    }

    /// <summary>Выдать Build‑задачу: приоритет площадкам, которые готовы строить и имеют свободный слот.</summary>
    public bool TryGetBuildJob(WorkerAgent worker, out BuildJob job)
    {
        foreach (var site in _buildSites)
        {
            if (!site) continue;
            if (!site.CanBuildNow()) continue;
            if (!site.HasFreeBuilderSlot()) continue;

            job = new BuildJob { Site = site };
            return true;
        }

        job = null;
        return false;
    }
}
