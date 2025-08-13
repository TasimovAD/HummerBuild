// Assets/_HummerBuild/Construction/Runtime/ConstructionStage.cs
using UnityEngine;
using System;
using System.Collections.Generic;

public enum BuildMode { Batch, Flow } // Batch = нужен полный комплект, Flow = строим по мере поступления

[Serializable]
public class StageRequirement
{
    public ResourceDef Resource;
    public int Amount; // всего нужно на этап
}

[CreateAssetMenu(menuName = "HummerBuild/Construction Stage", fileName = "stage_")]
public class ConstructionStage : ScriptableObject
{
    public string Id;
    public string Title;
    public BuildMode Mode = BuildMode.Flow;
    public float WorkAmount = 120f;      // трудоёмкость в “очках”
    public float TimeMultiplier = 1f;    // множитель сложности/погоды
    public List<StageRequirement> Requirements = new List<StageRequirement>();
}
