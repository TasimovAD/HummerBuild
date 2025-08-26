using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Утилита для создания UI префаба для взаимодействия с багажником.
/// Используйте в редакторе для быстрого создания UI.
/// </summary>
public class TrunkUISetup : MonoBehaviour
{
    [Header("Настройки UI")]
    [Tooltip("Размер панели")]
    public Vector2 panelSize = new Vector2(300, 150);
    
    [Tooltip("Цвет фона панели")]
    public Color backgroundColor = new Color(0, 0, 0, 0.8f);

    /// <summary>
    /// Создает префаб UI для багажника. Вызывайте из контекстного меню.
    /// </summary>
    [ContextMenu("Create Trunk UI Prefab")]
    public void CreateTrunkUIPrefab()
    {
        // Создаем корневую панель
        GameObject panel = new GameObject("TrunkInteractionPanel");
        
        // Добавляем компонент Image для фона
        Image bgImage = panel.AddComponent<Image>();
        bgImage.color = backgroundColor;
        
        // Настраиваем RectTransform
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.sizeDelta = panelSize;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;

        // Добавляем Vertical Layout Group для автоматического размещения элементов
        VerticalLayoutGroup layoutGroup = panel.AddComponent<VerticalLayoutGroup>();
        layoutGroup.spacing = 10f;
        layoutGroup.padding = new RectOffset(20, 20, 20, 20);
        layoutGroup.childAlignment = TextAnchor.MiddleCenter;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;

        // Создаем текст подсказки
        GameObject hintTextGO = CreateTextElement("HintText", "Подойдите к багажнику", panel.transform);
        TextMeshProUGUI hintText = hintTextGO.GetComponent<TextMeshProUGUI>();
        hintText.fontSize = 14f;
        hintText.color = Color.white;
        hintText.alignment = TextAlignmentOptions.Center;
        
        // Настраиваем Layout Element для текста
        LayoutElement hintLayout = hintTextGO.AddComponent<LayoutElement>();
        hintLayout.preferredHeight = 30f;

        // Создаем кнопку загрузки
        GameObject loadButton = CreateButtonElement("LoadButton", "Загрузить в багажник", panel.transform);
        SetupButton(loadButton, Color.green);
        
        // Создаем кнопку выгрузки  
        GameObject unloadButton = CreateButtonElement("UnloadButton", "Выгрузить из багажника", panel.transform);
        SetupButton(unloadButton, Color.yellow);

        // Привязываем скрипт для автоматической настройки
        TrunkUIPrefabLinker linker = panel.AddComponent<TrunkUIPrefabLinker>();
        linker.hintText = hintText;
        linker.loadButton = loadButton.GetComponent<Button>();
        linker.unloadButton = unloadButton.GetComponent<Button>();

        Debug.Log($"[TrunkUISetup] UI префаб создан: {panel.name}");
        
        // Сохраняем как префаб (в редакторе)
        #if UNITY_EDITOR
        string prefabPath = "Assets/_Game/Prefabs/UI/TrunkInteractionPanel.prefab";
        // Создаем директорию если не существует
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(prefabPath));
        UnityEditor.PrefabUtility.SaveAsPrefabAsset(panel, prefabPath);
        Debug.Log($"[TrunkUISetup] Префаб сохранен: {prefabPath}");
        #endif
    }

    /// <summary>
    /// Создает текстовый элемент
    /// </summary>
    GameObject CreateTextElement(string name, string text, Transform parent)
    {
        GameObject textGO = new GameObject(name);
        textGO.transform.SetParent(parent, false);

        // Добавляем TextMeshProUGUI
        TextMeshProUGUI textComp = textGO.AddComponent<TextMeshProUGUI>();
        textComp.text = text;
        textComp.fontSize = 16f;
        textComp.color = Color.white;
        textComp.alignment = TextAlignmentOptions.Center;

        return textGO;
    }

    /// <summary>
    /// Создает элемент кнопки
    /// </summary>
    GameObject CreateButtonElement(string name, string text, Transform parent)
    {
        GameObject buttonGO = new GameObject(name);
        buttonGO.transform.SetParent(parent, false);

        // Добавляем Image для фона кнопки
        Image buttonImage = buttonGO.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        // Добавляем компонент Button
        Button button = buttonGO.AddComponent<Button>();
        button.targetGraphic = buttonImage;

        // Создаем текст для кнопки
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);

        TextMeshProUGUI buttonText = textGO.AddComponent<TextMeshProUGUI>();
        buttonText.text = text;
        buttonText.fontSize = 14f;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;

        // Настраиваем RectTransform для текста кнопки
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return buttonGO;
    }

    /// <summary>
    /// Настраивает внешний вид кнопки
    /// </summary>
    void SetupButton(GameObject buttonGO, Color normalColor)
    {
        Button button = buttonGO.GetComponent<Button>();
        Image buttonImage = buttonGO.GetComponent<Image>();

        // Настраиваем цветовую схему кнопки
        ColorBlock colors = button.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = Color.Lerp(normalColor, Color.white, 0.3f);
        colors.pressedColor = Color.Lerp(normalColor, Color.black, 0.3f);
        colors.disabledColor = Color.gray;
        button.colors = colors;

        // Настраиваем размер кнопки
        LayoutElement layout = buttonGO.AddComponent<LayoutElement>();
        layout.preferredHeight = 40f;
        layout.preferredWidth = 200f;
    }
}

/// <summary>
/// Компонент для связи UI элементов с VehicleTrunkPlayerInteraction.
/// Автоматически привязывается к созданному префабу.
/// </summary>
public class TrunkUIPrefabLinker : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI hintText;
    public Button loadButton;
    public Button unloadButton;

    /// <summary>
    /// Привязывает UI элементы к компоненту взаимодействия с багажником
    /// </summary>
    public void LinkToTrunkInteraction(VehicleTrunkPlayerInteraction trunkInteraction)
    {
        if (!trunkInteraction)
        {
            Debug.LogWarning("[TrunkUIPrefabLinker] Не передан VehicleTrunkPlayerInteraction");
            return;
        }

        // Привязываем элементы
        trunkInteraction.interactionPanel = gameObject;
        trunkInteraction.hintText = hintText;
        trunkInteraction.loadButton = loadButton;
        trunkInteraction.unloadButton = unloadButton;

        // Привязываем методы к кнопкам
        if (loadButton)
        {
            loadButton.onClick.RemoveAllListeners();
            loadButton.onClick.AddListener(trunkInteraction.LoadToTrunk);
        }

        if (unloadButton)
        {
            unloadButton.onClick.RemoveAllListeners();
            unloadButton.onClick.AddListener(trunkInteraction.UnloadFromTrunk);
        }

        Debug.Log("[TrunkUIPrefabLinker] UI связан с VehicleTrunkPlayerInteraction");
    }

    /// <summary>
    /// Автоматически ищет и привязывается к ближайшему VehicleTrunkPlayerInteraction
    /// </summary>
    void Start()
    {
        // Ищем в родительских объектах
        VehicleTrunkPlayerInteraction trunkInteraction = GetComponentInParent<VehicleTrunkPlayerInteraction>();
        
        if (!trunkInteraction)
        {
            // Ищем в сцене
            trunkInteraction = FindObjectOfType<VehicleTrunkPlayerInteraction>();
        }

        if (trunkInteraction)
        {
            LinkToTrunkInteraction(trunkInteraction);
        }
    }
}