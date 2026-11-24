#if UNITY_EDITOR // Only compile this script inside the Unity Editor
using UnityEditor; // Gives access to editor classes like MenuItem and AssetDatabase
using UnityEngine; // Gives access to Unity classes like GameObject and Debug

public static class MazeTaskMenu // Adds a "Maze Tasks" menu to the Unity top bar
{
    static readonly string[] LayerNames = { "Bent", "Fried", "Missing", "Part" }; // The layers this tool creates

    [MenuItem("Tools/Maze Tasks/Create Task Layers")] // Adds this method to the Tools menu
    static void CreateLayers() // Adds the task layers to the project
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset"); // Load the file that stores the layers
        if (assets == null || assets.Length == 0) // If the file could not be opened
        {
            Debug.LogError("Couldn't open ProjectSettings/TagManager.asset"); // Show an error in the Console
            return; // Leave the function
        }

        var tagManager = new SerializedObject(assets[0]); // Wrap the file so we can edit it
        SerializedProperty layers = tagManager.FindProperty("layers"); // Get the list of layers
        string added = "", skipped = ""; // Texts for the final message

        foreach (string layerName in LayerNames) // Go through each layer we want
        {
            bool exists = false; // True if the layer already exists
            for (int i = 0; i < layers.arraySize; i++) // Go through every layer slot
                if (layers.GetArrayElementAtIndex(i).stringValue == layerName) { exists = true; break; } // Stop if we find it

            if (exists) { skipped += layerName + " "; continue; } // Skip layers that already exist

            bool placed = false; // True once the layer was put in a free slot
            for (int i = 8; i < layers.arraySize; i++) // Start at slot 8, because 0-7 are built-in
            {
                var slot = layers.GetArrayElementAtIndex(i); // Get this slot
                if (string.IsNullOrEmpty(slot.stringValue)) // If the slot is empty
                {
                    slot.stringValue = layerName; // Put the layer name in it
                    added += layerName + " "; // Remember that we added it
                    placed = true; // It is placed
                    break; // Stop looking for a slot
                }
            }
            if (!placed) Debug.LogError($"No free layer slot for '{layerName}'. Free one up in Project Settings > Tags and Layers."); // Show an error if there was no free slot
        }

        tagManager.ApplyModifiedProperties(); // Save the changes to the layers
        AssetDatabase.SaveAssets(); // Write the changes to disk
        Debug.Log($"Maze Tasks layers - added: [{added.Trim()}]  already existed: [{skipped.Trim()}]"); // Print what was added and what already existed
    }

    [MenuItem("Tools/Maze Tasks/Add Controller To Selected Maze")] // Adds this method to the Tools menu
    static void AddController() // Adds the MazeTaskController to the selected object
    {
        GameObject maze = Selection.activeGameObject; // The object selected in the Hierarchy
        if (maze == null) return; // Stop if nothing is selected

        if (maze.GetComponent<MazeTaskController>() != null) // If it already has a controller
        {
            Debug.Log(maze.name + " already has a MazeTaskController."); // Tell the developer
            return; // Leave the function
        }

        Undo.AddComponent<MazeTaskController>(maze); // Add the controller, so Undo can remove it
        Debug.Log("Added MazeTaskController to " + maze.name + // Print a message in the Console
                  ". Now set platform Layers to Bent / Fried / Missing / Part."); // Tell what to do next
    }

    [MenuItem("Tools/Maze Tasks/Add Controller To Selected Maze", true)] // Makes this method the check for the menu item above
    static bool AddControllerValidate() => Selection.activeGameObject != null; // The menu item only works when an object is selected
}
#endif // End of the editor-only part