using UnityEngine;

[CreateAssetMenu(menuName = "Game/Building Stage")]
public class BuildingStageData : ScriptableObject {
    public string stageId;         // "foundation", "walls", "roof"
    public string displayName;
    public float durationSec = 5f; // длительность работ
    public ResourceNeed[] needs;   // что списать со склада перед стартом
}
