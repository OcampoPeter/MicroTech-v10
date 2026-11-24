using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Grabbable))]
public class MapPortal : MonoBehaviour
{
    [Header("Part list (drag your PartCatalog here)")]
    public PartCatalog catalog;

    [Header("Where this item sends you")]
    [Tooltip("Used only if the part is not found in the catalog. The exact scene name, it must be in File > Build Profiles > Scene List.")]
    public string sceneToLoad = "Maze";

    [Header("Interaction")]
    public Key interactKey = Key.E;
    [Tooltip("Seconds to wait before the scene loads. Gives time for a loading screen later.")]
    public float loadDelay = 0.5f;

    public static string ReturnScene;

    private Grabbable grabbable;
    private PartState part;
    private bool loading;

    void OnEnable()
    {
        loading = false;
    }

    void Start()
    {
        grabbable = GetComponent<Grabbable>();
        part = GetComponent<PartState>();

        if (catalog != null && part != null)
        {
            PartEntry entry = catalog.Find(part.partId);
            if (entry != null && !string.IsNullOrEmpty(entry.sceneName))
            {
                sceneToLoad = entry.sceneName;
                part.difficulty = entry.difficulty;
            }
        }
    }

    void Update()
    {
        if (loading || Keyboard.current == null) return;

        if (!grabbable.IsHeld) return;

        if (part != null && part.IsFixed) return;

        if (Keyboard.current[interactKey].wasPressedThisFrame)
            StartCoroutine(GoToMap());
    }

    System.Collections.IEnumerator GoToMap()
    {
        loading = true;
        ReturnScene = SceneManager.GetActiveScene().name;
        PartRepair.CurrentPart = part != null ? part.partId : null;
        PartRepair.CurrentDifficulty = part != null ? part.difficulty : 1;
        Debug.Log("Going to map: " + sceneToLoad);

        yield return new WaitForSeconds(loadDelay);

        if (MazeLoader.Instance != null)
            MazeLoader.Instance.LoadMaze(sceneToLoad, gameObject.scene);
        else
            SceneManager.LoadScene(sceneToLoad);
    }
}