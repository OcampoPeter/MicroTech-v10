using System.Collections; // Gives access to IEnumerator for coroutines
using System.Collections.Generic; // Gives access to List
using UnityEngine; // Gives access to Unity classes like MonoBehaviour, Color, Renderer

public class BentPlatform : MonoBehaviour // A platform that starts bent and gets fixed by landing on it
{
    Color bentColor; // Color of the most bent look
    Color fixedColor; // Color of the fixed platform
    bool tintModels; // True = color the bent models too
    bool tintFixed; // True = color the platform after it is fixed
    float glow; // How much the colors glow (0 = no glow)
    bool blendWhileBent; // True = color shifts toward "fixed" with each hit
    public float straightenTime = 0.5f; // Seconds it takes to straighten when fixed

    public bool IsFixed { get; private set; } // True once the platform is fixed
    public int JumpCount { get; private set; } // How many landings were counted so far

    MazeTaskGroup group; // The task this platform belongs to
    int jumpsToFix = 3; // Landings needed to fix it
    Renderer[] ownRenderers; // The platform's normal look
    Collider[] ownColliders; // The platform's normal collision
    List<GameObject> stages; // The bent models, most bent first
    GameObject bentVisual; // The bent model being shown right now
    bool hasModel; // True if we are using bent models
    bool keepsOwnSize; // True = model keeps the size it was made at
    Quaternion fixedRotation; // The rotation it has when fixed
    Bounds platformBounds; // The platform's size before any model
    float fadeTime; // Seconds a color change takes
    Color currentColor; // The color the platform has right now

    public void Init(MazeTaskGroup taskGroup, int jumps, Vector3 tilt, // Sets this platform up (called by MazeTaskController)
                     GameObject[] bentStages, bool modelKeepsOwnSize, float hitCooldown, // More setup values
                     Color bentCol, Color fixedCol, bool colorModels, bool colorFixed, // Colors and color options
                     float glowAmount, bool blendColors, float colorFadeTime) // Glow and fade options
    {
        group = taskGroup; // Remember the task group
        bentColor = bentCol; // Save the bent color
        fixedColor = fixedCol; // Save the fixed color
        tintModels = colorModels; // Save if models get colored
        tintFixed = colorFixed; // Save if the fixed platform gets colored
        glow = glowAmount; // Save the glow amount
        blendWhileBent = blendColors; // Save if colors blend with each hit
        fadeTime = colorFadeTime; // Save the fade time
        currentColor = bentColor; // The platform starts with the bent color
        ownRenderers = GetComponentsInChildren<Renderer>(); // Find all renderers on this platform
        ownColliders = GetComponents<Collider>(); // Find all colliders on this platform
        keepsOwnSize = modelKeepsOwnSize; // Save if models keep their own size
        platformBounds = TaskVisuals.GetBounds(ownRenderers, ownColliders); // Remember the platform's size

        stages = new List<GameObject>(); // Make an empty list for the models
        if (bentStages != null) // If any models were given
            foreach (var m in bentStages) // Go through each model slot
                if (m != null) stages.Add(m); // Add it only if the slot is not empty

        hasModel = stages.Count > 0; // We use models only if there is at least one

        jumpsToFix = hasModel ? stages.Count : Mathf.Max(1, jumps); // One landing per model, or the number from the controller

        var step = StepTrigger.Create(gameObject); // Make the sensor that counts landings
        step.cooldown = hitCooldown; // Wait this long between counted hits
        step.onPlayerStep += OnStep; // Call OnStep every time the player lands

        fixedRotation = transform.localRotation; // Remember the flat rotation

        if (hasModel) // If we use bent models
        {
            foreach (var r in ownRenderers) r.enabled = false; // Hide the normal platform look
            ShowStage(0); // Show the most bent model
        }
        else // No models
        {
            transform.localRotation = fixedRotation * Quaternion.Euler(tilt); // Tilt the platform
            Tint(ownRenderers, bentColor); // Color it as bent
        }

        group.Register(this); // Tell the task group this platform exists
    }

    void ShowStage(int index) // Shows one of the bent models
    {
        if (bentVisual != null) Destroy(bentVisual); // Remove the model shown before

        GameObject model = stages[index]; // Get the model for this stage
        bentVisual = Instantiate(model, transform); // Create it as a child of the platform
        bentVisual.name = "BentModel_" + index; // Give it a clear name
        bentVisual.transform.localPosition = Vector3.zero; // Put it in the middle of the platform
        bentVisual.transform.localRotation = Quaternion.identity; // No extra rotation

        float fit = keepsOwnSize ? 1f : TaskVisuals.FitFactor(model, platformBounds); // Keep its size, or resize it to fit the platform

        Vector3 s = transform.lossyScale; // The platform's real scale
        Vector3 inverse = new Vector3(Safe(1f / s.x), Safe(1f / s.y), Safe(1f / s.z)); // The opposite scale, to cancel it out
        bentVisual.transform.localScale = Vector3.Scale(model.transform.localScale * fit, inverse); // Apply only the size we chose

        if (tintModels) // If models should be colored
        {
            Color target = blendWhileBent ? Color.Lerp(bentColor, fixedColor, (float)index / jumpsToFix) : bentColor; // Pick the color for this stage
            Renderer[] modelRenderers = bentVisual.GetComponentsInChildren<Renderer>(); // Find the model's renderers

            if (index == 0) Tint(modelRenderers, target); // First model: set the color right away
            else StartCoroutine(FadeColor(modelRenderers, currentColor, target)); // Later models: fade to the new color
            currentColor = target; // Remember the current color
        }

        bool modelHasCollider = bentVisual.GetComponentInChildren<Collider>() != null; // Check if the model has its own collision
        foreach (var c in ownColliders) c.enabled = !modelHasCollider; // Use the model's collision instead of the platform's
    }

    void OnStep() // Runs every time the player lands on the platform
    {
        if (IsFixed) return; // Already fixed, so do nothing

        JumpCount++; // Count this landing

        if (JumpCount >= jumpsToFix) // Enough landings
        {
            Fix(); // Fix the platform
        }
        else if (hasModel) // Not done yet and we use models
        {
            ShowStage(JumpCount); // Show the next, less bent model
        }
        else if (blendWhileBent) // Not done yet, no models, and blending is on
        {
            Color target = Color.Lerp(bentColor, fixedColor, (float)JumpCount / jumpsToFix); // Color a bit closer to fixed
            StartCoroutine(FadeColor(ownRenderers, currentColor, target)); // Fade to that color
            currentColor = target; // Remember the current color
        }
    }

    void Fix() // The platform is repaired
    {
        IsFixed = true; // Mark it as fixed

        if (hasModel) // If we used bent models
        {
            if (bentVisual != null) Destroy(bentVisual); // Remove the bent model
            foreach (var r in ownRenderers) r.enabled = true; // Bring back the normal look
            foreach (var c in ownColliders) c.enabled = true; // Bring back the normal collision

            if (tintFixed) StartCoroutine(FadeColor(ownRenderers, currentColor, fixedColor)); // Fade to the fixed color if turned on
        }
        else // No models
        {
            StartCoroutine(Straighten()); // Rotate the platform back to flat
            StartCoroutine(FadeColor(ownRenderers, currentColor, fixedColor)); // Fade to the fixed color
        }

        group.MarkComplete(this); // Tell the task group this platform is done
    }

    IEnumerator Straighten() // Smoothly rotates the platform back to flat
    {
        Quaternion start = transform.localRotation; // The tilted rotation we start from
        for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(0.01f, straightenTime)) // Go from 0 to 1 over the straighten time
        {
            transform.localRotation = Quaternion.Slerp(start, fixedRotation, t); // Rotate a bit closer to flat
            yield return null; // Wait one frame
        }
        transform.localRotation = fixedRotation; // Make sure it ends perfectly flat
    }

    IEnumerator FadeColor(Renderer[] targets, Color from, Color to) // Slowly changes color from one to another
    {
        for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(0.01f, fadeTime)) // Go from 0 to 1 over the fade time
        {
            Tint(targets, Color.Lerp(from, to, t)); // Set a color between the two
            yield return null; // Wait one frame
        }
        Tint(targets, to); // Make sure it ends on the final color
    }

    void Tint(Renderer[] renderers, Color color) // Colors every renderer in the list
    {
        foreach (var r in renderers) TaskVisuals.SetColor(r, color, glow); // Set the color with the glow amount
    }

    float Safe(float v) => float.IsInfinity(v) || float.IsNaN(v) ? 1f : v; // Returns 1 if the value is infinity or not a number
}