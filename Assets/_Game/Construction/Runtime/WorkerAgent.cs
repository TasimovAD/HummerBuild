// Assets/_HummerBuild/Construction/Runtime/WorkerAgent.cs
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
    public float BuildSpeedFactor = 1f; // множитель вклада в Build
    public float WagePerMinute = 5f;

    [Header("Ссылки")]
    public NavMeshAgent Agent;
    public Transform HandCarrySocket; // куда цепляем prop

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
        Agent.speed = WalkSpeed;
    }

    void OnEnable()
    {
        StartCoroutine(MainLoop());
    }

    IEnumerator MainLoop()
    {
        for (;;)
        {
            // 1) Пытаемся взять доставку
            if (JobManager.Instance.TryGetNextHaulJob(this, out _haul))
            {
                _site = _haul.Site;

                int capacityByMass = Mathf.Max(1, Mathf.FloorToInt(CarryCapacityKg / Mathf.Max(0.01f, _haul.Resource.UnitMass)));
                int chunk = _haul.ReserveChunk(capacityByMass);
                if (chunk > 0)
                {
                    _carryingRes = _haul.Resource;
                    _carryingAmount = 0; // пока не взяли

                    // Идём на Склад
                    var storagePos = _site.Storage.transform.position;
                    yield return MoveTo(storagePos);

                    // Забираем со склада:
                    int taken = _site.Storage.Remove(_carryingRes, chunk);
                    if (taken <= 0)
                    {
                        // не удалось — бросаем джоб
                        _haul.CompleteChunk(0);
                        ClearCarry();
                        yield return null;
                        continue;
                    }

                    _carryingAmount = taken;
                    AttachCarryProp(_carryingRes);

                    // Везём на площадку
                    var drop = _site.transform.position;
                    yield return MoveTo(drop);

                    int delivered = _site.Buffer.Add(_carryingRes, _carryingAmount);
                    _haul.CompleteChunk(delivered);
                    ClearCarry();
                    // следующий цикл — новая задача
                    continue;
                }
            }

            // 2) Если нет доставки — пробуем строить
            if (JobManager.Instance.TryGetBuildJob(this, out _build))
            {
                _site = _build.Site;

                // Строим, пока можно
                _site.ActiveWorkersCount++;
                float t = 0f;
                while (_site != null && _site.CanBuildNow())
                {
                    // Можно добавить вклад конкретного рабочего:
                    // _site.AddBuildContribution(BuildSpeedFactor);
                    yield return new WaitForSeconds(0.5f);
                    t += 0.5f;
                }
                _site.ActiveWorkersCount = Mathf.Max(0, _site.ActiveWorkersCount - 1);
                _build = null;
                continue;
            }

            // 3) Ничего нет — постоять рядом с площадкой
            yield return new WaitForSeconds(0.5f);
        }
    }

    IEnumerator MoveTo(Vector3 pos)
    {
        Agent.isStopped = false;
        Agent.SetDestination(pos);
        while (Agent.pathPending || Agent.remainingDistance > Agent.stoppingDistance + 0.1f)
            yield return null;
        Agent.isStopped = true;
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
