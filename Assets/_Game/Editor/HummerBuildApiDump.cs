// Assets/_Game/Editor/HummerBuildApiDump.cs
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class HummerBuildApiDump
{
    [MenuItem("Tools/HummerBuild/Generate API Manifest")]
    public static void Generate()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# HummerBuild API Manifest");
        sb.AppendLine($"_Generated: {DateTime.Now:yyyy-MM-dd HH:mm}_");
        sb.AppendLine();

        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a =>
            {
                var n = a.GetName().Name;
                // Включаем обычные пользовательские сборки (Assembly-CSharp и asmdef-проекты)
                return n == "Assembly-CSharp" || n.StartsWith("Assembly-CSharp-") || n.StartsWith("HB") || n.StartsWith("Hummer");
            })
            .ToArray();

        foreach (var asm in assemblies)
        {
            Type[] types;
            try { types = asm.GetTypes(); }
            catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray(); }

            var interesting = types
                .Where(t => !t.IsAbstract &&
                            (typeof(MonoBehaviour).IsAssignableFrom(t) ||
                             typeof(ScriptableObject).IsAssignableFrom(t)))
                .OrderBy(t => t.Namespace)
                .ThenBy(t => t.Name)
                .ToList();

            if (interesting.Count == 0) continue;

            sb.AppendLine($"## Assembly: `{asm.GetName().Name}`");
            sb.AppendLine();

            foreach (var t in interesting)
            {
                sb.AppendLine($"### {t.FullName}");
                sb.AppendLine(t.IsSubclassOf(typeof(MonoBehaviour)) ? "_MonoBehaviour_" : "_ScriptableObject_");
                sb.AppendLine();

                // Поля (public + [SerializeField] private)
                var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                              .Where(f => f.IsPublic || f.GetCustomAttributes(typeof(SerializeField), true).Any())
                              .OrderBy(f => f.IsPublic ? 0 : 1).ThenBy(f => f.Name)
                              .ToList();
                if (fields.Count > 0)
                {
                    sb.AppendLine("**Fields**:");
                    foreach (var f in fields)
                    {
                        var access = f.IsPublic ? "public" : "[SerializeField]";
                        sb.AppendLine($"- `{access} {f.FieldType.Name} {f.Name}`");
                    }
                    sb.AppendLine();
                }

                // Свойства (public, declared only)
                var props = t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                             .Where(p => p.GetIndexParameters().Length == 0)
                             .OrderBy(p => p.Name).ToList();
                if (props.Count > 0)
                {
                    sb.AppendLine("**Properties**:");
                    foreach (var p in props)
                        sb.AppendLine($"- `public {p.PropertyType.Name} {p.Name} {{ get; set; }}`");
                    sb.AppendLine();
                }

                // Методы (public instance declared only), фильтруем Unity-магические и аксессоры
                string[] unityMagic = { "Awake", "Start", "OnEnable", "OnDisable", "Update", "FixedUpdate", "LateUpdate",
                                        "OnDestroy", "OnDrawGizmos", "OnDrawGizmosSelected", "OnGUI" };
                var methods = t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                               .Where(m => !m.IsSpecialName && !unityMagic.Contains(m.Name))
                               .OrderBy(m => m.Name).ToList();
                if (methods.Count > 0)
                {
                    sb.AppendLine("**Methods**:");
                    foreach (var m in methods)
                    {
                        var pars = string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                        sb.AppendLine($"- `{m.ReturnType.Name} {m.Name}({pars})`");
                    }
                    sb.AppendLine();
                }

                sb.AppendLine("---");
                sb.AppendLine();
            }
        }

        var dir = "Assets/_Game/Docs";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "HummerBuild_API.md");
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh();
        Debug.Log($"[HummerBuildApiDump] Manifest generated: {path}");
    }
}
