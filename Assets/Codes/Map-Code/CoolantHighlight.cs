using System.Collections.Generic; // lets us use List
using UnityEngine; // Unity basics
using UnityEngine.UI; // lets us use UI Image

[DefaultExecutionOrder(1000)] // run late, after the camera has moved
public class CoolantHighlight : MonoBehaviour // draws outline boxes around Coolant platforms
{
    [Header("Find")] // Inspector title
    [SerializeField] LayerMask coolantLayer; // which layer the Coolant platforms are on
    [SerializeField] HeatSystem heat; // the heat script (found automatically if empty)
    [SerializeField] RectTransform lineParent; // the UI object that holds the lines
    [SerializeField] Camera playerCamera; // the camera used to draw (Main Camera if empty)

    [Header("Radius")] // Inspector title
    [SerializeField] float showRadius = 30f; // only platforms this close to the player show lines
    [Range(0.1f, 1f)] // slider from 0.1 to 1
    [SerializeField] float fadeStart = 0.7f; // lines start fading at this part of the radius

    [Header("Look")] // Inspector title
    [SerializeField] Color lineColor = new Color(0.25f, 0.8f, 1f, 1f); // color of the lines
    [SerializeField] float lineThickness = 3f; // how thick the lines are
    [Range(0.1f, 0.5f)] // slider from 0.1 to 0.5
    [SerializeField] float lineLength = 0.5f; // 0.5 = full lines, 0.25 = only the corners

    class Box // holds the data for one outlined platform
    {
        public Transform t; // the platform's transform (null = bounds already in world space)
        public Bounds bounds; // the size of the box
        public RectTransform[] lines = new RectTransform[24]; // the 24 line pieces (12 edges x 2)
        public Image[] images = new Image[24]; // the images of those lines
        public bool visible; // true while the box is showing
    }

    readonly List<Box> boxes = new List<Box>(); // all the boxes we made
    Camera cam; // the camera we are using right now
    Canvas canvas; // the canvas the lines live on
    bool shown; // remembers if lines were showing last frame

    void Start() // runs once at the start
    {
        if (coolantLayer.value == 0) coolantLayer = LayerMask.GetMask("Coolant"); // use the "Coolant" layer if none picked
        if (heat == null) heat = FindFirstObjectByType<HeatSystem>(); // find the heat script if empty

        if (lineParent == null && heat != null && heat.HeatBar != null) // try to use the heat bar's canvas
        {
            Canvas hc = heat.HeatBar.GetComponentInParent<Canvas>(true); // get the canvas of the heat bar
            if (hc != null) lineParent = hc.rootCanvas.GetComponent<RectTransform>(); // use its main canvas
        }
        if (lineParent == null) // still nothing found
        {
            Canvas c = FindFirstObjectByType<Canvas>(); // grab any canvas in the scene
            if (c != null) lineParent = c.GetComponent<RectTransform>(); // use it
        }
        if (lineParent == null) { Debug.LogWarning("CoolantHighlight: no Canvas found."); enabled = false; return; } // no canvas, so stop this script
        canvas = lineParent.GetComponentInParent<Canvas>(); // remember the canvas

        MeshFilter[] filters = FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None); // get every mesh in the scene
        foreach (MeshFilter mf in filters) // go through each mesh
        {
            if ((coolantLayer.value & (1 << mf.gameObject.layer)) == 0) continue; // skip if not on the Coolant layer
            if (mf.sharedMesh == null) continue; // skip if it has no mesh
            AddBox(mf.transform, mf.sharedMesh.bounds); // make a box that follows this object
        }

        Collider[] cols = FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None); // get every collider in the scene
        foreach (Collider col in cols) // go through each collider
        {
            if ((coolantLayer.value & (1 << col.gameObject.layer)) == 0) continue; // skip if not on the Coolant layer
            if (col.GetComponent<MeshFilter>() != null) continue; // skip if it already has a mesh box
            AddBox(null, col.bounds); // make a box from the collider's size
        }

        Debug.Log("CoolantHighlight: made outlines for " + boxes.Count + " object(s) on the Coolant layer"); // tell how many boxes were made
        if (boxes.Count == 0) // no boxes at all
            Debug.LogWarning("CoolantHighlight: nothing is on the Coolant layer."); // warn that the layer is empty
    }

    void AddBox(Transform t, Bounds b) // makes one box with its 24 lines
    {
        Box box = new Box { t = t, bounds = b }; // create the box data
        for (int i = 0; i < box.lines.Length; i++) // make each line piece
        {
            GameObject go = new GameObject("CoolantLine", typeof(RectTransform), typeof(Image)); // new UI object with an Image
            go.transform.SetParent(lineParent, false); // put it inside the canvas

            Image img = go.GetComponent<Image>(); // get its Image
            img.color = lineColor; // give it the line color
            img.raycastTarget = false; // clicks pass through it

            go.SetActive(false); // hide it for now
            box.lines[i] = go.GetComponent<RectTransform>(); // save the line's transform
            box.images[i] = img; // save the line's image
        }
        boxes.Add(box); // add the box to the list
    }

    void LateUpdate() // runs every frame after everything moved
    {
        if (heat == null) return; // no heat script, do nothing
        cam = playerCamera != null ? playerCamera : Camera.main; // pick the camera to use
        if (cam == null) return; // no camera, do nothing

        bool show = heat.IsInDanger; // only show lines when the player is in danger
        if (!show) // not in danger
        {
            if (shown) HideAll(); // hide the lines if they were showing
            shown = false; // remember they are hidden
            return; // stop here
        }
        shown = true; // remember they are showing

        Camera uiCam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) // check the canvas type
                       ? canvas.worldCamera : null; // use the canvas camera if needed
        Vector3 playerPos = heat.transform.position; // where the player is

        foreach (Box box in boxes) // go through every box
            DrawBox(box, uiCam, playerPos); // draw it
    }

    void DrawBox(Box box, Camera uiCam, Vector3 playerPos) // draws one box
    {
        float dist = DistanceToBox(box, playerPos); // how far the player is from the box
        if (dist >= showRadius) // too far away
        {
            if (box.visible) HideBox(box); // hide it if it was showing
            return; // stop here
        }
        box.visible = true; // mark it as showing

        float fade = Mathf.InverseLerp(showRadius, showRadius * fadeStart, dist); // 1 when close, 0 at the edge
        Color col = lineColor; // start from the line color
        col.a = lineColor.a * fade; // make it more see-through when far
        foreach (Image img in box.images) img.color = col; // apply the color to every line

        Vector3[] c = new Vector3[8]; // the 8 corners of the box
        Vector3 min = box.bounds.min; // lowest corner of the box
        Vector3 max = box.bounds.max; // highest corner of the box
        for (int i = 0; i < 8; i++) // build each corner
        {
            Vector3 p = new Vector3((i & 1) == 0 ? min.x : max.x, // pick min or max for X
                                    (i & 2) == 0 ? min.y : max.y, // pick min or max for Y
                                    (i & 4) == 0 ? min.z : max.z); // pick min or max for Z
            c[i] = box.t != null ? box.t.TransformPoint(p) : p; // move the corner into world space
        }

        int line = 0; // which line piece we are on
        for (int i = 0; i < 8; i++) // go through each corner
        {
            for (int bit = 1; bit <= 4; bit *= 2) // check each direction (X, Y, Z)
            {
                if ((i & bit) != 0) continue; // skip so each edge is only made once

                Vector3 a = c[i]; // one end of the edge
                Vector3 b = c[i | bit]; // the other end of the edge
                DrawSegment(box.lines[line++], a, Vector3.Lerp(a, b, lineLength), uiCam); // draw the piece from end A
                DrawSegment(box.lines[line++], b, Vector3.Lerp(b, a, lineLength), uiCam); // draw the piece from end B
            }
        }
    }

    float DistanceToBox(Box box, Vector3 worldPoint) // gets how far a point is from the box
    {
        if (box.t == null) // the box is already in world space
            return Vector3.Distance(box.bounds.ClosestPoint(worldPoint), worldPoint); // distance to the closest point

        Vector3 local = box.t.InverseTransformPoint(worldPoint); // move the point into the box's own space
        Vector3 closestLocal = box.bounds.ClosestPoint(local); // closest point on the box
        return Vector3.Distance(box.t.TransformPoint(closestLocal), worldPoint); // distance back in world space
    }

    void DrawSegment(RectTransform line, Vector3 a, Vector3 b, Camera uiCam) // draws one line piece on the screen
    {
        float near = cam.nearClipPlane + 0.01f; // the closest distance the camera can see
        Vector3 camPos = cam.transform.position; // where the camera is
        Vector3 fwd = cam.transform.forward; // the way the camera faces
        float za = Vector3.Dot(a - camPos, fwd); // how far in front of the camera point A is
        float zb = Vector3.Dot(b - camPos, fwd); // how far in front of the camera point B is

        if (za < near && zb < near) { line.gameObject.SetActive(false); return; } // both behind the camera, hide the line
        if (za < near) a = Vector3.Lerp(a, b, (near - za) / (zb - za)); // cut off the part of A behind the camera
        else if (zb < near) b = Vector3.Lerp(b, a, (near - zb) / (za - zb)); // cut off the part of B behind the camera

        Vector3 sa = cam.WorldToScreenPoint(a); // point A on the screen
        Vector3 sb = cam.WorldToScreenPoint(b); // point B on the screen

        RectTransformUtility.ScreenPointToLocalPointInRectangle(lineParent, sa, uiCam, out Vector2 la); // screen point A to canvas position
        RectTransformUtility.ScreenPointToLocalPointInRectangle(lineParent, sb, uiCam, out Vector2 lb); // screen point B to canvas position

        Vector2 delta = lb - la; // the direction from A to B
        line.localPosition = (la + lb) * 0.5f; // put the line in the middle of A and B
        line.sizeDelta = new Vector2(delta.magnitude, lineThickness); // set the line length and thickness
        line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg); // turn the line to face B

        if (!line.gameObject.activeSelf) line.gameObject.SetActive(true); // show the line if it was hidden
    }

    void HideBox(Box box) // hides one box
    {
        box.visible = false; // mark it as hidden
        foreach (RectTransform l in box.lines) // go through its lines
            if (l != null) l.gameObject.SetActive(false); // turn each line off
    }

    void HideAll() // hides every box
    {
        foreach (Box box in boxes) HideBox(box); // hide each one
    }

    void OnDrawGizmosSelected() // draws helpers in the Scene view when selected
    {
        HeatSystem h = heat != null ? heat : FindFirstObjectByType<HeatSystem>(); // get the heat script
        if (h == null) return; // none found, do nothing

        Gizmos.color = Color.yellow; // use yellow
        Gizmos.DrawWireSphere(h.transform.position, showRadius); // draw the radius as a ball
    }
}