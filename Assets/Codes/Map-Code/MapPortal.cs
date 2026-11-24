using UnityEngine; // Unity basics
using UnityEngine.InputSystem; // lets us read the keyboard
using UnityEngine.SceneManagement; // lets us load scenes

[RequireComponent(typeof(Grabbable))] // this object must be grabbable
public class MapPortal : MonoBehaviour // sends the player to a map when they press a key while holding this item
{
    [Header("Part list (drag your PartCatalog here)")] // Inspector title
    public PartCatalog catalog; // the list of parts and their scenes

    [Header("Where this item sends you")] // Inspector title
    [Tooltip("Used only if the part is not found in the catalog. The exact scene name, it must be in File > Build Profiles > Scene List.")] // hint in the Inspector
    public string sceneToLoad = "Maze"; // the backup scene to load

    [Header("Interaction")] // Inspector title
    public Key interactKey = Key.E; // the key to press
    [Tooltip("Seconds to wait before the scene loads. Gives time for a loading screen later.")] // hint in the Inspector
    public float loadDelay = 0.5f; // seconds to wait before loading

    public static string ReturnScene; // the scene to come back to

    private Grabbable grabbable; // the Grabbable on this object
    private PartState part; // the part info (optional)
    private bool loading; // true once loading has started

    void Start() // runs once at the start
    {
        grabbable = GetComponent<Grabbable>(); // get the Grabbable
        part = GetComponent<PartState>(); // get the PartState if there is one

        if (catalog != null && part != null) // we have a catalog and a part
        {
            PartEntry entry = catalog.Find(part.partId); // find this part in the catalog
            if (entry != null && !string.IsNullOrEmpty(entry.sceneName)) // the part has a scene set
            {
                sceneToLoad = entry.sceneName; // use the scene from the catalog
                part.difficulty = entry.difficulty; // use the difficulty from the catalog
            }
        }
    }

    void Update() // runs every frame
    {
        if (loading || Keyboard.current == null) return; // stop if already loading or no keyboard

        if (!grabbable.IsHeld) return; // only works while holding this item

        if (part != null && part.IsFixed) return; // a fixed part has nothing left to repair

        if (Keyboard.current[interactKey].wasPressedThisFrame) // the key was just pressed
            StartCoroutine(GoToMap()); // start going to the map
    }

    System.Collections.IEnumerator GoToMap() // loads the map after a short wait
    {
        loading = true; // mark as loading
        ReturnScene = SceneManager.GetActiveScene().name; // remember the scene we came from
        PartRepair.CurrentPart = part != null ? part.partId : null; // remember which part we are fixing
        PartRepair.CurrentDifficulty = part != null ? part.difficulty : 1; // remember how hard its maze is
        Debug.Log("Going to map: " + sceneToLoad); // show the scene name in the Console

        yield return new WaitForSeconds(loadDelay); // wait a moment
        SceneManager.LoadScene(sceneToLoad); // load the map scene
    }
}