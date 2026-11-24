using System.Collections.Generic; // Gives access to HashSet
using UnityEngine; // Gives access to Unity attributes like RuntimeInitializeOnLoadMethod

public static class PartRepair // A static class that remembers which parts are fixed, readable from any script
{
    public static string CurrentPart; // The id of the part being fixed right now (set by MapPortal)

    public static int CurrentDifficulty = 1; // How hard the maze being entered is (1 = easiest, set by MapPortal)

    private static readonly HashSet<string> fixedParts = new HashSet<string>(); // The ids of every part that is already fixed

    public static bool IsFixed(string id) { return fixedParts.Contains(id); } // Returns true if this part id is in the fixed list

    public static void MarkFixed(string id) // Adds a part to the fixed list
    {
        if (!string.IsNullOrEmpty(id)) fixedParts.Add(id); // Only add it if the id is not null or empty
    }

    public static void MarkCurrentFixed() { MarkFixed(CurrentPart); } // Marks the part being fixed right now as fixed

    public static void ResetAll() // Clears all repair progress
    {
        fixedParts.Clear(); // Forget every fixed part
        CurrentPart = null; // No part is being fixed
        CurrentDifficulty = 1; // Go back to the easiest difficulty
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] // Unity calls this method automatically when Play starts
    static void ClearOnPlay() { ResetAll(); } // Starts every Play session clean, even when Unity skips the domain reload
}