// Assets/_HummerBuild/Construction/UI/BuildPanelUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class BuildPanelUI : MonoBehaviour
{
    public BuildSite Target;

    [Header("Верх")]
    public TMP_Text TitleText;
    public TMP_Text StageText;
    public Slider StageProgressSlider;

    [Header("Список ресурсов")]
    public Transform ResourceListRoot;
    public GameObject ResourceRowPrefab; // элемент списка: иконка, названия, бары

    [Header("Кнопки")]
    public Toggle FlowModeToggle;
    public Button PauseButton;

    void OnEnable()
    {
        if (Target != null)
        {
            Target.OnUIChanged += Refresh;
            Target.OnStageProgressChanged += OnProgress;
        }
        Refresh();
    }

    void OnDisable()
    {
        if (Target != null)
        {
            Target.OnUIChanged -= Refresh;
            Target.OnStageProgressChanged -= OnProgress;
        }
    }

    void OnProgress(float v)
    {
        var st = GetStage();
        if (st != null) StageProgressSlider.value = Mathf.Clamp01(v / Mathf.Max(1f, st.WorkAmount));
    }

    ConstructionStage GetStage() => Target == null ? null : (Target.Plan != null && Target.CurrentStageIndex < Target.Plan.Stages.Count ? Target.Plan.Stages[Target.CurrentStageIndex] : null);

    public void Refresh()
    {
        if (!Target) return;

        var st = GetStage();
        TitleText.text = Target.Plan ? Target.Plan.PlanId : "Объект";
        StageText.text = st ? $"{st.Title}" : "Готово";

        if (FlowModeToggle) FlowModeToggle.isOn = (st && st.Mode == BuildMode.Flow);

        // Пересобираем список ресурсов
        foreach (Transform c in ResourceListRoot) Destroy(c.gameObject);
        if (st)
        {
            foreach (var (res, required, onSite) in Target.GetStageResourceInfo())
            {
                var go = Instantiate(ResourceRowPrefab, ResourceListRoot);
                var row = go.GetComponent<ResourceRowUI>();
                row.Bind(res, required, onSite);
            }
        }

        // Прогресс
        OnProgress(Target.StageProgress);
    }

    // UI callbacks
    public void OnFlowModeChanged(bool isFlow)
    {
        var st = GetStage();
        if (st == null) return;
        st.Mode = isFlow ? BuildMode.Flow : BuildMode.Batch; // если хочешь — делай копию Stage на runtime
        Target?.GetType().GetMethod("RecomputeNeeds", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.Invoke(Target, null);
    }

    public void OnPauseClicked()
    {
        Target?.Pause(!Target.IsPaused);
        PauseButton.GetComponentInChildren<TMP_Text>().text = Target.IsPaused ? "Продолжить" : "Пауза";
    }
}
