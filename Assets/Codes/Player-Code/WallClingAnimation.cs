using UnityEngine; // Gives access to Unity classes like MonoBehaviour, Animator, Quaternion

public class WallClingAnimation : MonoBehaviour // Plays the wall hang and wall run animations
{
    private enum HangAction { Drop = 0, Climb = 1, JumpAway = 2 } // The ways to leave the wall (same numbers as the HangAction blend tree)

    [Header("References")] // Title in the Inspector
    [Tooltip("Leave empty to find the Animator on the model automatically.")] // Hover text in the Inspector
    [SerializeField] private Animator animator; // The Animator on the model

    [Header("Testing")] // Title in the Inspector
    [Tooltip("Prints what this script does in the Console. Turn off when everything works.")] // Hover text in the Inspector
    [SerializeField] private bool debugLogs = true; // True = print messages in the Console

    [Header("Keep the model inside the capsule")] // Title in the Inspector
    [Tooltip("Moves the model up (+) or down (-) while hanging. Model too high = more negative (-0.8, -1.0). Too low = closer to 0.")] // Hover text in the Inspector
    [SerializeField] private float hangOffsetY = -0.5f; // How far the model moves down while hanging
    [Tooltip("How fast the model slides to the hang height and back.")] // Hover text in the Inspector
    [SerializeField] private float offsetSpeed = 10f; // How fast the model slides
    [Tooltip("Seconds the offset stays after leaving the wall, so Hop Up / Jump / Drop also play inside the capsule.")] // Hover text in the Inspector
    [SerializeField] private float releaseHold = 0.15f; // Seconds the offset stays after leaving the wall

    [Tooltip("Seconds the model stays at hang height after a Hop Up, so it does not rise above the capsule and then snap back down when you grab the wall again.")] // Hover text in the Inspector
    [SerializeField] private float hopHoldTime = 1.6f; // Seconds the offset stays after a Hop Up

    [Header("Wall run")] // Title in the Inspector
    [Tooltip("Angle between the wall and the character's body while wall running. 90 = upright, 105 = the head leans 15 degrees away from the wall, so the feet stay on the wall.")] // Hover text in the Inspector
    [SerializeField] private float wallRunAngle = 105f; // The body angle while wall running
    [Tooltip("How fast the tilt eases in and out (higher = faster).")] // Hover text in the Inspector
    [SerializeField] private float leanSpeed = 8f; // How fast the lean changes
    [Tooltip("How far to look sideways for the wall (meters).")] // Hover text in the Inspector
    [SerializeField] private float wallCheckDistance = 1.6f; // How far to look for the wall
    [Tooltip("Tick this if the HEAD leans toward the wall instead of away from it.")] // Hover text in the Inspector
    [SerializeField] private bool flipLean = false; // True = lean the other way
    [Tooltip("Moves the model sideways so the feet touch the wall while wall running.")] // Hover text in the Inspector
    [SerializeField] private bool feetTouchWall = true; // True = move the model so the feet touch the wall
    [Tooltip("Small gap between the feet and the wall (meters). 0 = the feet touch it exactly. Raise it if the model sinks into the wall.")] // Hover text in the Inspector
    [SerializeField] private float footGap = 0.05f; // Gap between the feet and the wall

    private static readonly int WallRunHash = Animator.StringToHash("WallRun"); // Fast id of the WallRun parameter
    private static readonly int WallClingHash = Animator.StringToHash("WallCling"); // Fast id of the WallCling parameter
    private static readonly int HangGrabHash = Animator.StringToHash("HangGrab"); // Fast id of the HangGrab parameter
    private static readonly int HangActionHash = Animator.StringToHash("HangAction"); // Fast id of the HangAction parameter
    private static readonly int HangExitHash = Animator.StringToHash("HangExit"); // Fast id of the HangExit parameter

    [Header("Jump away from the wall")] // Title in the Inspector
    [Tooltip("Turns the model to face away from the wall when you jump away, so it does not snap back afterwards.")] // Hover text in the Inspector
    [SerializeField] private bool turnAwayOnJump = true; // True = turn the model away from the wall on a jump
    [Tooltip("How fast the model turns (higher = faster).")] // Hover text in the Inspector
    [SerializeField] private float turnSpeed = 40f; // How fast the model turns away
    [Tooltip("Longest time the model keeps facing away. It also stops when you land.")] // Hover text in the Inspector
    [SerializeField] private float faceAwayTime = 2.5f; // Longest time the model faces away
    [Tooltip("How fast the model turns back to normal after the jump (lower = slower and smoother).")] // Hover text in the Inspector
    [SerializeField] private float turnBackSpeed = 2.5f; // How fast the model turns back
    [Tooltip("Tick this if the model faces the WRONG way (toward the wall) when jumping away.")] // Hover text in the Inspector
    [SerializeField] private bool flipTurn = false; // True = face the other way

    private CharacterController controller; // The player's CharacterController
    private float faceAwayStart; // When the face away turn started
    private Quaternion modelBaseLocalRot; // The model's normal rotation
    private Quaternion awayWorldRot; // The rotation that faces away from the wall
    private float faceAwayUntil = -1f; // When the face away turn ends
    private PlayerWallRun wallRun; // The wall run script
    private PlayerMovement movement; // The movement script
    private bool wallRunPlaying; // The WallRun value we sent to the Animator last
    private float wallSide; // +1 wall on the right, -1 wall on the left
    private float leanNow; // The current lean (smoothed)
    private Vector3 runDir = Vector3.forward; // The direction we run along the wall
    private Vector3 appliedShift = Vector3.zero; // The sideways shift we added last frame
    private float wallDistance = 0.5f; // Distance from the player's center to the wall
    private Quaternion appliedLean = Quaternion.identity; // The lean we added last frame
    private Transform model; // The model that holds the Animator
    private Vector3 modelBaseLocalPos; // Where the model normally sits
    private float holdUntil; // Keeps the hang offset a little after leaving the wall
    private bool clinging; // True while hanging on the wall

    public bool IsClinging => clinging; // True while the player hangs on the wall

    private void Awake() // Runs when the object is created
    {
        controller = GetComponent<CharacterController>(); // Find the CharacterController
        wallRun = GetComponent<PlayerWallRun>(); // Find the wall run script
        movement = GetComponent<PlayerMovement>(); // Find the movement script
        if (animator == null) animator = GetComponentInChildren<Animator>(); // Find the Animator if none was assigned
        if (animator != null) // If we have an Animator
        {
            model = animator.transform; // The model is the Animator's object
            modelBaseLocalPos = model.localPosition; // Remember its normal position
            modelBaseLocalRot = model.localRotation; // Remember its normal rotation
        }
    }

    private void LateUpdate() // Runs every frame after all Update calls
    {
        if (model == null || model == transform) return; // Stop if there is no separate model

        model.rotation = Quaternion.Inverse(appliedLean) * model.rotation; // Take away last frame's lean so it never piles up
        appliedLean = Quaternion.identity; // Reset the stored lean
        model.position -= appliedShift; // Take away last frame's sideways shift
        appliedShift = Vector3.zero; // Reset the stored shift

        bool grounded = controller != null && controller.isGrounded; // Are we on the ground
        bool hanging = clinging || (Time.time < holdUntil && !grounded); // Should the model be lowered
        Vector3 target = modelBaseLocalPos + (hanging ? new Vector3(0f, hangOffsetY, 0f) : Vector3.zero); // The position the model should have
        float posSpeed = (grounded && !clinging) ? offsetSpeed * 4f : offsetSpeed; // Go back to normal quickly once on the floor
        model.localPosition = Vector3.Lerp(model.localPosition, target, 1f - Mathf.Exp(-posSpeed * Time.deltaTime)); // Slide the model toward the target

        if (controller != null && controller.isGrounded && Time.time - faceAwayStart > 0.3f) // Landed after the takeoff moment
            faceAwayUntil = -1f; // End the face away turn

        if (Time.time < faceAwayUntil) // Still facing away from the wall
            model.rotation = Quaternion.Slerp(model.rotation, awayWorldRot, 1f - Mathf.Exp(-turnSpeed * Time.deltaTime)); // Turn toward facing away
        else if (Quaternion.Angle(model.localRotation, modelBaseLocalRot) > 0.1f) // Not at the normal rotation yet
            model.localRotation = Quaternion.Slerp(model.localRotation, modelBaseLocalRot, 1f - Mathf.Exp(-turnBackSpeed * Time.deltaTime)); // Turn back to normal smoothly

        ApplyWallRun(); // Wall run animation and lean (runs last so nothing above undoes it)
    }

    private void ApplyWallRun() // Tells the Animator about wall running and leans the model toward the wall
    {
        bool running = wallRun != null && wallRun.IsWallRunning; // Are we wall running

        if (animator != null && running != wallRunPlaying) // The wall run state changed
        {
            wallRunPlaying = running; // Remember the new state
            animator.SetBool(WallRunHash, running); // Tell the Animator
            if (debugLogs) Debug.Log("[WallRun] Animator WallRun = " + running); // Print it in the Console
        }

        if (running && movement != null) // Wall running and we have the movement script
        {
            Vector3 dir = movement.HorizontalVelocity; dir.y = 0f; // The sideways direction we move
            if (dir.sqrMagnitude > 0.01f) // If we are really moving
            {
                runDir = dir.normalized; // Save the run direction
                Vector3 right = Vector3.Cross(Vector3.up, runDir); // The direction to our right
                Vector3 origin = controller != null ? controller.bounds.center : transform.position; // Start the rays from the middle of the player
                RaycastHit hit; // The ray hit info
                if (Physics.Raycast(origin, right, out hit, wallCheckDistance, wallRun.whatIsWall, QueryTriggerInteraction.Ignore)) { wallSide = 1f; wallDistance = hit.distance; } // Wall on the right
                else if (Physics.Raycast(origin, -right, out hit, wallCheckDistance, wallRun.whatIsWall, QueryTriggerInteraction.Ignore)) { wallSide = -1f; wallDistance = hit.distance; } // Wall on the left
            }
        }

        float tiltDegrees = Mathf.Max(0f, wallRunAngle - 90f); // Tilt amount in degrees
        float target = running ? wallSide * tiltDegrees : 0f; // The lean we want
        leanNow = Mathf.Lerp(leanNow, target, 1f - Mathf.Exp(-leanSpeed * Time.deltaTime)); // Ease toward the wanted lean

        if (model != null && model != transform && Mathf.Abs(leanNow) > 0.05f) // If there is a model and a noticeable lean
        {
            float angle = (flipLean ? -1f : 1f) * leanNow; // The lean angle, flipped if needed
            appliedLean = Quaternion.AngleAxis(angle, runDir); // Tilt around the running direction
            model.rotation = appliedLean * model.rotation; // Apply the tilt

            if (feetTouchWall) // If the feet should touch the wall
            {
                float weight = tiltDegrees > 0.01f ? Mathf.Abs(leanNow) / tiltDegrees : 1f; // How much of the lean is done
                Vector3 toWall = Vector3.Cross(Vector3.up, runDir) * Mathf.Sign(leanNow); // The direction toward the wall
                float gap = Mathf.Max(0f, wallDistance - footGap); // The distance to move
                appliedShift = toWall * gap * weight; // The shift to apply
                model.position += appliedShift; // Slide the model toward the wall
            }
        }
    }

    public void StartCling() // Call when the player grabs the wall
    {
        if (animator == null) // No Animator found
        {
            if (debugLogs) Debug.LogWarning("[WallCling] StartCling called but no Animator was found on the model."); // Warn the developer
            return; // Leave the function
        }

        if (clinging && debugLogs) // Already clinging, so StopCling never ran last time
            Debug.LogWarning("[WallCling] StartCling called while already clinging. Grabbing again anyway."); // Warn the developer

        clinging = true; // We are hanging now
        animator.ResetTrigger(HangExitHash); // Clear any old exit trigger
        animator.SetBool(WallClingHash, true); // Turn the cling state on
        animator.SetTrigger(HangGrabHash); // Play the grab animation once

        if (debugLogs) // If debug messages are on
            Debug.Log("[WallCling] StartCling: sent HangGrab to Animator on '" + animator.gameObject.name + "'."); // Print what we sent
    }

    public void DropFromWall() { Leave(HangAction.Drop); } // Player lets go and falls

    public void ClimbUp() { Leave(HangAction.Climb); } // Player hops up the wall

    public void JumpAway() { Leave(HangAction.JumpAway); } // Player jumps away from the wall

    private void JumpAway(Vector3 wallNormal) // Jump away, and turn the model to face away from the wall
    {
        Vector3 n = wallNormal; n.y = 0f; // The wall direction without up and down
        if (turnAwayOnJump && model != null && n.sqrMagnitude > 0.0001f) // If turning away is on and we have a direction
        {
            if (flipTurn) n = -n; // Flip the direction if needed
            awayWorldRot = Quaternion.LookRotation(n.normalized, Vector3.up); // The rotation that faces away from the wall
            faceAwayStart = Time.time; // Remember when the turn started
            faceAwayUntil = Time.time + faceAwayTime; // Set when the turn ends
            model.rotation = awayWorldRot; // Turn at once, together with the camera swing, so you never see a spin
        }
        Leave(HangAction.JumpAway); // Play the jump away animation
    }

    public void JumpFromWall(Vector3 wallNormal, Vector3 lookForward, float lookAtWallDot = 0.2f) // Hop Up if looking AT the wall, Jump From Wall if looking AWAY
    {
        Vector3 look = lookForward; look.y = 0f; // The look direction without up and down
        Vector3 n = wallNormal; n.y = 0f; // The wall direction without up and down

        if (look.sqrMagnitude < 0.0001f || n.sqrMagnitude < 0.0001f) // If either direction is empty
        {
            ClimbUp(); // Just hop up
            return; // Leave the function
        }

        bool lookingAtWall = Vector3.Dot(look.normalized, -n.normalized) >= lookAtWallDot; // True if we look toward the wall
        if (lookingAtWall) ClimbUp(); else JumpAway(wallNormal); // Hop up, or jump away
    }

    public void StopCling() { if (clinging) Leave(HangAction.Drop); } // Any other way the cling ends plays Hang Drop

    private void Leave(HangAction action) // Leaves the wall with the given animation
    {
        if (!clinging || animator == null) return; // Stop if we are not hanging
        if (debugLogs) Debug.Log("[WallCling] Leaving the wall: " + action); // Print which animation plays

        clinging = false; // We are not hanging anymore
        holdUntil = Time.time + (action == HangAction.Climb ? hopHoldTime : releaseHold); // Keep the hang offset for a short time
        animator.SetBool(WallClingHash, false); // Turn the cling state off
        animator.ResetTrigger(HangGrabHash); // Clear any old grab trigger

        animator.SetFloat(HangActionHash, (float)action); // Set the exact value so only one clip plays
        animator.SetTrigger(HangExitHash); // Play the exit animation
    }

    private void OnDisable() // Runs when this script is turned off
    {
        if (clinging && animator != null) // If still hanging
        {
            clinging = false; // We are not hanging anymore
            animator.SetBool(WallClingHash, false); // Never leave the bool stuck on
        }
    }
}