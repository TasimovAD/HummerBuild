// Assets/_Game/Construction/Runtime/UI/ResourceRowUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResourceRowUI : MonoBehaviour
{
    [Header("UI refs")]
    public Image Icon;
    public TMP_Text NameText;
    public TMP_Text ProgressText;   // например: "12 / 20"
    public TMP_Text InTransitText;  // опц.: "→ 5" (в пути)

    [Header("Placeholders")]
    public Sprite PlaceholderIcon;
    public string PlaceholderName = "—";
    [Range(0f,1f)] public float PlaceholderAlpha = 0.4f;

    [Header("Colors")]
    public Color NormalColor = Color.white;
    public Color PlaceholderColor = new Color(1,1,1,0.4f);

    // внутреннее: чтобы вернуться из placeholder в норму
    Color _origIconColor, _origNameColor, _origProgColor, _origTransitColor;

    void Awake()
    {
        if (Icon)        _origIconColor = Icon.color;
        if (NameText)    _origNameColor = NameText.color;
        if (ProgressText)_origProgColor = ProgressText.color;
        if (InTransitText) _origTransitColor = InTransitText.color;
    }

    /// Нормальное заполнение строки под реальный ресурс
    public void Bind(ResourceDef res, int required, int delivered, int inTransit = 0)
    {
        if (!res)
        {
            SetPlaceholder(true);
            return;
        }

        // иконки/тексты
        if (Icon)
        {
            Icon.sprite = res.Icon ? res.Icon : PlaceholderIcon;
            Icon.color = NormalColor;
        }
        if (NameText)
        {
            NameText.text = string.IsNullOrEmpty(res.DisplayName) ? res.Id : res.DisplayName;
            NameText.color = NormalColor;
        }
        if (ProgressText)
        {
            ProgressText.text = $"{Mathf.Clamp(delivered,0,required)} / {required}";
            ProgressText.color = NormalColor;
        }
        if (InTransitText)
        {
            // показывать только если есть что-то в пути
            if (inTransit > 0)
            {
                InTransitText.gameObject.SetActive(true);
                InTransitText.text = $"→ {inTransit}";
                InTransitText.color = NormalColor;
            }
            else
            {
                InTransitText.gameObject.SetActive(false);
            }
        }

        // вернуть цвета из placeholder в нормальные
        RestoreNormalColors();
    }

    /// Перевод строки в «пустышку» (ресурс не нужен для этапа)
    public void SetPlaceholder(bool on)
    {
        if (!on)
        {
            RestoreNormalColors();
            return;
        }

        if (Icon)
        {
            Icon.sprite = PlaceholderIcon;
            Icon.color = PlaceholderColor;
        }
        if (NameText)
        {
            NameText.text = PlaceholderName;
            NameText.color = PlaceholderColor;
        }
        if (ProgressText)
        {
            ProgressText.text = "— / —";
            ProgressText.color = PlaceholderColor;
        }
        if (InTransitText)
        {
            InTransitText.gameObject.SetActive(false);
        }
    }

    void RestoreNormalColors()
    {
        if (Icon)         Icon.color = _origIconColor.a > 0 ? _origIconColor : NormalColor;
        if (NameText)     NameText.color = _origNameColor.a > 0 ? _origNameColor : NormalColor;
        if (ProgressText) ProgressText.color = _origProgColor.a > 0 ? _origProgColor : NormalColor;
        if (InTransitText) InTransitText.color = _origTransitColor.a > 0 ? _origTransitColor : NormalColor;
    }
}
