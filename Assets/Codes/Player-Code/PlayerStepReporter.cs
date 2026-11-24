using UnityEngine; // Gives access to Unity classes like MonoBehaviour, Collider, Physics

public class PlayerStepReporter : MonoBehaviour // Goes on the player and tells platforms when the player lands on them
{
    public static PlayerStepReporter Current; // The reporter that is currently active in the scene

    const float AirTime = 0.12f; // Seconds without touching ground before the player counts as "in the air"

    const float MinUpNormal = 0.5f; // Surfaces pointing up more than this count as ground (ignores walls and ceilings)

    StepTrigger lastSurface; // The platform the player was last standing on
    float lastTouchTime = -10f; // When the player last touched ground

    public static void EnsureAttached() // Finds the player and adds this reporter to it
    {
        if (Current != null) return; // Already exists, so do nothing

        GameObject player = GameObject.FindWithTag("Player"); // Look for the object tagged "Player"
        if (player == null) return; // No player yet

        var existing = player.GetComponent<PlayerStepReporter>(); // Check if the player already has one
        Current = existing != null ? existing : player.AddComponent<PlayerStepReporter>(); // Use it, or add a new one
    }

    void Awake() => Current = this; // Become the active reporter

    Collider playerCol; // The player's own collider (used to look straight down)

    void Update() // Runs every frame
    {
        if (playerCol == null) playerCol = GetComponent<Collider>(); // Find the player's collider
        if (playerCol == null) return; // Stop if there is none

        Bounds b = playerCol.bounds; // The collider's size and position
        if (Physics.Raycast(b.center, Vector3.down, out RaycastHit hit, b.extents.y + 0.35f, // Look straight down from the middle of the player
                            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) // Ignore triggers
        {
            StepTrigger surface = hit.collider.GetComponentInParent<StepTrigger>(); // Find the platform under the feet
            if (surface != null && surface.mode == StepMode.StandOnIt) // Only platforms where standing counts
                surface.Report(hit.collider); // Tell the platform
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit) // CharacterController players: runs when the controller touches something
    {
        if (hit.normal.y < MinUpNormal) return; // Not the top of something, so ignore it
        Touch(hit.collider); // Handle the touch
    }

    void OnCollisionStay(Collision collision) // Rigidbody players: runs every physics step while touching something
    {
        foreach (var contact in collision.contacts) // Go through each contact point
        {
            if (contact.normal.y >= MinUpNormal) // If it is the top of something
            {
                Touch(collision.collider); // Handle the touch
                return; // Stop after the first good contact
            }
        }
    }

    void Touch(Collider ground) // The player is touching the top of this collider right now
    {
        StepTrigger surface = ground.GetComponentInParent<StepTrigger>(); // Find which task platform this collider belongs to

        float now = Time.time; // The current time
        bool wasInAir = now - lastTouchTime > AirTime; // True if the player had been in the air

        StepTrigger previous = lastSurface; // Remember where the player stood before this touch

        lastTouchTime = now; // Save this touch time
        lastSurface = surface; // Save this platform

        if (surface == null) return; // Not a task platform, so nothing to report

        if (surface.mode == StepMode.JumpOnIt) // Bent platforms
        {
            if (wasInAir && previous == surface) // A jump that lands back on the same platform
                surface.Report(ground); // Count it
        }
        else if (surface.mode == StepMode.StepOnIt) // Fried platforms
        {
            if (previous != surface || wasInAir) // Got onto the platform from somewhere else, or landed after being in the air
                surface.Report(ground); // Count it
        }
        else // Missing platforms
        {
            surface.Report(ground); // Just standing on it is enough
        }
    }
}