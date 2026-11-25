using UnityEngine; // Unity basics
using UnityEngine.SceneManagement; // lets us load scenes

public class FinalDoor : MonoBehaviour // the door the player walks through at the end of the game
{
    [Header("Settings")] // Inspector title
    public string playerTag = "Player"; // the tag of the player object
    public string mainMenuScene = "Main Menu"; // the exact name of the Main Menu scene
    [Tooltip("The door only works after this part is fixed. Leave empty to always work.")] // hint in the Inspector
    public string requiredPartId = "CPU"; // the last part, the door opens after it is fixed

    [Header("Glow when the door is active")] // Inspector title
    [Tooltip("The parts of the door that turn white. Leave empty to use every renderer on this object and its children.")] // hint in the Inspector
    public Renderer[] doorRenderers; // the door meshes that turn white
    [Tooltip("Optional. A white light that switches on when the door is active.")] // hint in the Inspector
    public Light glowLight; // an optional light for a real glow
    public float glowIntensity = 3f; // how bright the glow is

    private bool used; // true once the door was used
    private bool lit; // true once the door is glowing

    void Start() // runs once at the start
    {
        if (doorRenderers == null || doorRenderers.Length == 0) // no renderers were assigned
            doorRenderers = GetComponentsInChildren<Renderer>(); // use all renderers on the door

        if (glowLight != null) glowLight.enabled = false; // the light starts switched off
    }

    void Update() // runs every frame
    {
        if (lit) return; // already glowing, do nothing
        if (!IsActive()) return; // the door is not unlocked yet

        lit = true; // mark as glowing
        TurnOnGlow(); // make the door white and glowing
    }

    bool IsActive() // true when the door is unlocked
    {
        return string.IsNullOrEmpty(requiredPartId) || PartRepair.IsFixed(requiredPartId); // unlocked if no part is needed or that part is fixed
    }

    void TurnOnGlow() // makes the door white and glowing
    {
        foreach (Renderer r in doorRenderers) // go through each door mesh
        {
            if (r == null) continue; // skip empty slots
            foreach (Material m in r.materials) // go through each material
            {
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white); // make it white (URP shaders)
                if (m.HasProperty("_Color")) m.SetColor("_Color", Color.white); // make it white (standard shaders)
                m.EnableKeyword("_EMISSION"); // turn on emission
                if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", Color.white * glowIntensity); // make it glow white
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; // let the glow light up the surroundings
            }
        }

        if (glowLight != null) // a light was assigned
        {
            glowLight.color = Color.white; // make the light white
            glowLight.enabled = true; // switch the light on
        }
    }

    void OnTriggerEnter(Collider other) // runs when something enters the door trigger
    {
        if (used) return; // already used, do nothing
        if (!other.CompareTag(playerTag)) return; // only the player can use the door

        if (!IsActive()) // the last part is not fixed yet
        {
            Debug.Log("Door locked: " + requiredPartId + " is not fixed yet."); // tells us why nothing happened
            return; // stop here
        }

        used = true; // mark the door as used

        if (RunScores.Instance != null) // if the score keeper exists
        {
            RunScores.Instance.FinishRun(); // save the total as the high score if it is the best
            RunScores.Instance.NewRun(); // clear the maze scores for the next game
        }

        PartRepair.ResetAll(); // forget which parts were fixed, so a new game starts fresh
        Time.timeScale = 1f; // make sure time is running
        SceneManager.LoadScene(mainMenuScene); // go to the Main Menu
    }
}