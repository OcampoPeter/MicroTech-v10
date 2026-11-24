using UnityEngine; // Gives access to Unity classes like MonoBehaviour, Camera, Rigidbody
using UnityEngine.InputSystem; // Gives access to the new Input System (Keyboard, Mouse, Key)

public static class InspectMode // A static class, so any script can read it without a reference
{
    public static bool Active; // True while the player is inspecting an item
}

public class ItemGrabber : MonoBehaviour // Script attached to the player that handles picking up, inspecting and throwing
{
    [Header("References")] // Shows a "References" title in the Inspector
    public Camera cam; // The camera used to aim and to position the held item

    [Header("Pick up")] // Shows a "Pick up" title in the Inspector
    public Key pickupKey = Key.F; // The key that picks up or drops an item
    public float pickupRange = 8f; // Max distance from the player to pick up an item
    [Tooltip("How far in front of the camera the item floats while you carry it.")] // Hover text in the Inspector
    public float holdDistance = 3f; // Distance in front of the camera where the carried item floats
    [Tooltip("How quickly the item flies to its spot. Higher = snappier.")] // Hover text in the Inspector
    public float followSpeed = 12f; // How fast the item moves toward its hold spot

    [Header("Inspect")] // Shows an "Inspect" title in the Inspector
    public Key inspectKey = Key.Q; // The key that starts or stops inspecting
    [Tooltip("How close the item comes to the camera while inspecting.")] // Hover text in the Inspector
    public float inspectDistance = 1.5f; // Starting distance of the item from the camera when inspecting
    public float rotateSpeed = 0.3f; // How fast the item rotates when you drag the mouse
    [Tooltip("Mouse wheel: how much one scroll notch moves the item in or out.")] // Hover text in the Inspector
    public float zoomStep = 0.25f; // How much one scroll step changes the distance
    [Tooltip("Closest the item can come to the camera while inspecting.")] // Hover text in the Inspector
    public float minInspectDistance = 0.6f; // Smallest allowed distance while inspecting
    [Tooltip("Furthest the item can go from the camera while inspecting.")] // Hover text in the Inspector
    public float maxInspectDistance = 3f; // Largest allowed distance while inspecting

    [Header("Throw")] // Shows a "Throw" title in the Inspector
    [Tooltip("Dropping an item while you move or turn fast throws it. 1 = it keeps your speed, 2 = twice as hard, 0 = it just falls.")] // Hover text in the Inspector
    public float throwMultiplier = 1.5f; // Multiplies the item's speed when it is thrown
    [Tooltip("The fastest an item can be thrown.")] // Hover text in the Inspector
    public float maxThrowSpeed = 18f; // Upper limit for the throw speed
    [Tooltip("Dropping slower than this just lets the item fall.")] // Hover text in the Inspector
    public float minThrowSpeed = 1.5f; // Below this speed the item is dropped instead of thrown

    public static bool IsRotatingItem; // True while the player is rotating the item with the mouse

    public bool IsHolding { get { return held != null; } } // True when the player is carrying an item

    private Grabbable held; // The item currently being carried (null if none)
    private Rigidbody heldBody; // The Rigidbody of the carried item
    private bool heldWasKinematic; // Remembers if the item was kinematic before we picked it up
    private PlayerMovement movement; // Reference to the player's movement script
    private PlayerWallRun wallRun; // Reference to the player's wall run script
    private float currentInspectDistance; // Current distance while inspecting (changes with the mouse wheel)
    private Vector3 lastHeldPosition; // Item position from the last frame, used to measure speed
    private Vector3 heldVelocity; // Smoothed speed of the held item, used when throwing

    void Start() // Runs once when the scene starts
    {
        if (cam == null) cam = Camera.main; // If no camera was assigned, use the main camera
        movement = GetComponent<PlayerMovement>(); // Find the movement script on this object
        wallRun = GetComponent<PlayerWallRun>(); // Find the wall run script on this object
        InspectMode.Active = false; // Make sure we start the scene not inspecting
    }

    void OnDisable() // Runs when this script is turned off
    {
        if (InspectMode.Active) ExitInspect(); // Leave inspect mode so the player is not stuck frozen
    }

    void Update() // Runs every frame
    {
        if (Keyboard.current == null || Mouse.current == null) return; // Stop if there is no keyboard or mouse

        if (Keyboard.current[pickupKey].wasPressedThisFrame) // If the pickup key was pressed this frame
        {
            if (held == null) TryPickUp(); // Not holding anything, so try to pick something up
            else Drop(); // Already holding something, so drop it
        }

        if (held != null && Keyboard.current[inspectKey].wasPressedThisFrame) // If holding an item and the inspect key was pressed
        {
            if (InspectMode.Active) ExitInspect(); // Already inspecting, so stop
            else EnterInspect(); // Not inspecting, so start
        }

        IsRotatingItem = held != null && InspectMode.Active && Mouse.current.leftButton.isPressed; // Rotating only if holding, inspecting and the left mouse button is down

        if (held != null) // Only run the holding logic if we carry something
        {
            if (InspectMode.Active) // Zooming only works while inspecting
            {
                float scroll = Mouse.current.scroll.ReadValue().y; // Read the vertical mouse wheel movement
                if (Mathf.Abs(scroll) > 0.01f) // Only react if the wheel actually moved
                {
                    currentInspectDistance -= Mathf.Sign(scroll) * zoomStep; // Scroll up moves the item closer, scroll down moves it away
                    currentInspectDistance = Mathf.Clamp(currentInspectDistance, minInspectDistance, maxInspectDistance); // Keep the distance between the min and max
                }
            }

            float distance = InspectMode.Active ? currentInspectDistance : holdDistance; // Pick the inspect distance or the normal hold distance
            Vector3 target = cam.transform.position + cam.transform.forward * distance; // The spot in front of the camera where the item should float
            Vector3 newPosition = Vector3.Lerp(held.transform.position, target, followSpeed * Time.deltaTime); // Move smoothly part of the way toward that spot
            held.transform.position = newPosition; // Apply the new position to the item

            if (Time.deltaTime > 0f) // Avoid dividing by zero when the game is paused
            {
                Vector3 speedNow = (newPosition - lastHeldPosition) / Time.deltaTime; // Distance moved this frame divided by time gives the speed
                heldVelocity = Vector3.Lerp(heldVelocity, speedNow, 0.25f); // Blend with the old speed to smooth out sudden jumps
            }
            lastHeldPosition = newPosition; // Save the position for the next frame's measurement

            if (IsRotatingItem) // If the player is rotating the item
            {
                Vector2 delta = Mouse.current.delta.ReadValue(); // How much the mouse moved this frame
                held.transform.Rotate(cam.transform.up, -delta.x * rotateSpeed, Space.World); // Mouse left/right spins the item around the camera's up direction
                held.transform.Rotate(cam.transform.right, delta.y * rotateSpeed, Space.World); // Mouse up/down tilts the item around the camera's right direction
            }
        }
    }

    void EnterInspect() // Starts inspect mode
    {
        InspectMode.Active = true; // Tell every script that we are inspecting
        currentInspectDistance = inspectDistance; // Every inspect starts at the normal inspect distance

        if (movement != null) // If the player has a movement script
        {
            movement.ResetMotion(); // Stop any movement the player currently has
            movement.enabled = false; // Turn movement off so the player is frozen
        }
        if (wallRun != null) wallRun.enabled = false; // Turn wall running off too

        SetCameraInput(false); // Stop the camera from following the mouse
        Debug.Log("Inspecting: " + held.displayName); // Print the item name in the Console
    }

    void ExitInspect() // Stops inspect mode
    {
        InspectMode.Active = false; // Tell every script that we are no longer inspecting
        IsRotatingItem = false; // Make sure rotating is switched off

        if (movement != null) movement.enabled = true; // Give movement back to the player
        if (wallRun != null) wallRun.enabled = true; // Give wall running back to the player

        SetCameraInput(true); // Let the camera follow the mouse again
    }

    void SetCameraInput(bool on) // Turns the mouse look of the cameras on or off
    {
        var controllers = FindObjectsByType<Unity.Cinemachine.CinemachineInputAxisController>(FindObjectsInactive.Include); // Find every Cinemachine input controller, even inactive ones
        foreach (var c in controllers) c.enabled = on; // Turn each one on or off
    }

    void TryPickUp() // Tries to pick up the item the player is looking at
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward); // A ray from the camera going straight forward
        RaycastHit[] hits = Physics.RaycastAll(ray, pickupRange + 10f); // Get everything the ray hits, with extra length because the camera is behind the player
        System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance)); // Sort the hits from nearest to furthest

        Grabbable item = null; // Will hold the item we find
        foreach (RaycastHit hit in hits) // Go through each hit, nearest first
        {
            if (hit.collider.transform.IsChildOf(transform)) continue; // Skip anything that belongs to the player

            item = hit.collider.GetComponentInParent<Grabbable>(); // Look for a Grabbable on the hit object or its parents
            if (item == null) break; // Something solid that is not an item is in the way, so stop looking

            if (Vector3.Distance(transform.position, item.transform.position) > pickupRange) item = null; // Too far from the player, so ignore it
            break; // We checked the first valid hit, so stop
        }

        if (item == null) // Nothing could be picked up
        {
            Debug.Log("ItemGrabber: nothing to pick up. Look straight at the item and get closer."); // Tell the developer why nothing happened
            return; // Leave the function
        }

        held = item; // Remember the item we are now carrying
        held.IsHeld = true; // Tell the item that it is being held
        lastHeldPosition = held.transform.position; // Start measuring speed from the item's current position
        heldVelocity = Vector3.zero; // Reset the speed to zero
        heldBody = item.GetComponent<Rigidbody>(); // Get the item's Rigidbody if it has one
        if (heldBody != null) // If the item has a Rigidbody
        {
            heldWasKinematic = heldBody.isKinematic; // Remember the original kinematic setting
            heldBody.isKinematic = true; // Turn off physics so it does not fall or get pushed while held
        }
        Debug.Log("Picked up: " + item.displayName); // Print the item name in the Console
    }

    void Drop() // Drops or throws the held item
    {
        if (InspectMode.Active) ExitInspect(); // Dropping also ends inspecting

        if (heldBody != null) // If the item has a Rigidbody
        {
            heldBody.isKinematic = heldWasKinematic; // Restore the original kinematic setting

            if (!heldBody.isKinematic) // Only apply a throw if physics is active on the item
            {
                Vector3 throwVelocity = heldVelocity * throwMultiplier; // The item keeps the speed it had in your hands, scaled by the multiplier
                if (throwVelocity.magnitude >= minThrowSpeed) // Only throw if it is fast enough
                {
                    throwVelocity = Vector3.ClampMagnitude(throwVelocity, maxThrowSpeed); // Cap the speed at the maximum throw speed
                    heldBody.linearVelocity = throwVelocity; // Launch the item with that speed
                    heldBody.angularVelocity = Random.insideUnitSphere * Mathf.Min(throwVelocity.magnitude, 10f) * 0.4f; // Add a small random spin so it tumbles
                }
                else // Too slow to count as a throw
                {
                    heldBody.linearVelocity = Vector3.zero; // Let the item simply fall
                }
            }
        }
        heldVelocity = Vector3.zero; // Reset the stored speed
        held.IsHeld = false; // Tell the item that it is no longer held
        Debug.Log("Dropped: " + held.displayName); // Print the item name in the Console
        held = null; // We are not carrying anything anymore
        heldBody = null; // Forget the Rigidbody
        IsRotatingItem = false; // Make sure rotating is switched off
    }
}