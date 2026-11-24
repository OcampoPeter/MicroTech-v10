using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RunScores : MonoBehaviour
{
    public static RunScores Instance;

    [Header("Scene name of the Main Menu (exactly as in Build Profiles)")]
    public string mainMenuSceneName = "Main Menu";

    private readonly Dictionary<string, int> mazeScores = new Dictionary<string, int>();
    private readonly List<int> runs = new List<int>();

    public int HighScore { get; private set; }
    public IReadOnlyList<int> Runs { get { return runs; } }

    public int Total
    {
        get
        {
            int sum = 0;
            foreach (KeyValuePair<string, int> pair in mazeScores) sum += pair.Value;
            return sum;
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single && scene.name == mainMenuSceneName)
            NewRun();
    }

    string Key(string partId)
    {
        return string.IsNullOrEmpty(partId) ? "" : partId.Trim().ToUpperInvariant();
    }

    public void NewRun()
    {
        mazeScores.Clear();
    }

    public void SetScore(string partId, int score)
    {
        mazeScores[Key(partId)] = score;
    }

    public bool HasScore(string partId)
    {
        return mazeScores.ContainsKey(Key(partId));
    }

    public int GetScore(string partId)
    {
        int value;
        return mazeScores.TryGetValue(Key(partId), out value) ? value : 0;
    }

    public bool FinishRun()
    {
        int total = Total;
        runs.Add(total);
        if (total > HighScore)
        {
            HighScore = total;
            return true;
        }
        return false;
    }
}