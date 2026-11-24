using UnityEngine; // Gives access to Unity classes like MonoBehaviour
using UnityEngine.InputSystem; // Gives access to the new Input System (InputAction)

[DefaultExecutionOrder(-100)] // Runs before other scripts so they get fresh input
public class PlayerInputHandler : MonoBehaviour // Reads the player's input and shares it with other scripts
{
    [Header("Input Asset Link")] // Title in the Inspector
    public InputActionAsset inputActions; // The Input Actions file with Move, Sprint and Jump

    public Vector2 MoveInput { get; private set; } // The movement direction from the keys or stick
    public bool IsSprinting { get; private set; } // True while sprint is held
    public bool JumpTriggered { get; private set; } // True on the frame jump was pressed

    private InputAction moveAction; // The Move action
    private InputAction sprintAction; // The Sprint action
    private InputAction jumpAction; // The Jump action

    void Awake() // Runs when the object is created
    {
        if (inputActions != null) // If an Input Actions file was assigned
        {
            moveAction = inputActions.FindAction("Move"); // Find the Move action
            sprintAction = inputActions.FindAction("Sprint"); // Find the Sprint action
            jumpAction = inputActions.FindAction("Jump"); // Find the Jump action
        }
    }

    void OnEnable() => inputActions?.Enable(); // Turn the input on when this script turns on
    void OnDisable() => inputActions?.Disable(); // Turn the input off when this script turns off

    void Update() // Runs every frame
    {
        MoveInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero; // Read the movement direction
        IsSprinting = sprintAction != null && sprintAction.IsPressed(); // Check if sprint is held
        JumpTriggered = jumpAction != null && jumpAction.WasPressedThisFrame(); // Check if jump was pressed this frame
    }
}
