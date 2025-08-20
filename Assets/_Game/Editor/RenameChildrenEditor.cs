using UnityEditor;
using UnityEngine;

public class RenameChildrenEditor : EditorWindow
{
    private GameObject parentObject;
    private string baseName = "slot";

    [MenuItem("Tools/Rename Children")]
    static void ShowWindow()
    {
        GetWindow<RenameChildrenEditor>("Rename Children");
    }

    void OnGUI()
    {
        GUILayout.Label("Rename Children of GameObject", EditorStyles.boldLabel);

        parentObject = (GameObject)EditorGUILayout.ObjectField("Parent Object", parentObject, typeof(GameObject), true);
        baseName = EditorGUILayout.TextField("Base Name", baseName);

        if (GUILayout.Button("Rename All Children"))
        {
            if (parentObject != null)
            {
                RenameChildren();
            }
            else
            {
                Debug.LogWarning("Выберите родительский объект!");
            }
        }
    }

    void RenameChildren()
    {
        int count = 0;
        foreach (Transform child in parentObject.transform)
        {
            Undo.RecordObject(child.gameObject, "Rename Child");
            child.name = $"{baseName}_{count}";
            count++;
        }
        Debug.Log($"Переименовано {count} объектов.");
    }
}
