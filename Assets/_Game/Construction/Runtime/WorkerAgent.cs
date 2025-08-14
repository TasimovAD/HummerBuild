// Assets/_Game/Construction/Runtime/WorkerAgent.cs
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
    HaulJob _haul;
    BuildJob _build;
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
        // ждём JobManager
        float waited = 0f;
        while (JobManager.Instance == null && waited < 5f)
        {
            var found = FindObjectOfType<JobManager>();
            if (found != null) { var _ = JobManager.Instance; break; }
            waited += Time.unscaledDeltaTime;
            yield return null;
        }
        if (JobManager.Instance == null)
        {
            var all = Resources.FindObjectsOfTypeAll<JobManager>();
            Debug.LogError($"[WorkerAgent] JobManager.Instance == null после ожидания {waited:F2}s. Found={all.Length}. Scene='{gameObject.scene.name}', Worker='{name}'", this);
        }

        for (;;)
        {
            if (JobManager.Instance == null) { yield return new WaitForSeconds(0.5f); continue; }

            // 1) Доставка
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

                // кап по дефициту
                int deficit = _site.GetDeficit(_haul.Resource);
                if (deficit <= 0) { _haul.CompleteChunk(0); yield return null; continue; }

                // резерв с учётом массы и дефицита
                int chunk = Mathf.Min(_haul.ReserveChunk(capacityByMass), deficit);
                if (chunk <= 0) { yield return null; continue; }

                _carryingRes = _haul.Resource;
                _carryingAmount = 0;

                // — идём к складу (слоты/оффсет) —
                var loadSlots = _site.Storage.GetComponent<SlotPoints>();
                if (loadSlots && loadSlots.TryAcquire(out var loadSlot))
                {
                    yield return MoveTo(loadSlot.position);
                    loadSlots.Release(loadSlot);
                }
                else
                {
                    Vector3 basePos = _site.Storage.transform.position;
                    Vector3 p = basePos + Random.insideUnitSphere * 1.5f; p.y = basePos.y;
                    yield return MoveTo(p);
                }

                // забираем
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

                // — везём на площадку (слоты/оффсет) —
                var unloadSlots = _site.GetComponent<SlotPoints>();
                if (unloadSlots && unloadSlots.TryAcquire(out var dropSlot))
                {
                    yield return MoveTo(dropSlot.position);

                    // кап на выгрузке по дефициту
                    int deliverCap = _site.GetDeficit(_carryingRes);
                    if (deliverCap > 0)
                    {
                        int toDrop = Mathf.Min(_carryingAmount, deliverCap);
                        int delivered = SafeAdd(_site.Buffer, _carryingRes, toDrop);
                        _carryingAmount -= delivered;
                        _haul.CompleteChunk(delivered);
                    }
                    else
                    {
                        _haul.CompleteChunk(0);
                    }

                    unloadSlots.Release(dropSlot);
                }
                else
                {
                    Vector3 basePos = _site.transform.position;
                    Vector3 p = basePos + Random.insideUnitSphere * 1.5f; p.y = basePos.y;
                    yield return MoveTo(p);

                    int deliverCap = _site.GetDeficit(_carryingRes);
                    if (deliverCap > 0)
                    {
                        int toDrop = Mathf.Min(_carryingAmount, deliverCap);
                        int delivered = SafeAdd(_site.Buffer, _carryingRes, toDrop);
                        _carryingAmount -= delivered;
                        _haul.CompleteChunk(delivered);
                    }
                    else
                    {
                        _haul.CompleteChunk(0);
                    }
                }

                // очистка
                _carryingAmount = 0;
                ClearCarry();
                continue;
            }

            // 2) Стройка
            if (JobManager.Instance.TryGetBuildJob(this, out _build) && _build != null)
            {
                _site = _build.Site;
                if (_site == null) { yield return new WaitForSeconds(0.2f); continue; }

                _site.ActiveWorkersCount++;
                while (_site != null && _site.CanBuildNow())
                {
                    // если нужна “работа” от рабочего — добавь сюда начисление
                    // _site.AddBuildContribution(BuildSpeedFactor);
                    yield return new WaitForSeconds(0.5f);
                }
                _site.ActiveWorkersCount = Mathf.Max(0, _site.ActiveWorkersCount - 1);
                _build = null;
                continue;
            }

            // 3) Idle
            yield return new WaitForSeconds(0.25f);
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
