using UnityEngine;

/// Пример помощника для «подобрать/положить».
public class PlayerCarryPickup : MonoBehaviour
{
    [Header("Refs")]
    public Transform handCarrySocket;      // сокет в руке игрока
    public string carryTag = "Carryable";  // по желанию: предметы с этим тегом можно поднимать

    GameObject _carried;

    /// Поднять конкретный объект (вызывай из своего интеракта)
    public bool AttachToHand(GameObject target, Vector3 localPos, Vector3 localEuler, Vector3 localScale)
    {
        if (_carried || !target || !handCarrySocket) return false;

        _carried = target;
        var t = _carried.transform;
        t.SetParent(handCarrySocket, worldPositionStays: false);
        t.localPosition = localPos;
        t.localEulerAngles = localEuler;
        t.localScale = localScale;

        // уберём физику и коллайдеры, чтобы не мешали
        if (_carried.TryGetComponent<Rigidbody>(out var rb)) Destroy(rb);
        foreach (var col in _carried.GetComponentsInChildren<Collider>()) col.enabled = false;

        return true;
    }

    /// Положить/уронить из руки в мир
    public GameObject DetachFromHand(Vector3 dropWorldPos, Quaternion dropWorldRot, bool enablePhysics = true)
    {
        if (!_carried) return null;

        var go = _carried;
        _carried = null;

        var t = go.transform;
        t.SetParent(null, worldPositionStays: true);
        t.SetPositionAndRotation(dropWorldPos, dropWorldRot);

        foreach (var col in go.GetComponentsInChildren<Collider>()) col.enabled = true;
        if (enablePhysics && !go.TryGetComponent<Rigidbody>(out _))
        {
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 5f;
        }
        return go;
    }

    public bool HasItemInHand => _carried != null;

    // Пример «поднять ближайший по кнопке» (если нужно)
    public bool TryPickupNearest(float radius = 1.2f)
    {
        if (HasItemInHand) return false;
        var cands = Physics.OverlapSphere(transform.position, radius);
        foreach (var c in cands)
        {
            if (!string.IsNullOrEmpty(carryTag) && !c.CompareTag(carryTag)) continue;
            var go = c.attachedRigidbody ? c.attachedRigidbody.gameObject : c.gameObject;

            // базовые оффсеты в руке — под конкретный ресурс лучше хранить на префабе (см. ниже)
            return AttachToHand(go, Vector3.zero, Vector3.zero, Vector3.one);
        }
        return false;
    }
}
