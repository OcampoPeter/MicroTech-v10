using UnityEngine; // Gives access to Unity classes like MonoBehaviour

public class Grabbable : MonoBehaviour // Put this on any item the player can pick up
{
    public string displayName = "Item"; // The name shown for this item
    [TextArea] public string description = "A part of the PC."; // A longer text about the item, with a big text box in the Inspector

    public bool IsHeld { get; set; } // True while the player is holding this item (ItemGrabber changes it)
}