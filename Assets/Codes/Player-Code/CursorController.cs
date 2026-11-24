using UnityEngine; // Gives access to Unity classes like MonoBehaviour, Cursor, Texture2D
using UnityEngine.UI; // Gives access to UI classes like Image
using UnityEngine.InputSystem; // Gives access to the new Input System (Keyboard, Key)

public class CursorController : MonoBehaviour // Switches between a dot crosshair and the mouse arrow
{
    [Header("Toggle")] // Title in the Inspector
    [Tooltip("Press this key to switch between the dot and the mouse arrow.")] // Hover text in the Inspector
    public Key toggleKey = Key.LeftAlt; // The key that switches the cursor mode
    [Tooltip("ON: the scene starts with the dot. OFF: it starts with the mouse arrow.")] // Hover text in the Inspector
    public bool startLocked = true; // True = start with the dot

    [Header("Dot crosshair")] // Title in the Inspector
    public Color dotColor = Color.white; // Color of the dot
    public Color outlineColor = new Color(0f, 0f, 0f, 0.8f); // Color of the ring around the dot
    [Tooltip("Size of the dot in pixels. Bigger = easier to see.")] // Hover text in the Inspector
    public float dotSize = 10f; // Size of the dot in pixels
    [Tooltip("Dark ring around the dot so it shows on any background (pixels).")] // Hover text in the Inspector
    public float outlineThickness = 2f; // Thickness of the ring in pixels

    private bool locked; // True while the cursor is locked (dot mode)
    private GameObject crosshair; // The crosshair object
    private Sprite circleSprite; // The circle picture used for the dot

    void Start() // Runs once when the scene starts
    {
        locked = startLocked; // Start in the chosen mode
        BuildCrosshair(); // Build the dot
    }

    void Update() // Runs every frame
    {
        if (Keyboard.current != null) // If there is a keyboard
        {
            if (Keyboard.current[toggleKey].wasPressedThisFrame) locked = !locked; // Switch mode when the toggle key is pressed
            if (Keyboard.current.escapeKey.wasPressedThisFrame) locked = false; // Esc always frees the arrow
        }

        bool wantLocked = locked && !InspectMode.Active && Time.timeScale > 0f; // The arrow is free while inspecting or when the game is paused

        Cursor.lockState = wantLocked ? CursorLockMode.Locked : CursorLockMode.None; // Lock or free the cursor
        Cursor.visible = !wantLocked; // Show the arrow only when it is free
        bool firstPerson = CameraModeSwitcher.Instance == null // True if there is no switcher
                           || CameraModeSwitcher.Instance.Mode == CameraMode.FirstPerson; // Or the camera is in first person
        if (crosshair != null) crosshair.SetActive(wantLocked && firstPerson); // The dot only shows in first person
    }

    void BuildCrosshair() // Builds the dot in the middle of the screen
    {
        circleSprite = MakeCircleSprite(); // Make the circle picture

        crosshair = new GameObject("Crosshair"); // Make the crosshair object
        Canvas canvas = crosshair.AddComponent<Canvas>(); // Give it its own canvas
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; // Draw it on top of the screen
        canvas.sortingOrder = 100; // Put it on top of other UI

        MakeCircle("Outline", dotSize + outlineThickness * 2f, outlineColor); // Make the outer ring
        MakeCircle("Dot", dotSize, dotColor); // Make the inner dot
    }

    void MakeCircle(string circleName, float size, Color color) // Creates one circle
    {
        GameObject go = new GameObject(circleName); // Make the object
        go.transform.SetParent(crosshair.transform, false); // Put it under the crosshair
        Image img = go.AddComponent<Image>(); // Add an image
        img.sprite = circleSprite; // Use the circle picture
        img.color = color; // Set its color
        img.raycastTarget = false; // Make it ignore mouse clicks
        RectTransform rt = go.GetComponent<RectTransform>(); // Get its RectTransform
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); // Anchor to the center of the screen
        rt.anchoredPosition = Vector2.zero; // Put it exactly at the center
        rt.sizeDelta = new Vector2(size, size); // Set its size
    }

    Sprite MakeCircleSprite() // Draws a smooth white circle into a small texture
    {
        int res = 64; // The texture size in pixels
        Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false); // Make an empty texture
        tex.filterMode = FilterMode.Bilinear; // Smooth the picture when scaled
        float r = res * 0.5f; // The circle radius
        for (int y = 0; y < res; y++) // Go through each row
        {
            for (int x = 0; x < res; x++) // Go through each pixel in the row
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)); // Distance from the center
                float alpha = Mathf.Clamp01(r - d); // Opacity with a soft edge
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha)); // Paint the pixel white with that opacity
            }
        }
        tex.Apply(); // Finish the texture
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), 100f); // Turn the texture into a sprite
    }

    void OnDestroy() // Runs when this object is destroyed
    {
        if (crosshair != null) Destroy(crosshair); // Remove the crosshair too
    }
}