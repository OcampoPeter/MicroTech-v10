using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CoolantIcons : MonoBehaviour
{
    [Header("Find")]
    [SerializeField] LayerMask coolantLayer;       // pick "Coolant"
    [SerializeField] HeatSystem heat;              // leave empty = found automatically
    [SerializeField] RectTransform iconParent;     // leave empty = the Canvas is used

    [Header("Look")]
    [SerializeField] Sprite iconSprite;            // optional, empty = simple diamond
    [SerializeField] Color iconColor = new Color(0.25f, 0.8f, 1f);
    [SerializeField] float iconSize = 40f;
    [SerializeField] float heightAboveSurface = 1.5f;
    [SerializeField] float screenEdgeMargin = 40f; // keeps icons on screen

    class Marker
    {
        public Collider target;
        public RectTransform icon;
        public bool shown;
    }

    readonly List<Marker> markers = new List<Marker>();
    Camera cam;
    Canvas canvas;

    void Start()
    {
        if (coolantLayer.value == 0) coolantLayer = LayerMask.GetMask("Coolant");
        if (heat == null) heat = FindFirstObjectByType<HeatSystem>();

        // use the same Canvas as the heat bar, so the icons are always visible
        if (iconParent == null && heat != null && heat.HeatBar != null)
        {
            Canvas hc = heat.HeatBar.GetComponentInParent<Canvas>(true);
            if (hc != null) iconParent = hc.rootCanvas.GetComponent<RectTransform>();
        }

        if (iconParent == null)
        {
            Canvas c = FindFirstObjectByType<Canvas>();
            if (c != null) iconParent = c.GetComponent<RectTransform>();
        }
        if (iconParent == null) { Debug.LogWarning("CoolantIcons: no Canvas found."); enabled = false; return; }
        canvas = iconParent.GetComponentInParent<Canvas>();

        // one icon for every collider on the Coolant layer
        Collider[] all = FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Collider col in all)
        {
            if ((coolantLayer.value & (1 << col.gameObject.layer)) == 0) continue;
            markers.Add(new Marker { target = col, icon = CreateIcon() });
        }

        Debug.Log("CoolantIcons: found " + markers.Count + " object(s) on the Coolant layer");
        if (markers.Count == 0)
            Debug.LogWarning("CoolantIcons: nothing is on the Coolant layer. Set your platform's Layer to Coolant.");
    }

    RectTransform CreateIcon()
    {
        GameObject go = new GameObject("CoolantIcon", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(iconParent, false);

        Image img = go.GetComponent<Image>();
        img.sprite = iconSprite;
        img.color = iconColor;
        img.raycastTarget = false;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(iconSize, iconSize);
        if (iconSprite == null) rt.localEulerAngles = new Vector3(0f, 0f, 45f);   // square -> diamond

        go.SetActive(false);
        return rt;
    }

    void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null || heat == null) return;

        bool show = heat.IsInDanger;
        Camera uiCam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                       ? canvas.worldCamera : null;

        foreach (Marker m in markers)
        {
            if (m.target == null) continue;

            if (m.shown != show)
            {
                m.icon.gameObject.SetActive(show);
                m.shown = show;
            }
            if (!show) continue;

            // point just above the platform
            Bounds b = m.target.bounds;
            Vector3 world = b.center + Vector3.up * (b.extents.y + heightAboveSurface);
            Vector3 p = cam.WorldToScreenPoint(world);

            // behind the camera: flip so the icon points the way to turn
            if (p.z < 0f)
            {
                p.x = Screen.width - p.x;
                p.y = Screen.height - p.y;
            }
            p.x = Mathf.Clamp(p.x, screenEdgeMargin, Screen.width - screenEdgeMargin);
            p.y = Mathf.Clamp(p.y, screenEdgeMargin, Screen.height - screenEdgeMargin);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(iconParent, p, uiCam, out Vector2 local);
            m.icon.localPosition = local;
        }
    }
}