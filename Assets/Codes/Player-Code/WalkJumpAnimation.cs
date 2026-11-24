using UnityEngine; // Gives access to Unity classes like MonoBehaviour, Animator

public class PlayerAnimator : MonoBehaviour // Reads the movement scripts and sets the Animator parameters (it moves nothing)
{
    [Tooltip("Leave empty: it finds the Animator on the model under the Player.")] // Hover text in the Inspector
    public Animator animator; // The character's Animator
    [Tooltip("How smoothly Speed follows the real speed. Higher = smoother.")] // Hover text in the Inspector
    public float speedDamping = 0.25f; // How smoothly the Speed value follows
    [Tooltip("Above this speed (m/s) at takeoff, the jump counts as a sprint jump. Walk is 5, sprint is 10.")] // Hover text in the Inspector
    public float sprintJumpSpeed = 7f; // Speed that counts as a sprint jump
    [Tooltip("The ground flickers on slopes and steps. Grounded stays true this long (seconds) after leaving it.")] // Hover text in the Inspector
    public float groundedGrace = 0.08f; // Seconds Grounded stays true after leaving the ground

    private PlayerMovement movement; // The player's movement script
    private CharacterController controller; // The player's CharacterController
    private float lastGroundedTime; // When the player was last on the ground
    private bool wasGrounded = true; // Was the player grounded last frame
    private bool sprintJump; // True if the current jump started while sprinting

    void Start() // Runs once when the scene starts
    {
        movement = GetComponent<PlayerMovement>(); // Find the movement script
        controller = GetComponent<CharacterController>(); // Find the CharacterController
        if (animator == null) animator = GetComponentInChildren<Animator>(); // Find the Animator if none was assigned
    }

    void Update() // Runs every frame
    {
        if (animator == null || movement == null || controller == null) return; // Stop if something is missing

        animator.SetFloat("Speed", movement.HorizontalSpeed, speedDamping, Time.deltaTime); // Send the real sideways speed to the Animator

        float up = movement.velocity.y; // The current up/down speed
        if (controller.isGrounded) lastGroundedTime = Time.time; // Remember when we were last on the ground
        bool grounded = controller.isGrounded // On the ground now
            || (Time.time - lastGroundedTime < groundedGrace && up <= 1f); // Or only just left it and not rising fast

        if (wasGrounded && !grounded) // The moment of takeoff
            sprintJump = movement.HorizontalSpeed > sprintJumpSpeed; // Remember if we were sprinting
        wasGrounded = grounded; // Remember for the next frame

        animator.SetBool("Grounded", grounded); // Send the grounded state to the Animator
        animator.SetFloat("VerticalSpeed", up); // Send the up/down speed to the Animator
        animator.SetBool("SprintJump", sprintJump); // Send the sprint jump state to the Animator
    }
}