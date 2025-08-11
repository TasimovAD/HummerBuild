using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class BuildingSite : MonoBehaviour {
    [Header("Данные этапов (SO, порядок важен)")]
    public BuildingStageData[] stages;

    [Header("Откуда списываем ресурсы (склад)")]
    public StorageInventory storage;

    [Header("UI прогресса")]
    public Slider progressBar;

    [Header("Визуальные состояния (по кол-ву завершенных этапов)")]
    [Tooltip("0: пусто, 1: фундамент, 2: стены, 3: крыша...")]
    public GameObject[] stageVisuals;

    [Header("Текущий прогресс")]
    [Tooltip("Сколько этапов уже завершено")]
    public int currentStageIndex = 0;

    private bool _busy;

    private void Awake() {
        RefreshVisuals();
        if (progressBar) progressBar.value = 0f;
    }

    /// <summary>
    /// Можно ли начать текущий этап (хватает ли ресурсов, не идёт ли уже строительство)
    /// </summary>
    public bool CanStartCurrent() {
        if (_busy) return false;
        if (currentStageIndex >= stages.Length) return false;
        if (!storage) return false;

        var s = stages[currentStageIndex];
        if (s.needs != null) {
            foreach (var n in s.needs) {
                if (!n.type) return false;
                if (storage.Inventory.GetAmount(n.type) < n.amount) return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Возвращает строку с недостающими ресурсами (для UI подсказки)
    /// </summary>
    public string GetMissingNeedsText() {
        if (currentStageIndex >= stages.Length || !storage) return "";
        var s = stages[currentStageIndex];
        if (s.needs == null || s.needs.Length == 0) return "";

        var sb = new StringBuilder();
        foreach (var n in s.needs) {
            if (!n.type) continue;
            int have = storage.Inventory.GetAmount(n.type);
            int need = n.amount;
            if (have < need) {
                int miss = need - have;
                sb.AppendLine($"{n.type.displayName}: не хватает {miss}");
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Кнопка UI вызывает это
    /// </summary>
    public void StartStage() {
        if (!CanStartCurrent()) {
            Debug.LogWarning("[BuildingSite] Нельзя начать этап. " + GetMissingNeedsText());
            return;
        }

        var s = stages[currentStageIndex];

        // Списываем ресурсы со склада
        if (s.needs != null) {
            foreach (var n in s.needs) {
                storage.Inventory.Remove(n.type, n.amount);
            }
        }

        // Старт "строительства"
        StartCoroutine(RunStage(s));
    }

    private IEnumerator RunStage(BuildingStageData data) {
        _busy = true;

        float t = 0f;
        float dur = Mathf.Max(0.1f, data.durationSec);
        while (t < dur) {
            t += Time.deltaTime;
            if (progressBar) progressBar.value = t / dur;
            yield return null;
        }

        // Этап завершён
        _busy = false;
        if (progressBar) progressBar.value = 0f;

        currentStageIndex++;
        RefreshVisuals();

        // TODO: тут можно вызывать события (дом готов? продать и т.п.)
        if (currentStageIndex >= stages.Length) {
            Debug.Log("[BuildingSite] Все этапы завершены!");
        }
    }

    /// <summary>
    /// Включает один визуал согласно числу завершённых этапов
    /// </summary>
    public void RefreshVisuals() {
        if (stageVisuals == null || stageVisuals.Length == 0) return;

        for (int i = 0; i < stageVisuals.Length; i++) {
            if (!stageVisuals[i]) continue;
            // показываем состояние по индексу завершенности
            stageVisuals[i].SetActive(i == Mathf.Clamp(currentStageIndex, 0, stageVisuals.Length - 1));
        }
    }
}
