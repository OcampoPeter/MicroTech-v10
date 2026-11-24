using System.Collections;
using UnityEngine;

public class LoadingScreen : MonoBehaviour
{
    public static bool IsLoading = false; // false by default, so scenes without a loading screen never freeze

    public GameObject panel;           // the loading screen UI panel

    [Header("Automatic hide")]
    public float minShowTime = 0.5f;   // it always stays up at least this long (seconds)
    public float maxShowTime = 10f;    // safety: it never stays up longer than this (seconds)
    public int stableFrames = 30;      // how many smooth frames in a row before it hides
    public float maxFrameTime = 0.05f; // a frame slower than this counts as lag (0.05 = 20 FPS)

    void Awake()
    {
        IsLoading = true;
        if (panel != null) panel.SetActive(true); // show the panel right away
    }

    IEnumerator Start()
    {
        float shown = 0f;   // how long the panel has been up
        int smooth = 0;     // smooth frames in a row

        while (true)
        {
            yield return null; // wait one frame
            shown += Time.unscaledDeltaTime;

            if (Time.unscaledDeltaTime <= maxFrameTime) smooth++; // a smooth frame
            else smooth = 0;                                      // lag, start counting again

            if (shown >= maxShowTime) break;                           // safety, never stuck forever
            if (shown >= minShowTime && smooth >= stableFrames) break; // loaded and running smooth
        }

        if (panel != null) panel.SetActive(false); // hide the panel
        IsLoading = false;                         // let everything run again
    }

    void OnDestroy()
    {
        IsLoading = false; // never leave the game frozen if this object is removed
    }
}