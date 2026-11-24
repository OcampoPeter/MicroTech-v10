using UnityEngine; // Unity basics

[RequireComponent(typeof(Collider))] // this object must have a collider
public class FinishLine : MonoBehaviour // goes on the FINISH LINE object, finishes the run when the player touches it
{
    private bool finished = false; // true once the finish was reached

    void Reset() // runs when the script is first added
    {
        GetComponent<Collider>().isTrigger = true; // make the collider a trigger so the player walks through it
    }

    void OnTriggerEnter(Collider other) // runs when something touches the finish line
    {
        if (finished) return; // already finished, do nothing
        if (!other.CompareTag("Player")) return; // only the player counts

        finished = true; // mark as finished
        Debug.Log("Finish line reached!"); // confirms the finish was detected

        if (ScoreSystem.Instance != null) // if there is a ScoreSystem
            ScoreSystem.Instance.Finish(); // tell it we finished
        else // no ScoreSystem
            Debug.LogWarning("No ScoreSystem found in the maze scene."); // tells us it is missing
    }
}
