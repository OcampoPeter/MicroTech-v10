using System.Collections; // lets us use coroutines
using System.Collections.Generic; // lets us use List
using TMPro; // lets us use TextMeshPro
using UnityEngine; // Unity basics
using UnityEngine.SceneManagement; // lets us load and unload scenes
using UnityEngine.UI; // lets us use Image

public class MazeLoader : MonoBehaviour // loads mazes on top of the TS room and brings you back
{
    public static MazeLoader Instance; // easy access from other scripts

    [Tooltip("Seconds to wait after finishing a maze before going back to the TS room.")] // hint in the Inspector
    public float returnDelay = 0.5f; // seconds to wait before going back

    [Header("Loading screen")] // Inspector title
    [Tooltip("ON: this script shows its own black LOADING screen. Turn it OFF if you want to use only your LoadingScreen panels.")] // hint in the Inspector
    public bool useBuiltInLoadingScreen = true; // use the built-in loading screen
    public string loadingText = "LOADING..."; // the text shown on the loading screen
    public Color backgroundColor = Color.black; // the color of the loading screen
    public float fontSize = 48f; // the size of the loading text
    [Tooltip("The loading screen stays at least this long (seconds), so it does not flash.")] // hint in the Inspector
    public float minLoadingTime = 1f; // the shortest time the loading screen stays
    public float fadeTime = 0.25f; // seconds to fade the loading screen out
    [Tooltip("The loading screen only leaves after this many smooth frames in a row.")] // hint in the Inspector
    public int stableFrames = 30; // smooth frames needed in a row before hiding
    [Tooltip("A frame slower than this many seconds counts as lag. 0.05 = below 20 FPS.")] // hint in the Inspector
    public float maxFrameTime = 0.05f; // a frame slower than this counts as lag
    [Tooltip("Never wait longer than this (seconds) for the game to run smooth.")] // hint in the Inspector
    public float maxWaitForSmooth = 8f; // the longest we wait for smooth frames

    private Scene roomScene; // the TS room scene
    private string currentMaze; // the name of the maze that is loaded right now
    private readonly List<GameObject> hidden = new List<GameObject>(); // the TS room objects we switched off
    private bool busy; // true while loading or unloading
    private GameObject loadingRoot; // the loading screen object
    private CanvasGroup loadingGroup; // used to fade the loading screen

    public bool IsInMaze { get { return !string.IsNullOrEmpty(currentMaze); } } // true while a maze is loaded

    public bool InMaze { get { return IsInMaze; } } // same as IsInMaze, the name some scripts use
    public void LoadMaze(string mazeScene) { EnterMaze(mazeScene); } // same as EnterMaze, the name some scripts use
    public void LoadMaze(string mazeScene, object extra) { EnterMaze(mazeScene); } // same, for scripts that also send a second value (it is ignored)
    public void ReturnToTS() { ReturnToRoom(); } // same as ReturnToRoom, the name some scripts use

    void Awake() // runs when the object is created
    {
        if (Instance != null && Instance != this) // a MazeLoader already exists
        {
            Destroy(gameObject); // remove this extra copy
            return; // stop here
        }
        Instance = this; // save this script for easy access
        DontDestroyOnLoad(gameObject); // keep this object when the scene changes
        if (useBuiltInLoadingScreen) BuildLoadingScreen(); // create the loading screen
    }

    void BuildLoadingScreen() // creates the loading screen from code
    {
        loadingRoot = new GameObject("LoadingScreen"); // the main object
        loadingRoot.transform.SetParent(transform, false); // put it under this object

        Canvas canvas = loadingRoot.AddComponent<Canvas>(); // a canvas to draw on
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; // cover the whole screen
        canvas.sortingOrder = 999; // draw on top of everything

        loadingGroup = loadingRoot.AddComponent<CanvasGroup>(); // used for fading
        loadingGroup.alpha = 0f; // start invisible

        GameObject bg = new GameObject("Background"); // the colored background
        bg.transform.SetParent(loadingRoot.transform, false); // put it under the canvas
        Image img = bg.AddComponent<Image>(); // an image to fill the screen
        img.color = backgroundColor; // set its color
        Stretch(img.rectTransform); // make it fill the screen

        GameObject textObj = new GameObject("Text"); // the loading text
        textObj.transform.SetParent(loadingRoot.transform, false); // put it under the canvas
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>(); // a TextMeshPro text
        tmp.text = loadingText; // set the words
        tmp.alignment = TextAlignmentOptions.Center; // center them
        tmp.fontSize = fontSize; // set the size
        tmp.color = Color.white; // white text
        Stretch(tmp.rectTransform); // make it fill the screen

        loadingRoot.SetActive(false); // hide the loading screen for now
    }

    void Stretch(RectTransform rt) // makes a UI object fill the whole screen
    {
        rt.anchorMin = Vector2.zero; // bottom-left corner
        rt.anchorMax = Vector2.one; // top-right corner
        rt.offsetMin = Vector2.zero; // no margin
        rt.offsetMax = Vector2.zero; // no margin
    }

    IEnumerator ShowLoading() // shows the loading screen
    {
        if (loadingRoot == null) yield break; // no built-in loading screen, do nothing
        loadingRoot.SetActive(true); // switch it on
        loadingGroup.alpha = 1f; // show it fully at once, no fade, so it is up before any lag starts
        yield return null; // wait one frame
        yield return new WaitForEndOfFrame(); // wait until the screen was really drawn
    }

    IEnumerator HideLoading() // hides the loading screen
    {
        if (loadingRoot == null) yield break; // no built-in loading screen, do nothing
        yield return WaitUntilSmooth(); // keep the screen up until the game runs smooth
        yield return Fade(0f); // fade it out
        loadingRoot.SetActive(false); // switch it off
    }

    IEnumerator WaitUntilSmooth() // waits until the game runs smooth
    {
        int smooth = 0; // smooth frames in a row
        float waited = 0f; // how long we have waited
        while (waited < maxWaitForSmooth) // stop waiting after the maximum time
        {
            yield return null; // wait one frame
            float dt = Time.unscaledDeltaTime; // how long that frame took
            waited += dt; // add it to the waiting time
            if (dt <= maxFrameTime) smooth++; // fast frame, count it
            else smooth = 0; // lag, start counting again
            if (smooth >= stableFrames) break; // enough smooth frames in a row, stop waiting
        }
    }

    IEnumerator Fade(float target) // fades the loading screen to a target (0 = invisible, 1 = visible)
    {
        float start = loadingGroup.alpha; // where we start
        float t = 0f; // time passed
        while (t < fadeTime) // until the fade is done
        {
            t += Time.unscaledDeltaTime; // count the time, even if the game is paused
            loadingGroup.alpha = Mathf.Lerp(start, target, t / fadeTime); // move the alpha
            yield return null; // wait one frame
        }
        loadingGroup.alpha = target; // finish exactly at the target
    }

    public void EnterMaze(string mazeScene) // call this to go into a maze
    {
        if (busy) return; // ignore if already loading
        StartCoroutine(EnterRoutine(mazeScene)); // start loading the maze
    }

    IEnumerator EnterRoutine(string mazeScene) // loads the maze and hides the TS room
    {
        busy = true; // mark as busy
        float startTime = Time.unscaledTime; // remember when loading started
        yield return ShowLoading(); // show the loading screen first

        roomScene = SceneManager.GetActiveScene(); // remember the TS room scene
        currentMaze = mazeScene; // remember which maze we load

        AsyncOperation op = SceneManager.LoadSceneAsync(mazeScene, LoadSceneMode.Additive); // load the maze on top of the TS room
        while (!op.isDone) yield return null; // wait until it has loaded

        SceneManager.SetActiveScene(SceneManager.GetSceneByName(mazeScene)); // make the maze the active scene

        hidden.Clear(); // forget the old hidden list
        foreach (GameObject go in roomScene.GetRootGameObjects()) // go through everything in the TS room
        {
            if (go.activeSelf) // only the ones that are switched on
            {
                hidden.Add(go); // remember it
                go.SetActive(false); // switch it off
            }
        }

        float left = minLoadingTime - (Time.unscaledTime - startTime); // how long the loading screen still has to stay
        if (left > 0f) yield return new WaitForSecondsRealtime(left); // wait if it was too short

        yield return HideLoading(); // hide the loading screen
        busy = false; // done loading
    }

    public void ReturnToRoom() // call this to go back to the TS room
    {
        if (busy || !IsInMaze) return; // ignore if busy or not in a maze
        StartCoroutine(ReturnRoutine()); // start going back
    }

    IEnumerator ReturnRoutine() // unloads the maze and shows the TS room again
    {
        busy = true; // mark as busy
        float startTime = Time.unscaledTime; // remember when loading started
        yield return ShowLoading(); // show the loading screen first
        yield return new WaitForSecondsRealtime(returnDelay); // wait a moment

        if (GameManager.Instance != null && GameManager.Instance.State == GameState.Complete) // the maze was finished
            PartRepair.MarkCurrentFixed(); // mark the part as fixed

        foreach (GameObject go in hidden) // go through the hidden TS room objects
            if (go != null) go.SetActive(true); // switch each one back on
        hidden.Clear(); // clear the list

        foreach (PartState p in FindObjectsByType<PartState>(FindObjectsInactive.Include)) p.Apply(); // refresh each part's look, so fixed parts turn green

        SceneManager.SetActiveScene(roomScene); // make the TS room the active scene again
        Time.timeScale = 1f; // make sure time is running

        AsyncOperation op = SceneManager.UnloadSceneAsync(currentMaze); // remove the maze
        while (op != null && !op.isDone) yield return null; // wait until it is gone

        currentMaze = null; // no maze is loaded now

        float left = minLoadingTime - (Time.unscaledTime - startTime); // how long the loading screen still has to stay
        if (left > 0f) yield return new WaitForSecondsRealtime(left); // wait if it was too short

        yield return HideLoading(); // hide the loading screen
        busy = false; // done
    }

    public void RestartMaze() // call this to restart the maze (R key)
    {
        if (busy || !IsInMaze) return; // ignore if busy or not in a maze
        StartCoroutine(RestartRoutine()); // start restarting
    }

    IEnumerator RestartRoutine() // reloads only the maze
    {
        busy = true; // mark as busy
        Time.timeScale = 1f; // make sure time is running
        string mazeName = currentMaze; // remember the maze name

        AsyncOperation unload = SceneManager.UnloadSceneAsync(mazeName); // remove the maze
        while (unload != null && !unload.isDone) yield return null; // wait until it is gone

        AsyncOperation load = SceneManager.LoadSceneAsync(mazeName, LoadSceneMode.Additive); // load the maze again
        while (!load.isDone) yield return null; // wait until it has loaded

        SceneManager.SetActiveScene(SceneManager.GetSceneByName(mazeName)); // make the maze the active scene
        busy = false; // done
    }
}
