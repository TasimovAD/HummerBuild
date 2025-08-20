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
    public PalletGroupManager palletGroup;
    public Transform handSocket;

    // Runtime
    BuildSite _site;
    JobManager.HaulJob _haul;
    JobManager.BuildJob _build;
    ResourceDef _carryingRes;
    int _carryingAmount;
    GameObject _carryPropInstance;

    // Движение
    bool isMoving = false;
    Vector3 lastDestination;

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
        float timeout = 15f;
        float t = 0f;

        while (JobManager.Instance == null && t < timeout)
        {
            var found = FindObjectOfType<JobManager>();
            if (found != null)
            {
                var _ = JobManager.Instance;
                break;
            }

            if (t > 2f && JobManager.Instance == null && found == null)
            {
                var go = new GameObject("Systems(JobManager_Auto)");
                go.AddComponent<JobManager>();
            }

            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (JobManager.Instance == null)
        {
            Debug.LogError($"[WorkerAgent] Не удалось получить JobManager.Instance за {timeout:F1}s. Worker='{name}'", this);
            while (JobManager.Instance == null) yield return new WaitForSeconds(0.5f);
        }

        for (;;)
        {
            if (JobManager.Instance == null) { yield return new WaitForSeconds(0.5f); continue; }

            bool buildFirst = true;
            try { buildFirst = JobManager.Instance.HasReadyBuildSites(); } catch { }

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
                            yield return new WaitForSeconds(0.5f);
                        }
                        _site.ActiveWorkersCount = Mathf.Max(0, _site.ActiveWorkersCount - 1);
                        _build = null;
                        continue;
                    }
                }
            }

            if (JobManager.Instance.TryGetNextHaulJob(this, out _haul))
            {
                if (_haul == null || _haul.Site == null || _haul.Resource == null)
                {
                    yield return new WaitForSeconds(0.2f);
                    continue;
                }

                _site = _haul.Site;
                if (_site.Storage == null || _site.Buffer == null)
                {
                    Debug.LogError($"[WorkerAgent] BuildSite '{_site.name}' без Storage или Buffer", _site);
                    yield return new WaitForSeconds(0.5f);
                    continue;
                }

                float unitMass = Mathf.Max(0.01f, _haul.Resource.UnitMass);
                int capacityByMass = Mathf.Max(1, Mathf.FloorToInt(CarryCapacityKg / unitMass));
                int deficit = _site.GetDeficit(_haul.Resource);
                if (deficit <= 0) { _haul.CompleteChunk(0); yield return null; continue; }

                int chunk = Mathf.Min(_haul.ReserveChunk(capacityByMass), deficit);
                if (chunk <= 0) { yield return null; continue; }

                _carryingRes = _haul.Resource;
                _carryingAmount = 0;

                yield return MoveViaPickupPoints(_site.Storage.gameObject);

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

                yield return MoveToPalletPickupPoint(_site, _carryingRes);

                int deliverCap = _site.GetDropCap(_carryingRes);
                int delivered = 0;

                if (deliverCap > 0)
                {
                    int toDrop = Mathf.Min(_carryingAmount, deliverCap);
                    delivered = SafeAdd(_site.Buffer, _carryingRes, toDrop);

                    if (_carryPropInstance && _site.buildPalletGroup && delivered > 0)
                    {
                        bool success = _site.buildPalletGroup.TryAddVisual(_carryingRes, _carryPropInstance);
                        if (success) _carryPropInstance = null;
                    }

                    _carryingAmount -= delivered;
                }

                _haul.CompleteChunk(delivered);

                if (_carryingAmount > 0)
                {
                    int returned = SafeAdd(_site.Storage, _carryingRes, _carryingAmount);
                    if (returned > 0)
                    {
                        _haul.CancelInTransit(returned);
                        _carryingAmount -= returned;
                    }
                }

                _carryingAmount = 0;
                ClearCarry();

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

            yield return new WaitForSeconds(0.25f);
        }
    }

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
            Debug.LogError($"[WorkerAgent] {name}: Нет NavMeshAgent", this);
            yield break;
        }

        if (isMoving && Vector3.Distance(pos, lastDestination) < 0.1f)
            yield break;

        while (isMoving)
            yield return null;

        isMoving = true;
        lastDestination = pos;

        Agent.isStopped = false;
        Agent.SetDestination(pos);

        while (Agent.pathPending)
            yield return null;

        while (Agent.remainingDistance > Agent.stoppingDistance + 0.05f)
            yield return null;

        Agent.isStopped = true;
        isMoving = false;
    }

    IEnumerator MoveToPalletPickupPoint(BuildSite site, ResourceDef res)
    {
        if (site.buildPalletGroup == null)
        {
            yield return MoveViaPickupPoints(site.gameObject);
            yield break;
        }

        var pallet = site.buildPalletGroup.GetPalletFor(res);
        if (pallet == null)
        {
            yield return MoveViaPickupPoints(site.gameObject);
            yield break;
        }

        var pp = pallet.GetComponent<PickupPoints>();
        if (pp && pp.TryAcquire(out var slot))
        {
            yield return MoveTo(slot.position);
            pp.Release(slot);
        }
        else
        {
            yield return MoveTo(pallet.transform.position + Vector3.right);
        }
    }

    void AttachCarryProp(ResourceDef res)
    {
        _carryPropInstance = palletGroup.Take(res);
        if (!_carryPropInstance) return;

        _carryPropInstance.transform.SetParent(HandCarrySocket);
        _carryPropInstance.transform.localPosition = Vector3.zero;
        _carryPropInstance.transform.localRotation = Quaternion.identity;
    }

    void ClearCarry()
    {
        _carryingRes = null;
        _carryingAmount = 0;

        if (_carryPropInstance && _carryPropInstance.transform.parent == HandCarrySocket)
            Destroy(_carryPropInstance);

        _carryPropInstance = null;
    }

    int SafeRemove(InventoryProviderAdapter inv, ResourceDef res, int amount)
    {
        if (!inv || !res) return 0;
        return inv.Remove(res, amount);
    }

    int SafeAdd(InventoryProviderAdapter inv, ResourceDef res, int amount)
    {
        if (!inv || !res) return 0;
        return inv.Add(res, amount);
    }
}
