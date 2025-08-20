using UnityEngine;
using UnityEditor;
using System.Linq;

public class SortByHeightEditor : EditorWindow
{
    private GameObject parentObject;
    private bool ascending = true;

    [MenuItem("Tools/Sort Children by Height")]
    public static void ShowWindow()
    {
        GetWindow<SortByHeightEditor>("Sort by Height");
    }

    private void OnGUI()
    {
        GUILayout.Label("Sort Children by Y Position", EditorStyles.boldLabel);

        parentObject = (GameObject)EditorGUILayout.ObjectField("Parent Object", parentObject, typeof(GameObject), true);
        ascending = EditorGUILayout.Toggle("Ascending (Bottom to Top)", ascending);

        if (GUILayout.Button("Sort"))
        {
            if (parentObject == null)
            {
                Debug.LogWarning("Please assign a parent object.");
                return;
            }

            SortChildren();
        }
    }

    private void SortChildren()
    {
        var children = parentObject.transform.Cast<Transform>().ToList();

        children = ascending
            ? children.OrderBy(t => t.position.y).ToList()
            : children.OrderByDescending(t => t.position.y).ToList();

        for (int i = 0; i < children.Count; i++)
        {
            Undo.SetTransformParent(children[i], parentObject.transform, "Sort Children");
            children[i].SetSiblingIndex(i);
        }

        Debug.Log($"Sorted {children.Count} children by Y position.");
    }
}
