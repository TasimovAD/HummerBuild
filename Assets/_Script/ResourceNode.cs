using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ResourceNode : MonoBehaviour
{
    [Header("Что добываем")]
    public ResourceType outputType;
    public int batchAmount = 5;
    public float harvestTime = 2f;
    public int maxBatches = 20; // 0 = бесконечно

    [Header("UI (для мобилки)")]
    public Slider progressBar; // прогресс добычи
    public GameObject harvestButton; // кнопка "Собрать ресурсы"

    private int _done;
    private bool _busy;
    private CharacterInventory _currentPlayer;

    private void Start()
    {
        if (harvestButton) harvestButton.SetActive(false);
        if (progressBar) progressBar.gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponentInParent<CharacterInventory>();
        if (player)
        {
            _currentPlayer = player;
            if (harvestButton)
            {
                harvestButton.SetActive(true);
                harvestButton.GetComponent<Button>().onClick.RemoveAllListeners();
                harvestButton.GetComponent<Button>().onClick.AddListener(() => TryStartHarvest());
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var player = other.GetComponentInParent<CharacterInventory>();
        if (player && player == _currentPlayer)
        {
            _currentPlayer = null;
            if (harvestButton) harvestButton.SetActive(false);
        }
    }

    public void TryStartHarvest()
    {
        if (_busy) return;
        if (maxBatches > 0 && _done >= maxBatches)
        {
            Debug.Log("Ресурс исчерпан");
            return;
        }
        if (_currentPlayer == null) return;

        StartCoroutine(HarvestRoutine(_currentPlayer));
    }

    private IEnumerator HarvestRoutine(InventoryProvider target)
    {
        _busy = true;
        if (progressBar) { progressBar.gameObject.SetActive(true); progressBar.value = 0f; }

        float t = 0f;
        float dur = Mathf.Max(0.1f, harvestTime);
        while (t < dur)
        {
            t += Time.deltaTime;
            if (progressBar) progressBar.value = t / dur;
            yield return null;
        }

        int added = _currentPlayer.Inventory.Add(outputType, batchAmount);
        Debug.Log($"[Node] Added {added} {outputType?.id}");

        if (progressBar) progressBar.gameObject.SetActive(false);
        _busy = false;
    }
}
