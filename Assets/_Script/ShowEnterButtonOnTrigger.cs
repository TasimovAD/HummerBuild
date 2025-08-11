using UnityEngine;

public class ShowEnterButtonOnTrigger : MonoBehaviour
{
    [Header("Reference")]
    public CarEnterExit_RCCP carEnterExit;

    [Header("Detection")]
    [Tooltip("Проверять по тегу игрока")]
    public string playerTag = "Player";

    [Tooltip("Опционально: ограничить срабатывание по слоям (оставь 0, чтобы игнорировать)")]
    public LayerMask playerLayerMask;

    [Tooltip("Писать логи в Console для отладки")]
    public bool debugLogs = false;

    private void Reset()
    {
        // Попробовать найти CarEnterExit_RCCP на родителях
        if (!carEnterExit)
            carEnterExit = GetComponentInParent<CarEnterExit_RCCP>();

        // Убедимся, что на этом объекте есть 3D-коллайдер и IsTrigger = true
        var col = GetComponent<Collider>();
        if (!col) col = gameObject.AddComponent<BoxCollider>();
        if (col) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        var go = GetRootObject(other);
        if (IsPlayer(go))
        {
            if (debugLogs) Debug.Log($"[EnterTrigger] OnTriggerEnter by {go.name}", this);
            carEnterExit?.ShowEnterButton(true);
        }
        else
        {
            if (debugLogs) Debug.Log($"[EnterTrigger] Ignored {go.name} (tag={go.tag}, layer={LayerMask.LayerToName(go.layer)})", this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var go = GetRootObject(other);
        if (IsPlayer(go))
        {
            if (debugLogs) Debug.Log($"[EnterTrigger] OnTriggerExit by {go.name}", this);
            carEnterExit?.ShowEnterButton(false);
        }
    }

    private GameObject GetRootObject(Collider other)
    {
        if (other.attachedRigidbody)
            return other.attachedRigidbody.gameObject;
        return other.transform.root.gameObject;
    }

    private bool IsPlayer(GameObject go)
    {
        // Сначала по слоям (если задан playerLayerMask)
        if (playerLayerMask.value != 0)
        {
            if (((1 << go.layer) & playerLayerMask.value) == 0)
                return false;
        }

        // Потом по тегу
        if (!string.IsNullOrEmpty(playerTag) && playerTag != "Untagged")
            return go.CompareTag(playerTag);

        // Если тег не задан — считаем подходящим только по слою
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        var col = GetComponent<Collider>();
        if (!col) return;

        Gizmos.color = Color.cyan;

        if (col is BoxCollider box)
        {
            var m = transform.localToWorldMatrix;
            Gizmos.matrix = m;
            Gizmos.DrawWireCube(box.center, box.size);
        }
        else if (col is SphereCollider sph)
        {
            Gizmos.DrawWireSphere(transform.TransformPoint(sph.center), sph.radius);
        }
        else if (col is CapsuleCollider cap)
        {
            // Примитивная визуализация капсулы
            Gizmos.DrawWireSphere(transform.TransformPoint(cap.center + Vector3.up * (cap.height/2 - cap.radius)), cap.radius);
            Gizmos.DrawWireSphere(transform.TransformPoint(cap.center - Vector3.up * (cap.height/2 - cap.radius)), cap.radius);
        }
    }
}
