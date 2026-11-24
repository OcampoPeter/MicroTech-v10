using UnityEngine; // Gives access to Unity classes like MonoBehaviour, Vector3, Mathf

public class PlayerWallClimb : MonoBehaviour // Does the climb jumps up a wall (PlayerWallBrake decides when)
{
    [Header("Wall climb jump")] // Title in the Inspector
    [Tooltip("How many climb jumps you get before touching the floor.")] // Hover text in the Inspector
    public int maxJumps = 2; // How many climb jumps you get
    [Tooltip("How high one climb jump goes (meters). Speed = sqrt(height x 2 x gravity).")] // Hover text in the Inspector
    public float jumpHeight = 3.5f; // How high one climb jump goes
    [Tooltip("A small push away from the wall so you do not scrape it. 0 = straight up.")] // Hover text in the Inspector
    public float awayForce = 1.5f; // Small push away from the wall

    [Tooltip("After a climb jump the wall run is blocked for this long (seconds), so it does not grab you back onto the wall.")] // Hover text in the Inspector
    public float wallRunBlockTime = 0.6f; // Seconds the wall run is blocked after a climb jump

    [Header("Smooth launch")] // Title in the Inspector
    [Tooltip("Seconds the upward speed takes to build up. 0 = instant (the old, jerky launch). Higher = softer start. Try 0.15 to 0.3.")] // Hover text in the Inspector
    public float launchRampTime = 0.2f; // Seconds the upward speed takes to build up

    [Header("Testing")] // Title in the Inspector
    public bool showDebug = true; // True = show small debug text under the brake box

    public int JumpsLeft { get; private set; } // How many climb jumps are left
    public bool CanJump { get { return JumpsLeft > 0; } } // True if a climb jump is available

    private CharacterController controller; // The player's CharacterController
    private PlayerMovement movement; // The player's movement script
    private PlayerWallRun wallRun; // The wall run script

    private float rampStart = -1f; // Time the launch started (-1 = not launching)
    private float rampTargetUp; // The upward speed we are building up to

    void Start() // Runs once when the scene starts
    {
        controller = GetComponent<CharacterController>(); // Find the CharacterController
        movement = GetComponent<PlayerMovement>(); // Find the movement script
        wallRun = GetComponent<PlayerWallRun>(); // Find the wall run script
        JumpsLeft = maxJumps; // Start with all the jumps
    }

    void Update() // Runs every frame
    {
        if (controller.isGrounded) JumpsLeft = maxJumps; // Touching the floor brings the jumps back

        if (rampStart >= 0f) // If a launch is happening
        {
            float t = launchRampTime <= 0f ? 1f : (Time.time - rampStart) / launchRampTime; // How far through the launch we are
            float upSpeed = Mathf.Lerp(0f, rampTargetUp, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t))); // The upward speed for this moment
            Vector3 v = movement.velocity; // The current velocity
            v.y = Mathf.Max(v.y, upSpeed); // Never slow you down, only build up
            movement.velocity = v; // Apply the new velocity
            if (t >= 1f) rampStart = -1f; // The launch is finished
        }
    }

    public void Jump(Vector3 wallNormal) // Called by PlayerWallBrake to do a climb jump
    {
        if (!CanJump) return; // Stop if no jumps are left
        JumpsLeft--; // Use one jump

        float up = Mathf.Sqrt(jumpHeight * -2f * movement.gravity); // The upward speed needed to reach the jump height
        if (launchRampTime > 0f) // Soft launch
        {
            movement.velocity = Vector3.zero; // Start from zero
            rampTargetUp = up; // Build up to this speed
            rampStart = Time.time; // Remember when the launch started
        }
        else // Instant launch
        {
            movement.velocity = new Vector3(0f, up, 0f); // Launch upward at once
            rampStart = -1f; // No ramp needed
        }
        movement.SetHorizontalVelocity(Vector3.zero); // Stop any sideways speed
        movement.AddPush(wallNormal * awayForce); // Push a little away from the wall
        if (wallRun != null) wallRun.BlockFor(wallRunBlockTime); // Block the wall run for a moment
        Debug.Log("[Climb] Climb jump! Jumps left: " + JumpsLeft); // Print the jumps left in the Console
    }

    void OnGUI() // Draws the debug text on screen
    {
        if (!showDebug || Event.current.type != EventType.Repaint) return; // Only draw when debug is on, and only on repaint
        GUI.Box(new Rect(10f, 125f, 220f, 28f), "Climb jumps left: " + JumpsLeft + " / " + maxJumps); // Show the jumps left
    }
}