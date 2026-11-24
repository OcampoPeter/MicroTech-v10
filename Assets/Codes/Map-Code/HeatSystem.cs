using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HeatSystem : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] string activeSceneName = "CPU";   // heat only works in this scene

    [Header("Heat Settings")]
    [SerializeField] float maxHeat = 100f;
    [SerializeField] float dangerThreshold = 90f;      // danger zone starts here
    [SerializeField] float heatPerSecond = 2.5f;       // normal buildup (below danger)
    [SerializeField] float coolPerSecond = 15f;        // cooling while standing on coolant
    [SerializeField] float dangerSeconds = 60f;        // time to go from 90% to 100% (1 minute)

    [Header("Overheat Result")]
    [SerializeField] int corruptionOnOverheat = 100;   // corruption given at 100% heat

    [Header("Cooling (step check)")]
    [SerializeField] LayerMask coolantLayer;           // pick "Coolant"
    [SerializeField] float footRadius = 0.4f;          // size of the check at your feet
    [SerializeField] bool debugLogs = true;            // prints cooling ON/OFF in the Console

    [Header("UI")]
    [SerializeField] Slider heatBar;
    [SerializeField] Image fillImage;                  // the Slider's "Fill" object

    [Header("Bar Colors (blue -> orange -> red)")]
    [SerializeField] Color coolColor = new Color(0.25f, 0.55f, 1f);
    [SerializeField] Color warmColor = new Color(1f, 0.6f, 0.1f);
    [SerializeField] Color hotColor  = new Color(0.95f, 0.15f, 0.1f);

    CharacterController cc;
    float currentHeat;
    bool overheated;
    bool systemActive;
    bool wasCooling;

    // other scripts (like CoolantIcons) read this to know when to show warnings
    public Slider HeatBar => heatBar;
    public bool IsInDanger => systemActive && !overheated && currentHeat >= dangerThreshold;

    void Awake()
    {
        cc = GetComponent<CharacterController>();

        // if the Coolant Layer field was left on "Nothing", use the layer named "Coolant"
        if (coolantLayer.value == 0)
            coolantLayer = LayerMask.GetMask("Coolant");
    }

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
        currentHeat = 0f;
        overheated = false;

        if (heatBar != null)
        {
            heatBar.value = 0f;
            heatBar.gameObject.SetActive(systemActive);
        }
        UpdateBarColor();
    }

    void Update()
    {
        if (!systemActive || overheated) return;
        if (LoadingScreen.IsLoading) return; // heat does not build while the loading screen is up
        if (RunTimer.Instance != null && !RunTimer.Instance.HasStarted) return; // heat waits until the player first moves

        bool cooling = IsOnCoolant();
        if (cooling != wasCooling)
        {
            wasCooling = cooling;
            if (debugLogs) Debug.Log("HeatSystem: cooling " + (cooling ? "ON" : "OFF"));
        }

        if (cooling)
        {
            currentHeat -= coolPerSecond * Time.deltaTime;
        }
        else if (currentHeat >= dangerThreshold)
        {
            // danger zone: slowly climbs from 90% to 100% over dangerSeconds
            float dangerRate = (maxHeat - dangerThreshold) / dangerSeconds;
            currentHeat += dangerRate * Time.deltaTime;
        }
        else
        {
            currentHeat += heatPerSecond * Time.deltaTime;
        }

        currentHeat = Mathf.Clamp(currentHeat, 0f, maxHeat);

        if (heatBar != null)
            heatBar.value = currentHeat / maxHeat;
        UpdateBarColor();

        if (currentHeat >= maxHeat)
            Overheat();
    }

    // Looks at the spot under your feet: is there a Coolant-layer object?
    bool IsOnCoolant()
    {
        return Physics.CheckSphere(GetFeetPosition(), footRadius, coolantLayer,
                                   QueryTriggerInteraction.Collide);
    }

    Vector3 GetFeetPosition()
    {
        if (cc == null) cc = GetComponent<CharacterController>();
        if (cc == null) return transform.position;

        Bounds b = cc.bounds;
        return new Vector3(b.center.x, b.min.y, b.center.z);
    }

    void Overheat()
    {
        overheated = true;
        Debug.Log("Heat reached 100%!");

        if (CorruptionSystem.Instance != null)
            CorruptionSystem.Instance.AddCorruption(corruptionOnOverheat);
        else
            Debug.LogWarning("HeatSystem: no CorruptionSystem found in the scene.");
    }

    void UpdateBarColor()
    {
        if (fillImage == null) return;

        // blue -> orange from 0% to 90%, orange -> red from 90% to 100%
        if (currentHeat < dangerThreshold)
        {
            fillImage.color = Color.Lerp(coolColor, warmColor, currentHeat / dangerThreshold);
        }
        else
        {
            float t = (currentHeat - dangerThreshold) / (maxHeat - dangerThreshold);
            fillImage.color = Color.Lerp(warmColor, hotColor, t);
        }
    }

    // Select the player in the Scene view to see the foot check (green ball)
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(GetFeetPosition(), footRadius);
    }
}