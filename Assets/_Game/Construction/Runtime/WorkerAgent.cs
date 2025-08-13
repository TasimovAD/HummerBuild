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
    public Transform HandCarrySocket; // сокет в руке для пропа переносимого ресурса

    // Runtime
    BuildSite _site;
    HaulJob _haul;
    BuildJob _build;
    ResourceDef _carryingRes;
    int _carryingAmount;
    GameObject _carryPropInstance;

    void Awake()
    {
        // Автопод хват агента
        if (!Agent) Agent = GetComponent<NavMeshAgent>();
        if (!Agent)
        {
            Debug.LogError($"[WorkerAgent] {name}: Навешай NavMeshAgent на объект.", this);
        }
        else
        {
            Agent.speed = WalkSpeed;
        }
    }

    void OnEnable()
    {
        StartCoroutine(MainLoop());
    }

    IEnumerator MainLoop()
    {
        // === ЖДЁМ ПОЯВЛЕНИЯ JobManager.Instance (на случай домен‑скипа, аддитивных сцен и т.п.) ===
        float waited = 0f;
        while (JobManager.Instance == null && waited < 5f)
        {
            // Подстраховка: попробуем найти менеджер вручную
            var found = FindObjectOfType<JobManager>();
            if (found != null)
            {
                // Доступ к JobManager.Instance вызовет ленивый фолбэк (если он реализован в JobManager)
                var _ = JobManager.Instance;
                break;
            }
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        if (JobManager.Instance == null)
        {
            var all = Resources.FindObjectsOfTypeAll<JobManager>();
            Debug.LogError($"[WorkerAgent] JobManager.Instance == null после ожидания {waited:F2}s. " +
                           $"Найдено через Resources: {all.Length}. Scene='{gameObject.scene.name}', Worker='{name}'", this);
            // Не выходим — ниже будет мягкий ретрай раз в 0.5с
        }

        // === Основной цикл ===
        for (;;)
        {
            // Если менеджер так и не появился — ретраим без спама
            if (JobManager.Instance == null)
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            // 1) Пытаемся взять задачу Доставки (Haul)
            if (JobManager.Instance.TryGetNextHaulJob(this, out _haul))
            {
                if (_haul == null) { yield return new WaitForSeconds(0.2f); continue; }
                if (_haul.Site == null)
                {
                    Debug.LogWarning("[WorkerAgent] Получен HaulJob без Site. Пропущу.", this);
                    yield return new WaitForSeconds(0.2f);
                    continue;
                }
                if (_haul.Resource == null)
                {
                    Debug.LogWarning($"[WorkerAgent] HaulJob для '{_haul.Site.name}' имеет Resource=null. Проверь Requirements у этапа.", this);
                    yield return new WaitForSeconds(0.2f);
                    continue;
                }

                _site = _haul.Site;

                // Проверим обязательные ссылки на стороне BuildSite
                if (_site.Storage == null)
                {
                    Debug.LogError($"[WorkerAgent] BuildSite '{_site.name}' не имеет Storage (адаптер склада). Назначь в инспекторе.", _site);
                    yield return new WaitForSeconds(0.5f);
                    continue;
                }
                if (_site.Buffer == null)
                {
                    Debug.LogError($"[WorkerAgent] BuildSite '{_site.name}' не имеет Buffer (адаптер буфера). Назначь в инспекторе.", _site);
                    yield return new WaitForSeconds(0.5f);
                    continue;
                }

                // Рассчитываем переносимую порцию по массе ресурса
                float unitMass = Mathf.Max(0.01f, _haul.Resource.UnitMass);
                int capacityByMass = Mathf.Max(1, Mathf.FloorToInt(CarryCapacityKg / unitMass));

                int chunk = _haul.ReserveChunk(capacityByMass);
                if (chunk <= 0)
                {
                    // Нечего резервировать — попробуем на следующем кадре другие задачи
                    yield return null;
                    continue;
                }

                _carryingRes = _haul.Resource;
                _carryingAmount = 0;

                // Идём к складу и забираем ресурс
                Vector3 storagePos = _site.Storage.transform.position;
                yield return MoveTo(storagePos);

                int taken = SafeRemove(_site.Storage, _carryingRes, chunk);
                if (taken <= 0)
                {
                    // На складе пусто — освобождаем резерв и идём дальше
                    _haul.CompleteChunk(0);
                    ClearCarry();
                    yield return new WaitForSeconds(0.2f);
                    continue;
                }

                _carryingAmount = taken;
                AttachCarryProp(_carryingRes);

                // Несём на площадку и выгружаем
                Vector3 drop = _site.transform.position;
                yield return MoveTo(drop);

                int delivered = SafeAdd(_site.Buffer, _carryingRes, _carryingAmount);
                _haul.CompleteChunk(delivered);
                ClearCarry();

                // Идём за следующей задачей
                continue;
            }

            // 2) Если доставок нет — пробуем строить (Build)
            if (JobManager.Instance.TryGetBuildJob(this, out _build) && _build != null)
            {
                _site = _build.Site;
                if (_site == null) { yield return new WaitForSeconds(0.2f); continue; }

                _site.ActiveWorkersCount++;
                // Держимся в цикле, пока площадка реально может строить
                while (_site != null && _site.CanBuildNow())
                {
                    // Здесь можно добавить вклад конкретного рабочего, если нужно:
                    // _site.AddBuildContribution(BuildSpeedFactor);
                    yield return new WaitForSeconds(0.5f);
                }
                _site.ActiveWorkersCount = Mathf.Max(0, _site.ActiveWorkersCount - 1);
                _build = null;
                continue;
            }

            // 3) Ничего не нашлось — немного подождать
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
