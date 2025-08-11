using UnityEngine;

[CreateAssetMenu(menuName = "Game/Resource Type")]
public class ResourceType : ScriptableObject {
    public string id;              // уникальный: "log", "cement", "sand", "water", "mix"
    public string displayName;     // Лес: "Бревно"
    public Sprite icon;
    public string unitName = "шт"; // или "кг", "л"
    public float kgPerUnit = 1f;
    public int stackLimit = 999;
}
