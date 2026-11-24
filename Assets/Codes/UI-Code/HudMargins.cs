using UnityEngine; // Unity basics

[ExecuteAlways] // also runs in the editor when not playing
[RequireComponent(typeof(RectTransform))] // this object must be a UI object
public class HUDMargins : MonoBehaviour // keeps the HUD away from the screen edges
{
    [Header("Space from the screen edge (canvas units, about pixels)")] // Inspector title
    [Range(0f, 300f)] public float left = 40f; // space from the left edge
    [Range(0f, 300f)] public float right = 40f; // space from the right edge
    [Range(0f, 300f)] public float top = 30f; // space from the top edge
    [Range(0f, 300f)] public float bottom = 30f; // space from the bottom edge

    void OnEnable() { Apply(); } // apply the margins when turned on
    void OnValidate() { Apply(); } // apply again when you change a number in the Inspector

    void Update() // runs every frame
    {
        if (Application.isPlaying) Apply(); // keep it correct if the screen size changes
    }

    void Apply() // sets the margins
    {
        RectTransform rt = transform as RectTransform; // get this UI object
        if (rt == null) return; // not a UI object, do nothing

        rt.anchorMin = Vector2.zero; // stretch from the bottom-left corner
        rt.anchorMax = Vector2.one; // stretch to the top-right corner
        rt.pivot = new Vector2(0.5f, 0.5f); // set the pivot to the center
        rt.offsetMin = new Vector2(left, bottom); // move the left and bottom edges inward
        rt.offsetMax = new Vector2(-right, -top); // move the right and top edges inward
    }
}