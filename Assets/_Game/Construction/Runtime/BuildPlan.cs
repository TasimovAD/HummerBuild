// Assets/_HummerBuild/Construction/Runtime/BuildPlan.cs
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "HummerBuild/Build Plan", fileName = "buildplan_")]
public class BuildPlan : ScriptableObject
{
    public string PlanId;
    public List<ConstructionStage> Stages = new List<ConstructionStage>();
    public bool AutoStartNext = true;
}
