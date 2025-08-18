#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[ExecuteAlways]
public class BuildSiteGizmo : MonoBehaviour
{
    private BuildSite _site;

    void OnDrawGizmos()
    {
        if (_site == null) _site = GetComponent<BuildSite>();
        if (_site == null) return;

        Color color;

        if (_site.Stage == null)
            color = Color.blue; // Завершено
        else if (_site.ActiveWorkersCount > 0)
            color = Color.green; // Идёт строительство
        else if (_site.CanBuildNow())
            color = Color.yellow; // Готово к строительству
        else
            color = Color.red; // Ждёт ресурсы

        Gizmos.color = color;
        Gizmos.DrawWireCube(transform.position + Vector3.up * 1f, new Vector3(3, 1, 3));
    }
}
#endif
