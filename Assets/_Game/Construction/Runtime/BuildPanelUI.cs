// Assets/_Game/Construction/Runtime/BuildPanelUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildPanelUI : MonoBehaviour
{
    [Header("Target")]
    public BuildSite Target;

    [Header("UI")]
    public Transform ResourceListRoot;
    public GameObject ResourceRowPrefab; // префаб со скриптом ResourceRowUI
    public Slider ProgressBar;
    public TMP_Text StageTitle;

    void OnEnable()
    {
        if (Target != null)
        {
            Target.OnUIChanged += Refresh;
            Target.OnStageProgressChanged += OnProgressChanged;
        }
        Refresh();
    }

    void OnDisable()
    {
        if (Target != null)
        {
            Target.OnUIChanged -= Refresh;
            Target.OnStageProgressChanged -= OnProgressChanged;
        }
    }

    void OnProgressChanged(float v)
    {
        if (ProgressBar != null && Target != null && Target.Plan != null)
        {
            var stage = (Target.CurrentStageIndex < Target.Plan.Stages.Count)
                ? Target.Plan.Stages[Target.CurrentStageIndex]
                : null;

            float work = (stage != null) ? Mathf.Max(0.0001f, stage.WorkAmount) : 1f;
            ProgressBar.value = Mathf.Clamp01(v / work);
        }
    }

    public void Refresh()
    {
        if (!Target) return;

        // Заголовок — используем Title из ConstructionStage
        if (StageTitle)
        {
            var stage = (Target.Plan && Target.CurrentStageIndex < Target.Plan.Stages.Count)
                ? Target.Plan.Stages[Target.CurrentStageIndex]
                : null;
            StageTitle.text = stage ? stage.Title : "Завершено";
        }

        // Очистить список ресурсов
        if (ResourceListRoot)
        {
            for (int i = ResourceListRoot.childCount - 1; i >= 0; i--)
                Destroy(ResourceListRoot.GetChild(i).gameObject);
        }

        // Заполнить ресурсами (берём строки для UI, которые не убывают в Batch)
        foreach (var rowData in Target.GetStageUIRows())
        {
            var go = Instantiate(ResourceRowPrefab, ResourceListRoot);
            var row = go.GetComponent<ResourceRowUI>();

            // Если у тебя Bind(res, required, current) — передаём deliveredUI
            row.Bind(rowData.res, rowData.required, rowData.deliveredUI);

            // Если у тебя есть перегрузка с "в пути", можно вместо этого:
            // row.Bind(rowData.res, rowData.required, rowData.deliveredUI, rowData.inTransit);
        }

        // Обновить прогресс
        OnProgressChanged(Target.StageProgress);
    }

    // Кнопка из UI (опционально)
    public void TogglePause()
    {
        if (!Target) return;
        Target.Pause(!Target.IsPaused);
        Refresh();
    }
}
