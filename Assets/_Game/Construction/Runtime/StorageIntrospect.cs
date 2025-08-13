// Assets/_Game/Construction/Runtime/StorageIntrospect.cs
using UnityEngine;
using System.Linq;
using System.Reflection;

public class StorageIntrospect : MonoBehaviour
{
    public MonoBehaviour provider; // перетащи сюда свой StorageInventory

    [ContextMenu("Dump Provider API")]
    void Dump()
    {
        if (!provider) { Debug.LogError("[Introspect] provider == null", this); return; }
        var t = provider.GetType();
        var methods = t.GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)
            .Where(m => !m.IsSpecialName)
            .Select(m => $"{m.Name}({string.Join(", ", m.GetParameters().Select(p=>$"{p.ParameterType.Name} {p.Name}"))}) -> {m.ReturnType.Name}");
        Debug.Log("[Introspect] Methods on " + t.Name + ":\n" + string.Join("\n", methods), provider);
    }

    void Start() { Dump(); } // можно вызвать и из контекстного меню
}
