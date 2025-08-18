using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public enum WorkerRole { Universal, Concrete, Mason, Carpenter }

public class WorkerAgent : MonoBehaviour
{
    [Header("Параметры")]
    public WorkerRole Role = WorkerRole.Universal;
    public int CarryCapacityKg = 15;
    public float WalkSpeed = 3.5f;
    public float BuildSpeedFactor = 1f;
    public float WagePerMinute = 5f;

    [Header("Ссылки")]
    public NavMeshAgent Agent;
    public Transform HandCarrySocket;

    // Runtime
    BuildSite _site;
    JobManager.HaulJob _haul;
    JobManager.BuildJob _build;
    ResourceDef _carryingRes;
    int _carryingAmount;
    GameObject _carryPropInstance;

    void Awake()
    {
        if (!Agent) Agent = GetComponent<NavMeshAgent>();
        if (!Agent)
            Debug.LogError($"[WorkerAgent] {name}: Навешай NavMeshAgent на объект.", this);
        else
            Agent.speed = WalkSpeed;
    }

    void OnEnable()
    {
        StartCoroutine(MainLoop());
    }

    IEnumerator MainLoop()
    {
        // ЖДЁМ/ИЩЕМ JobManager
    float timeout = 15f; // ждём до 15 сек (можно меньше/больше)
    float t = 0f;

    while (JobManager.Instance == null && t < timeout)
    {
        // попробуем найти вручную в сцене
        var found = FindObjectOfType<JobManager>();
        if (found != null)
        {
            // Awake у него уже проставит Instance
            var _ = JobManager.Instance;
            break;
        }

        // на крайний случай: создадим пустой менеджер динамически (если забыли добавить на сцену)
        if (t > 2f && JobManager.Instance == null && found == null)
        {
            var go = new GameObject("Systems(JobManager_Auto)");
            go.AddComponent<JobManager>();
            // после этого Awake выставит Instance и DontDestroyOnLoad
        }

        t += Time.unscaledDeltaTime;
        yield return null;
    }

    if (JobManager.Instance == null)
    {
        Debug.LogError($"[WorkerAgent] Не удалось получить JobManager.Instance за {timeout:F1}s. Worker='{name}'", this);
        // мягкий ретрай без спама
        while (JobManager.Instance == null) { yield return new WaitForSeconds(0.5f); }
    }

    // --- основной цикл как был ---
    for (;;)
    {
        if (JobManager.Instance == null) { yield return new WaitForSeconds(0.5f); continue; }

        // далее ваш приоритет Build → Haul → Build (как мы делали)

            // ========= ПРИОРИТЕТ: СНАЧАЛА СТРОЙКА (если есть где строить) =========
            bool buildFirst = true; // по умолчанию — отдаём приоритет стройке
            try { buildFirst = JobManager.Instance.HasReadyBuildSites(); } catch { /* на всякий */ }

            if (buildFirst)
            {
                if (JobManager.Instance.TryGetBuildJob(this, out _build) && _build != null)
                {
                    _site = _build.Site;
                    if (_site != null)
                    {
                        _site.ActiveWorkersCount++;
                        while (_site != null && _site.CanBuildNow())
                        {
                            // при желании: вклад от конкретного рабочего
                            // _site.AddBuildContribution(BuildSpeedFactor);
                            yield return new WaitForSeconds(0.5f);
                        }
                        _site.ActiveWorkersCount = Mathf.Max(0, _site.ActiveWorkersCount - 1);
                        _build = null;
                        continue;
                    }
                }
            }

            // ========= ЕСЛИ СТРОИТЬ НЕГДЕ — ПРОБУЕМ ДОСТАВКУ =========
            if (JobManager.Instance.TryGetNextHaulJob(this, out _haul))
            {
                if (_haul == null) { yield return new WaitForSeconds(0.2f); continue; }
                if (_haul.Site == null) { Debug.LogWarning("[WorkerAgent] HaulJob без Site.", this); yield return new WaitForSeconds(0.2f); continue; }
                if (_haul.Resource == null) { Debug.LogWarning($"[WorkerAgent] HaulJob '{_haul.Site?.name}' Resource=null.", this); yield return new WaitForSeconds(0.2f); continue; }

                _site = _haul.Site;

                if (_site.Storage == null) { Debug.LogError($"[WorkerAgent] BuildSite '{_site.name}' без Storage (адаптера).", _site); yield return new WaitForSeconds(0.5f); continue; }
                if (_site.Buffer == null)  { Debug.LogError($"[WorkerAgent] BuildSite '{_site.name}' без Buffer (адаптера).", _site); yield return new WaitForSeconds(0.5f); continue; }

                // грузоподъёмность по массе
                float unitMass = Mathf.Max(0.01f, _haul.Resource.UnitMass);
                int capacityByMass = Mathf.Max(1, Mathf.FloorToInt(CarryCapacityKg / unitMass));

                // кап по дефициту (на момент резервирования)
                int deficit = _site.GetDeficit(_haul.Resource);
                if (deficit <= 0) { _haul.CompleteChunk(0); yield return null; continue; }

                // резерв с учётом массы и дефицита
                int chunk = Mathf.Min(_haul.ReserveChunk(capacityByMass), deficit);
                if (chunk <= 0) { yield return null; continue; }

                _carryingRes = _haul.Resource;
                _carryingAmount = 0;

                // — идём к складу через PickupPoints —
                yield return MoveViaPickupPoints(_site.Storage.gameObject);

                // забираем (может оказаться меньше, чем зарезервировано)
                int taken = SafeRemove(_site.Storage, _carryingRes, chunk);
                if (taken <= 0)
                {
                    _haul.CompleteChunk(0);
                    ClearCarry();
                    yield return new WaitForSeconds(0.2f);
                    continue;
                }

                _carryingAmount = taken;
                AttachCarryProp(_carryingRes);

                // — везём на стройку через её PickupPoints —
                yield return MoveViaPickupPoints(_site.gameObject);

                // кап на выгрузке (БЕЗ учёта inTransit)
                int deliverCap = _site.GetDropCap(_carryingRes);
                int delivered = 0;

                if (deliverCap > 0)
                {
                    int toDrop = Mathf.Min(_carryingAmount, deliverCap);
                    delivered = SafeAdd(_site.Buffer, _carryingRes, toDrop);
                    _carryingAmount -= delivered;
                }

                // фикс реальной доставки
                _haul.CompleteChunk(delivered);

                // если остался хвост — возвращаем на склад и снимаем зависший inTransit
                if (_carryingAmount > 0)
                {
                    int returned = SafeAdd(_site.Storage, _carryingRes, _carryingAmount);
                    if (returned > 0)
                    {
                        _haul.CancelInTransit(returned);
                        _carryingAmount -= returned;
                    }
                }

                // очистка
                _carryingAmount = 0;
                ClearCarry();

                // после доставки попробуем ещё раз строить (вдруг уже есть слот/готовность)
                if (JobManager.Instance.TryGetBuildJob(this, out _build) && _build != null)
                {
                    _site = _build.Site;
                    if (_site != null)
                    {
                        _site.ActiveWorkersCount++;
                        while (_site != null && _site.CanBuildNow())
                            yield return new WaitForSeconds(0.5f);
                        _site.ActiveWorkersCount = Mathf.Max(0, _site.ActiveWorkersCount - 1);
                        _build = null;
                        continue;
                    }
                }

                continue;
            }

            // ========= НИЧЕГО НЕ НАШЛОСЬ — КОРОТКИЙ IDLE =========
            yield return new WaitForSeconds(0.25f);
        }
    }

    // ---------- ДВИЖЕНИЕ / ВСПОМОГАТЕЛЬНЫЕ ----------

    IEnumerator MoveViaPickupPoints(GameObject target)
    {
        var pp = target ? target.GetComponent<PickupPoints>() : null;
        if (pp && pp.TryAcquire(out var slot))
        {
            yield return MoveTo(slot.position);
            pp.Release(slot);
        }
        else
        {
            Vector3 basePos = target ? target.transform.position : transform.position;
            Vector3 p = basePos + Random.insideUnitSphere * 1.5f; p.y = basePos.y;
            yield return MoveTo(p);
        }
    }

    IEnumerator MoveTo(Vector3 pos)
    {
        if (!Agent)
        {
            Debug.LogError($"[WorkerAgent] {name}: Нет NavMeshAgent — перемещение невозможно.", this);
            yield break;
        }

        Agent.isStopped = false;
        Agent.SetDestination(pos);

        while (Agent.pathPending || Agent.remainingDistance > Agent.stoppingDistance + 0.05f)
            yield return null;

        Agent.isStopped = true;
    }

    int SafeRemove(InventoryProviderAdapter inv, ResourceDef res, int amount)
    {
        if (!inv) { Debug.LogError("[WorkerAgent] SafeRemove: Inventory == null", this); return 0; }
        if (!res) { Debug.LogError("[WorkerAgent] SafeRemove: Resource == null", this); return 0; }
        return inv.Remove(res, amount);
    }

    int SafeAdd(InventoryProviderAdapter inv, ResourceDef res, int amount)
    {
        if (!inv) { Debug.LogError("[WorkerAgent] SafeAdd: Inventory == null", this); return 0; }
        if (!res) { Debug.LogError("[WorkerAgent] SafeAdd: Resource == null", this); return 0; }
        return inv.Add(res, amount);
    }

    void AttachCarryProp(ResourceDef res)
    {
        if (!res || !res.CarryProp || !HandCarrySocket) return;
        _carryPropInstance = Instantiate(res.CarryProp, HandCarrySocket);
        _carryPropInstance.transform.localPosition = Vector3.zero;
        _carryPropInstance.transform.localRotation = Quaternion.identity;
    }

    void ClearCarry()
    {
        _carryingRes = null;
        _carryingAmount = 0;
        if (_carryPropInstance) Destroy(_carryPropInstance);
        _carryPropInstance = null;
    }
}
