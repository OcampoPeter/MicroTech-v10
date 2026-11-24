// WHERE TO PUT THIS FILE: Assets > Codes > UI-Code  (replace the old CoolantHighlight.cs)
// WHERE TO USE IT: on the "CoolantIcons" object (same one as before)
// Draws the edges of a box around Coolant platforms on the screen, always on top (shows through walls).
// Only platforms close to the player are drawn, and they fade out toward the edge of the radius.
// Needs the HeatSystem.cs that has IsInDanger and HeatBar.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(1000)]   // runs after the camera has moved, so the lines stay on the platform
public class CoolantHighlight : MonoBehaviour
{
    [Header("Find")]
    [SerializeField] LayerMask coolantLayer;      // pick "Coolant" (empty = auto)
    [SerializeField] HeatSystem heat;             // leave empty = found automatically
    [SerializeField] RectTransform lineParent;    // leave empty = the heat bar's Canvas
    [SerializeField] Camera playerCamera;         // leave empty = the Main Camera

    [Header("Radius")]
    [SerializeField] float showRadius = 30f;      // only platforms this close to the player show lines
    [Range(0.1f, 1f)]
    [SerializeField] float fadeStart = 0.7f;      // lines start fading at this part of the radius

    [Header("Look")]
    [SerializeField] Color lineColor = new Color(0.25f, 0.8f, 1f, 1f);
    [SerializeField] float lineThickness = 3f;
    [Range(0.1f, 0.5f)]
    [SerializeField] float lineLength = 0.5f;     // 0.5 = full lines, 0.25 = only the corners

    class Box
    {
        public Transform t;        // null = bounds are already in world space
        public Bounds bounds;
        public RectTransform[] lines = new RectTransform[24];   // 12 edges x 2 pieces
        public Image[] images = new Image[24];
        public bool visible;
    }

    readonly List<Box> boxes = new List<Box>();
    Camera cam;
    Canvas canvas;
    bool shown;

    void Start()
    {
        if (coolantLayer.value == 0) coolantLayer = LayerMask.GetMask("Coolant");
        if (heat == null) heat = FindFirstObjectByType<HeatSystem>();

        if (lineParent == null && heat != null && heat.HeatBar != null)
        {
            Canvas hc = heat.HeatBar.GetComponentInParent<Canvas>(true);
            if (hc != null) lineParent = hc.rootCanvas.GetComponent<RectTransform>();
        }
        if (lineParent == null)
        {
            Canvas c = FindFirstObjectByType<Canvas>();
            if (c != null) lineParent = c.GetComponent<RectTransform>();
        }
        if (lineParent == null) { Debug.LogWarning("CoolantHighlight: no Canvas found."); enabled = false; return; }
        canvas = lineParent.GetComponentInParent<Canvas>();

        // boxes from meshes on the Coolant layer (follows rotation and moving platforms)
        MeshFilter[] filters = FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (MeshFilter mf in filters)
        {
            if ((coolantLayer.value & (1 << mf.gameObject.layer)) == 0) continue;
            if (mf.sharedMesh == null) continue;
            AddBox(mf.transform, mf.sharedMesh.bounds);
        }

        // objects on the layer with a collider but no mesh
        Collider[] cols = FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Collider col in cols)
        {
            if ((coolantLayer.value & (1 << col.gameObject.layer)) == 0) continue;
            if (col.GetComponent<MeshFilter>() != null) continue;
            AddBox(null, col.bounds);
        }

        Debug.Log("CoolantHighlight: made outlines for " + boxes.Count + " object(s) on the Coolant layer");
        if (boxes.Count == 0)
            Debug.LogWarning("CoolantHighlight: nothing is on the Coolant layer.");
    }

    void AddBox(Transform t, Bounds b)
    {
        Box box = new Box { t = t, bounds = b };
        for (int i = 0; i < box.lines.Length; i++)
        {
            GameObject go = new GameObject("CoolantLine", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(lineParent, false);

            Image img = go.GetComponent<Image>();
            img.color = lineColor;
            img.raycastTarget = false;

            go.SetActive(false);
            box.lines[i] = go.GetComponent<RectTransform>();
            box.images[i] = img;
        }
        boxes.Add(box);
    }

    void LateUpdate()
    {
        if (heat == null) return;
        cam = playerCamera != null ? playerCamera : Camera.main;
        if (cam == null) return;

        bool show = heat.IsInDanger;
        if (!show)
        {
            if (shown) HideAll();
            shown = false;
            return;
        }
        shown = true;

        Camera uiCam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                       ? canvas.worldCamera : null;
        Vector3 playerPos = heat.transform.position;

        foreach (Box box in boxes)
            DrawBox(box, uiCam, playerPos);
    }

    void DrawBox(Box box, Camera uiCam, Vector3 playerPos)
    {
        // how far is the player from the box?
        float dist = DistanceToBox(box, playerPos);
        if (dist >= showRadius)
        {
            if (box.visible) HideBox(box);
            return;
        }
        box.visible = true;

        // full strength close up, fades out toward the edge of the radius
        float fade = Mathf.InverseLerp(showRadius, showRadius * fadeStart, dist);
        Color col = lineColor;
        col.a = lineColor.a * fade;
        foreach (Image img in box.images) img.color = col;

        // the 8 corners of the box, in the world
        Vector3[] c = new Vector3[8];
        Vector3 min = box.bounds.min;
        Vector3 max = box.bounds.max;
        for (int i = 0; i < 8; i++)
        {
            Vector3 p = new Vector3((i & 1) == 0 ? min.x : max.x,
                                    (i & 2) == 0 ? min.y : max.y,
                                    (i & 4) == 0 ? min.z : max.z);
            c[i] = box.t != null ? box.t.TransformPoint(p) : p;
        }

        // 12 edges: corners that differ in exactly one direction
        int line = 0;
        for (int i = 0; i < 8; i++)
        {
            for (int bit = 1; bit <= 4; bit *= 2)
            {
                if ((i & bit) != 0) continue;

                Vector3 a = c[i];
                Vector3 b = c[i | bit];
                DrawSegment(box.lines[line++], a, Vector3.Lerp(a, b, lineLength), uiCam);
                DrawSegment(box.lines[line++], b, Vector3.Lerp(b, a, lineLength), uiCam);
            }
        }
    }

    float DistanceToBox(Box box, Vector3 worldPoint)
    {
        if (box.t == null)
            return Vector3.Distance(box.bounds.ClosestPoint(worldPoint), worldPoint);

        Vector3 local = box.t.InverseTransformPoint(worldPoint);
        Vector3 closestLocal = box.bounds.ClosestPoint(local);
        return Vector3.Distance(box.t.TransformPoint(closestLocal), worldPoint);
    }

    void DrawSegment(RectTransform line, Vector3 a, Vector3 b, Camera uiCam)
    {
        // cut the part that is behind the camera
        float near = cam.nearClipPlane + 0.01f;
        Vector3 camPos = cam.transform.position;
        Vector3 fwd = cam.transform.forward;
        float za = Vector3.Dot(a - camPos, fwd);
        float zb = Vector3.Dot(b - camPos, fwd);

        if (za < near && zb < near) { line.gameObject.SetActive(false); return; }
        if (za < near) a = Vector3.Lerp(a, b, (near - za) / (zb - za));
        else if (zb < near) b = Vector3.Lerp(b, a, (near - zb) / (za - zb));

        Vector3 sa = cam.WorldToScreenPoint(a);
        Vector3 sb = cam.WorldToScreenPoint(b);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(lineParent, sa, uiCam, out Vector2 la);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(lineParent, sb, uiCam, out Vector2 lb);

        Vector2 delta = lb - la;
        line.localPosition = (la + lb) * 0.5f;
        line.sizeDelta = new Vector2(delta.magnitude, lineThickness);
        line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

        if (!line.gameObject.activeSelf) line.gameObject.SetActive(true);
    }

    void HideBox(Box box)
    {
        box.visible = false;
        foreach (RectTransform l in box.lines)
            if (l != null) l.gameObject.SetActive(false);
    }

    void HideAll()
    {
        foreach (Box box in boxes) HideBox(box);
    }

    // Select the CoolantIcons object in the Scene view to see the radius (yellow ball)
    void OnDrawGizmosSelected()
    {
        HeatSystem h = heat != null ? heat : FindFirstObjectByType<HeatSystem>();
        if (h == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(h.transform.position, showRadius);
    }
}