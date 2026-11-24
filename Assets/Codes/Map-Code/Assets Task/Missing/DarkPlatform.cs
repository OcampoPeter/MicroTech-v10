using System.Collections; // Gives access to IEnumerator for coroutines
using UnityEngine; // Gives access to Unity classes like MonoBehaviour, Color, Renderer

[System.Serializable] // Lets this class show up in the Inspector
public class MissingSettings // All the dark platform settings, kept on the MazeTaskController
{
    [Header("Colors")] // Title in the Inspector
    [Tooltip("A platform that is still missing its part: a faded blue block.\n(For a see-through look the platform's material must be a transparent one. The color's Alpha is the amount of fade.)")] // Hover text in the Inspector
    public Color darkColor = new Color(0.12f, 0.25f, 0.6f, 0.5f); // Color of a platform that is missing its part
    [Tooltip("A small glow on the faded block so it can be seen from far away. 0 = no glow.")] // Hover text in the Inspector
    public float darkEmission = 0.35f; // Small glow of the dark platform
    [Tooltip("A platform that got its part and lit up.")] // Hover text in the Inspector
    public Color litColor = new Color(0.25f, 0.6f, 1f); // Color of a platform that got its part
    [Tooltip("How much the lit color glows (neon). 0 = no glow.")] // Hover text in the Inspector
    public float emission = 1f; // How much the lit platform glows
    [Tooltip("How many seconds the platform takes to light up.")] // Hover text in the Inspector
    public float litFadeTime = 0.6f; // Seconds the lighting up takes

    [Header("Timing")] // Title in the Inspector
    [Tooltip("A platform ignores new steps for this long after being stepped on.")] // Hover text in the Inspector
    public float stepCooldown = 0.3f; // Time a platform ignores new steps
}

public class DarkPlatform : MonoBehaviour // A dark platform that lights up when the player brings a part
{
    public bool IsLit { get; private set; } // True once the platform got its part

    Renderer[] renderers; // The renderers we color
    MissingSettings look; // Colors and timing from the controller
    MazeTaskGroup group; // The task this platform belongs to

    public void Init(MazeTaskGroup taskGroup, MissingSettings settings) // Sets this platform up (called by MazeTaskController)
    {
        group = taskGroup; // Remember the task group
        look = settings; // Save the settings
        renderers = GetComponentsInChildren<Renderer>(); // Find all renderers on this platform
        Tint(look.darkColor, look.darkEmission); // Start with the dark look

        var step = StepTrigger.Create(gameObject); // Make the sensor for this platform
        step.mode = StepMode.StandOnIt; // Just standing on it counts, no jump needed
        step.debugLog = false; // Keep the Console quiet, because standing reports often
        step.cooldown = look.stepCooldown; // Ignore new steps for a short time
        step.onPlayerStep += OnStep; // Call OnStep when the player steps on it

        group.Register(this); // Tell the task group this platform exists
    }

    void OnStep() // The player stepped on the platform
    {
        if (IsLit) return; // Already lit, so do nothing

        PartPickup part = PartPickup.Carried; // Get the part the player carries
        if (part == null) return; // Not carrying a part yet

        part.Consume(); // The part is used up
        IsLit = true; // Mark the platform as lit
        StartCoroutine(FadeIn()); // Slowly light it up
        group.MarkComplete(this); // Tell the task group this platform is done
    }

    IEnumerator FadeIn() // Slowly lights the platform up
    {
        for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(0.01f, look.litFadeTime)) // Go from 0 to 1 over the fade time
        {
            Tint(Color.Lerp(look.darkColor, look.litColor, t), Mathf.Lerp(look.darkEmission, look.emission, t)); // Blend the color and glow a bit closer to lit
            yield return null; // Wait one frame
        }
        Tint(look.litColor, look.emission); // Make sure it ends on the final lit look
    }

    void Tint(Color color, float glow) // Colors every renderer on this platform
    {
        foreach (var r in renderers) TaskVisuals.SetColor(r, color, glow); // Set the color and glow
    }
}