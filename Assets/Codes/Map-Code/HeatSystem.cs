// WHERE TO PUT THIS FILE: Assets > Codes > Player-Code (replace the old HeatSystem.cs)
// WHERE TO USE IT: drag it onto Player3DModel (the object with the Character Controller)

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
    [SerializeField] float dangerSeconds = 30f;        // time to go from 90% to 100%

    [Header("Overheat Result")]
    [SerializeField] int corruptionOnOverheat = 100;   // corruption given at 100% heat

    [Header("Cooling (step check)")]
    [SerializeField] LayerMask coolantLayer;           // pick "Coolant"
    [SerializeField] float footRadius = 0.4f;          // size of the check at your feet

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

    // other scripts (like CoolantIcons) read this to know when to show warnings
    public bool IsInDanger => systemActive && !overheated && currentHeat >= dangerThreshold;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
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

        if (IsOnCoolant())
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