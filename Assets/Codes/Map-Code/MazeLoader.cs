using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MazeLoader : MonoBehaviour
{
    public static MazeLoader Instance;

    private Scene tsScene;
    private string mazeName;
    private readonly List<GameObject> switchedOff = new List<GameObject>();
    private bool busy;

    public bool InMaze { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) return;
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void LoadMaze(string sceneName, Scene fromScene)
    {
        if (busy || InMaze) return;
        tsScene = fromScene;
        mazeName = sceneName;
        StartCoroutine(LoadRoutine());
    }

    public void RestartMaze()
    {
        if (busy || !InMaze) return;
        StartCoroutine(RestartRoutine());
    }

    public void ReturnToTS()
    {
        if (busy || !InMaze) return;
        StartCoroutine(ReturnRoutine());
    }

    IEnumerator LoadRoutine()
    {
        busy = true;
        SwitchOffTS();
        AsyncOperation load = SceneManager.LoadSceneAsync(mazeName, LoadSceneMode.Additive);
        while (!load.isDone) yield return null;
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(mazeName));
        InMaze = true;
        busy = false;
    }

    IEnumerator RestartRoutine()
    {
        busy = true;
        Time.timeScale = 1f;
        SceneManager.SetActiveScene(tsScene);
        AsyncOperation unload = SceneManager.UnloadSceneAsync(mazeName);
        while (unload != null && !unload.isDone) yield return null;
        AsyncOperation load = SceneManager.LoadSceneAsync(mazeName, LoadSceneMode.Additive);
        while (!load.isDone) yield return null;
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(mazeName));
        busy = false;
    }

    IEnumerator ReturnRoutine()
    {
        busy = true;
        Time.timeScale = 1f;
        SceneManager.SetActiveScene(tsScene);
        AsyncOperation unload = SceneManager.UnloadSceneAsync(mazeName);
        while (unload != null && !unload.isDone) yield return null;
        SwitchOnTS();
        InMaze = false;
        busy = false;
    }

    void SwitchOffTS()
    {
        switchedOff.Clear();
        GameObject[] roots = tsScene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].activeSelf)
            {
                roots[i].SetActive(false);
                switchedOff.Add(roots[i]);
            }
        }
    }

    void SwitchOnTS()
    {
        for (int i = 0; i < switchedOff.Count; i++)
        {
            if (switchedOff[i] != null) switchedOff[i].SetActive(true);
        }
        switchedOff.Clear();
    }
}