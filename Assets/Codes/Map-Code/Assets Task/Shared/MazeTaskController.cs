using System.Collections.Generic; // Gives access to List, HashSet and Dictionary
using UnityEngine; // Gives access to Unity classes like MonoBehaviour, GameObject, Vector3
using UnityEngine.Events; // Gives access to UnityEvent

public class MazeTaskController : MonoBehaviour // Put this on one object per maze scene, it sets up all the task platforms
{
    [Header("Where to look")] // Title in the Inspector
    [Tooltip("ON: finds platforms anywhere in this scene, just change their Layer.\nOFF: only looks at objects inside this object (its children).")] // Hover text in the Inspector
    public bool scanWholeScene = true; // True = scan the whole scene, false = only this object's children

    [Header("Layer names (must match the layers in Project Settings)")] // Title in the Inspector
    public string bentLayer = "Bent"; // Layer name for bent platforms
    public string missingLayer = "Missing"; // Layer name for dark platforms
    public string friedLayer = "Fried"; // Layer name for power nodes
    public string partLayer = "Part"; // Layer name for parts and part spawn points
    [Tooltip("Extra layers for extra Fried sets, e.g. Fried2, Fried3. Each layer in this list is its own separate set. Create the layers first in Project Settings > Tags and Layers.")] // Hover text in the Inspector
    public string[] extraFriedLayers = new string[0]; // Layer names for extra Fried sets
    [Tooltip("Layer given to marked platforms that were NOT picked.")] // Hover text in the Inspector
    public string normalLayer = "Default"; // Layer that unpicked platforms go back to

    [Header("How many are active  (-1 = every platform marked with that layer)")] // Title in the Inspector
    public int bentCount = -1; // How many bent platforms are active
    public int missingCount = -1; // How many dark platforms are active
    public int friedCount = -1; // How many Fried nodes are active

    [Header("Randomizing")] // Title in the Inspector
    public bool randomize = true; // True = pick and order things randomly
    public bool useSeed = false; // True = use the fixed seed below
    public int seed = 12345; // The fixed seed, so the random result is always the same

    [Header("Bent settings")] // Title in the Inspector
    public int jumpsToFix = 3; // How many landings it takes to fix a bent platform
    public Vector3 bentTilt = new Vector3(0f, 0f, 14f); // How much the platform tilts when there is no bent model
    [Tooltip("Seconds a bent platform ignores new landings after a hit. Raise it if one jump still counts twice; lower it to hit faster.")] // Hover text in the Inspector
    public float hitCooldown = 0.8f; // Seconds a bent platform ignores new landings

    [Header("Bent look (optional)")] // Title in the Inspector
    [Tooltip("Drag the bent models here, MOST bent first (e.g. VeryBent, Bent, ABitBent). Each landing moves to the next one; after the last, the normal platform comes back. Landings needed = number of models. Leave empty to just tilt the platform.")] // Hover text in the Inspector
    public GameObject[] bentStages; // The bent models, most bent first
    [Tooltip("ON: the bent models keep the size they were made at.\nOFF: each model is resized to fit the width and depth of the platform it is on (small platform = small model, big platform = big model).")] // Hover text in the Inspector
    public bool bentModelKeepsOwnSize = true; // True = models keep their own size

    [Header("Bent colors")] // Title in the Inspector
    [Tooltip("Color of the most bent look. With bent models, the first model gets this color.")] // Hover text in the Inspector
    public Color bentColor = new Color(1f, 0.5f, 0.05f); // Orange, the bent color
    [Tooltip("Color of the fixed platform. Bent models get closer to this color with each hit.")] // Hover text in the Inspector
    public Color fixedColor = new Color(0.2f, 1f, 0.35f); // Neon green, the fixed color
    [Tooltip("ON: also color the bent models (VeryBent, Bent, ABitBent).\nOFF: keep the models' own materials.")] // Hover text in the Inspector
    public bool colorBentModels = true; // True = the bent models get colored
    [Tooltip("ON: color the normal platform with the fixed color once it is fixed.\nOFF: it keeps its own material.")] // Hover text in the Inspector
    public bool colorFixedPlatform = true; // True = the fixed platform gets colored
    [Tooltip("How much the Bent colors glow (neon). 0 = no glow. Lower = softer glow. Fried uses 2.")] // Hover text in the Inspector
    public float bentGlow = 1f; // How much the bent colors glow
    [Tooltip("ON: the color slowly moves from Bent Color toward Fixed Color with each hit.\nOFF: it stays Bent Color until fixed, then turns Fixed Color.")] // Hover text in the Inspector
    public bool blendColorWhileBent = true; // True = the color shifts with each hit
    [Tooltip("How many seconds each color change takes. Higher = slower fade.")] // Hover text in the Inspector
    public float bentColorFadeTime = 0.6f; // Seconds each color change takes

    [Header("Fried settings (colors and timing)")] // Title in the Inspector
    public FriedSettings fried = new FriedSettings(); // Colors, models and timing for Fried

    [Header("Missing settings (dark platform colors)")] // Title in the Inspector
    public MissingSettings missingLook = new MissingSettings(); // Colors and timing for dark platforms

    [Header("Part settings (the part the player carries)")] // Title in the Inspector
    public PartSettings partSettings = new PartSettings(); // Settings for the carried part

    [Header("Events")] // Title in the Inspector
    public UnityEvent onMazeCompleted = new UnityEvent(); // Fires once when every task is done

    public bool IsMazeComplete { get; private set; } // True once the whole maze is done

    readonly List<MazeTaskGroup> activeGroups = new List<MazeTaskGroup>(); // All the task groups made by this controller
    System.Random rng; // The random number generator
    bool fired; // True once the completion event was fired

    void Start() // Runs once when the scene starts
    {
        rng = useSeed ? new System.Random(seed) : new System.Random(); // Use the seed if turned on, otherwise a random one
        Setup(); // Build all the tasks
    }

    void Setup() // Scans the scene and builds every task
    {
        int bentL = LayerIndex(bentLayer); // Number of the Bent layer
        int missingL = LayerIndex(missingLayer); // Number of the Missing layer
        int friedL = LayerIndex(friedLayer); // Number of the Fried layer
        int partL = LayerIndex(partLayer); // Number of the Part layer
        int normalL = Mathf.Max(0, LayerIndex(normalLayer)); // Number of the normal layer (never below 0)

        var bent = new List<GameObject>(); // Objects found on the Bent layer
        var missing = new List<GameObject>(); // Objects found on the Missing layer
        var fried = new List<GameObject>(); // Objects found on the Fried layer
        var partObjects = new List<GameObject>(); // Objects found on the Part layer

        var extraIdx = new int[extraFriedLayers.Length]; // Layer numbers of the extra Fried layers
        var extraSets = new List<List<GameObject>>(); // One list of objects per extra Fried layer
        for (int i = 0; i < extraFriedLayers.Length; i++) // Go through each extra Fried layer
        {
            extraIdx[i] = LayerIndex(extraFriedLayers[i]); // Save its layer number
            extraSets.Add(new List<GameObject>()); // Make an empty list for its objects
        }

        foreach (Transform t in GetAllTransforms()) // Go through every object in the search area
        {
            if (t == transform) continue; // Skip this controller object
            int l = t.gameObject.layer; // The layer of this object
            if (l == bentL && bentL >= 0) bent.Add(t.gameObject); // On the Bent layer
            else if (l == missingL && missingL >= 0) missing.Add(t.gameObject); // On the Missing layer
            else if (l == friedL && friedL >= 0) fried.Add(t.gameObject); // On the Fried layer
            else if (l == partL && partL >= 0) partObjects.Add(t.gameObject); // On the Part layer
            else // Not on any main layer
            {
                int k = System.Array.IndexOf(extraIdx, l); // Check if it is on an extra Fried layer
                if (k >= 0) extraSets[k].Add(t.gameObject); // Add it to that extra set
            }
        }

        SetupBent(Pick(bent, bentCount, normalL)); // Build the bent platforms
        SetupMissing(Pick(missing, missingCount, normalL), partObjects); // Build the dark platforms and parts
        SetupFried(fried, extraSets, normalL); // Build the Fried nodes

        if (activeGroups.Count == 0) // If no tasks were made
            Debug.LogWarning($"[{name}] No task platforms found. Set a platform's Layer to " + // Warn the developer
                             $"{bentLayer} / {missingLayer} / {friedLayer}.", this); // Tell which layers to use

        StartCoroutine(SendObjectiveTotals()); // Tell the quest panel how many of each task exist
    }

    void SetupBent(List<GameObject> platforms) // Turns the chosen platforms into bent platforms
    {
        if (platforms.Count == 0) return; // Nothing to set up
        var group = MakeGroup("Bent"); // Make the task group for bent platforms
        foreach (var p in platforms) // Go through each platform
        {
            if (!HasCollider(p)) continue; // Skip platforms without a collider
            p.AddComponent<BentPlatform>().Init(group, jumpsToFix, bentTilt, bentStages, bentModelKeepsOwnSize, hitCooldown, // Add and set up the BentPlatform
                bentColor, fixedColor, colorBentModels, colorFixedPlatform, bentGlow, blendColorWhileBent, bentColorFadeTime); // Pass the color settings
        }
    }

    void SetupMissing(List<GameObject> darkPlatforms, List<GameObject> partLayerObjects) // Builds the dark platforms and the parts
    {
        int needed = darkPlatforms.Count; // One part is needed per dark platform

        var spawnPoints = new List<Transform>(); // Empty objects where a part will spawn
        var readyParts = new List<GameObject>(); // Objects that are already a part
        foreach (var go in partLayerObjects) // Go through each Part layer object
        {
            bool hasLook = go.GetComponentInChildren<Renderer>() != null || go.GetComponent<Collider>() != null; // True if it has a renderer or collider
            if (hasLook) readyParts.Add(go); // It is a ready-made part
            else spawnPoints.Add(go.transform); // It is an empty spawn point
        }

        foreach (Transform t in GetAllTransforms()) // Go through every object again
            if (t.GetComponent<PartPickup>() != null && !readyParts.Contains(t.gameObject)) // If it already has PartPickup and is not in the list
                readyParts.Add(t.gameObject); // Add it as a ready-made part

        var slots = new List<(GameObject part, Transform spawn)>(); // Every possible place for a part
        foreach (var p in readyParts) slots.Add((p, null)); // Each ready-made part is one slot
        foreach (var sp in spawnPoints) slots.Add((null, sp)); // Each spawn point is one slot
        if (randomize) Shuffle(slots); // Mix the order of the slots

        int used = 0; // How many slots are used so far
        foreach (var slot in slots) // Go through each slot
        {
            bool use = used < needed; // Use it only if more parts are still needed

            if (slot.part != null) // A ready-made part
            {
                if (use) // This part is used
                {
                    var pickup = slot.part.GetComponent<PartPickup>(); // Get its PartPickup
                    if (pickup == null) pickup = slot.part.AddComponent<PartPickup>(); // Add one if missing
                    pickup.Init(partSettings); // Set it up
                    slot.part.SetActive(true); // Make sure it is visible
                }
                else // This part is not needed
                {
                    slot.part.SetActive(false); // Switch it off
                }
            }
            else if (use) // A spawn point that is needed
            {
                SpawnPart(slot.spawn); // Create a part there
            }

            if (use) used++; // Count the used slot
        }

        if (needed == 0) return; // No dark platforms, so nothing more to do

        if (used < needed) // Not enough parts for all the dark platforms
            Debug.LogWarning($"[{name}] {needed} dark platforms but only {used} parts / spawn points on the " + // Warn the developer
                             $"{partLayer} layer. The Missing task can't be finished.", this); // Explain the problem

        var group = MakeGroup("MissingParts"); // Make the task group for dark platforms
        foreach (var p in darkPlatforms) // Go through each dark platform
        {
            if (!HasCollider(p)) continue; // Skip platforms without a collider
            p.AddComponent<DarkPlatform>().Init(group, missingLook); // Add and set up the DarkPlatform
        }
    }

    void SpawnPart(Transform point) // Creates a part at a spawn point
    {
        GameObject go; // The part we create
        if (partSettings.partPrefab != null) // If a prefab was given
        {
            go = Instantiate(partSettings.partPrefab, point.position, partSettings.partPrefab.transform.rotation); // Create it, keeping the prefab's own rotation
        }
        else // No prefab given
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Sphere); // Make a simple ball
            go.transform.position = point.position; // Put it at the spawn point
            go.transform.localScale = Vector3.one * 0.6f; // Make it a small size
            TaskVisuals.SetColor(go.GetComponent<Renderer>(), partSettings.fallbackColor, 1.5f); // Color it and make it glow
        }

        go.name = "Part (spawned at " + point.name + ")"; // Give it a clear name

        var pickup = go.GetComponent<PartPickup>(); // Get its PartPickup
        if (pickup == null) pickup = go.AddComponent<PartPickup>(); // Add one if missing
        pickup.Init(partSettings); // Set it up
    }

    void SetupFried(List<GameObject> marked, List<List<GameObject>> extraSets, int normalL) // Sorts Fried platforms into sets and builds them
    {
        bool anyExtra = false; // True if any extra Fried layer has objects
        foreach (var s in extraSets) if (s.Count > 0) anyExtra = true; // Check each extra set
        if (marked.Count == 0 && !anyExtra) return; // Nothing marked on any Fried layer

        var defaultSet = new List<GameObject>(); // Platforms that share the default set
        var groupSets = new Dictionary<FriedGroup, List<GameObject>>(); // Platforms grouped by their FriedGroup
        foreach (var go in marked) // Go through each Fried platform
        {
            var fg = go.GetComponentInParent<FriedGroup>(); // Look for a FriedGroup above it
            if (fg == null) // No FriedGroup
            {
                defaultSet.Add(go); // It goes in the default set
            }
            else // It has a FriedGroup
            {
                if (!groupSets.TryGetValue(fg, out var list)) // If this group has no list yet
                    groupSets[fg] = list = new List<GameObject>(); // Make a new list for it
                list.Add(go); // Add the platform to its group's list
            }
        }

        MazeTaskGroup taskGroup = null; // One task group shared by all Fried sets

        if (defaultSet.Count > 0) // If the default set has platforms
        {
            taskGroup = MakeGroup("Fried"); // Make the Fried task group
            BuildFriedSet(Pick(defaultSet, friedCount, normalL), taskGroup, randomize); // Build the default set
        }

        foreach (var pair in groupSets) // Go through each FriedGroup set
        {
            if (taskGroup == null) taskGroup = MakeGroup("Fried"); // Make the task group if it does not exist yet
            BuildFriedSet(Pick(pair.Value, pair.Key.nodeCount, normalL), taskGroup, pair.Key.randomizeOrder); // Build it with the group's own settings
        }

        foreach (var set in extraSets) // Go through each extra Fried layer set
        {
            if (set.Count == 0) continue; // Skip empty sets
            if (taskGroup == null) taskGroup = MakeGroup("Fried"); // Make the task group if it does not exist yet
            BuildFriedSet(Pick(set, friedCount, normalL), taskGroup, randomize); // Build the extra set
        }
    }

    void BuildFriedSet(List<GameObject> platforms, MazeTaskGroup taskGroup, bool shuffleOrder) // Turns one set of platforms into Fried nodes
    {
        var ordered = new List<GameObject>(platforms); // Copy the list so we can reorder it
        if (shuffleOrder) Shuffle(ordered); // Mix the stepping order if turned on

        var components = new List<PowerNode>(); // The PowerNode scripts we add
        for (int i = 0; i < ordered.Count; i++) // Go through each platform in order
        {
            if (!HasCollider(ordered[i])) continue; // Skip platforms without a collider
            var node = ordered[i].AddComponent<PowerNode>(); // Add the PowerNode script
            node.sequenceIndex = i; // Give it its place in the order
            components.Add(node); // Add it to the list
        }

        var array = components.ToArray(); // Turn the list into an array
        if (array.Length == 0) return; // Nothing to set up
        foreach (var n in array) n.Init(taskGroup, array, fried); // Set up every node
    }

    System.Collections.IEnumerator SendObjectiveTotals() // Sends the task counts to the quest panel
    {
        yield return null; // Wait one frame so every task has registered
        if (ObjectivesUI.Instance == null) yield break; // Stop if there is no quest panel

        int b = 0, m = 0, f = 0; // Totals for Bent, Missing and Fried
        foreach (var g in activeGroups) // Go through each task group
        {
            if (g.taskName == "Bent") b += g.Total; // Add to the Bent total
            else if (g.taskName == "MissingParts") m += g.Total; // Add to the Missing total
            else if (g.taskName == "Fried") f += g.Total; // Add to the Fried total
        }
        Debug.Log($"[{name}] Objectives -> Bent {b}, Missing {m}, Fried {f}"); // Print the totals in the Console
        ObjectivesUI.Instance.SetTotals(b, m, f); // Send the totals to the quest panel
    }

    List<Transform> GetAllTransforms() // Returns every object to check
    {
        var result = new List<Transform>(); // The list we return

        if (scanWholeScene) // Scan the whole scene
        {
            foreach (var root in gameObject.scene.GetRootGameObjects()) // Go through every top-level object
                result.AddRange(root.GetComponentsInChildren<Transform>(true)); // Add it and everything under it, even inactive ones
        }
        else // Only scan this object's children
        {
            result.AddRange(GetComponentsInChildren<Transform>(true)); // Add all children, even inactive ones
        }

        return result; // Give back the list
    }

    MazeTaskGroup MakeGroup(string taskName) // Creates a new task group object
    {
        var go = new GameObject(taskName + "Task"); // Make an empty object for the group
        go.transform.SetParent(transform, false); // Put it under this controller
        var group = go.AddComponent<MazeTaskGroup>(); // Add the MazeTaskGroup script
        group.taskName = taskName; // Give it its name
        group.onAllCompleted.AddListener(CheckAllDone); // Check the whole maze when this group finishes
        activeGroups.Add(group); // Remember the group
        return group; // Give back the group
    }

    void CheckAllDone() // Checks if every task group is finished
    {
        if (fired) return; // Already fired, so do nothing
        foreach (var g in activeGroups) // Go through each task group
            if (g.Total > 0 && !g.IsComplete) return; // Stop if any group is not finished

        fired = true; // Remember we fired the event
        IsMazeComplete = true; // The maze is done
        Debug.Log($"[{name}] All maze tasks complete"); // Print a message in the Console
        onMazeCompleted?.Invoke(); // Fire the completion event
    }

    List<GameObject> Pick(List<GameObject> marked, int count, int normalL) // Chooses which marked platforms stay active
    {
        var chosen = PickSubset(marked, count); // Pick the platforms that stay active
        var set = new HashSet<GameObject>(chosen); // Put them in a set for fast checking
        foreach (var go in marked) // Go through every marked platform
            if (!set.Contains(go)) go.layer = normalL; // Unpicked ones go back to the normal layer
        return chosen; // Give back the picked platforms
    }

    List<GameObject> PickSubset(List<GameObject> source, int count) // Picks a number of platforms from a list
    {
        if (count < 0 || count >= source.Count) return new List<GameObject>(source); // Use all of them if the count is -1 or too big
        var copy = new List<GameObject>(source); // Copy the list
        if (randomize) Shuffle(copy); // Mix the order if randomizing
        return copy.GetRange(0, Mathf.Max(0, count)); // Take the first few
    }

    void Shuffle<T>(IList<T> list) // Mixes the order of a list
    {
        for (int i = list.Count - 1; i > 0; i--) // Go from the end of the list to the start
        {
            int j = rng.Next(i + 1); // Pick a random spot
            (list[i], list[j]) = (list[j], list[i]); // Swap the two items
        }
    }

    int LayerIndex(string layerName) // Turns a layer name into its number
    {
        int i = LayerMask.NameToLayer(layerName); // Look up the layer number
        if (i < 0) // If the layer does not exist
            Debug.LogWarning($"[{name}] Layer '{layerName}' doesn't exist. " + // Warn the developer
                             "Use Tools > Maze Tasks > Create Task Layers.", this); // Tell how to create the layers
        return i; // Give back the number (-1 if missing)
    }

    bool HasCollider(GameObject go) // Checks that a platform has a collider
    {
        if (go.GetComponent<Collider>() != null) return true; // It has one, so all good
        Debug.LogWarning($"{go.name} is marked as a task platform but has no Collider.", go); // Warn the developer
        return false; // It has no collider
    }
}