// Assets/_Game/Construction/Runtime/ResourceRowUI.cs
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ResourceRowUI : MonoBehaviour
{
    public TMP_Text NameText;
    public TMP_Text CountText;     // формат: "доставлено/нужно (в пути X)"
    public Image Icon;             // опционально

    public void Bind(ResourceDef res, int required, int deliveredUI, int inTransit = 0)
    {
        if (NameText)  NameText.text = res ? res.DisplayName : "—";
        if (CountText)
        {
            if (inTransit > 0)
                CountText.text = $"{deliveredUI}/{required}  (в пути {inTransit})";
            else
                CountText.text = $"{deliveredUI}/{required}";
        }
        if (Icon && res && res.Icon) Icon.sprite = res.Icon;
    }
}
