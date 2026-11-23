using UnityEngine;

/// <summary>
/// Drives the wall hang animations in PlayerAnimController.
/// Does NOT touch PlayerAnimator or any locomotion / air parameter.
///
/// ANIMATOR PARAMETERS (add these):
///   HangGrab    (Trigger)  -> Any State -> Braced Hang uses THIS, not WallCling
///   WallCling   (Bool)     -> still set, but no arrow needs it
///   HangAction  (Float)   0 = Drop, 1 = Climb up, 2 = Jump away   (blend tree parameter)
///   HangExit    (Trigger)
///
/// USAGE (from your wall script):
///   clingAnim.StartCling();   // player grabs the wall  -> "Hang Braced", then "Hang Idle"
///   clingAnim.DropFromWall(); // player lets go         -> Hang Drop
///   clingAnim.ClimbUp();      // player climbs up       -> Hang Jump Up
///   clingAnim.JumpAway();     // player jumps off wall  -> Hang Jump Opposite Wall
///
/// NOTE: Apply Root Motion is off, so the animation plays in place. Your own code
/// still has to move the player (climb up onto the ledge, push away from the wall).
/// </summary>
public class WallClingAnimation : MonoBehaviour
{
    private enum HangAction { Drop = 0, Climb = 1, JumpAway = 2 }

    [Header("References")]
    [Tooltip("Leave empty to find the Animator on the model automatically.")]
    [SerializeField] private Animator animator;

    [Header("Testing")]
    [Tooltip("Prints what this script does in the Console. Turn off when everything works.")]
    [SerializeField] private bool debugLogs = true;

    private static readonly int WallClingHash = Animator.StringToHash("WallCling");
    private static readonly int HangGrabHash = Animator.StringToHash("HangGrab");
    private static readonly int HangActionHash = Animator.StringToHash("HangAction");
    private static readonly int HangExitHash = Animator.StringToHash("HangExit");

    private bool clinging;

    public bool IsClinging => clinging;

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    /// <summary>Call when the player grabs the wall.</summary>
    public void StartCling()
    {
        if (animator == null)
        {
            if (debugLogs) Debug.LogWarning("[WallCling] StartCling called but no Animator was found on the model.");
            return;
        }

        // If we still think we are clinging, StopCling never ran last time (a missing hook).
        // Do NOT ignore the grab: send it again so the animation still plays.
        if (clinging && debugLogs)
            Debug.LogWarning("[WallCling] StartCling called while already clinging. StopCling was probably never called (EDIT 5 in PlayerWallBrake?). Grabbing again anyway.");

        clinging = true;
        animator.ResetTrigger(HangExitHash);
        animator.SetBool(WallClingHash, true);
        animator.SetTrigger(HangGrabHash);   // fires Any State / Air -> Braced Hang ONCE

        if (debugLogs)
            Debug.Log("[WallCling] StartCling: sent HangGrab to Animator on '" + animator.gameObject.name + "'.");
    }

    /// <summary>Player lets go and falls.</summary>
    public void DropFromWall() => Leave(HangAction.Drop);

    /// <summary>Player climbs up onto the ledge.</summary>
    public void ClimbUp() => Leave(HangAction.Climb);

    /// <summary>Player jumps away from the wall.</summary>
    public void JumpAway() => Leave(HangAction.JumpAway);

    /// <summary>
    /// Jumping off the wall: plays Hop Up (ClimbUp) if the player is looking AT the wall,
    /// or Jump From Wall (JumpAway) if the player is looking AWAY from it.
    /// wallNormal points away from the wall. lookForward is the camera's forward direction.
    /// </summary>
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
        if (lookingAtWall) ClimbUp(); else JumpAway();
    }

    /// <summary>
    /// Fallback if the cling ends for any other reason (wall gone, landed, etc.).
    /// Plays the drop animation so the player is never stuck in the hang pose.
    /// </summary>
    public void StopCling() => Leave(HangAction.Drop);

    private void Leave(HangAction action)
    {
        if (!clinging || animator == null) return;
        if (debugLogs) Debug.Log("[WallCling] Leaving the wall: " + action);
        clinging = false;
        animator.SetBool(WallClingHash, false);
        animator.ResetTrigger(HangGrabHash);

        // Set the exact value (no damping) so the blend tree plays ONE clip, not a mix.
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