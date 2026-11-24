using System; // lets us use [Serializable]
using System.Collections.Generic; // lets us use List
using UnityEngine; // Unity basics
#if UNITY_EDITOR // only compiled inside the Unity editor
using UnityEditor; // lets us use SceneAsset (drag a scene in)
#endif // end of the editor-only part

[Serializable] // lets this show up in the Inspector
public class PartEntry // one row: a PC part and the scene it sends you to
{
    public string partId; // the name of the part, for example RAM
    public GameObject prefab; // the part object, drag it from the Project window
    public int difficulty = 1; // how hard this part's maze is
#if UNITY_EDITOR // only compiled inside the Unity editor
    public SceneAsset scene; // drag the maze scene here
#endif // end of the editor-only part
    [HideInInspector] public string sceneName; // the scene name, saved automatically from the dragged scene
}

[CreateAssetMenu(fileName = "PartCatalog", menuName = "Map/Part Catalog")] // adds Create > Map > Part Catalog
public class PartCatalog : ScriptableObject // the list of all parts and their scenes
{
    public List<PartEntry> parts = new List<PartEntry>(); // all the part rows

    void OnValidate() // runs when you change something in the Inspector
    {
#if UNITY_EDITOR // only compiled inside the Unity editor
        foreach (PartEntry p in parts) // go through each row
        {
            if (p.scene != null) p.sceneName = p.scene.name; // save the dragged scene's name
            if (string.IsNullOrEmpty(p.partId) && p.prefab != null) p.partId = p.prefab.name; // use the prefab name if no id was typed
        }
#endif // end of the editor-only part
    }

    public PartEntry Find(string partId) // finds the row for a part
    {
        foreach (PartEntry p in parts) // go through each row
            if (p.partId == partId) return p; // return the matching row
        return null; // nothing found
    }

    public void ApplyTo(GameObject instance, PartEntry entry) // sets up a spawned part with its scene and difficulty
    {
        if (instance == null || entry == null) return; // nothing to set up

        MapPortal portal = instance.GetComponent<MapPortal>(); // get the portal on the part
        if (portal != null) portal.sceneToLoad = entry.sceneName; // tell it which scene to load

        PartState state = instance.GetComponent<PartState>(); // get the part info on the part
        if (state != null) // the part has info
        {
            state.partId = entry.partId; // set the part name
            state.difficulty = entry.difficulty; // set the maze difficulty
        }
    }
}