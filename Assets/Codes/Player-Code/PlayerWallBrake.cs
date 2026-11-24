using UnityEngine; // Gives access to Unity classes like MonoBehaviour, Vector3, Physics
using UnityEngine.InputSystem; // Gives access to the new Input System (Keyboard, Key)

[DefaultExecutionOrder(10)] // Runs after PlayerMovement
public class PlayerWallBrake : MonoBehaviour // Lets the player brake, hang and slip on a wall
{
    [Header("General")] // Title in the Inspector
    public bool canBrake = true; // True = wall braking is allowed
    [Tooltip("Layers that count as a wall. Everything = any solid wall.")] // Hover text in the Inspector
    public LayerMask wallLayers = ~0; // Which layers count as a wall
    [Tooltip("How far in front of you a wall can be (meters).")] // Hover text in the Inspector
    public float wallCheckDistance = 1.0f; // How far away a wall can be
    [Tooltip("How wide to look for a wall, in degrees left and right of where you look. 90 = any wall in front of you or beside you. 0 = only straight ahead.")] // Hover text in the Inspector
    [Range(0f, 90f)] public float wallSearchAngle = 90f; // How wide the wall search is
    [Tooltip("Hold Space the whole time. Let go and you fall. (W is only needed to start.)")] // Hover text in the Inspector
    public bool needHoldSpace = true; // True = you must hold Space to keep braking
    [Tooltip("Seconds after a wall run ends before braking can start. Stops the wall run and the brake from mixing.")] // Hover text in the Inspector
    public float wallRunLockout = 0.4f; // Seconds braking is blocked after a wall run

    [Header("Brake (the drift)")] // Title in the Inspector
    [Tooltip("OFF: any speed going UP is removed when the brake starts, so you only slide down and sideways, never up. ON: you keep the upward speed.")] // Hover text in the Inspector
    public bool keepUpwardSpeed = false; // True = keep the upward speed when braking starts
    [Tooltip("How fast your speed fades (meters per second, every second). Lower = longer, smoother drift. Stop time = speed / this number.")] // Hover text in the Inspector
    public float brakeDeceleration = 10f; // How fast the speed fades

    [Header("Hang and slip")] // Title in the Inspector
    public float hangBeforeSlip = 2f; // Seconds you hang still before the slip starts
    public float slipTime = 3f; // Seconds the slip lasts, then you fall
    [Tooltip("Sliding speeds (m/s), spread evenly over the slip time. It speeds up gradually from the first number to the last.")] // Hover text in the Inspector
    public float[] slipSpeeds = new float[] { 0.5f, 1f, 2f, 3.5f }; // The sliding speeds from first to last

    [Header("Climb jump")] // Title in the Inspector
    [Tooltip("After you let go of Space, you have this long (seconds) to press it again for a climb jump. Too slow and you fall.")] // Hover text in the Inspector
    public float climbWindow = 0.6f; // Seconds you have to press Space again

    [Header("Cooldown")] // Title in the Inspector
    public float cooldown = 5f; // Seconds before you can brake again

    [Header("Testing")] // Title in the Inspector
    public bool showDebug = true; // True = show small debug text in the top-left of the Game view

    private enum State { None, Brake, Hang, Slip } // The stages of a wall brake
    private State state = State.None; // The current stage

    private CharacterController controller; // The player's CharacterController
    private PlayerMovement movement; // The player's movement script
    private PlayerInputHandler input; // The player's input script
    private PlayerWallRun wallRun; // The wall run script
    private PlayerWallClimb climb; // The wall climb script
    private WallClingAnimation clingAnim; // Plays the hang animations
    [Tooltip("Seconds the jump animation plays BEFORE the player actually leaves the wall. 0 = instant.")] // Hover text in the Inspector
    public float jumpWindUp = 0.2f; // Seconds the jump animation leads the jump
    private float pendingJumpAt = -1f; // Time when the delayed climb jump really happens
    private float spaceReleasedAt = -1f; // Time when Space was let go

    private Vector3 vel; // The speed we control while braking
    private Vector3 wallNormal; // The direction the wall faces
    private float timer; // Time spent in Hang or Slip
    private float noWallTime; // How long no wall has been found
    private float cooldownEnd; // Time when the cooldown ends
    private bool wasGrounded; // Were we on the ground last frame
    private float lastWallRunTime = -10f; // When we last wall ran

    public float CooldownLeft { get { return Mathf.Max(0f, cooldownEnd - Time.time); } } // Seconds of cooldown left

    void Start() // Runs once when the scene starts
    {
        controller = GetComponent<CharacterController>(); // Find the CharacterController
        movement = GetComponent<PlayerMovement>(); // Find the movement script
        input = GetComponent<PlayerInputHandler>(); // Find the input script
        wallRun = GetComponent<PlayerWallRun>(); // Find the wall run script
        climb = GetComponent<PlayerWallClimb>(); // Find the wall climb script
        clingAnim = GetComponent<WallClingAnimation>(); // Find the cling animation script
        if (clingAnim == null) Debug.LogWarning("[Brake] WallClingAnimation is not on the Player, so there will be no hang animation."); // Warn if it is missing
    }

    void OnDisable() // Runs when this script is turned off
    {
        if (state != State.None) EndBrake("script turned off", false); // End the brake so the player is not stuck
    }

    void Update() // Runs every frame
    {
        bool groundedNow = controller.isGrounded; // Are we on the ground now
        if (wallRun != null && wallRun.IsWallRunning) lastWallRunTime = Time.time; // Remember the last wall run time

        if (state != State.None) RunBrake(); // Already braking, so keep going
        else TryStart(groundedNow); // Not braking, so check if we can start

        wasGrounded = groundedNow; // Remember for the next frame
    }

    void TryStart(bool groundedNow) // Checks if the brake can start
    {
        if (!canBrake || Time.timeScale == 0f) return; // Not allowed, or the game is paused
        if (!input.JumpTriggered) return; // Space was not pressed
        if (groundedNow || wasGrounded) return; // On the ground, so Space is a normal jump
        if (CooldownLeft > 0f) return; // Still on cooldown
        if (input.MoveInput.y < 0.5f || Mathf.Abs(input.MoveInput.x) > 0.2f) return; // W alone only, no A or D
        if (wallRun != null && wallRun.IsWallRunning) return; // Never start while wall running
        if (Time.time < lastWallRunTime + wallRunLockout) return; // Or right after a wall run

        Vector3 normal; // The wall's direction
        if (!FindWall(out normal)) return; // Stop if there is no wall

        wallNormal = normal; // Save the wall direction
        vel = new Vector3(movement.HorizontalVelocity.x, movement.velocity.y, movement.HorizontalVelocity.z); // Start with the speed we have now

        float into = Vector3.Dot(vel, -wallNormal); // How much of the speed pushes into the wall
        if (into > 0f) vel += wallNormal * into; // Remove it, so we slide along the wall

        if (!keepUpwardSpeed && vel.y > 0f) vel.y = 0f; // The brake is for going down and sideways, not up

        movement.ResetMotion(); // Reset the normal movement
        movement.IsClimbing = true; // PlayerMovement waits while this is true
        if (clingAnim != null) clingAnim.StartCling(); // Play the Braced Hang, then Hang Idle animation
        state = State.Brake; // Start the Brake stage
        timer = 0f; // Reset the timer
        noWallTime = 0f; // Reset the no wall time
        spaceReleasedAt = -1f; // Space has not been let go yet
        pendingJumpAt = -1f; // No climb jump is waiting
        Debug.Log("[Brake] Started. Speed " + vel.magnitude.ToString("F1") + " m/s, it will take about " + (vel.magnitude / brakeDeceleration).ToString("F1") + " s to stop."); // Print the start info in the Console
    }

    void RunBrake() // Runs every frame while braking
    {
        if (pendingJumpAt >= 0f) // A climb jump is waiting for its wind-up
        {
            if (Time.time >= pendingJumpAt) // The wind-up is over
            {
                pendingJumpAt = -1f; // Clear the waiting jump
                EndBrake("wall climb jump", false); // End the brake
                climb.Jump(wallNormal); // Do the climb jump
            }
            return; // Wait until the jump happens
        }

        if (spaceReleasedAt >= 0f && input.JumpTriggered && climb != null && climb.CanJump // Space was let go and pressed again, and a climb jump is available
            && Time.time - spaceReleasedAt <= climbWindow) // And it was pressed in time
        {
            if (clingAnim != null) // If there is a cling animation
                clingAnim.JumpFromWall(wallNormal, // Play Hop Up or Jump From Wall
                    movement.playerCamera != null ? movement.playerCamera.forward : -transform.forward); // Depending on where we look
            pendingJumpAt = Time.time + jumpWindUp; // Wait so the animation leads the jump
            return; // Leave the function
        }

        if (needHoldSpace && Keyboard.current != null && !Keyboard.current[Key.Space].isPressed) // Space is not held
        {
            bool canClimbJump = climb != null && climb.CanJump; // Is a climb jump available
            if (spaceReleasedAt < 0f) // Space was just let go
            {
                spaceReleasedAt = Time.time; // Remember when
                Debug.Log("[Brake] Space released. " + (canClimbJump ? "Press it again within " + climbWindow + " s for a climb jump." : "No climb jump available (" + (climb == null ? "PlayerWallClimb is not on the Player" : "no jumps left") + ") so you fall.")); // Print what happens next
            }
            if (!canClimbJump || Time.time - spaceReleasedAt > climbWindow) // No climb jump possible, or too slow
            {
                EndBrake("Space released", true); // End the brake and start the cooldown
                return; // Leave the function
            }
        }
        else if (spaceReleasedAt >= 0f && Time.time - spaceReleasedAt > climbWindow) // Space is held again but the window is over
        {
            spaceReleasedAt = -1f; // Forget the release
        }

        Vector3 n; // The wall's direction
        if (FindWall(out n, 180f)) { wallNormal = n; noWallTime = 0f; } // Search all around, so looking away does not lose the wall
        else // No wall found
        {
            noWallTime += Time.deltaTime; // Count how long the wall is missing
            if (noWallTime > 0.25f) { EndBrake("wall is gone", true); return; } // End the brake if it is gone too long
        }

        if (state == State.Brake) // Slowing down
        {
            float speed = Mathf.MoveTowards(vel.magnitude, 0f, brakeDeceleration * Time.deltaTime); // A steady slowdown, the same amount every second
            vel = vel.sqrMagnitude > 0.0001f ? vel.normalized * speed : Vector3.zero; // Apply the new speed in the same direction
            if (speed <= 0.05f) // Almost stopped
            {
                vel = Vector3.zero; // Stop completely
                state = State.Hang; // Start hanging
                timer = 0f; // Reset the timer
                Debug.Log("[Brake] Stopped. Hanging for " + hangBeforeSlip + " s."); // Print a message in the Console
            }
        }
        else if (state == State.Hang) // Hanging still
        {
            vel = Vector3.zero; // Do not move
            timer += Time.deltaTime; // Count the hang time
            if (timer >= hangBeforeSlip) // Hung long enough
            {
                state = State.Slip; // Start slipping
                timer = 0f; // Reset the timer
                Debug.Log("[Brake] Slipping down for " + slipTime + " s."); // Print a message in the Console
            }
        }
        else if (state == State.Slip) // Sliding down
        {
            float t = slipTime > 0f ? Mathf.Clamp01(timer / slipTime) : 1f; // How far through the slip we are (0 to 1)
            vel = Vector3.down * SlipSpeedAt(t); // Slide down at the speed for this moment
            timer += Time.deltaTime; // Count the slip time
            if (timer >= slipTime) { EndBrake("time is up, falling", true); return; } // Time is up, so fall
        }

        controller.Move(vel * Time.deltaTime); // Move the player

        if ((controller.collisionFlags & CollisionFlags.Below) != 0 && vel.y <= 0.01f) // Touched the floor while going down
            EndBrake("landed", false); // End the brake
    }

    void EndBrake(string why, bool startCooldown) // Ends the wall brake
    {
        if (clingAnim != null) clingAnim.StopCling(); // Play the Hang Drop animation (does nothing if a jump already played)
        state = State.None; // Go back to no state
        if (movement != null) // If there is a movement script
        {
            movement.IsClimbing = false; // Give control back to PlayerMovement
            movement.velocity = new Vector3(0f, Mathf.Min(vel.y, 0f), 0f); // Hand back the up/down speed (never upward)
            movement.SetHorizontalVelocity(new Vector3(vel.x, 0f, vel.z)); // Hand back the sideways speed
        }
        if (startCooldown) cooldownEnd = Time.time + cooldown; // Start the cooldown if wanted
        Debug.Log("[Brake] Ended: " + why + (startCooldown ? ". Cooldown " + cooldown + " s." : ".")); // Print why it ended
    }

    float SlipSpeedAt(float t) // Gets the slip speed for a moment in the slip (t from 0 to 1)
    {
        if (slipSpeeds == null || slipSpeeds.Length == 0) return 1f; // No speeds given, so use 1
        if (slipSpeeds.Length == 1) return slipSpeeds[0]; // Only one speed, so use it
        float f = t * (slipSpeeds.Length - 1); // Position in the list
        int i = Mathf.Min((int)f, slipSpeeds.Length - 2); // The speed we are coming from
        return Mathf.Lerp(slipSpeeds[i], slipSpeeds[i + 1], f - i); // Blend between two speeds
    }

    bool FindWall(out Vector3 normal) // Looks for a wall using the normal search angle
    {
        return FindWall(out normal, wallSearchAngle); // Use the Inspector angle
    }

    bool FindWall(out Vector3 normal, float searchAngle) // Looks for the nearest upright wall in a fan of directions
    {
        normal = Vector3.zero; // Start with no wall direction
        Transform cam = movement.playerCamera; // The player's camera
        Vector3 look = cam != null ? cam.forward : -transform.forward; // The direction we look
        look.y = 0f; // Remove up and down
        if (look.sqrMagnitude < 0.0001f) return false; // No direction to look in
        look.Normalize(); // Make it length 1

        Vector3 origin = transform.position + controller.center; // Start the rays from the middle of the player
        float dist = controller.radius + wallCheckDistance; // How far the rays go
        float best = float.MaxValue; // The closest hit distance found so far
        bool found = false; // True once a wall is found

        int rays = searchAngle <= 0f ? 1 : Mathf.CeilToInt(searchAngle / 20f) * 2 + 1; // How many rays to shoot
        for (int r = 0; r < rays; r++) // Go through each ray
        {
            float angle = rays == 1 ? 0f : Mathf.Lerp(-searchAngle, searchAngle, r / (float)(rays - 1)); // The angle of this ray
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * look; // The direction of this ray

            foreach (RaycastHit h in Physics.RaycastAll(origin, dir, dist, wallLayers, QueryTriggerInteraction.Ignore)) // Go through everything the ray hits
            {
                if (h.collider.transform.IsChildOf(transform)) continue; // Never the player's own parts
                if (h.distance >= best) continue; // Skip hits farther than the best one
                if (Mathf.Abs(h.normal.y) > 0.3f) continue; // Only upright walls
                best = h.distance; // Save the new closest distance
                normal = new Vector3(h.normal.x, 0f, h.normal.z).normalized; // Save the wall's direction
                found = true; // We found a wall
            }
        }
        return found; // Give back if a wall was found
    }

    void OnGUI() // Draws the debug text on screen
    {
        if (!showDebug || Event.current.type != EventType.Repaint) return; // Only draw when debug is on, and only on repaint
        string text = "Wall brake: " + state + // Show the current stage
            "\nspeed: " + vel.magnitude.ToString("F1") + " m/s" + // Show the speed
            ((state == State.Hang || state == State.Slip) ? "\ntimer: " + timer.ToString("F1") + " s" : "") + // Show the timer while hanging or slipping
            "\ncooldown: " + CooldownLeft.ToString("F1") + " s" + // Show the cooldown
            (spaceReleasedAt >= 0f && state != State.None ? "\nPRESS SPACE NOW: " + Mathf.Max(0f, climbWindow - (Time.time - spaceReleasedAt)).ToString("F2") + " s" : ""); // Show the climb jump countdown
        GUI.Box(new Rect(10f, 10f, 220f, 105f), text); // Draw the box with the text
    }
}