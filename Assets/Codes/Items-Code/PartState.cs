using UnityEngine; // Gives access to Unity classes like MonoBehaviour, Color, Renderer
using UnityEngine.SceneManagement; // lets us move the new part into the right scene

public class PartState : MonoBehaviour // Put this on a PC part to show whether it is broken or fixed
{
    [Header("Must be different for every part, for example RAM or CoolerFan")] // Title in the Inspector
    public string partId = "RAM"; // The unique id of this part

    [Header("How hard this part's maze is (1 = easiest, 5 = hardest)")] // Title in the Inspector
    [Range(1, 5)] public int difficulty = 1; // Difficulty slider from 1 to 5

    [Header("Optional: the model that replaces this part once it is fixed (for example RAM-Fixed)")] // Title in the Inspector
    public GameObject fixedPrefab; // The prefab that appears instead of this part after it is fixed

    [Header("Look: the part's color in each state (used when there is no Fixed Prefab)")] // Title in the Inspector
    public Color brokenColor = new Color(1f, 0.35f, 0.35f); // Color used while the part is broken (reddish)
    public Color fixedColor = new Color(0.45f, 1f, 0.55f); // Color used once the part is fixed (greenish)

    [Header("Optional: objects only shown in one state (sparks, smoke, a green light...)")] // Title in the Inspector
    public GameObject[] showWhenBroken; // Objects that are only visible while the part is broken
    public GameObject[] showWhenFixed; // Objects that are only visible once the part is fixed

    private bool swapped; // True once this part was replaced by its fixed model

    public bool IsFixed { get { return PartRepair.IsFixed(partId); } } // True if this part has been fixed

    void Start() // Runs once when the scene starts
    {
        Apply(); // Show the correct look right away
    }

    public void Apply() // Updates the look of this part
    {
        if (swapped) return; // Already replaced, do nothing

        bool fixedNow = IsFixed; // Check once whether the part is fixed

        if (fixedNow && fixedPrefab != null) // The part is fixed and has a fixed model
        {
            SwapToFixed(); // Replace this part with the fixed model
            return; // Stop here
        }

        Color c = fixedNow ? fixedColor : brokenColor; // Pick the color that matches the state

        foreach (Renderer r in GetComponentsInChildren<Renderer>()) // Go through every renderer on this object and its children
        {
            Material m = r.material; // Get this renderer's own material copy, so other objects keep their color
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c); // If the shader uses _BaseColor (URP), set it
            else if (m.HasProperty("_Color")) m.SetColor("_Color", c); // Otherwise if it uses _Color (built-in), set that
        }

        foreach (GameObject g in showWhenBroken) if (g != null) g.SetActive(!fixedNow); // Show these objects only while the part is broken
        foreach (GameObject g in showWhenFixed) if (g != null) g.SetActive(fixedNow); // Show these objects only once the part is fixed
    }

    void SwapToFixed() // Replaces this broken part with its fixed model
    {
        swapped = true; // Make sure this only happens once

        GameObject fixedObj = Instantiate(fixedPrefab, transform.position, transform.rotation); // Create the fixed model in the same spot
        fixedObj.name = fixedPrefab.name; // Remove the (Clone) from its name
        SceneManager.MoveGameObjectToScene(fixedObj, gameObject.scene); // Make sure it belongs to the same scene as this part (the TS room)

        Destroy(gameObject); // Remove the broken part
    }
}