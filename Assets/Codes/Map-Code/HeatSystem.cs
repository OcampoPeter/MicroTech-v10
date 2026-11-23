using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HeatSystem : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] string activeSceneName = "CPU";   // heat only works in this scene

    [Header("Heat Settings")]
    [SerializeField] float maxHeat = 100f;
    [SerializeField] float deathThreshold = 90f;   // die at 90%
    [SerializeField] float heatPerSecond = 2f;     // slow buildup
    [SerializeField] float coolPerSecond = 15f;    // faster cooldown on coolant

    [Header("Cooling")]
    [SerializeField] LayerMask coolantLayer;       // pick "Coolant" in the Inspector

    [Header("UI")]
    [SerializeField] Slider heatBar;
    [SerializeField] Image fillImage;              // drag the Slider's "Fill" object here

    [Header("Bar Colors (blue -> orange -> red)")]
    [SerializeField] Color coolColor = new Color(0.25f, 0.55f, 1f);   // blue
    [SerializeField] Color warmColor = new Color(1f, 0.6f, 0.1f);     // orange
    [SerializeField] Color hotColor  = new Color(0.95f, 0.15f, 0.1f); // red

    float currentHeat;
    int coolantCount;      // how many coolant zones we're touching
    bool isDead;
    bool systemActive;

    bool IsCooling => coolantCount > 0;

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplyScene(SceneManager.GetActiveScene().name);
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyScene(scene.name);
    }

    void ApplyScene(string sceneName)
    {
        systemActive = sceneName == activeSceneName;

        // fresh start every time we enter (or leave) the CPU scene
        currentHeat = 0f;
        coolantCount = 0;
        isDead = false;

        if (heatBar != null)
        {
            heatBar.value = 0f;
            heatBar.gameObject.SetActive(systemActive);
        }
        UpdateBarColor();
    }

    void Update()
    {
        if (!systemActive || isDead) return;

        if (IsCooling)
            currentHeat -= coolPerSecond * Time.deltaTime;
        else
            currentHeat += heatPerSecond * Time.deltaTime;

        currentHeat = Mathf.Clamp(currentHeat, 0f, maxHeat);

        if (heatBar != null)
            heatBar.value = currentHeat / maxHeat;
        UpdateBarColor();

        if (currentHeat >= deathThreshold)
            Die();
    }

    void UpdateBarColor()
    {
        if (fillImage == null) return;

        // 0 = no heat, 1 = death threshold (so it's fully red right at 90%)
        float t = Mathf.Clamp01(currentHeat / deathThreshold);

        fillImage.color = t < 0.5f
            ? Color.Lerp(coolColor, warmColor, t / 0.5f)
            : Color.Lerp(warmColor, hotColor, (t - 0.5f) / 0.5f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (systemActive && IsInCoolantLayer(other.gameObject))
            coolantCount++;
    }

    void OnTriggerExit(Collider other)
    {
        if (systemActive && IsInCoolantLayer(other.gameObject))
            coolantCount = Mathf.Max(0, coolantCount - 1);
    }

    bool IsInCoolantLayer(GameObject obj)
    {
        return (coolantLayer.value & (1 << obj.layer)) != 0;
    }

    void Die()
    {
        isDead = true;
        Debug.Log("Player overheated!");
        // trigger death screen / respawn here
    }
}