using UnityEngine; // Gives access to Unity classes like MonoBehaviour, Animator, Quaternion

public class HeadOffset : MonoBehaviour // Lowers the neck, fixes the head tilt and turns the head toward the camera
{
    [Header("References (leave empty, found by itself)")] // Title in the Inspector
    public Animator animator; // The character's Animator
    public Transform lookCamera; // The camera the head looks toward

    [Header("Neck height")] // Title in the Inspector
    [Tooltip("Moves the neck (and head) DOWN, in meters. Start at 0.05. Too much will squash the shoulders.")] // Hover text in the Inspector
    public float neckLower = 0.05f; // How far the neck moves down

    [Header("Tilt fix")] // Title in the Inspector
    [Tooltip("Degrees added to the head. Use the number that fixed the tilt.")] // Hover text in the Inspector
    public Vector3 headEuler = new Vector3(10f, 0f, 0f); // Extra rotation added to the head

    [Header("Head look")] // Title in the Inspector
    public bool headLook = true; // True = the head follows the camera
    [Tooltip("Most the head can turn left or right (degrees).")] // Hover text in the Inspector
    public float maxYaw = 80f; // Most the head can turn sideways
    [Tooltip("Most the head can nod up or down (degrees).")] // Hover text in the Inspector
    public float maxPitch = 30f; // Most the head can nod
    [Tooltip("How much of the camera's up/down look the head copies. 0 = never nods.")] // Hover text in the Inspector
    [Range(0f, 1f)] public float pitchAmount = 0.5f; // How much of the up/down look the head copies
    [Tooltip("How much of the turn goes to the neck (the rest goes to the head).")] // Hover text in the Inspector
    [Range(0f, 1f)] public float neckShare = 0.4f; // How much of the turn the neck does
    [Tooltip("How fast the head follows. Higher = snappier, lower = smoother.")] // Hover text in the Inspector
    public float followSpeed = 8f; // How fast the head follows

    private Transform head, neck; // The head and neck bones
    private float yaw, pitch; // The current head turn and nod

    void Start() // Runs once when the scene starts
    {
        if (animator == null) animator = GetComponentInChildren<Animator>(); // Find the Animator if none was assigned
        if (animator == null || !animator.isHuman) return; // Stop if there is no humanoid Animator
        head = animator.GetBoneTransform(HumanBodyBones.Head); // Find the head bone
        neck = animator.GetBoneTransform(HumanBodyBones.Neck); // Find the neck bone

        if (lookCamera == null) // If no camera was assigned
        {
            PlayerMovement m = GetComponent<PlayerMovement>(); // Find the movement script
            if (m != null) lookCamera = m.playerCamera; // Use its camera
            if (lookCamera == null && Camera.main != null) lookCamera = Camera.main.transform; // Otherwise use the main camera
        }
    }

    void LateUpdate() // Runs every frame after the Animator
    {
        if (head == null) return; // Stop if there is no head bone

        if (neck != null && neckLower != 0f) // If the neck should be lowered
            neck.position += Vector3.down * neckLower; // Move the neck down (the head comes with it)

        head.localRotation *= Quaternion.Euler(headEuler); // Apply the tilt fix

        if (!headLook || lookCamera == null) return; // Stop if head look is off or there is no camera

        Vector3 body = animator.transform.forward; body.y = 0f; body.Normalize(); // The way the body faces
        Vector3 look = lookCamera.forward; // The way the camera looks
        Vector3 lookFlat = new Vector3(look.x, 0f, look.z); // The camera direction without up and down
        if (lookFlat.sqrMagnitude < 0.0001f) return; // Stop if the camera looks straight up or down
        lookFlat.Normalize(); // Make it length 1

        float targetYaw = Vector3.SignedAngle(body, lookFlat, Vector3.up); // How far to turn the head sideways
        float targetPitch = -Mathf.Asin(Mathf.Clamp(look.y, -1f, 1f)) * Mathf.Rad2Deg * pitchAmount; // How far to nod the head
        targetYaw = Mathf.Clamp(targetYaw, -maxYaw, maxYaw); // Keep the turn within the limit
        targetPitch = Mathf.Clamp(targetPitch, -maxPitch, maxPitch); // Keep the nod within the limit

        float k = 1f - Mathf.Exp(-followSpeed * Time.deltaTime); // How much to move toward the target this frame
        yaw = Mathf.Lerp(yaw, targetYaw, k); // Move the turn smoothly
        pitch = Mathf.Lerp(pitch, targetPitch, k); // Move the nod smoothly

        Vector3 right = Vector3.Cross(Vector3.up, body); // The body's right direction
        Quaternion turn = Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.AngleAxis(pitch, right); // The full turn and nod

        Quaternion neckTurn = Quaternion.Slerp(Quaternion.identity, turn, neckShare); // The part of the turn for the neck
        Quaternion headTurn = Quaternion.Slerp(Quaternion.identity, turn, 1f - neckShare); // The part of the turn for the head
        if (neck != null) neck.rotation = neckTurn * neck.rotation; // Turn the neck
        head.rotation = headTurn * head.rotation; // Turn the head
    }
}