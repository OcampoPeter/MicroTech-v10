using System.Collections.Generic; // Gives access to generic collections like List
using UnityEngine; // Gives access to Unity classes like MonoBehaviour, Camera, Vector3

public enum PartFacing // Which way counts as "the front" of the player
{
    MovementDirection, // The way the player is moving (the part trails behind you)
    CameraForward, // The way the camera looks
    PlayerForward // The way the player object itself points
}

[System.Serializable] // Lets this class show up in the Inspector
public class PartSettings // All the part settings, kept on the MazeTaskController
{
    [Header("What to spawn")] // Title in the Inspector
    [Tooltip("The part to spawn at each spawn point (a prefab). Leave empty to spawn a simple glowing ball.")] // Hover text in the Inspector
    public GameObject partPrefab; // The part to spawn
    [Tooltip("Color of the glowing ball (only used when no prefab is given).")] // Hover text in the Inspector
    public Color fallbackColor = new Color(1f, 0.55f, 0.1f); // Color of the fallback ball

    [Header("Look of the part (works on your prefab too)")] // Title in the Inspector
    [Tooltip("ON: color and glow the part with the two settings below.\nOFF: keep the prefab's own materials.")] // Hover text in the Inspector
    public bool colorPart = true; // True = color and glow the part
    [Tooltip("The color of the part.")] // Hover text in the Inspector
    public Color partColor = new Color(0.25f, 0.6f, 1f); // The part's color
    [Tooltip("How much the part glows (neon). 0 = no glow. Lower = softer.")] // Hover text in the Inspector
    public float partGlow = 1.5f; // How much the part glows

    [Header("Picking up")] // Title in the Inspector
    [Tooltip("The key that picks up the part (the player must be close). Examples: F, E, G.")] // Hover text in the Inspector
    public string interactKey = "F"; // The key that picks up the part
    [Tooltip("How close the player must be, in meters.")] // Hover text in the Inspector
    public float interactRange = 3f; // Distance needed to pick up the part
    [Tooltip("Show a 'Press F' message when the player is close.")] // Hover text in the Inspector
    public bool showPrompt = true; // True = show the pickup message

    [Header("Carrying (floats behind the player)")] // Title in the Inspector
    [Tooltip("Where the part floats, as a fraction of the player's height.\nX = right, Y = up, Z = forward (negative = behind the player).")] // Hover text in the Inspector
    public Vector3 backOffset = new Vector3(0f, 0.7f, -0.3f); // Where the part floats around the player
    [Tooltip("What counts as the FRONT of the player. The part floats on the opposite side.\nMovementDirection = the way you are moving (best if it appears in front).\nCameraForward = the way the camera looks.\nPlayerForward = the way the player object points.")] // Hover text in the Inspector
    public PartFacing facing = PartFacing.MovementDirection; // Which way counts as the player's front
    [Tooltip("How quickly the part swings around when you turn. Higher = faster.")] // Hover text in the Inspector
    public float turnSpeed = 6f; // How fast the part swings around
    [Tooltip("Only used by MovementDirection: the player must move faster than this (meters per second) to change the facing.")] // Hover text in the Inspector
    public float minMoveSpeed = 0.5f; // Minimum speed needed to change the facing
    [Tooltip("How smoothly it follows the player. Higher = slower and floatier. Lower = tighter.")] // Hover text in the Inspector
    public float followSmoothTime = 0.2f; // How smoothly the part follows
    [Tooltip("How far it bobs up and down, in meters.")] // Hover text in the Inspector
    public float bobHeight = 0.15f; // How far the part bobs up and down
    public float bobSpeed = 3f; // How fast the part bobs
    [Tooltip("How fast the part spins while it waits on the ground (degrees per second).")] // Hover text in the Inspector
    public float spinSpeed = 90f; // Spin speed while waiting on the ground
    [Tooltip("How fast the part turns while you carry it (degrees per second). Keep it small for a slow turn. 0 = no turning.")] // Hover text in the Inspector
    public float carrySpinSpeed = 15f; // Spin speed while carried

    [Header("Dying")] // Title in the Inspector
    [Tooltip("ON: if the player suddenly jumps far in one frame (a respawn after dying), the part goes back to where it started.")] // Hover text in the Inspector
    public bool autoDetectRespawn = true; // True = detect a respawn automatically
    [Tooltip("How far (in meters, in one frame) the player must jump to count as a respawn.")] // Hover text in the Inspector
    public float respawnJumpDistance = 10f; // Distance that counts as a respawn
}

public class PartPickup : MonoBehaviour // A part the player picks up and carries to a dark platform
{
    public static PartPickup Carried { get; private set; } // The part the player is carrying right now (null if none)

    public bool IsCarried { get; private set; } // True while the player carries this part
    public bool IsUsed { get; private set; } // True once a platform used this part

    PartSettings s; // Settings from the controller
    Transform player; // The player (found by the "Player" tag)
    Collider playerCol; // The player's collider
    Vector3 homePos; // Where this part started
    Quaternion homeRot; // The rotation this part started with
    Vector3 followVelocity; // Used by the smooth follow
    Vector3 lastPlayerPos; // Used to notice a respawn
    Vector3 facing = Vector3.forward; // The way the player faces (flat on the ground)
    bool nearPlayer; // True when the player is close enough to pick it up
    static GUIStyle promptStyle; // Style of the "Press F" message

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] // Unity calls this method automatically when Play starts
    static void ResetStatics() => Carried = null; // Clear any leftover carried part

    public void Init(PartSettings settings) // Sets this part up (called by MazeTaskController)
    {
        s = settings; // Save the settings
        homePos = transform.position; // Remember the start position
        homeRot = transform.rotation; // Remember the start rotation

        foreach (var c in GetComponentsInChildren<Collider>()) c.isTrigger = true; // Make colliders triggers so they do not block the player

        if (s.colorPart) // If the part should be colored
            foreach (var r in GetComponentsInChildren<Renderer>()) // Go through every renderer
                TaskVisuals.SetColor(r, s.partColor, s.partGlow); // Set the color and glow
    }

    public static void OnPlayerDied() // Call this when the player dies
    {
        if (Carried != null) Carried.ReturnHome(); // The carried part goes back to its start
    }

    void OnDestroy() // Runs when this object is destroyed
    {
        if (Carried == this) Carried = null; // Stop counting this part as carried
    }

    void Update() // Runs every frame
    {
        if (IsUsed || s == null) return; // Stop if the part is used or not set up

        FindPlayer(); // Make sure we have the player
        if (player == null) return; // Stop if there is no player

        if (IsCarried) // If the player is carrying this part
        {
            if (s.autoDetectRespawn && Vector3.Distance(player.position, lastPlayerPos) > s.respawnJumpDistance) // If the player jumped very far in one frame
            {
                ReturnHome(); // Put the part back where it started
                return; // Leave the function
            }
            Vector3 moved = player.position - lastPlayerPos; // How far the player moved this frame
            lastPlayerPos = player.position; // Save the position for the next frame
            UpdateFacing(moved); // Update which way the player faces
            FollowPlayer(); // Float behind the player
            return; // Leave the function
        }

        transform.Rotate(Vector3.up, s.spinSpeed * Time.deltaTime, Space.World); // Spin in place while waiting
        transform.position = homePos + Vector3.up * Mathf.Sin(Time.time * s.bobSpeed) * s.bobHeight; // Bob up and down

        nearPlayer = Carried == null && DistanceToPlayer() <= s.interactRange; // Close enough and not carrying another part
        if (nearPlayer && KeyPressed(s.interactKey)) PickUp(); // Pick it up when the key is pressed
    }

    void FindPlayer() // Finds the player once
    {
        if (player != null) return; // Already found
        GameObject go = GameObject.FindWithTag("Player"); // Look for the object tagged "Player"
        if (go == null) return; // Not found yet
        player = go.transform; // Save the player's transform
        playerCol = go.GetComponent<Collider>(); // Save the player's collider
        lastPlayerPos = player.position; // Start tracking the player's position
    }

    float DistanceToPlayer() // Distance from the part to the closest point of the player
    {
        Vector3 target = playerCol != null // If the player has a collider
            ? playerCol.bounds.ClosestPoint(transform.position) // Use the closest point on it
            : player.position; // Otherwise use the player's position
        return Vector3.Distance(target, transform.position); // Return the distance
    }

    void PickUp() // The player picks up the part
    {
        IsCarried = true; // Mark it as carried
        Carried = this; // This is now the carried part
        nearPlayer = false; // Hide the pickup message
        followVelocity = Vector3.zero; // Reset the follow speed
        lastPlayerPos = player.position; // Start tracking from the current position
        facing = StartFacing(); // Set the starting facing direction
    }

    void FollowPlayer() // Floats smoothly behind the player
    {
        float height = 2f; // Default player height
        Vector3 feet = player.position; // Default feet position
        if (playerCol != null) // If the player has a collider
        {
            Bounds b = playerCol.bounds; // Get its bounds
            height = b.size.y; // Use its real height
            feet = new Vector3(player.position.x, b.min.y, player.position.z); // Use the bottom of the collider as the feet
        }

        Vector3 offset = Quaternion.LookRotation(facing, Vector3.up) * (s.backOffset * height); // The spot behind the player, turned to match the facing
        Vector3 target = feet + offset + Vector3.up * Mathf.Sin(Time.time * s.bobSpeed) * s.bobHeight; // Add a gentle bob to that spot

        transform.position = Vector3.SmoothDamp(transform.position, target, ref followVelocity, Mathf.Max(0.01f, s.followSmoothTime)); // Move smoothly to the spot
        transform.Rotate(Vector3.up, s.carrySpinSpeed * Time.deltaTime, Space.World); // Turn slowly while carried
    }

    Vector3 StartFacing() // Which way the player faces when the part is first picked up
    {
        Vector3 f; // The direction we will return
        if (s.facing == PartFacing.PlayerForward) // If the player's own direction is used
        {
            f = player.forward; // Use the way the player points
        }
        else // Otherwise
        {
            Camera cam = Camera.main; // Get the main camera
            f = cam != null ? cam.transform.forward : player.forward; // Use the camera direction, or the player's if there is no camera
        }
        return Flat(f, Vector3.forward); // Remove the up/down part and return it
    }

    void UpdateFacing(Vector3 moved) // Updates which way the player faces, using the chosen mode
    {
        Vector3 target = facing; // Start with the current facing

        switch (s.facing) // Check which mode is chosen
        {
            case PartFacing.PlayerForward: // Use the player's own direction
                target = Flat(player.forward, facing); // Flat version of the player's forward
                break; // Done with this mode

            case PartFacing.CameraForward: // Use the camera direction
                Camera cam = Camera.main; // Get the main camera
                if (cam != null) target = Flat(cam.transform.forward, facing); // Flat version of the camera's forward
                break; // Done with this mode

            default: // MovementDirection
                Vector3 flatMove = new Vector3(moved.x, 0f, moved.z); // The movement without up/down
                if (Time.deltaTime > 0f && flatMove.magnitude / Time.deltaTime > s.minMoveSpeed) // Only if the player moves fast enough
                    target = flatMove.normalized; // Face the way the player moves
                break; // Done with this mode
        }

        float t = 1f - Mathf.Exp(-s.turnSpeed * Time.deltaTime); // How much to turn this frame
        facing = Vector3.Slerp(facing, target, t); // Turn smoothly so it does not snap
        if (facing.sqrMagnitude < 0.0001f) facing = target; // Avoid a zero direction
    }

    static Vector3 Flat(Vector3 v, Vector3 fallback) // Removes the up/down part of a direction
    {
        v.y = 0f; // Remove the height
        return v.sqrMagnitude > 0.0001f ? v.normalized : fallback; // Return the flat direction, or the fallback if it is empty
    }

    void ReturnHome() // Puts the part back where it started
    {
        IsCarried = false; // No longer carried
        if (Carried == this) Carried = null; // Stop counting it as the carried part
        followVelocity = Vector3.zero; // Reset the follow speed
        transform.position = homePos; // Go back to the start position
        transform.rotation = homeRot; // Go back to the start rotation
    }

    public void Consume() // Called by a DarkPlatform when it takes this part
    {
        IsUsed = true; // Mark it as used
        IsCarried = false; // No longer carried
        if (Carried == this) Carried = null; // Stop counting it as the carried part
        gameObject.SetActive(false); // Hide the part
    }

    static bool KeyPressed(string keyName) // Checks if a key was pressed this frame
    {
#if ENABLE_INPUT_SYSTEM // If the new Input System is used
        var keyboard = UnityEngine.InputSystem.Keyboard.current; // Get the current keyboard
        if (keyboard != null && System.Enum.TryParse<UnityEngine.InputSystem.Key>(keyName, true, out var key)) // Turn the key name into a Key
            return keyboard[key].wasPressedThisFrame; // True if that key was pressed this frame
        return false; // Key not found, so nothing was pressed
#else // If the old Input Manager is used
        return System.Enum.TryParse<KeyCode>(keyName, true, out var code) && Input.GetKeyDown(code); // Turn the key name into a KeyCode and check it
#endif // End of the input system check
    }

    void OnGUI() // Draws the "Press F" message on screen
    {
        if (!nearPlayer || IsCarried || IsUsed || s == null || !s.showPrompt) return; // Only draw when the player is close and the prompt is on

        Camera cam = Camera.main; // Get the main camera
        if (cam == null) return; // Stop if there is no camera

        Vector3 screen = cam.WorldToScreenPoint(transform.position + Vector3.up * 0.8f); // Find the screen spot just above the part
        if (screen.z < 0f) return; // Stop if the spot is behind the camera

        if (promptStyle == null) // Make the style only once
        {
            promptStyle = new GUIStyle(GUI.skin.label) // Copy the default label style
            {
                alignment = TextAnchor.MiddleCenter, // Center the text
                fontSize = 22, // Make the text big
                fontStyle = FontStyle.Bold // Make the text bold
            };
            promptStyle.normal.textColor = Color.white; // Make the text white
        }

        GUI.Label(new Rect(screen.x - 100f, Screen.height - screen.y - 20f, 200f, 40f), // Draw the text in a box on the screen
                  "Press " + s.interactKey.ToUpper(), promptStyle); // The message, like "Press F"
    }
}