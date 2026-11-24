using UnityEngine; // Gives access to Unity classes like MonoBehaviour, Vector3, Physics

[DefaultExecutionOrder(-50)] // Runs before PlayerMovement so IsWallRunning is up to date
public class PlayerWallRun : MonoBehaviour // Lets the player run along walls and jump off them
{
    [Header("References")] // Title in the Inspector
    public CharacterController controller; // The player's CharacterController
    private PlayerInputHandler input; // The player's input script
    private PlayerMovement movement; // The player's movement script

    [Header("Wall Settings")] // Title in the Inspector
    [Tooltip("Speed along the wall. Lower = slower wall run. (Old value was 9.)")] // Hover text in the Inspector
    public float wallRunSpeed = 4.5f; // Speed along the wall
    [Tooltip("If you arrive faster than the wall run speed, you KEEP that speed and it fades down to the wall run speed bit by bit. This is how fast it fades (meters per second, every second). Lower = you keep your speed longer.")] // Hover text in the Inspector
    public float wallRunSlowdown = 5f; // How fast extra speed fades down
    [Tooltip("How fast you sink while on the wall (meters per second). 0 = you hold your height.")] // Hover text in the Inspector
    public float wallRunGravity = 0.5f; // How fast you sink on the wall
    [Tooltip("How quickly your up/down speed settles once you are on the wall. Lower = you carry more of your jump's rise onto the wall.")] // Hover text in the Inspector
    public float wallRunVerticalSmoothing = 40f; // How fast the up/down speed settles
    [Tooltip("Small push into the wall so you keep touching it while running along it.")] // Hover text in the Inspector
    public float wallStickForce = 3f; // Push into the wall
    [Tooltip("Seconds you stay on the wall after contact is lost for a moment.")] // Hover text in the Inspector
    public float contactGrace = 0.15f; // Grace time after losing contact

    [Header("Wall Jump Settings")] // Title in the Inspector
    public float wallJumpUpForce = 6f; // How high the wall jump goes
    public float wallJumpSideForce = 12f; // Pushes you away from the wall
    public float wallJumpForwardForce = 6f; // Pushes you on along the wall
    [Tooltip("After you wall jump off a wall, you can't wall run or wall jump that same wall again for this many seconds. Jumping off a DIFFERENT wall frees it again straight away.")] // Hover text in the Inspector
    public float sameWallLockTime = 1.5f; // Seconds the same wall stays locked

    [Header("Leaving the wall")] // Title in the Inspector
    [Tooltip("If you press toward AWAY from the wall at least this much (1 = straight away), the wall run lets go. Lower = lets go more easily.")] // Hover text in the Inspector
    [Range(0.1f, 1f)] public float awayLetGoAmount = 0.5f; // How much you must press away to let go

    [Header("Detection Settings")] // Title in the Inspector
    public LayerMask whatIsWall; // Which layers count as a wall

    public bool IsWallRunning { get; private set; } // True while the player is wall running
    private Vector3 currentWallNormal; // The direction the current wall faces
    private float lastTouchTime = -10f; // When the wall was last touched

    private float runSpeed; // The speed we are running at right now
    private float blockedUntil = -10f; // Other scripts can block wall running until this time

    private float wallJumpCooldown = 0.35f; // Seconds after a wall jump before the wall can be used again
    private float lastWallJumpTime = -10f; // When the last wall jump happened

    private GameObject currentWallObject; // The wall we are touching now
    private GameObject lockedWall; // The wall we last jumped off
    private Vector3 lockedWallNormal; // The direction the locked wall faces
    private float lockedWallUntil; // When the wall lock ends

    void Start() // Runs once when the scene starts
    {
        input = GetComponent<PlayerInputHandler>(); // Find the input script
        movement = GetComponent<PlayerMovement>(); // Find the movement script
        if (controller == null) controller = GetComponent<CharacterController>(); // Find the CharacterController if none was assigned
    }

    public void BlockFor(float seconds) // Blocks wall running for a moment (used by PlayerWallClimb)
    {
        blockedUntil = Time.time + seconds; // Set when the block ends
        StopWallRunning(); // Stop wall running now
    }

    void Update() // Runs every frame
    {
        if (LoadingScreen.IsLoading) // Frozen while the loading screen is up
        {
            StopWallRunning(); // No wall running
            return; // Leave the function
        }

        if (Time.time < blockedUntil) // Wall running is blocked
        {
            StopWallRunning(); // No wall running
            return; // Leave the function
        }

        if (controller.isGrounded) lockedWall = null; // Landing frees every wall again

        if (movement != null && movement.IsClimbing) // A climb is moving the player
        {
            StopWallRunning(); // No wall running meanwhile
            return; // Leave the function
        }

        if (Time.time < lastWallJumpTime + wallJumpCooldown) // Just did a wall jump
        {
            StopWallRunning(); // No wall running yet
            return; // Leave the function
        }

        bool isMovingForward = input.MoveInput.y > 0; // Is W pressed
        bool inAir = !controller.isGrounded; // Are we in the air
        bool holdingLateralInput = Mathf.Abs(input.MoveInput.x) > 0.1f; // Is A or D pressed
        bool touchingWall = Time.time - lastTouchTime <= contactGrace; // Did we touch a wall a moment ago

        bool pressingAway = false; // True if the player presses away from the wall
        if (movement != null && movement.playerCamera != null && input.MoveInput.sqrMagnitude > 0.01f) // If there is input and a camera
        {
            float angle = Mathf.Atan2(input.MoveInput.x, input.MoveInput.y) * Mathf.Rad2Deg + movement.playerCamera.eulerAngles.y; // The angle the player wants to go
            Vector3 want = Quaternion.Euler(0f, angle, 0f) * Vector3.forward; // The world direction the player wants to go
            pressingAway = Vector3.Dot(want, currentWallNormal) > awayLetGoAmount; // Check if it points away from the wall
        }

        if (inAir && isMovingForward && touchingWall && holdingLateralInput && !pressingAway && !IsWallLocked()) // All wall run conditions are met
        {
            StartWallRunning(); // Wall run
        }
        else // Something is missing
        {
            StopWallRunning(); // Do not wall run
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit) // Runs when the controller touches something
    {
        if (Time.time < lastWallJumpTime + wallJumpCooldown) return; // Ignore touches right after a wall jump

        if (((1 << hit.gameObject.layer) & whatIsWall) != 0) // If it is on a wall layer
        {
            if (Mathf.Abs(hit.normal.y) < 0.3f) // If it is an upright wall
            {
                lastTouchTime = Time.time; // Remember when we touched it
                currentWallObject = hit.gameObject; // Remember which wall it is

                Vector3 flat = new Vector3(hit.normal.x, 0f, hit.normal.z); // The wall direction without up and down
                if (flat.sqrMagnitude > 0.0001f) currentWallNormal = flat.normalized; // Save it
            }
        }
    }

    void StartWallRunning() // Runs every frame while wall running
    {
        bool justStarted = !IsWallRunning; // True on the first frame of the wall run
        IsWallRunning = true; // We are wall running now

        if (justStarted) // Arriving on the wall
        {
            float arrive = movement != null ? movement.HorizontalVelocity.magnitude : 0f; // The speed we arrived with
            runSpeed = Mathf.Max(wallRunSpeed, arrive); // Keep it, but never go below the wall run speed
        }
        runSpeed = Mathf.MoveTowards(runSpeed, wallRunSpeed, wallRunSlowdown * Time.deltaTime); // Fade down to the wall run speed with a steady slowdown

        Vector3 look = (movement != null && movement.playerCamera != null) ? movement.playerCamera.forward : -transform.forward; // The direction we look
        look.y = 0f; // Remove up and down
        Vector3 wallForward = Vector3.Cross(currentWallNormal, Vector3.up); // The direction along the wall
        if (Vector3.Dot(wallForward, look) < 0f) // If it points the wrong way
        {
            wallForward = -wallForward; // Flip it
        }
        wallForward.Normalize(); // Make it length 1

        float currentUp = movement != null ? movement.velocity.y : 0f; // The current up/down speed
        float verticalSpeed = Mathf.MoveTowards(currentUp, -wallRunGravity, wallRunVerticalSmoothing * Time.deltaTime); // Settle the up/down speed smoothly

        Vector3 runVelocity = wallForward * runSpeed - currentWallNormal * wallStickForce; // Run along the wall with a small pull into it
        controller.Move((runVelocity + Vector3.up * verticalSpeed) * Time.deltaTime); // Move the player

        if (movement != null) // If there is a movement script
        {
            Quaternion faceRun = Quaternion.Euler(0f, Mathf.Atan2(wallForward.x, wallForward.z) * Mathf.Rad2Deg + movement.modelRotationOffset, 0f); // The rotation that faces the running direction
            transform.rotation = Quaternion.RotateTowards(transform.rotation, faceRun, movement.rotationSpeed * Time.deltaTime); // Turn gradually to face it

            movement.velocity = new Vector3(0f, verticalSpeed, 0f); // Share the up/down speed with PlayerMovement
            movement.SetHorizontalVelocity(wallForward * runSpeed); // Share the run speed so you keep it when leaving the wall
            movement.ClearPush(); // Remove any old push

            if (input.JumpTriggered) // The player pressed jump
            {
                lastWallJumpTime = Time.time; // Remember when the wall jump happened
                movement.CancelGroundJumpWindow(); // Make sure no normal jump also happens

                lockedWall = currentWallObject; // Lock this wall for a while
                lockedWallNormal = currentWallNormal; // Remember which way it faces
                lockedWallUntil = Time.time + sameWallLockTime; // Set when the lock ends

                Vector3 directionalPush = currentWallNormal * wallJumpSideForce; // Push away from the wall
                Vector3 forwardPush = wallForward * wallJumpForwardForce; // Push on along the wall
                movement.AddPush(directionalPush + forwardPush); // Add both pushes

                float verticalPush = Mathf.Sqrt(wallJumpUpForce * -2f * movement.gravity); // The upward speed for the jump
                movement.velocity = new Vector3(0f, verticalPush, 0f); // Launch upward

                StopWallRunning(); // The wall run ends
            }
        }
    }

    void StopWallRunning() // Ends the wall run
    {
        IsWallRunning = false; // We are not wall running anymore
    }

    bool IsWallLocked() // True while the wall we touch is the one we just jumped off
    {
        if (lockedWall == null || Time.time >= lockedWallUntil) return false; // No lock, or the lock is over
        if (currentWallObject != lockedWall) return false; // A different wall object
        return Vector3.Dot(currentWallNormal, lockedWallNormal) > 0.7f; // Same object only counts if it faces the same way
    }
}