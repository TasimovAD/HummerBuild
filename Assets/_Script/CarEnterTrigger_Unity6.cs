using UnityEngine;

/// <summary>
/// Триггер для входа в машину - Unity 6 Starter Assets версия
/// Показывает кнопку входа при приближении игрока к машине
/// </summary>
[RequireComponent(typeof(Collider))]
public class CarEnterTrigger_Unity6 : MonoBehaviour
{
    [Header("Car Enter System")]
    [Tooltip("Ссылка на систему входа/выхода в машину")]
    public CarEnterExit_Unity6StarterAssets carEnterSystem;

    [Header("Trigger Settings")]
    [Tooltip("Тег игрока для проверки")]
    public string playerTag = "Player";

    [Tooltip("Показывать отладочные сообщения")]
    public bool debugMode = false;

    private void Awake()
    {
        // Убеждаемся, что коллайдер настроен как триггер
        var trigger = GetComponent<Collider>();
        if (!trigger.isTrigger)
        {
            trigger.isTrigger = true;
            if (debugMode)
                Debug.Log($"CarEnterTrigger: Установлен isTrigger = true для {gameObject.name}");
        }

        // Автоматически находим CarEnterExit_Unity6StarterAssets если не задан
        if (!carEnterSystem)
        {
            carEnterSystem = GetComponentInParent<CarEnterExit_Unity6StarterAssets>();
            if (!carEnterSystem)
                carEnterSystem = FindObjectOfType<CarEnterExit_Unity6StarterAssets>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) return;

        if (debugMode)
            Debug.Log($"CarEnterTrigger: Player entered trigger zone - {other.name}");

        if (carEnterSystem && !carEnterSystem.IsInCar)
        {
            carEnterSystem.ShowEnterButton(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other)) return;

        if (debugMode)
            Debug.Log($"CarEnterTrigger: Player exited trigger zone - {other.name}");

        if (carEnterSystem)
        {
            carEnterSystem.ShowEnterButton(false);
        }
    }

    private bool IsPlayer(Collider other)
    {
        return other.CompareTag(playerTag);
    }

    private void OnDrawGizmosSelected()
    {
        // Показываем зону триггера в редакторе
        var trigger = GetComponent<Collider>();
        if (trigger)
        {
            Gizmos.color = Color.green;
            Gizmos.matrix = transform.localToWorldMatrix;
            
            if (trigger is BoxCollider box)
            {
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (trigger is SphereCollider sphere)
            {
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            }
            else if (trigger is CapsuleCollider capsule)
            {
                // Упрощенное отображение капсулы как сферы
                Gizmos.DrawWireSphere(capsule.center, capsule.radius);
            }
        }
    }
}