using UnityEngine; // Gives access to Unity classes like MonoBehaviour, Vector3, Mathf
using UnityEngine.InputSystem; // Gives access to the new Input System (Keyboard, Key)

public class PlayerMovement : MonoBehaviour // Handles walking, sprinting, jumping and gravity
{
    [Header("References")] // Title in the Inspector
    public CharacterController controller; // The player's CharacterController
    public Transform playerCamera; // The camera that decides which way is forward
    private PlayerInputHandler input; // The script that reads the player's input
    private PlayerWallRun wallRun; // The wall run script

    [Header("Speeds")] // Title in the Inspector
    public float walkSpeed = 5f; // Normal walking speed
    public float maxSprintSpeed = 10f; // Top sprint speed
    public float accelerationRate = 18f; // How fast the sprint speed builds up
    public float decelerationRate = 14f; // How fast it falls back down
    public float modelRotationOffset = 180f; // Extra turn so the model faces the right way
    [Tooltip("Turn the character to face where it walks. CameraModeSwitcher turns this off in first person.")] // Hover text in the Inspector
    public bool rotateToMovement = true; // True = the character turns to face its walking direction

    [Header("Smoothness")] // Title in the Inspector
    [Tooltip("How quickly you reach your target speed on the ground. Higher = snappier, lower = smoother.")] // Hover text in the Inspector
    public float groundAcceleration = 45f; // How fast you reach your speed on the ground
    [Tooltip("How quickly you stop on the ground. Higher = stops faster.")] // Hover text in the Inspector
    public float groundDeceleration = 35f; // How fast you stop on the ground
    [Tooltip("Steering strength in the air while you hold a direction.")] // Hover text in the Inspector
    public float airAcceleration = 20f; // How strongly you steer in the air
    [Tooltip("How fast you lose momentum in the air with no input. Low = you keep drifting.")] // Hover text in the Inspector
    public float airDeceleration = 6f; // How fast you slow down in the air with no input
    [Tooltip("Extra strength when you steer AGAINST your current direction in the air. Higher = easier to turn around mid-jump.")] // Hover text in the Inspector
    public float airReverseMultiplier = 3f; // Extra steering when turning around in the air
    [Tooltip("How fast the character turns to face where it moves (degrees per second).")] // Hover text in the Inspector
    public float rotationSpeed = 720f; // Turn speed of the character

    [Header("Crouch and Dash (set by PlayerCrouch and PlayerDash)")] // Title in the Inspector
    [Tooltip("Walking speed while crouched = normal speed x this. You also cannot sprint or jump while crouched.")] // Hover text in the Inspector
    public float crouchSpeedMultiplier = 0.5f; // Speed multiplier while crouched
    [HideInInspector] public bool crouching; // True while the player is crouching
    [HideInInspector] public bool IsDashing; // True while PlayerDash moves the player and this script waits
    [HideInInspector] public bool IsSliding; // True while PlayerSlide moves the player and this script waits
    [HideInInspector] public bool IsClimbing; // True while PlayerClimb moves the player and this script waits

    [Header("Gravity & Jump")] // Title in the Inspector
    public float gravity = -19.62f; // How strongly gravity pulls down
    public float jumpHeight = 2.5f; // How high a full jump goes
    [Tooltip("Turn off to stop the player from jumping (the TS room does this through SceneCameraMode).")] // Hover text in the Inspector
    public bool canJump = true; // True = the player can jump
    [Tooltip("Grace time after leaving the ground where you can still jump.")] // Hover text in the Inspector
    public float coyoteTime = 0.12f; // Seconds you can still jump after leaving the ground
    [Tooltip("How long a jump press is remembered if you pressed it a moment too early.")] // Hover text in the Inspector
    public float jumpBufferTime = 0.12f; // Seconds an early jump press is remembered
    [HideInInspector] public Vector3 velocity; // Only the up/down speed lives here

    [Header("Snappy Feel")] // Title in the Inspector
    [Tooltip("Gravity is this many times stronger while you FALL. Higher = sharper, less floaty drops. 1 = the old floaty fall.")] // Hover text in the Inspector
    public float fallGravityMultiplier = 1.7f; // Extra gravity while falling
    [Tooltip("Gravity is this many times stronger while you rise and have let go of Space. Higher = a quick tap gives a much shorter hop. 1 = no variable jump.")] // Hover text in the Inspector
    public float shortJumpMultiplier = 2.8f; // Extra gravity when you let go of Space early
    [Tooltip("The jump when you hold Space the whole way is unchanged (it still reaches Jump Height).")] // Hover text in the Inspector
    public bool variableJump = true; // True = a short tap gives a short hop
    [Tooltip("The little burst of speed (m/s) you get the moment you start sprinting on the ground. 0 = no kick.")] // Hover text in the Inspector
    public float sprintKick = 2.5f; // Burst of speed when sprinting starts
    [Tooltip("The kick can push you this much (m/s) above max sprint speed, then it settles back down by itself.")] // Hover text in the Inspector
    public float sprintKickOvershoot = 1.5f; // How far above max sprint speed the kick can go

    [Header("Wall Jump Push")] // Title in the Inspector
    [Tooltip("How fast the sideways push from a wall jump fades while in the air (units per second).")] // Hover text in the Inspector
    public float airPushDecay = 10f; // How fast the push fades in the air
    [Tooltip("How fast the sideways push fades once you are on the ground.")] // Hover text in the Inspector
    public float groundPushDecay = 40f; // How fast the push fades on the ground

    [Header("Air Time Boost")] // Title in the Inspector
    [Tooltip("Seconds in the air before you start gaining extra speed.")] // Hover text in the Inspector
    public float airBoostDelay = 0.6f; // Seconds in the air before the boost starts
    [Tooltip("Extra speed gained per second of air time (meters per second, per second).")] // Hover text in the Inspector
    public float airSpeedGain = 3f; // Speed gained each second in the air
    [Tooltip("The most bonus speed you can gain from air time.")] // Hover text in the Inspector
    public float maxAirBonus = 8f; // Highest bonus from air time
    [Tooltip("How fast the bonus fades after you land.")] // Hover text in the Inspector
    public float airBonusFadeOnLand = 30f; // How fast the bonus fades on landing
    [Tooltip("Off = the air bonus stops growing while you wall run, so wall running no longer makes you faster and faster.")] // Hover text in the Inspector
    public bool airBonusGrowsOnWall = false; // True = the bonus keeps growing while wall running

    [Header("High Fall Momentum")] // Title in the Inspector
    public float fallSpeedThreshold = 12f; // Fall speed needed before you gain momentum on landing
    public float momentumCarryFactor = 0.4f; // How much of the extra fall speed becomes run speed

    public float HorizontalSpeed { get; private set; } // The real sideways speed in meters per second (for a HUD)

    private float currentSpeed; // The speed we are running at right now
    private float airTime; // Seconds since you left the ground
    private float airBonus; // Extra speed earned from air time
    private float highestFallSpeedThisJump; // The fastest fall speed during this jump
    private Vector3 push; // Sideways push that fades out by itself
    private Vector3 horizontalVelocity; // The real sideways speed, smoothed
    private float lastGroundedTime = -999f; // When we were last on the ground
    private float lastJumpPressedTime = -999f; // When jump was last pressed
    private bool jumpCutActive; // True only while rising from a normal ground jump
    private bool wasSprintWanted; // Was sprint wanted last frame

    void Start() // Runs once when the scene starts
    {
        input = GetComponent<PlayerInputHandler>(); // Find the input script
        wallRun = GetComponent<PlayerWallRun>(); // Find the wall run script
        if (controller == null) controller = GetComponent<CharacterController>(); // Find the CharacterController if none was assigned
        currentSpeed = walkSpeed; // Start at walking speed
    }

    public void AddPush(Vector3 amount) // Adds a sideways push (used by PlayerWallRun for the wall jump)
    {
        amount.y = 0f; // Remove any up/down part
        push += amount; // Add the push
    }

    public void ClearPush() // Removes the sideways push
    {
        push = Vector3.zero; // Set the push to nothing
    }

    public void SetHorizontalVelocity(Vector3 value) // Sets the sideways speed (used by PlayerWallRun)
    {
        value.y = 0f; // Remove any up/down part
        horizontalVelocity = value; // Save the new sideways speed
        HorizontalSpeed = value.magnitude; // Update the speed number
    }

    public void CarrySpeed(float speed) // Keeps your speed after a slide (used by PlayerSlide)
    {
        currentSpeed = Mathf.Max(currentSpeed, speed); // Never drop below the given speed
    }

    public Vector3 HorizontalVelocity { get { return horizontalVelocity; } } // The real sideways velocity

    public void SetRunSpeed(float value) // Sets the run speed (used by PlayerSlide when a slide ends)
    {
        currentSpeed = value; // Save the new run speed
    }

    public void CancelGroundJumpWindow() // Stops a wall jump from also triggering a normal jump
    {
        lastGroundedTime = -999f; // Forget that we were on the ground
        jumpCutActive = false; // Turn off the short jump cut
    }

    public void ResetMotion() // Call this when the player respawns
    {
        velocity = Vector3.zero; // Stop up/down movement
        push = Vector3.zero; // Remove the push
        horizontalVelocity = Vector3.zero; // Stop sideways movement
        currentSpeed = walkSpeed; // Go back to walking speed
        highestFallSpeedThisJump = 0f; // Forget the fall speed
        airTime = 0f; // Reset the air time
        airBonus = 0f; // Remove the air bonus
        jumpCutActive = false; // Turn off the short jump cut
    }

    void Update() // Runs every frame
    {
        if (LoadingScreen.IsLoading) return; // Frozen while the loading screen is up

        if (controller.isGrounded) // On the ground
        {
            airTime = 0f; // Reset the air time
            airBonus = Mathf.MoveTowards(airBonus, 0f, airBonusFadeOnLand * Time.deltaTime); // Fade the air bonus away
        }
        else // In the air
        {
            airTime += Time.deltaTime; // Count the air time
            bool onWall = wallRun != null && wallRun.IsWallRunning; // Are we wall running
            if (airTime > airBoostDelay && (!onWall || airBonusGrowsOnWall)) // Long enough in the air, and allowed to grow
                airBonus = Mathf.MoveTowards(airBonus, maxAirBonus, airSpeedGain * Time.deltaTime); // Grow the air bonus
        }

        if (IsDashing || IsSliding || IsClimbing) { jumpCutActive = false; return; } // Other scripts move the player now, so skip this movement

        if (wallRun != null && wallRun.IsWallRunning) { jumpCutActive = false; return; } // The wall run script moves the player now, so skip this movement

        velocity.x = 0f; // Never store sideways speed here
        velocity.z = 0f; // Never store sideways speed here

        if (controller.isGrounded) lastGroundedTime = Time.time; // Remember when we were last on the ground
        if (input.JumpTriggered) lastJumpPressedTime = Time.time; // Remember when jump was last pressed

        if (!controller.isGrounded) // In the air
        {
            if (velocity.y < highestFallSpeedThisJump) // Falling faster than before
            {
                highestFallSpeedThisJump = velocity.y; // Save the new fastest fall speed
            }
        }

        if (controller.isGrounded && velocity.y < 0) // Just landed
        {
            float positiveFallSpeed = Mathf.Abs(highestFallSpeedThisJump); // The fall speed as a positive number
            if (positiveFallSpeed > fallSpeedThreshold) // Fell fast enough
            {
                currentSpeed += (positiveFallSpeed - fallSpeedThreshold) * momentumCarryFactor; // Turn some of the fall speed into run speed
            }

            velocity.y = -2f; // Push down a little so we stay on the ground
            highestFallSpeedThisJump = 0f; // Reset the fall speed
        }

        bool sprintWanted = input.IsSprinting && input.MoveInput.magnitude > 0 && !crouching; // Sprint only if held, moving and not crouching

        if (sprintWanted && !wasSprintWanted && controller.isGrounded && sprintKick > 0f) // Sprint just started on the ground
            currentSpeed = Mathf.Min(currentSpeed + sprintKick, maxSprintSpeed + sprintKickOvershoot); // Give a quick burst of speed
        wasSprintWanted = sprintWanted; // Remember for the next frame
        float targetSpeed = sprintWanted ? maxSprintSpeed : walkSpeed; // The speed we want to reach
        if (crouching) targetSpeed *= crouchSpeedMultiplier; // Slower while crouching
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, (currentSpeed < targetSpeed ? accelerationRate : decelerationRate) * Time.deltaTime); // Move the run speed toward the target

        Vector3 positionBefore = transform.position; // Remember where we started this frame

        Vector3 desiredVelocity = Vector3.zero; // The speed we WANT to have
        Vector3 direction = new Vector3(input.MoveInput.x, 0f, input.MoveInput.y).normalized; // The input direction
        if (direction.magnitude >= 0.1f && playerCamera != null) // If the player is pressing a direction
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + playerCamera.eulerAngles.y; // The angle to move, relative to the camera
            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward; // The world direction to move

            desiredVelocity = moveDir.normalized * (currentSpeed + airBonus); // The speed we want, including the air bonus

            if (rotateToMovement) // If the character should turn
            {
                Quaternion targetRotation = Quaternion.Euler(0f, targetAngle + modelRotationOffset, 0f); // The rotation to face
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime); // Turn gradually instead of snapping
            }
        }

        bool hasDesired = desiredVelocity.sqrMagnitude > 0.0001f; // True if we want to move
        float accel; // How fast we change speed
        if (controller.isGrounded) accel = hasDesired ? groundAcceleration : groundDeceleration; // Ground values
        else accel = hasDesired ? airAcceleration : airDeceleration; // Air values

        if (!controller.isGrounded && hasDesired && Vector3.Dot(horizontalVelocity, desiredVelocity) < 0f) // Steering against our direction in the air
            accel *= airReverseMultiplier; // Turn around faster
        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, desiredVelocity, accel * Time.deltaTime); // Move the real speed toward the wanted speed

        bool jumpBuffered = Time.time - lastJumpPressedTime <= jumpBufferTime; // Was jump pressed a moment ago
        bool recentlyGrounded = Time.time - lastGroundedTime <= coyoteTime; // Were we on the ground a moment ago
        if (jumpBuffered && recentlyGrounded && canJump && !crouching) // All jump conditions are met
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity); // Launch upward
            jumpCutActive = true; // This rise can be cut short by letting go of Space
            lastJumpPressedTime = -999f; // Use up the jump press
            lastGroundedTime = -999f; // Use up the ground time
        }

        float gravityNow = gravity; // The gravity we use this frame
        if (velocity.y < 0f) // Falling
        {
            gravityNow *= fallGravityMultiplier; // Stronger gravity for sharper falls
            jumpCutActive = false; // The jump cut no longer matters
        }
        else if (variableJump && jumpCutActive && Keyboard.current != null && !Keyboard.current[Key.Space].isPressed) // Rising and Space was let go
        {
            gravityNow *= shortJumpMultiplier; // Stronger gravity for a short hop
        }
        velocity.y += gravityNow * Time.deltaTime; // Apply gravity

        float decay = controller.isGrounded ? groundPushDecay : airPushDecay; // How fast the push fades
        push = Vector3.MoveTowards(push, Vector3.zero, decay * Time.deltaTime); // Fade the push

        Vector3 total = horizontalVelocity + push + new Vector3(0f, velocity.y, 0f); // Walking + push + up/down speed in one move
        controller.Move(total * Time.deltaTime); // Move the player

        Vector3 moved = transform.position - positionBefore; // How far we really moved
        moved.y = 0f; // Ignore up/down
        HorizontalSpeed = Time.deltaTime > 0f ? moved.magnitude / Time.deltaTime : 0f; // The real sideways speed
    }
}