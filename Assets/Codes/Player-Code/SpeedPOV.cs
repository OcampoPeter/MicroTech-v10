using UnityEngine; // Gives access to Unity classes like MonoBehaviour, Mathf
using Unity.Cinemachine; // Gives access to CinemachineCamera

public class SpeedFOV : MonoBehaviour // Widens the camera's field of view the faster you go
{
    [Header("References (leave empty to find them by itself)")] // Title in the Inspector
    public PlayerMovement player; // The player's movement script
    [Tooltip("The CinemachineCamera objects to widen. Empty = it finds every CinemachineCamera in the scene.")] // Hover text in the Inspector
    public CinemachineCamera[] cameras; // The cameras to widen

    [Header("The effect")] // Title in the Inspector
    [Tooltip("Below this speed (m/s) the view stays normal.")] // Hover text in the Inspector
    public float startSpeed = 7f; // Speed where the widening starts
    [Tooltip("At this speed (m/s) the view is at its widest.")] // Hover text in the Inspector
    public float fullSpeed = 18f; // Speed where the view is widest
    [Tooltip("How many degrees wider the view gets at full speed.")] // Hover text in the Inspector
    public float extraFOV = 18f; // Extra degrees at full speed
    [Tooltip("How fast it widens (higher = snappier).")] // Hover text in the Inspector
    public float widenSpeed = 6f; // How fast the view widens
    [Tooltip("How fast it goes back to normal.")] // Hover text in the Inspector
    public float narrowSpeed = 4f; // How fast the view narrows

    private float[] baseFOV; // The normal field of view of each camera
    private float extra; // The extra degrees right now

    void Start() // Runs once when the scene starts
    {
        if (player == null) player = GetComponent<PlayerMovement>(); // Find the movement script if none was assigned
        if (cameras == null || cameras.Length == 0) // If no cameras were assigned
            cameras = FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None); // Find every CinemachineCamera, even inactive ones

        baseFOV = new float[cameras.Length]; // Make room for each normal field of view
        for (int i = 0; i < cameras.Length; i++) // Go through each camera
            if (cameras[i] != null) baseFOV[i] = cameras[i].Lens.FieldOfView; // Remember its normal field of view
    }

    void LateUpdate() // Runs every frame after all Update calls
    {
        if (player == null || cameras == null) return; // Stop if something is missing

        float t = Mathf.InverseLerp(startSpeed, fullSpeed, player.HorizontalSpeed); // How fast we are, from 0 to 1
        float target = t * extraFOV; // The extra degrees we want

        float rate = target > extra ? widenSpeed : narrowSpeed; // Widen quickly, narrow a bit slower
        extra = Mathf.Lerp(extra, target, rate * Time.deltaTime); // Move toward the target smoothly

        for (int i = 0; i < cameras.Length; i++) // Go through each camera
        {
            if (cameras[i] == null) continue; // Skip destroyed cameras
            cameras[i].Lens.FieldOfView = baseFOV[i] + extra; // Apply the normal view plus the extra
        }
    }

    void OnDisable() // Runs when this script is turned off
    {
        if (cameras == null || baseFOV == null) return; // Stop if something is missing
        for (int i = 0; i < cameras.Length && i < baseFOV.Length; i++) // Go through each camera
            if (cameras[i] != null) cameras[i].Lens.FieldOfView = baseFOV[i]; // Put the normal view back
    }
}