using UnityEngine; // Unity basics

public class FinishDetector : MonoBehaviour // goes on the PLAYER, detects the finish line
{
    [Tooltip("Which layers count as a finish line. Set this to the Finish layer.")] // hint in the Inspector
    public LayerMask finishLayers; // the layers that count as a finish line

    private bool finished = false; // true once the finish was reached

    void Start() // runs once at the start
    {
        Debug.Log("FinishDetector is alive on: " + gameObject.name); // proves this script is on an active object
    }

    void OnTriggerEnter(Collider other) // runs when we touch a trigger
    {
        Debug.Log("Touched trigger: " + other.name + " (layer " + LayerMask.LayerToName(other.gameObject.layer) + ")"); // shows what we touched

        if (finished) return; // already finished, do nothing

        if ((finishLayers.value & (1 << other.gameObject.layer)) == 0) // not a finish layer
        {
            Debug.Log("Not on a finish layer, ignoring. Check the Finish Layers setting."); // tells us why it was ignored
            return; // stop here
        }

        finished = true; // mark as finished
        Debug.Log("Finish line reached!"); // confirms the finish was detected

        if (ScoreSystem.Instance != null) // if there is a ScoreSystem
            ScoreSystem.Instance.Finish(); // tell it we finished
        else // no ScoreSystem
            Debug.LogWarning("No ScoreSystem found in the maze scene."); // tells us it is missing
    }
}