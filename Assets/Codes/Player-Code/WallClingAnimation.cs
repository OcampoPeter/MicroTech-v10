using UnityEngine;

// Plays the wall hang animations, keeps the model inside the capsule while hanging,
// and drives the wall run animation (Animator bool "WallRun") with a lean toward the wall.
// Animator parameters needed: HangGrab (Trigger), WallCling (Bool), HangAction (Float), HangExit (Trigger).
public class WallClingAnimation : MonoBehaviour
{
    // 0 = Drop, 1 = Climb (Hop Up), 2 = JumpAway (Jump From Wall). Same numbers as the HangAction blend tree.
    private enum HangAction { Drop = 0, Climb = 1, JumpAway = 2 }

    [Header("References")]
    [Tooltip("Leave empty to find the Animator on the model automatically.")]
    [SerializeField] private Animator animator;

    [Header("Testing")]
    [Tooltip("Prints what this script does in the Console. Turn off when everything works.")]
    [SerializeField] private bool debugLogs = true;

    [Header("Keep the model inside the capsule")]
    [Tooltip("Moves the model up (+) or down (-) while hanging. Model too high = more negative (-0.8, -1.0). Too low = closer to 0.")]
    [SerializeField] private float hangOffsetY = -0.5f;
    [Tooltip("How fast the model slides to the hang height and back.")]
    [SerializeField] private float offsetSpeed = 10f;
    [Tooltip("Seconds the offset stays after leaving the wall, so Hop Up / Jump / Drop also play inside the capsule.")]
    [SerializeField] private float releaseHold = 0.15f;

    [Tooltip("Seconds the model stays at hang height after a Hop Up, so it does not rise above the capsule and then snap back down when you grab the wall again.")]
    [SerializeField] private float hopHoldTime = 1.6f;

    [Header("Wall run")]
    [Tooltip("Angle between the wall and the character's body while wall running. 90 = upright, 105 = the head leans 15 degrees away from the wall, so the feet stay on the wall.")]
    [SerializeField] private float wallRunAngle = 105f;
    [Tooltip("How fast the tilt eases in and out (higher = faster).")]
    [SerializeField] private float leanSpeed = 8f;
    [Tooltip("How far to look sideways for the wall (meters).")]
    [SerializeField] private float wallCheckDistance = 1.6f;
    [Tooltip("Tick this if the HEAD leans toward the wall instead of away from it.")]
    [SerializeField] private bool flipLean = false;
    [Tooltip("Moves the model sideways so the feet touch the wall while wall running.")]
    [SerializeField] private bool feetTouchWall = true;
    [Tooltip("Small gap between the feet and the wall (meters). 0 = the feet touch it exactly. Raise it if the model sinks into the wall.")]
    [SerializeField] private float footGap = 0.05f;

    private static readonly int WallRunHash = Animator.StringToHash("WallRun");
    private static readonly int WallClingHash = Animator.StringToHash("WallCling");
    private static readonly int HangGrabHash = Animator.StringToHash("HangGrab");
    private static readonly int HangActionHash = Animator.StringToHash("HangAction");
    private static readonly int HangExitHash = Animator.StringToHash("HangExit");

    [Header("Jump away from the wall")]
    [Tooltip("Turns the model to face away from the wall when you jump away, so it does not snap back afterwards.")]
    [SerializeField] private bool turnAwayOnJump = true;
    [Tooltip("How fast the model turns (higher = faster).")]
    [SerializeField] private float turnSpeed = 40f;
    [Tooltip("Longest time the model keeps facing away. It also stops when you land.")]
    [SerializeField] private float faceAwayTime = 2.5f;
    [Tooltip("How fast the model turns back to normal after the jump (lower = slower and smoother).")]
    [SerializeField] private float turnBackSpeed = 2.5f;
    [Tooltip("Tick this if the model faces the WRONG way (toward the wall) when jumping away.")]
    [SerializeField] private bool flipTurn = false;

    private CharacterController controller;
    private float faceAwayStart;
    private Quaternion modelBaseLocalRot;
    private Quaternion awayWorldRot;
    private float faceAwayUntil = -1f;
    private PlayerWallRun wallRun;
    private PlayerMovement movement;
    private bool wallRunPlaying;           // the Animator WallRun bool we sent last
    private float wallSide;                // +1 wall on the right, -1 wall on the left (while running)
    private float leanNow;                 // current lean (smoothed)
    private Vector3 runDir = Vector3.forward;
    private Vector3 appliedShift = Vector3.zero;          // the sideways shift we added last frame, removed first thing next frame
    private float wallDistance = 0.5f;                    // distance from the player centre to the wall
    private Quaternion appliedLean = Quaternion.identity;   // the lean we added last frame, removed first thing next frame
    private Transform model;              // the model that holds the Animator
    private Vector3 modelBaseLocalPos;    // where the model normally sits
    private float holdUntil;              // keeps the hang offset a little after leaving the wall
    private bool clinging;

    public bool IsClinging => clinging;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        wallRun = GetComponent<PlayerWallRun>();
        movement = GetComponent<PlayerMovement>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            model = animator.transform;
            modelBaseLocalPos = model.localPosition;
            modelBaseLocalRot = model.localRotation;
        }
    }

    private void LateUpdate()
    {
        if (model == null || model == transform) return;

        // Take away last frame's wall run lean first, so the lean never piles up.
        model.rotation = Quaternion.Inverse(appliedLean) * model.rotation;
        appliedLean = Quaternion.identity;
        model.position -= appliedShift;
        appliedShift = Vector3.zero;

        // Slides the model down while hanging, and back up after.
        // The lowered hang height must never stay on when the feet are on the floor (it sinks the feet into the ground).
        bool grounded = controller != null && controller.isGrounded;
        bool hanging = clinging || (Time.time < holdUntil && !grounded);
        Vector3 target = modelBaseLocalPos + (hanging ? new Vector3(0f, hangOffsetY, 0f) : Vector3.zero);
        float posSpeed = (grounded && !clinging) ? offsetSpeed * 4f : offsetSpeed;   // back to normal quickly once on the floor
        model.localPosition = Vector3.Lerp(model.localPosition, target, 1f - Mathf.Exp(-posSpeed * Time.deltaTime));

        // After a jump away: face away from the wall for a moment, then turn back to normal smoothly.
        // Landing ends the "face away" early (after a short moment so the takeoff does not count as landed).
        if (controller != null && controller.isGrounded && Time.time - faceAwayStart > 0.3f)
            faceAwayUntil = -1f;

        if (Time.time < faceAwayUntil)
            model.rotation = Quaternion.Slerp(model.rotation, awayWorldRot, 1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
        else if (Quaternion.Angle(model.localRotation, modelBaseLocalRot) > 0.1f)
            model.localRotation = Quaternion.Slerp(model.localRotation, modelBaseLocalRot, 1f - Mathf.Exp(-turnBackSpeed * Time.deltaTime));

        ApplyWallRun();   // wall run animation + lean (runs last so nothing above undoes it)
    }

    // Wall run: tells the Animator, finds which side the wall is on, and leans the model toward it.
    private void ApplyWallRun()
    {
        bool running = wallRun != null && wallRun.IsWallRunning;

        if (animator != null && running != wallRunPlaying)
        {
            wallRunPlaying = running;
            animator.SetBool(WallRunHash, running);
            if (debugLogs) Debug.Log("[WallRun] Animator WallRun = " + running);
        }

        if (running && movement != null)
        {
            Vector3 dir = movement.HorizontalVelocity; dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
            {
                runDir = dir.normalized;
                Vector3 right = Vector3.Cross(Vector3.up, runDir);
                Vector3 origin = controller != null ? controller.bounds.center : transform.position;
                RaycastHit hit;
                if (Physics.Raycast(origin, right, out hit, wallCheckDistance, wallRun.whatIsWall, QueryTriggerInteraction.Ignore)) { wallSide = 1f; wallDistance = hit.distance; }
                else if (Physics.Raycast(origin, -right, out hit, wallCheckDistance, wallRun.whatIsWall, QueryTriggerInteraction.Ignore)) { wallSide = -1f; wallDistance = hit.distance; }
            }
        }

        // Tilt amount in degrees: 105 degrees from the wall = the head leans 15 degrees away from it.
        float tiltDegrees = Mathf.Max(0f, wallRunAngle - 90f);
        float target = running ? wallSide * tiltDegrees : 0f;
        leanNow = Mathf.Lerp(leanNow, target, 1f - Mathf.Exp(-leanSpeed * Time.deltaTime));

        if (model != null && model != transform && Mathf.Abs(leanNow) > 0.05f)
        {
            // Tilt around the feet (the model's origin), so the feet stay where they are and the head moves away.
            float angle = (flipLean ? -1f : 1f) * leanNow;
            appliedLean = Quaternion.AngleAxis(angle, runDir);
            model.rotation = appliedLean * model.rotation;

            if (feetTouchWall)
            {
                // Slide the model toward the wall until the feet touch it.
                float weight = tiltDegrees > 0.01f ? Mathf.Abs(leanNow) / tiltDegrees : 1f;
                Vector3 toWall = Vector3.Cross(Vector3.up, runDir) * Mathf.Sign(leanNow);
                float gap = Mathf.Max(0f, wallDistance - footGap);
                appliedShift = toWall * gap * weight;
                model.position += appliedShift;
            }
        }
    }

    // Call when the player grabs the wall: Braced Hang, then Hang Idle.
    public void StartCling()
    {
        if (animator == null)
        {
            if (debugLogs) Debug.LogWarning("[WallCling] StartCling called but no Animator was found on the model.");
            return;
        }

        // If we still think we are clinging, StopCling never ran last time. Grab again anyway.
        if (clinging && debugLogs)
            Debug.LogWarning("[WallCling] StartCling called while already clinging. Grabbing again anyway.");

        clinging = true;
        animator.ResetTrigger(HangExitHash);
        animator.SetBool(WallClingHash, true);
        animator.SetTrigger(HangGrabHash);   // fires Any State -> Braced Hang once

        if (debugLogs)
            Debug.Log("[WallCling] StartCling: sent HangGrab to Animator on '" + animator.gameObject.name + "'.");
    }

    // Player lets go and falls.
    public void DropFromWall() { Leave(HangAction.Drop); }

    // Player hops up the wall.
    public void ClimbUp() { Leave(HangAction.Climb); }

    // Player jumps away from the wall.
    public void JumpAway() { Leave(HangAction.JumpAway); }

    // Jump away, and turn the model to face away from the wall (wallNormal points away from the wall).
    private void JumpAway(Vector3 wallNormal)
    {
        Vector3 n = wallNormal; n.y = 0f;
        if (turnAwayOnJump && model != null && n.sqrMagnitude > 0.0001f)
        {
            if (flipTurn) n = -n;
            awayWorldRot = Quaternion.LookRotation(n.normalized, Vector3.up);
            faceAwayStart = Time.time;
            faceAwayUntil = Time.time + faceAwayTime;
            model.rotation = awayWorldRot;   // turn at once, together with the camera swing, so you never see a spin
        }
        Leave(HangAction.JumpAway);
    }

    // Jump off the wall: Hop Up if looking AT the wall, Jump From Wall if looking AWAY.
    // wallNormal points away from the wall. lookForward is the camera's forward direction.
    public void JumpFromWall(Vector3 wallNormal, Vector3 lookForward, float lookAtWallDot = 0.2f)
    {
        Vector3 look = lookForward; look.y = 0f;
        Vector3 n = wallNormal; n.y = 0f;

        if (look.sqrMagnitude < 0.0001f || n.sqrMagnitude < 0.0001f)
        {
            ClimbUp();
            return;
        }

        bool lookingAtWall = Vector3.Dot(look.normalized, -n.normalized) >= lookAtWallDot;
        if (lookingAtWall) ClimbUp(); else JumpAway(wallNormal);
    }

    // Any other way the cling ends (slip over, Space released, landed, wall gone): Hang Drop.
    public void StopCling() { if (clinging) Leave(HangAction.Drop); }

    private void Leave(HangAction action)
    {
        if (!clinging || animator == null) return;
        if (debugLogs) Debug.Log("[WallCling] Leaving the wall: " + action);

        clinging = false;
        holdUntil = Time.time + (action == HangAction.Climb ? hopHoldTime : releaseHold);
        animator.SetBool(WallClingHash, false);
        animator.ResetTrigger(HangGrabHash);

        // Exact value (no damping) so the blend tree plays ONE clip, not a mix.
        animator.SetFloat(HangActionHash, (float)action);
        animator.SetTrigger(HangExitHash);
    }

    private void OnDisable()
    {
        // Never leave the bool stuck on if the component is disabled.
        if (clinging && animator != null)
        {
            clinging = false;
            animator.SetBool(WallClingHash, false);
        }
    }
}