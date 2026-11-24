using System.Collections; // Gives access to IEnumerator for coroutines
using UnityEngine; // Gives access to Unity classes like MonoBehaviour, Color, Renderer

[System.Serializable] // Lets this class show up in the Inspector
public class FriedSettings // All the Fried settings, kept on the MazeTaskController
{
    [Header("Models (optional)")] // Title in the Inspector
    [Tooltip("Model shown while a node is OFF (dark). Leave empty to keep the platform's own look.")] // Hover text in the Inspector
    public GameObject darkModel; // Model used when a node is off
    [Tooltip("Model shown while a node is flashing, lit, or wrong. Leave empty to keep the platform's own look.")] // Hover text in the Inspector
    public GameObject litModel; // Model used when a node is flashing, lit or wrong
    [Tooltip("ON: models keep the size they were made at.\nOFF: each model is resized to fit the width and depth of the platform it is on (small platform = small model, big platform = big model).")] // Hover text in the Inspector
    public bool modelsKeepOwnSize = true; // True = models keep their own size
    [Tooltip("ON: also color the models with the colors below.\nOFF: models keep their own materials.\n(Without models, the platform itself is always colored.)")] // Hover text in the Inspector
    public bool colorModels = false; // True = the models get colored too

    [Header("Colors")] // Title in the Inspector
    [Tooltip("A node that is off.")] // Hover text in the Inspector
    public Color darkColor = new Color(0.1f, 0.1f, 0.12f); // Color of a node that is off
    [Tooltip("The color a node flashes when it shows you the order.")] // Hover text in the Inspector
    public Color flashColor = new Color(0.3f, 0.8f, 1f); // Color of a node while it flashes
    [Tooltip("A node you stepped on in the right order.")] // Hover text in the Inspector
    public Color litColor = new Color(0.4f, 1f, 0.5f); // Color of a correct node
    [Tooltip("A node you stepped on in the wrong order.")] // Hover text in the Inspector
    public Color wrongColor = new Color(1f, 0.25f, 0.2f); // Color of a wrong node
    [Tooltip("How much the lit colors glow. 0 = no glow.")] // Hover text in the Inspector
    public float emission = 2f; // How much the lit colors glow

    [Header("Timing (seconds)")] // Title in the Inspector
    [Tooltip("Wait before the first flashing starts when the scene begins.")] // Hover text in the Inspector
    public float startDelay = 1.0f; // Wait before the first flashing
    [Tooltip("How long each node stays lit while flashing.")] // Hover text in the Inspector
    public float flashDuration = 0.5f; // How long one flash lasts
    [Tooltip("Pause between one flash and the next.")] // Hover text in the Inspector
    public float gapBetweenFlashes = 0.25f; // Pause between flashes
    [Tooltip("How long the red wrong-step color shows before everything turns off.")] // Hover text in the Inspector
    public float wrongShowTime = 0.6f; // How long the red color shows
    [Tooltip("Pause after a wrong step, before the order flashes again.")] // Hover text in the Inspector
    public float delayBeforeReplay = 1.0f; // Pause before the order replays
    [Tooltip("A node ignores new jumps for this long after being jumped on. Keep it a bit longer than one jump.")] // Hover text in the Inspector
    public float stepCooldown = 0.8f; // Time a node ignores new jumps after one jump
}

public class PowerNode : MonoBehaviour // A power node the player must step on in the right order
{
    public int sequenceIndex; // This node's place in the order (0 = first)
    public bool IsLit { get; private set; } // True while this node is lit correctly

    Renderer[] ownRenderers; // The platform's own look
    Collider[] ownColliders; // The platform's own collision
    GameObject visual; // The model shown right now (null = platform's own look)
    GameObject shownPrefab; // Which model prefab is shown
    Bounds platformBounds; // The platform's size before any model
    bool usesModels; // True if a dark or lit model was given
    FriedSettings look; // Colors, models and timings from the controller
    MazeTaskGroup group; // The task this belongs to (only the first node uses it)
    PowerNode[] sequence; // All nodes in the correct order
    PowerNode controller; // The first node, which runs the whole sequence
    int progress; // Controller only: which node must be stepped on next
    bool busy; // Controller only: true while flashing, so steps are ignored
    bool completed; // Controller only: true when the sequence is done

    public void Init(MazeTaskGroup taskGroup, PowerNode[] orderedNodes, FriedSettings settings) // Sets this node up (called by MazeTaskController)
    {
        look = settings; // Save the settings
        ownRenderers = GetComponentsInChildren<Renderer>(); // Find all renderers on this platform
        ownColliders = GetComponents<Collider>(); // Find all colliders on this platform
        platformBounds = TaskVisuals.GetBounds(ownRenderers, ownColliders); // Remember the platform's size
        usesModels = look.darkModel != null || look.litModel != null; // Check if any model was given
        sequence = orderedNodes; // Save the ordered list of nodes
        controller = orderedNodes[0]; // The first node is the controller
        SetDark(); // Start with the node turned off

        var step = StepTrigger.Create(gameObject); // Make the sensor for this node
        step.mode = StepMode.JumpOnIt; // The player must jump on it to activate it
        step.cooldown = look.stepCooldown; // Ignore new jumps for a short time
        step.onPlayerStep += () => controller.HandleStep(this); // Tell the controller when this node is stepped on

        if (this == controller) // Only the first node runs the logic
        {
            group = taskGroup; // Remember the task group
            group.Register(this); // Tell the task group this task exists
            StartCoroutine(PlayDemo(look.startDelay)); // Start the first flashing
        }
    }

    void HandleStep(PowerNode stepped) // The player stepped on a node
    {
        if (completed || busy) return; // Ignore steps while flashing or when done
        if (stepped.IsLit) return; // Already lit, so stepping again is not a mistake

        if (stepped == sequence[progress]) // Is this the right node?
        {
            stepped.IsLit = true; // Mark it as lit
            stepped.ShowLit(look.litColor); // Show it in the lit color
            progress++; // Now wait for the next node

            if (progress >= sequence.Length) // All nodes are lit
            {
                completed = true; // The sequence is done
                group.MarkComplete(this); // Tell the task group it is complete
            }
        }
        else // Wrong node
        {
            StartCoroutine(WrongStep(stepped)); // Show red, reset and flash again
        }
    }

    IEnumerator WrongStep(PowerNode stepped) // Handles a wrong step
    {
        busy = true; // Ignore steps for now
        stepped.ShowLit(look.wrongColor); // Show the wrong node in red
        yield return new WaitForSeconds(look.wrongShowTime); // Wait while the red color shows

        progress = 0; // Start the order from the beginning
        foreach (var n in sequence) n.SetDark(); // Turn every node off

        yield return PlayDemo(look.delayBeforeReplay); // Flash the order again
    }

    IEnumerator PlayDemo(float initialDelay) // Flashes the nodes one by one in the right order
    {
        busy = true; // Ignore steps while flashing
        yield return new WaitForSeconds(Mathf.Max(0.1f, initialDelay)); // Wait before starting

        foreach (var n in sequence) // Go through the nodes in order
        {
            n.ShowLit(look.flashColor); // Flash this node
            yield return new WaitForSeconds(look.flashDuration); // Keep it lit for a moment
            n.SetDark(); // Turn it off again
            yield return new WaitForSeconds(look.gapBetweenFlashes); // Pause before the next node
        }

        busy = false; // The player can step again
    }

    void SetDark() // Turns the node off
    {
        IsLit = false; // It is no longer lit
        Show(look.darkModel, look.darkColor, 0f); // Show the dark model or color with no glow
    }

    void ShowLit(Color color) // Lights the node up
    {
        Show(look.litModel, color, look.emission); // Show the lit model with the given color and glow
    }

    void Show(GameObject prefab, Color color, float glow) // Shows a model or the platform's own look, and colors it
    {
        if (usesModels) SwapModel(prefab); // Change the model if models are used

        if (visual == null || look.colorModels) // Color the platform always, a model only if Color Models is on
        {
            Renderer[] targets = visual != null ? visual.GetComponentsInChildren<Renderer>() : ownRenderers; // Pick the model's renderers or the platform's
            foreach (var r in targets) TaskVisuals.SetColor(r, color, glow); // Set the color and glow
        }
    }

    void SwapModel(GameObject prefab) // Replaces the model on this node (null = platform's own look)
    {
        if (prefab == shownPrefab && (visual != null || prefab == null)) return; // Already showing this one, so do nothing

        if (visual != null) Destroy(visual); // Remove the old model
        visual = null; // Forget the old model
        shownPrefab = prefab; // Remember which prefab is shown now

        if (prefab == null) // No model wanted
        {
            foreach (var r in ownRenderers) r.enabled = true; // Show the platform's own look
            foreach (var c in ownColliders) c.enabled = true; // Use the platform's own collision
            return; // Leave the function
        }

        foreach (var r in ownRenderers) r.enabled = false; // Hide the platform's own look

        visual = Instantiate(prefab, transform); // Create the model as a child of the platform
        visual.name = "FriedModel"; // Give it a clear name
        visual.transform.localPosition = Vector3.zero; // Put it in the middle of the platform
        visual.transform.localRotation = Quaternion.identity; // No extra rotation

        float fit = look.modelsKeepOwnSize ? 1f : TaskVisuals.FitFactor(prefab, platformBounds); // Keep its size, or resize it to fit the platform

        Vector3 s = transform.lossyScale; // The platform's real scale
        Vector3 inverse = new Vector3(Safe(1f / s.x), Safe(1f / s.y), Safe(1f / s.z)); // The opposite scale, to cancel it out
        visual.transform.localScale = Vector3.Scale(prefab.transform.localScale * fit, inverse); // Apply only the size we chose

        bool modelHasCollider = visual.GetComponentInChildren<Collider>() != null; // Check if the model has its own collision
        foreach (var c in ownColliders) c.enabled = !modelHasCollider; // Use the model's collision instead of the platform's
    }

    float Safe(float v) => float.IsInfinity(v) || float.IsNaN(v) ? 1f : v; // Returns 1 if the value is infinity or not a number
}