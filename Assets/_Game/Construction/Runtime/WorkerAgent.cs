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

    // Контроллер переноски (предпочтительно)
    public WorkerCarryController CarryController;

    // Фоллбэк, если нет CarryController
    public Animator AnimatorFallback;
    public string CarryBoolParam = "Carry";

    // Доступ снаружи (для UI и пр.)
    public ResourceDef CurrentCarryResource => _carryingRes;
    public bool IsCarrying => _carryPropInstance != null;

    // Доп. ссылки для забора визуала с палет
    public PalletGroupManager palletGroup;
    public Transform handSocketFallback; // если нет CarryController/HandCarrySocket

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
            Debug.LogError($"[WorkerAgent] {name}: навесь NavMeshAgent.", this);
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
        float timeout = 15f;
        float t = 0f;
        while (JobManager.Instance == null && t < timeout)
        {
            var found = FindObjectOfType<JobManager>();
            if (found != null) { var _ = JobManager.Instance; break; }
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
            Debug.LogError($"[WorkerAgent] Не получил JobManager.Instance за {timeout:F1}s. Worker='{name}'", this);
            while (JobManager.Instance == null) yield return new WaitForSeconds(0.5f);
        }

        for (;;)
        {
            if (JobManager.Instance == null) { yield return new WaitForSeconds(0.5f); continue; }

            // 1) Пытаемся строить (приоритет)
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
                            yield return new WaitForSeconds(0.5f);

                        _site.ActiveWorkersCount = Mathf.Max(0, _site.ActiveWorkersCount - 1);
                        _build = null;
                        continue;
                    }
                }
            }

            // 2) Доставка
            if (JobManager.Instance.TryGetNextHaulJob(this, out _haul))
            {
                if (_haul == null) { yield return new WaitForSeconds(0.2f); continue; }
                if (_haul.Site == null) { Debug.LogWarning("[WorkerAgent] HaulJob без Site.", this); yield return new WaitForSeconds(0.2f); continue; }
                if (_haul.Resource == null) { Debug.LogWarning($"[WorkerAgent] HaulJob '{_haul.Site?.name}' Resource=null.", this); yield return new WaitForSeconds(0.2f); continue; }

                _site = _haul.Site;

                if (_site.Storage == null) { Debug.LogError($"[WorkerAgent] BuildSite '{_site.name}' без Storage.", _site); yield return new WaitForSeconds(0.5f); continue; }
                if (_site.Buffer == null)  { Debug.LogError($"[WorkerAgent] BuildSite '{_site.name}' без Buffer.", _site);  yield return new WaitForSeconds(0.5f); continue; }

                float unitMass = Mathf.Max(0.01f, _haul.Resource.UnitMass);
                int capacityByMass = Mathf.Max(1, Mathf.FloorToInt(CarryCapacityKg / unitMass));

                int deficit = _site.GetDeficit(_haul.Resource);
                if (deficit <= 0) { _haul.CompleteChunk(0); yield return null; continue; }

                int chunk = Mathf.Min(_haul.ReserveChunk(capacityByMass), deficit);
                if (chunk <= 0) { yield return null; continue; }

                _carryingRes = _haul.Resource;
                _carryingAmount = 0;

                // путь к складу
                yield return MoveViaPickupPoints(_site.Storage.gameObject);

                // ВИЗУАЛ В РУКИ (ключевая часть) - сначала забираем визуал
                AttachCarryProp(_carryingRes);
                
                // забираем с инвентаря (после того, как визуал уже в руках)
                int taken = SafeRemove(_site.Storage, _carryingRes, chunk);
                if (taken <= 0)
                {
                    _haul.CompleteChunk(0);
                    ClearCarry();
                    yield return new WaitForSeconds(0.2f);
                    continue;
                }

                _carryingAmount = taken;

                // везём на стройку
                yield return MoveViaPickupPoints(_site.gameObject);

                // выгрузка в буфер стройки (без учёта inTransit)
                int deliverCap = _site.GetDropCap(_carryingRes);
                int delivered = 0;
                if (deliverCap > 0)
                {
                    int toDrop = Mathf.Min(_carryingAmount, deliverCap);
                    delivered = SafeAdd(_site.Buffer, _carryingRes, toDrop);

                    // визуально — сбросить проп в зону дропа, если что-то доставили
                    if (_carryPropInstance && _site.DropRoot && delivered > 0)
                    {
                        _carryPropInstance.transform.SetParent(_site.DropRoot);
                        Vector3 offset = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
                        _carryPropInstance.transform.localPosition = offset;
                        _carryPropInstance.transform.localRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
                        _carryPropInstance = null; // чтобы ClearCarry не удалил уже «сброшенный» проп
                    }

                    _carryingAmount -= delivered;
                }

                _haul.CompleteChunk(delivered);

                // вернуть хвост на склад и снять зависший inTransit
                if (_carryingAmount > 0)
                {
                    int returned = SafeAdd(_site.Storage, _carryingRes, _carryingAmount);
                    if (returned > 0)
                    {
                        _haul.CancelInTransit(returned);
                        _carryingAmount -= returned;
                    }
                }

                // очистка рук/анимации
                _carryingAmount = 0;
                ClearCarry();

                // после доставки — снова попробуем строить
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

            // 3) Idle
            yield return new WaitForSeconds(0.25f);
        }
    }

    // ---------- движение / вспомогательное ----------

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
            Debug.LogError($"[WorkerAgent] {name}: нет NavMeshAgent.", this);
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

    // ---------- визуал в руках ----------

    void AttachCarryProp(ResourceDef res)
    {
        if (!res) return;
        
        // ВАЖНО: очищаем предыдущий объект перед забором нового
        if (_carryPropInstance)
        {
            ClearCarry();
        }
        
        // ВАЖНО: сбрасываем ссылку на предыдущий объект
        _carryPropInstance = null;

        GameObject picked = null;

        // 1) пробуем забрать готовый проп с палеты через PalletGroupManager
        if (palletGroup)
        {
            var pallet = palletGroup.GetPalletFor(res);   // вернёт палету под этот ресурс
            if (pallet)
            {
                var slots = pallet.GetComponent<ResourcePalletSlots>();
                if (slots)
                {
                    picked = slots.Take();                // может вернуть null, если пусто
                    // ВАЖНО: если получили объект с палеты, проверяем, что он валидный
                    if (picked && !picked.activeInHierarchy)
                    {
                        picked = null; // объект неактивен, создаем новый
                    }
                    // ВАЖНО: проверяем, что объект не был уничтожен
                    if (picked && picked == null)
                    {
                        picked = null; // объект был уничтожен, создаем новый
                    }
                    // ВАЖНО: проверяем, что объект не был уничтожен Unity
                    if (picked && picked.Equals(null))
                    {
                        picked = null; // объект был уничтожен Unity, создаем новый
                    }
                }
            }
        }

        // 2) fallback — если палета пустая, создаём из CarryProp
        if (!picked)
        {
            var prefab = res.CarryProp;
            if (prefab)
            {
                picked = Instantiate(prefab);
            }
            else
            {
                // Нет визуала вообще — включим только анимацию
                if (CarryController)
                {
                    CarryController.Attach(null);
                    if (Agent) Agent.speed = CarryController.currentMoveMul * WalkSpeed;
                }
                else if (AnimatorFallback && !string.IsNullOrEmpty(CarryBoolParam))
                {
                    AnimatorFallback.SetBool(CarryBoolParam, true);
                }
                return;
            }
        }

        _carryPropInstance = picked;

        // ВАЖНО: проверяем, что объект все еще существует
        if (!_carryPropInstance)
        {
            Debug.LogWarning($"[WorkerAgent] Не удалось получить визуальный объект для ресурса {res?.Id}", this);
            return;
        }
        
        // ВАЖНО: проверяем, что объект не был уничтожен Unity
        if (_carryPropInstance.Equals(null))
        {
            Debug.LogWarning($"[WorkerAgent] Объект для ресурса {res?.Id} был уничтожен Unity", this);
            return;
        }

        // 3) гарантируем CarryPropTag (иконки статуса и проверки совпадения)
        var tag = _carryPropInstance.GetComponentInChildren<CarryPropTag>();
        if (!tag) tag = _carryPropInstance.AddComponent<CarryPropTag>();
        tag.resource = res;

        // 4) анимация + посадка
        if (CarryController)
        {
            // Контроллер сам выключит rigidbody/коллайдеры и посадит по CarryGrip
            CarryController.Attach(_carryPropInstance);
            if (Agent) Agent.speed = CarryController.currentMoveMul * WalkSpeed;
        }
        else
        {
            // Фоллбэк: прицепим к сокету и включим bool
            Transform socket = HandCarrySocket ? HandCarrySocket : handSocketFallback;
            if (socket)
            {
                _carryPropInstance.transform.SetParent(socket, false);
                _carryPropInstance.transform.localPosition = Vector3.zero;
                _carryPropInstance.transform.localRotation = Quaternion.identity;
            }

            // отключим физику, чтобы не падал из рук
            if (_carryPropInstance.TryGetComponent<Rigidbody>(out var rb)) Destroy(rb);
            foreach (var c in _carryPropInstance.GetComponentsInChildren<Collider>()) c.enabled = false;

            if (AnimatorFallback && !string.IsNullOrEmpty(CarryBoolParam))
                AnimatorFallback.SetBool(CarryBoolParam, true);
        }
        
        // ВАЖНО: проверяем, что объект успешно прикреплен
        if (_carryPropInstance && _carryPropInstance.transform.parent != null)
        {
            Debug.Log($"[WorkerAgent] Ресурс {res?.Id} успешно прикреплен к рабочему {name}", this);
        }
        else
        {
            Debug.LogWarning($"[WorkerAgent] Не удалось прикрепить ресурс {res?.Id} к рабочему {name}", this);
            // ВАЖНО: если не удалось прикрепить, очищаем состояние
            _carryPropInstance = null;
            _carryingRes = null;
            _carryingAmount = 0;
            return; // ВАЖНО: выходим из метода, если не удалось прикрепить
        }
        
        // ВАЖНО: устанавливаем ресурс только если объект успешно прикреплен
        _carryingRes = res;
    }

    void ClearCarry()
    {
        _carryingRes = null;
        _carryingAmount = 0;

        if (CarryController)
        {
            // если проп всё ещё в руке — удалим, затем детач
            if (CarryController.CurrentProp && CarryController.CurrentProp.transform.parent == CarryController.handSocket)
                Destroy(CarryController.CurrentProp);

            CarryController.Detach();
            if (Agent) Agent.speed = WalkSpeed; // вернуть скорость
        }
        else
        {
            // ВАЖНО: проверяем, что объект все еще существует и прикреплен
            if (_carryPropInstance && _carryPropInstance.transform.parent != null)
                Destroy(_carryPropInstance);
        }

        _carryPropInstance = null;

        // фоллбэк-анимацию тоже выключим
        if (!CarryController && AnimatorFallback && !string.IsNullOrEmpty(CarryBoolParam))
            AnimatorFallback.SetBool(CarryBoolParam, false);
            
        // ВАЖНО: сбрасываем скорость
        if (Agent) Agent.speed = WalkSpeed;
    }
}
