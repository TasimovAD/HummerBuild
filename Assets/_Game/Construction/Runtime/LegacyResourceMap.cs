using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(menuName="HummerBuild/Legacy Resource Map", fileName="legacy_res_map")]
public class LegacyResourceMap : ScriptableObject
{
    [Serializable] public class Pair
    {
        public ResourceDef modern;          // твой новый ResourceDef (res_cement и т.п.)
        public ScriptableObject legacy;     // старый SO (cement, log, sand, ...)
    }

    public List<Pair> pairs = new List<Pair>();

    public ScriptableObject ToLegacy(ResourceDef modern)
    {
        if (!modern) return null;
        foreach (var p in pairs) if (p.modern == modern) return p.legacy;
        return null;
    }
}
