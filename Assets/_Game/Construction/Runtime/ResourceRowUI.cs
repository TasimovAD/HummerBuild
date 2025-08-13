// Assets/_HummerBuild/Construction/UI/ResourceRowUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResourceRowUI : MonoBehaviour
{
    public Image Icon;
    public TMP_Text NameText;
    public TMP_Text CountersText; // формат: "На площадке X / Нужно Y"

    public void Bind(ResourceDef res, int required, int onSite)
    {
        if (Icon) Icon.sprite = res.Icon;
        if (NameText) NameText.text = res.DisplayName;
        if (CountersText) CountersText.text = $"На площадке {onSite} / Нужно {required}";
    }
}
