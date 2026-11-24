using System.Collections; // lets us use IEnumerator
using UnityEngine; // Unity basics

public class LoadingScreen : MonoBehaviour // shows a loading panel until the game runs smooth
{
    public static bool IsLoading = false; // true while loading, other scripts can check it

    public GameObject panel; // the loading screen panel

    [Header("Automatic hide")] // Inspector title
    public float minShowTime = 0.5f; // shortest time the panel stays up
    public float maxShowTime = 10f; // longest time the panel stays up
    public int stableFrames = 30; // smooth frames needed in a row before hiding
    public float maxFrameTime = 0.05f; // a frame slower than this counts as lag

    void Awake() // runs when the object loads
    {
        IsLoading = true; // mark the game as loading
        if (panel != null) panel.SetActive(true); // show the panel right away
    }

    IEnumerator Start() // runs at the start and can wait between frames
    {
        float shown = 0f; // how long the panel has been up
        int smooth = 0; // smooth frames in a row

        while (true) // keep checking every frame
        {
            yield return null; // wait one frame
            shown += Time.unscaledDeltaTime; // add the frame time to the timer

            if (Time.unscaledDeltaTime <= maxFrameTime) smooth++; // fast frame, count it
            else smooth = 0; // lag, start counting again

            if (shown >= maxShowTime) break; // took too long, stop waiting
            if (shown >= minShowTime && smooth >= stableFrames) break; // shown long enough and running smooth, stop waiting
        }

        if (panel != null) panel.SetActive(false); // hide the panel
        IsLoading = false; // loading is finished
    }

    void OnDestroy() // runs when this object is removed
    {
        IsLoading = false; // make sure the game is never left frozen
    }
}