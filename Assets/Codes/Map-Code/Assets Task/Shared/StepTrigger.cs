using System; // Gives access to Action
using UnityEngine; // Gives access to Unity classes like MonoBehaviour, Collider, Debug

public enum StepMode // How a platform decides that the player "stepped" on it
{
    JumpOnIt, // Only a jump made while standing on it counts (used by Bent)
    StepOnIt, // Getting onto it counts, by walking or jumping (used by Fried)
    StandOnIt // Just standing on it counts (used by Missing)
}

public class StepTrigger : MonoBehaviour // Goes on a task platform and tells when the player lands on it
{
    public StepMode mode = StepMode.JumpOnIt; // Which kind of step counts on this platform

    public event Action onPlayerStep; // Fires each time the player lands on this platform

    public float cooldown = 0.8f; // Seconds to ignore new landings after a hit
    float lastHitTime = -999f; // When the last hit was counted

    public bool debugLog = true; // True = print a message in the Console on every landing

    public static StepTrigger Create(GameObject platform, float worldHeight = 0.3f) // Adds a StepTrigger to a platform, or returns the one it has
    {
        var step = platform.GetComponent<StepTrigger>(); // Look for an existing StepTrigger
        if (step == null) step = platform.AddComponent<StepTrigger>(); // Add one if there is none

        PlayerStepReporter.EnsureAttached(); // Make sure the player can report landings
        return step; // Give back the StepTrigger
    }

    public void Report(Collider surface) // Called by PlayerStepReporter when the player lands here
    {
        if (Time.time - lastHitTime < cooldown) return; // Too soon after the last hit, so ignore it
        lastHitTime = Time.time; // Remember when this hit happened

        if (debugLog) // If debug messages are on
            Debug.Log($"[StepTrigger] player landed on '{name}' (collider: '{surface.name}')", this); // Print which platform and collider were hit

        onPlayerStep?.Invoke(); // Tell the task script that the player stepped
    }

    void Update() // Runs every frame
    {
        if (PlayerStepReporter.Current == null) // If the player has no reporter yet
            PlayerStepReporter.EnsureAttached(); // Keep trying to attach one
    }
}