// Assets/_Game/Construction/Runtime/BuildPanelUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class BuildPanelUI : MonoBehaviour
{
    [Header("Target")]
    public BuildSite Target;

    [Header("UI")]
    public Slider ProgressBar;
    public TMP_Text StageTitle;

    [Header("Pre-placed rows (fixed count, order = visual)")]
    public List<ResourceRowUI> Rows = new List<ResourceRowUI>();

    [Tooltip("Заполнять строки слева-направо данными этапа, остаток — как пустышки")]
    public bool FillLeftToRight = true;

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
        if (!ProgressBar || !Target) return;

        var stage = (Target.Plan && Target.CurrentStageIndex < Target.Plan.Stages.Count)
            ? Target.Plan.Stages[Target.CurrentStageIndex]
            : null;

        float work = (stage != null) ? Mathf.Max(0.0001f, stage.WorkAmount) : 1f;
        ProgressBar.value = Mathf.Clamp01(v / work);
    }

    public void Refresh()
    {
        if (!Target)
        {
            SetAllPlaceholders();
            if (StageTitle) StageTitle.text = "-";
            if (ProgressBar) ProgressBar.value = 0f;
            return;
        }

        // Заголовок
        if (StageTitle)
        {
            var stage = (Target.Plan && Target.CurrentStageIndex < Target.Plan.Stages.Count)
                ? Target.Plan.Stages[Target.CurrentStageIndex]
                : null;
            StageTitle.text = stage ? stage.Title : "Завершено";
        }

        // Соберём данные этапа
        var data = new List<BuildSite.StageResourceUI>();
        foreach (var rowData in Target.GetStageUIRows())
            data.Add(rowData);

        // Заполняем первые N строк данными, остальные — placeholder
        int n = Rows.Count;
        for (int i = 0; i < n; i++)
        {
            var row = Rows[i];
            if (!row) continue;

            if (i < data.Count)
            {
                var d = data[i];
                row.Bind(d.res, d.required, d.deliveredUI /*, d.inTransit*/);
            }
            else
            {
                row.SetPlaceholder(true);
            }
        }

        OnProgressChanged(Target.StageProgress);
    }

    void SetAllPlaceholders()
    {
        foreach (var r in Rows)
            if (r) r.SetPlaceholder(true);
    }

    // Кнопка (опц.)
    public void TogglePause()
    {
        if (!Target) return;
        Target.Pause(!Target.IsPaused);
        Refresh();
    }
}
