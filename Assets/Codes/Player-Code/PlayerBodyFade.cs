using UnityEngine; // Gives access to Unity classes like MonoBehaviour, Material, Renderer

public class PlayerBodyFade : MonoBehaviour // Makes the player's body see-through when the camera is close
{
    [Header("Fade by camera distance (meters)")] // Title in the Inspector
    [Tooltip("THE RADIUS: when the camera is closer to the player than this, the body is fully faded.")] // Hover text in the Inspector
    public float fadeRadius = 2f; // Closer than this = fully faded
    [Tooltip("Farther than this the body is fully solid. Between the two it fades smoothly.")] // Hover text in the Inspector
    public float solidBeyond = 3.5f; // Farther than this = fully solid

    [Header("Fade by looking up (degrees) - OFF by default, the camera distance does the work")] // Title in the Inspector
    public bool useLookUpFade = false; // True = also fade when looking up
    [Tooltip("Looking up less than this = no fade.")] // Hover text in the Inspector
    public float lookUpStart = 20f; // Angle where the look up fade starts
    [Tooltip("Looking up this much or more = fully faded.")] // Hover text in the Inspector
    public float lookUpFull = 50f; // Angle where the look up fade is complete

    [Header("Fade by looking down at the player from above (degrees)")] // Title in the Inspector
    public bool useLookDownFade = false; // True = also fade when looking down at the player
    [Tooltip("Looking down less steeply than this = no fade.")] // Hover text in the Inspector
    public float lookDownStart = 35f; // Angle where the look down fade starts
    [Tooltip("Looking down this steeply or more = fully faded.")] // Hover text in the Inspector
    public float lookDownFull = 65f; // Angle where the look down fade is complete

    [Header("Look")] // Title in the Inspector
    [Tooltip("How see-through the body gets at most. 0 = invisible, 1 = solid.")] // Hover text in the Inspector
    [Range(0f, 1f)] public float minAlpha = 0.12f; // The lowest opacity the body can have
    [Tooltip("How fast it fades in and out.")] // Hover text in the Inspector
    public float fadeSpeed = 6f; // How fast the fade changes

    private CharacterController controller; // The player's CharacterController
    private PlayerMovement movement; // The player's movement script
    private Renderer[] renderers; // All the renderers of the player
    private Material[] mats; // Our own copies of the materials, so the originals stay untouched
    private float[] baseAlpha; // The original opacity of each material
    private bool[] isTransparent; // True if a material is in see-through mode
    private float alpha = 1f; // The current opacity of the body
    private float lastDistance; // The last camera distance, for the debug text

    [Header("Testing")] // Title in the Inspector
    public bool showDebug = true; // True = show small debug text on screen

    static readonly int BaseColor = Shader.PropertyToID("_BaseColor"); // The color name used by URP shaders
    static readonly int ColorProp = Shader.PropertyToID("_Color"); // The color name used by built-in shaders

    void Start() // Runs once when the scene starts
    {
        controller = GetComponent<CharacterController>(); // Find the CharacterController
        movement = GetComponent<PlayerMovement>(); // Find the movement script

        renderers = GetComponentsInChildren<Renderer>(true); // Get every renderer, even inactive ones
        System.Collections.Generic.List<Material> list = new System.Collections.Generic.List<Material>(); // A list for the materials
        foreach (Renderer r in renderers) // Go through each renderer
        {
            if (r.name.Contains("Hitbox")) continue; // Leave the hitbox alone
            foreach (Material m in r.materials) list.Add(m); // Add its material copies to the list
        }
        mats = list.ToArray(); // Turn the list into an array
        baseAlpha = new float[mats.Length]; // Make room for each original opacity
        isTransparent = new bool[mats.Length]; // Make room for each see-through flag
        for (int i = 0; i < mats.Length; i++) baseAlpha[i] = GetAlpha(mats[i]); // Save each original opacity
    }

    void LateUpdate() // Runs every frame after all Update calls
    {
        if (mats == null || mats.Length == 0) return; // Stop if there are no materials

        if (CameraModeSwitcher.Instance != null && CameraModeSwitcher.Instance.Mode == CameraMode.FirstPerson) return; // In first person the body is already hidden

        Transform cam = Camera.main != null ? Camera.main.transform : ((movement != null) ? movement.playerCamera : null); // The camera that is really on screen
        if (cam == null) return; // Stop if there is no camera

        Vector3 center = transform.position + (controller != null ? controller.center : Vector3.up); // The middle of the player's body

        float dist = Vector3.Distance(cam.position, center); // How far the camera is from the body
        lastDistance = dist; // Save it for the debug text
        float distanceAlpha = Mathf.Lerp(minAlpha, 1f, Mathf.InverseLerp(fadeRadius, solidBeyond, dist)); // Opacity based on the distance

        float pitchUp = Mathf.Asin(Mathf.Clamp(cam.forward.y, -1f, 1f)) * Mathf.Rad2Deg; // How far the camera looks up (positive = up)
        float lookAlpha = 1f; // Opacity from looking up (starts solid)
        if (useLookUpFade) // If look up fade is on
            lookAlpha = Mathf.Lerp(1f, minAlpha, Mathf.InverseLerp(lookUpStart, lookUpFull, pitchUp)); // Fade more the higher we look

        float lookDownAlpha = 1f; // Opacity from looking down (starts solid)
        if (useLookDownFade) // If look down fade is on
            lookDownAlpha = Mathf.Lerp(1f, minAlpha, Mathf.InverseLerp(lookDownStart, lookDownFull, -pitchUp)); // Fade more the steeper we look down

        float target = Mathf.Min(distanceAlpha, Mathf.Min(lookAlpha, lookDownAlpha)); // Use the most see-through result
        alpha = Mathf.MoveTowards(alpha, target, fadeSpeed * Time.deltaTime); // Move the opacity smoothly toward the target

        bool needTransparent = alpha < 0.99f; // See-through mode is needed only when faded
        for (int i = 0; i < mats.Length; i++) // Go through each material
        {
            if (mats[i] == null) continue; // Skip destroyed materials
            if (needTransparent != isTransparent[i]) SetTransparent(mats[i], needTransparent, i); // Switch the mode if needed
            if (isTransparent[i]) SetAlpha(mats[i], baseAlpha[i] * alpha); // Apply the opacity in see-through mode
        }
    }

    float GetAlpha(Material m) // Reads the opacity of a material
    {
        if (m.HasProperty(BaseColor)) return m.GetColor(BaseColor).a; // URP shaders
        if (m.HasProperty(ColorProp)) return m.GetColor(ColorProp).a; // Built-in shaders
        return 1f; // No color found, so assume solid
    }

    void SetAlpha(Material m, float a) // Sets the opacity of a material
    {
        if (m.HasProperty(BaseColor)) { Color c = m.GetColor(BaseColor); c.a = a; m.SetColor(BaseColor, c); } // URP shaders
        else if (m.HasProperty(ColorProp)) { Color c = m.GetColor(ColorProp); c.a = a; m.SetColor(ColorProp, c); } // Built-in shaders
    }

    void SetTransparent(Material m, bool on, int index) // Switches a material between solid and see-through
    {
        isTransparent[index] = on; // Remember the new mode
        if (!m.HasProperty("_Surface")) return; // Not a URP Lit style material, so stop

        if (on) // Switch to see-through
        {
            m.SetFloat("_Surface", 1f); // Set the surface to transparent
            m.SetFloat("_Blend", 0f); // Use normal alpha blending
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); // Blend using the source alpha
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); // Blend using the opposite alpha
            m.SetFloat("_ZWrite", 0f); // Do not write to the depth buffer
            m.SetOverrideTag("RenderType", "Transparent"); // Mark it as transparent
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); // Turn on the transparent shader feature
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent; // Draw it with the transparent objects
        }
        else // Switch back to solid
        {
            SetAlpha(m, baseAlpha[index]); // Restore the original opacity
            m.SetFloat("_Surface", 0f); // Set the surface to opaque
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One); // Normal source blend
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero); // Normal destination blend
            m.SetFloat("_ZWrite", 1f); // Write to the depth buffer again
            m.SetOverrideTag("RenderType", "Opaque"); // Mark it as opaque
            m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT"); // Turn off the transparent shader feature
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry; // Draw it with the solid objects
        }
    }

    void OnGUI() // Draws the debug text on screen
    {
        if (!showDebug || Event.current.type != EventType.Repaint) return; // Only draw when debug is on, and only on repaint
        GUI.Box(new Rect(10f, 160f, 260f, 48f), "camera distance: " + lastDistance.ToString("F1") + " m\nbody opacity: " + alpha.ToString("F2")); // Show the distance and opacity
    }
}