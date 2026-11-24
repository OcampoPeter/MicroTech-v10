using UnityEngine; // Unity basics

[DefaultExecutionOrder(100)] // run after CameraModeSwitcher's Start
public class SceneCameraMode : MonoBehaviour // sets the camera view for this scene
{
    public CameraMode mode = CameraMode.ThirdPerson; // the view to use in this scene

    [Tooltip("Untick in the TS room so the player can only walk.")] // hint in the Inspector
    public bool allowJump = true; // true if the player can jump here

    void Start() // runs once at the start
    {
        if (CameraModeSwitcher.Instance != null) // the switcher exists
            CameraModeSwitcher.Instance.SetMode(mode); // set the camera view
        else // no switcher found
            Debug.LogWarning("SceneCameraMode: no CameraModeSwitcher found. Is the Player in this scene?"); // warn about it

        PlayerMovement movement = FindFirstObjectByType<PlayerMovement>(); // find the player movement script
        if (movement != null) movement.canJump = allowJump; // turn jumping on or off
        else Debug.LogWarning("SceneCameraMode: no PlayerMovement found, so Allow Jump was not applied."); // warn about it

        Debug.Log("SceneCameraMode: view = " + mode.ToString() + ", allow jump = " + allowJump.ToString()); // show the result in the Console
    }
}