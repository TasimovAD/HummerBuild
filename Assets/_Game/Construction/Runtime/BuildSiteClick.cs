using UnityEngine;

public class BuildSiteClick : MonoBehaviour
{
    public BuildPanelUI Panel;
    BuildSite _site;

    void Awake()
    {
        _site = GetComponent<BuildSite>();
    }

    void OnMouseUpAsButton() // требует Collider и включенной камеры
    {
        if (!_site || !Panel) return;
        Panel.Target = _site;
        Panel.gameObject.SetActive(true);
        Panel.Refresh(); // на всякий случай вручную обновим
    }
}
