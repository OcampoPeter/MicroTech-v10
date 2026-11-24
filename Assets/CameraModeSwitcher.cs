using UnityEngine; // Unity basics
using UnityEngine.SceneManagement; // lets us read the scene name

public enum CameraMode // the two camera views
{
    FirstPerson, // see through the player's eyes
    ThirdPerson // see the player from behind
}

public class CameraModeSwitcher : MonoBehaviour // switches between the two cameras
{
    public static CameraModeSwitcher Instance; // easy access from other scripts

    [Header("The two cameras (the CinemachineCamera objects)")] // Inspector title
    public GameObject firstPersonCamera; // the first person camera
    public GameObject thirdPersonCamera; // the third person camera

    [Header("Automatic by scene name")] // Inspector title
    public bool decideBySceneName = true; // pick the view from the scene name
    [Tooltip("The exact name of the TS scene. This scene is first person with no jumping. Every other scene is third person.")] // hint in the Inspector
    public string firstPersonSceneName = "TS Room"; // the scene that uses first person

    [Header("Used only when 'Decide By Scene Name' is off")] // Inspector title
    public CameraMode startMode = CameraMode.ThirdPerson; // the view to start with

    [Tooltip("Parts of the character to hide in first person (leave empty to hide every Renderer under the Player).")] // hint in the Inspector
    public Renderer[] modelRenderers; // the body parts to hide in first person

    public CameraMode Mode { get; private set; } // the view we are using now

    void Awake() // runs when the object loads
    {
        Instance = this; // save this script for easy access
        if (modelRenderers == null || modelRenderers.Length == 0) // no body parts picked
            modelRenderers = GetComponentsInChildren<Renderer>(); // use every renderer under the Player
    }

    void Start() // runs once at the start
    {
        if (firstPersonCamera == null) // slot is empty
            firstPersonCamera = GameObject.Find("FirstPersonCamera"); // find the camera by its name

        CameraMode chosen = startMode; // start with the chosen mode
        if (decideBySceneName) // pick the view from the scene name
        {
            string sceneName = SceneManager.GetActiveScene().name; // get the scene name
            bool isTsScene = sceneName == firstPersonSceneName; // true if this is the TS scene
            chosen = isTsScene ? CameraMode.FirstPerson : CameraMode.ThirdPerson; // first person in TS, third person elsewhere

            PlayerMovement movement = GetComponent<PlayerMovement>(); // get the movement script
            if (movement != null) movement.canJump = !isTsScene; // no jumping in the TS scene

            Debug.Log("Scene '" + sceneName + "' -> " + chosen.ToString() + ", jumping " + (!isTsScene ? "on" : "off")); // show the result in the Console
        }

        SetMode(chosen); // apply the chosen view
    }

    public void SetMode(CameraMode mode) // changes the camera view
    {
        Mode = mode; // remember the new view
        bool first = mode == CameraMode.FirstPerson; // true if first person

        if (firstPersonCamera != null) firstPersonCamera.SetActive(first); // turn the first person camera on or off
        if (thirdPersonCamera != null) thirdPersonCamera.SetActive(!first); // turn the third person camera on or off

        PlayerMovement mover = GetComponent<PlayerMovement>(); // get the movement script
        if (mover != null) mover.rotateToMovement = !first; // only turn toward walking direction in third person

        foreach (Renderer r in modelRenderers) // go through each body part
            if (r != null) r.enabled = !first; // hide it in first person, show it in third person

        Debug.Log("Camera mode: " + mode.ToString()); // show the mode in the Console
    }

    public void Toggle() // switches to the other view
    {
        SetMode(Mode == CameraMode.FirstPerson ? CameraMode.ThirdPerson : CameraMode.FirstPerson); // flip between the two views
    }
}