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

    void OnEnable() // runs when the scene loads AND every time this object is switched back on (for example when we return from a maze)
    {
        IsLoading = true; // mark the game as loading
        if (panel != null) panel.SetActive(true); // show the panel right away
        StartCoroutine(WaitUntilSmooth()); // start waiting for smooth frames
    }

    IEnumerator WaitUntilSmooth() // waits until the game runs smooth, then hides the panel
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

    void OnDisable() // runs when this object is switched off (for example when we go into a maze)
    {
        IsLoading = false; // make sure the game is never left frozen
    }

    void OnDestroy() // runs when this object is removed
    {
        IsLoading = false; // make sure the game is never left frozen
    }
}