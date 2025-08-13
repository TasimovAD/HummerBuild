// Assets/_HummerBuild/Construction/Runtime/ResourceDef.cs
using UnityEngine;

[CreateAssetMenu(menuName = "HummerBuild/Resource", fileName = "res_")]
public class ResourceDef : ScriptableObject
{
    [Header("Идентификаторы")]
    public string Id;                 // уникальный id (например "wood", "cement")
    public string DisplayName;        // имя для UI

    [Header("Визуал")]
    public Sprite Icon;               // иконка для TopBar/панели
    public GameObject CarryProp;      // префаб, который будет нести рабочий (опц.)

    [Header("Баланс")]
    public float UnitMass = 1f;       // масса за 1 ед. для логистики
}
