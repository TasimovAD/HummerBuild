using UnityEngine;

public class UIPanelTrigger : MonoBehaviour
{
    [Header("Ссылка на UI панель")]
    public GameObject Panel;

    private void Start()
    {
        if (Panel != null)
            Panel.SetActive(false); // при старте выключаем
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) // проверяем, что вошёл игрок
        {
            if (Panel != null)
                Panel.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (Panel != null)
                Panel.SetActive(false);
        }
    }
}
