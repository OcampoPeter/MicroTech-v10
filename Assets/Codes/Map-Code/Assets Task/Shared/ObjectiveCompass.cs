using System.Collections.Generic; // Gives access to List, HashSet and Dictionary
using UnityEngine; // Gives access to Unity classes like MonoBehaviour, Camera, Transform
using UnityEngine.UI; // Gives access to UI classes like Image and Text

[DefaultExecutionOrder(1000)] // Makes this script run after most other scripts
public class ObjectiveCompass : MonoBehaviour // A compass bar at the top of the screen that points to objectives
{
    [Header("Size and position (change these live while playing)")] // Title in the Inspector
    [Tooltip("Makes the whole compass bigger or smaller. 1 = normal.")] // Hover text in the Inspector
    public float scale = 1f; // Size multiplier of the whole compass
    public float barWidth = 600f; // Width of the bar
    public float barHeight = 50f; // Height of the bar
    [Tooltip("Gap from the top of the screen. Raise it to move the compass down.")] // Hover text in the Inspector
    public float distanceFromTop = 80f; // Gap between the bar and the top of the screen
    [Tooltip("Move left (negative) or right (positive) from the center.")] // Hover text in the Inspector
    public float horizontalOffset = 0f; // Left or right shift from the center

    [Header("How much you can see")] // Title in the Inspector
    [Tooltip("Degrees shown across the whole bar. 180 = like Skyrim.")] // Hover text in the Inspector
    public float viewAngle = 180f; // Degrees shown across the bar
    [Tooltip("Markers fade out near the edges of the bar.")] // Hover text in the Inspector
    public float edgeFade = 25f; // Degrees over which markers fade at the edges

    [Header("Marker colors")] // Title in the Inspector
    public Color bentColor = new Color(1f, 0.5f, 0.05f); // Orange, for bent platforms
    public Color missingColor = new Color(0.15f, 0.3f, 1f); // Blue, for dark platforms
    public Color friedColor = new Color(1f, 0.9f, 0.2f); // Yellow, for Fried nodes
    public Color partColor = new Color(0.55f, 0.9f, 1f); // Light blue, for loose parts
    public Color exitColor = new Color(0.2f, 1f, 0.4f); // Green, for the exit

    [Header("Exit")] // Title in the Inspector
    [Tooltip("Leave empty: it looks for an object named FinishLine.")] // Hover text in the Inspector
    public Transform exitTarget; // The exit the compass points to

    [Header("Other")] // Title in the Inspector
    public bool showDistance = true; // True = show the distance under each marker
    [Tooltip("Turn the N/E/S/W letters if your map's 'north' is not the Z direction. 0 = north is +Z.")] // Hover text in the Inspector
    public float northOffset = 0f; // Rotates the N/E/S/W letters
    [Tooltip("How often (seconds) the list of tasks is refreshed.")] // Hover text in the Inspector
    public float refreshTime = 0.25f; // Seconds between task list refreshes
    [Tooltip("Leave EMPTY to pick the camera you are looking through automatically.\nDrag a camera here only if the compass follows the wrong one.")] // Hover text in the Inspector
    public Camera viewCamera; // A camera to force the compass to use
    [Tooltip("Optional smoothing. 0 = none (best). Higher = snappier, lower = smoother but it lags behind.")] // Hover text in the Inspector
    public float headingSmoothing = 0f; // Smoothing of the compass turning

    struct Target // One thing the compass points to
    {
        public Transform t; // The object to point to
        public Color c; // The marker color
    }

    class Marker // One marker shown on the bar
    {
        public RectTransform root; // The marker's main object
        public Image icon; // The diamond shape
        public Text dist; // The distance text
        public Color color; // The marker color
    }

    RectTransform bar; // The compass bar
    Font font; // The font used for texts
    Camera cam; // The camera the compass follows
    Transform player; // The player
    float nextRefresh; // Time of the next task refresh
    float nextCamCheck; // Time of the next camera check
    string lastCamName = ""; // Name of the last camera used
    float smoothYaw; // The smoothed heading
    bool yawReady; // True once the heading has a first value

    readonly List<Target> targets = new List<Target>(); // Everything the compass points to right now
    readonly HashSet<Transform> targetSet = new HashSet<Transform>(); // Same targets, for fast checking
    readonly Dictionary<Transform, Marker> active = new Dictionary<Transform, Marker>(); // Which marker belongs to which target
    readonly List<Marker> freeMarkers = new List<Marker>(); // Unused markers waiting to be reused
    readonly List<Transform> toRemove = new List<Transform>(); // Temporary list of targets to remove

    readonly List<Image> ticks = new List<Image>(); // The small tick marks on the bar
    readonly List<float> tickAngles = new List<float>(); // The angle of each tick mark
    readonly Text[] letters = new Text[4]; // The N, E, S, W letters

    float LetterY { get { return -barHeight * 0.28f; } } // Height of the letters inside the bar
    float MarkerY { get { return barHeight * 0.24f; } } // Height of the markers inside the bar

    void Start() // Runs once when the scene starts
    {
        Canvas c = null; // The canvas we draw on
        if (ObjectivesUI.Instance != null) c = ObjectivesUI.Instance.GetComponentInParent<Canvas>(); // Use the quest panel's canvas if possible
        if (c == null) c = FindGameCanvas(); // Otherwise find any game canvas
        if (c == null) // If no canvas exists
        {
            Debug.LogWarning("[ObjectiveCompass] No Canvas found in the scene."); // Warn the developer
            enabled = false; // Turn this script off
            return; // Leave the function
        }

        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Load Unity's built-in font

        if (exitTarget == null) // If no exit was assigned
        {
            GameObject f = GameObject.Find("FinishLine"); // Look for an object named FinishLine
            if (f != null) exitTarget = f.transform; // Use it as the exit
        }

        BuildBar(c.rootCanvas.transform); // Build the compass bar on the canvas
    }

    Canvas FindGameCanvas() // Finds a canvas that is not the loading screen one
    {
        Canvas[] all = FindObjectsByType<Canvas>(FindObjectsSortMode.None); // Get every canvas
        foreach (Canvas cv in all) // Go through each canvas
        {
            if (cv.GetComponentInChildren<LoadingScreen>(true) != null) continue; // Skip the loading screen canvas
            return cv; // Use the first good canvas
        }
        return all.Length > 0 ? all[0] : null; // Fall back to the first canvas, or none
    }

    void BuildBar(Transform parent) // Creates the compass bar and its parts
    {
        GameObject barGO = new GameObject("ObjectiveCompass", typeof(RectTransform), typeof(Image), typeof(RectMask2D)); // Make the bar object with an image and a mask
        bar = barGO.GetComponent<RectTransform>(); // Save its RectTransform
        bar.SetParent(parent, false); // Put it on the canvas
        bar.anchorMin = new Vector2(0.5f, 1f); // Anchor to the top center
        bar.anchorMax = new Vector2(0.5f, 1f); // Anchor to the top center
        bar.pivot = new Vector2(0.5f, 1f); // Pivot at the top center
        ApplyLayout(); // Set size and position

        Image bg = barGO.GetComponent<Image>(); // Get the bar's background image
        bg.color = new Color(0f, 0f, 0f, 0.35f); // Make it a see-through black
        bg.raycastTarget = false; // Make it ignore mouse clicks

        string[] names = { "N", "E", "S", "W" }; // The four direction letters
        for (int i = 0; i < 4; i++) // Go through each letter
            letters[i] = MakeText(bar, "Letter " + names[i], 18, FontStyle.Bold, names[i]); // Create its text

        for (int a = 0; a < 360; a += 15) // Go around the circle every 15 degrees
        {
            if (a % 90 == 0) continue; // Skip the spots where a letter is
            Image tick = MakeImage(bar, "Tick", new Vector2(1.5f, 8f)); // Create a small tick mark
            ticks.Add(tick); // Save the tick
            tickAngles.Add(a); // Save its angle
        }

        Image mid = MakeImage(bar, "Center", new Vector2(2f, 8f)); // Create the little mark in the middle
        RectTransform mrt = mid.rectTransform; // Get its RectTransform
        mrt.anchorMin = new Vector2(0.5f, 1f); // Anchor to the top center
        mrt.anchorMax = new Vector2(0.5f, 1f); // Anchor to the top center
        mrt.pivot = new Vector2(0.5f, 1f); // Pivot at the top center
        mrt.anchoredPosition = Vector2.zero; // Put it exactly at the anchor
        mid.color = new Color(1f, 1f, 1f, 0.8f); // Make it a see-through white
    }

    void ApplyLayout() // Applies size, position and scale
    {
        bar.sizeDelta = new Vector2(barWidth, barHeight); // Set the bar size
        bar.anchoredPosition = new Vector2(horizontalOffset, -distanceFromTop); // Set the bar position
        bar.localScale = Vector3.one * Mathf.Max(0.1f, scale); // Set the bar scale (never below 0.1)
    }

    Image MakeImage(Transform parent, string objName, Vector2 size) // Creates a UI image
    {
        GameObject go = new GameObject(objName, typeof(RectTransform), typeof(Image)); // Make the object with an image
        RectTransform rt = go.GetComponent<RectTransform>(); // Get its RectTransform
        rt.SetParent(parent, false); // Put it under the parent
        rt.sizeDelta = size; // Set its size
        Image img = go.GetComponent<Image>(); // Get the image
        img.raycastTarget = false; // Make it ignore mouse clicks
        return img; // Give back the image
    }

    Text MakeText(Transform parent, string objName, int size, FontStyle style, string content) // Creates a UI text
    {
        GameObject go = new GameObject(objName, typeof(RectTransform), typeof(Text)); // Make the object with a text
        RectTransform rt = go.GetComponent<RectTransform>(); // Get its RectTransform
        rt.SetParent(parent, false); // Put it under the parent
        rt.sizeDelta = new Vector2(60f, 22f); // Set its size
        Text t = go.GetComponent<Text>(); // Get the text
        t.font = font; // Set the font
        t.fontSize = size; // Set the font size
        t.fontStyle = style; // Set the font style
        t.alignment = TextAnchor.MiddleCenter; // Center the text
        t.horizontalOverflow = HorizontalWrapMode.Overflow; // Do not cut the text sideways
        t.verticalOverflow = VerticalWrapMode.Overflow; // Do not cut the text up or down
        t.raycastTarget = false; // Make it ignore mouse clicks
        t.text = content; // Set the words
        return t; // Give back the text
    }

    Marker NewMarker() // Creates a new marker
    {
        Marker m = new Marker(); // Make an empty marker

        GameObject go = new GameObject("Marker", typeof(RectTransform)); // Make the marker object
        m.root = go.GetComponent<RectTransform>(); // Save its RectTransform
        m.root.SetParent(bar, false); // Put it on the bar
        m.root.sizeDelta = new Vector2(20f, 20f); // Set its size

        m.icon = MakeImage(m.root, "Icon", new Vector2(11f, 11f)); // Make the diamond image
        m.icon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f); // Turn the square 45 degrees to make a diamond

        m.dist = MakeText(m.root, "Distance", 11, FontStyle.Normal, ""); // Make the distance text
        m.dist.rectTransform.anchoredPosition = new Vector2(0f, -13f); // Put it under the diamond

        return m; // Give back the marker
    }

    void AddTarget(Transform t, Color c) // Adds something for the compass to point to
    {
        if (t == null) return; // Nothing to add
        if (!targetSet.Add(t)) return; // Already in the list
        targets.Add(new Target { t = t, c = c }); // Add it with its color
    }

    void RebuildTargets() // Rebuilds the list of things to point to
    {
        targets.Clear(); // Empty the target list
        targetSet.Clear(); // Empty the target set

        bool anyTasks = false; // True if at least one task exists
        bool allComplete = true; // True while every task is finished

        MazeTaskGroup[] groups = FindObjectsByType<MazeTaskGroup>(FindObjectsSortMode.None); // Get every task group
        foreach (MazeTaskGroup g in groups) // Go through each group
        {
            if (g.Total == 0) continue; // Skip empty groups
            anyTasks = true; // At least one task exists
            if (!g.IsComplete) allComplete = false; // This group is not finished

            Color col = ColorFor(g.taskName); // Get the color for this task type
            foreach (Object item in g.Items) // Go through each item in the group
            {
                if (item == null) continue; // Skip destroyed items
                if (g.IsItemDone(item)) continue; // Skip items that are already fixed

                Transform t = ToTransform(item); // Get the item's transform
                if (t == null || !t.gameObject.activeInHierarchy) continue; // Skip items that are missing or switched off

                AddTarget(t, col); // Point to this item
            }
        }

        if (anyTasks && allComplete) // Everything is fixed
        {
            targets.Clear(); // Remove all targets
            targetSet.Clear(); // Remove all targets from the set too
            if (exitTarget != null) AddTarget(exitTarget, exitColor); // Point only to the exit
        }
        else // Tasks are still open
        {
            PartPickup[] parts = FindObjectsByType<PartPickup>(FindObjectsSortMode.None); // Get every part
            foreach (PartPickup p in parts) // Go through each part
            {
                if (p == null || p.IsCarried || p.IsUsed) continue; // Skip parts that are carried or used
                AddTarget(p.transform, partColor); // Point to this loose part
            }
        }

        SyncMarkers(); // Match the markers to the targets
    }

    void SyncMarkers() // Gives each target its own marker and frees old markers
    {
        toRemove.Clear(); // Empty the remove list
        foreach (KeyValuePair<Transform, Marker> kv in active) // Go through each active marker
        {
            if (kv.Key == null || !targetSet.Contains(kv.Key)) toRemove.Add(kv.Key); // Mark it for removal if its target is gone
        }
        foreach (Transform key in toRemove) // Go through each marker to remove
        {
            Marker old = active[key]; // Get the old marker
            old.root.gameObject.SetActive(false); // Hide it
            freeMarkers.Add(old); // Keep it to reuse later
            active.Remove(key); // Stop tracking it
        }

        foreach (Target tg in targets) // Go through each target
        {
            Marker m; // The marker for this target
            if (active.TryGetValue(tg.t, out m)) // If it already has a marker
            {
                m.color = tg.c; // Update the color
                continue; // Go to the next target
            }

            if (freeMarkers.Count > 0) // If there is an unused marker
            {
                m = freeMarkers[freeMarkers.Count - 1]; // Take the last one
                freeMarkers.RemoveAt(freeMarkers.Count - 1); // Remove it from the free list
            }
            else // No unused marker
            {
                m = NewMarker(); // Make a new one
            }

            m.color = tg.c; // Set the color
            m.root.gameObject.SetActive(true); // Show the marker
            active[tg.t] = m; // Link the marker to its target
        }
    }

    Color ColorFor(string taskName) // Returns the marker color for a task type
    {
        if (taskName == "Bent") return bentColor; // Bent tasks use the bent color
        if (taskName == "MissingParts") return missingColor; // Missing tasks use the missing color
        if (taskName == "Fried") return friedColor; // Fried tasks use the Fried color
        return Color.white; // Anything else is white
    }

    static Transform ToTransform(Object o) // Gets a transform from any Unity object
    {
        if (o == null) return null; // No object, no transform
        Component c = o as Component; // Try to treat it as a component
        if (c != null) return c.transform; // Use the component's transform
        GameObject g = o as GameObject; // Try to treat it as a GameObject
        if (g != null) return g.transform; // Use the GameObject's transform
        return null; // Nothing worked
    }

    bool BarReady() // Checks that the bar and the 4 letters really exist
    {
        if (bar == null) return false; // The bar is missing
        for (int i = 0; i < letters.Length; i++) // Go through each letter
            if (letters[i] == null) return false; // A letter is missing
        return true; // Everything exists
    }

    void LateUpdate() // Runs every frame after all Update calls
    {
        if (!BarReady()) return; // Skip this frame if there is nothing to draw on

        ApplyLayout(); // Apply size and position so Inspector changes show live

        if (cam == null || !cam.isActiveAndEnabled || Time.time >= nextCamCheck) // Time to check the camera again
        {
            nextCamCheck = Time.time + 0.5f; // Check again in half a second
            cam = PickCamera(); // Pick the camera that draws the screen
            if (cam != null && cam.name != lastCamName) // If the camera changed
            {
                lastCamName = cam.name; // Remember its name
                Debug.Log("[ObjectiveCompass] Using camera: " + cam.name); // Print the camera name in the Console
            }
        }
        if (cam == null) return; // Stop if there is no camera

        if (player == null) // If we have not found the player yet
        {
            GameObject p = GameObject.FindWithTag("Player"); // Look for the object tagged "Player"
            if (p != null) player = p.transform; // Save the player
        }

        if (Time.time >= nextRefresh) // Time to refresh the targets
        {
            nextRefresh = Time.time + refreshTime; // Set the next refresh time
            RebuildTargets(); // Rebuild the target list
        }

        float yaw = SteadyYaw(GetYaw(cam.transform)); // The direction the camera faces, smoothed

        for (int i = 0; i < 4; i++) // Go through the N, E, S, W letters
        {
            float delta = Mathf.DeltaAngle(yaw, i * 90f + northOffset); // Angle between where we look and this letter
            float a = AlphaFor(delta); // How visible the letter is
            letters[i].rectTransform.anchoredPosition = new Vector2(XFor(delta), LetterY); // Move the letter to its spot on the bar
            letters[i].color = new Color(1f, 1f, 1f, a); // Set its see-through amount
        }

        for (int i = 0; i < ticks.Count; i++) // Go through each tick mark
        {
            if (ticks[i] == null) continue; // Skip a tick that was destroyed
            float delta = Mathf.DeltaAngle(yaw, tickAngles[i] + northOffset); // Angle between where we look and this tick
            float a = AlphaFor(delta); // How visible the tick is
            ticks[i].rectTransform.anchoredPosition = new Vector2(XFor(delta), LetterY); // Move the tick to its spot on the bar
            ticks[i].color = new Color(1f, 1f, 1f, a * 0.6f); // Set its see-through amount
        }

        Vector3 camPos = cam.transform.position; // The camera position
        Vector3 from = player != null ? player.position : camPos; // Measure distances from the player, or the camera if there is no player

        foreach (Target tg in targets) // Go through each target
        {
            Marker m; // The marker for this target
            if (tg.t == null || !active.TryGetValue(tg.t, out m)) continue; // Skip targets without a marker
            if (m.root == null || m.icon == null || m.dist == null) continue; // Skip a marker that was destroyed

            Vector3 d = tg.t.position - camPos; // Direction from the camera to the target
            float bearing = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg; // Compass angle of that direction
            float delta = Mathf.DeltaAngle(yaw, bearing); // Angle between where we look and the target
            float a = AlphaFor(delta); // How visible the marker is

            m.root.anchoredPosition = new Vector2(XFor(delta), MarkerY); // Move the marker to its spot on the bar
            m.icon.color = new Color(m.color.r, m.color.g, m.color.b, a); // Set the diamond color and see-through amount

            m.dist.gameObject.SetActive(showDistance); // Show or hide the distance text
            if (showDistance) // If distances are shown
            {
                m.dist.text = Mathf.RoundToInt((tg.t.position - from).magnitude) + "m"; // Write the distance in meters
                m.dist.color = new Color(1f, 1f, 1f, a * 0.85f); // Set the text see-through amount
            }
        }
    }

    Camera PickCamera() // Picks the camera that really draws the screen
    {
        if (viewCamera != null) return viewCamera; // Use the dragged-in camera if there is one

        Camera best = null; // The best camera found so far
        foreach (Camera c in Camera.allCameras) // Go through every active camera
        {
            if (!c.enabled || !c.gameObject.activeInHierarchy) continue; // Skip cameras that are off
            if (c.targetTexture != null) continue; // Skip cameras that draw to a texture
            if (best == null || c.depth > best.depth) best = c; // Keep the one with the highest depth
        }
        return best != null ? best : Camera.main; // Use the best one, or the main camera
    }

    static float GetYaw(Transform t) // Gets which way the camera faces, left and right only (0 = +Z)
    {
        Vector3 f = t.forward; // The way the camera looks
        Vector3 flatF = new Vector3(f.x, 0f, f.z); // That direction without up and down
        Vector3 dir; // The direction we will use

        if (flatF.magnitude > 0.35f) // If the camera looks fairly level
        {
            dir = flatF; // Use the flat forward direction
        }
        else // Looking almost straight up or down
        {
            Vector3 u = t.up; // The top of the screen direction
            Vector3 flatU = new Vector3(u.x, 0f, u.z); // That direction without up and down
            dir = f.y > 0f ? -flatU : flatU; // Pick the side based on looking up or down
        }

        if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward; // Use forward if the direction is empty
        return Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg; // Turn the direction into a compass angle
    }

    float SteadyYaw(float rawYaw) // Smooths tiny camera shakes
    {
        if (!yawReady || headingSmoothing <= 0f) // No smoothing wanted, or the first frame
        {
            smoothYaw = rawYaw; // Use the raw value
            yawReady = true; // We now have a first value
            return smoothYaw; // Give back the value
        }
        float k = 1f - Mathf.Exp(-headingSmoothing * Time.deltaTime); // How much to move toward the new value
        smoothYaw = Mathf.LerpAngle(smoothYaw, rawYaw, k); // Move smoothly toward the new value
        return smoothYaw; // Give back the smoothed value
    }

    float XFor(float delta) // Left and right position on the bar for an angle difference
    {
        float half = Mathf.Max(1f, viewAngle * 0.5f); // Half of the visible angle
        return delta / half * (barWidth * 0.5f); // Turn the angle into a position on the bar
    }

    float AlphaFor(float delta) // 1 = fully visible, 0 = hidden
    {
        float half = viewAngle * 0.5f; // Half of the visible angle
        return Mathf.Clamp01((half - Mathf.Abs(delta)) / Mathf.Max(1f, edgeFade)); // Fade out near the edges
    }

    void OnDestroy() // Runs when this object is destroyed
    {
        if (bar != null) Destroy(bar.gameObject); // Remove the compass bar too
    }
}