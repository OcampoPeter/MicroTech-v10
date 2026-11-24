using UnityEngine;

public class HazardDetector : MonoBehaviour
{
    [Tooltip("Which layers count as hazards. Empty = the layer called Hazard.")]
    public LayerMask hazardLayers;

    [Header("Defaults (used when the object has no Hazard script)")]
    public float timePenalty = 3f;           // +3 seconds
    public float corruptionAmount = 5f;      // +5% corruption
    public float cooldown = 2f;              // every 2 seconds while touching

    [Header("Debug")]
    public bool debugLogs = true;

    float lastHitTime = -10f;

    void Start()
    {
        // if the field was left on "Nothing", look for a layer called Hazard
        if (hazardLayers.value == 0)
            hazardLayers = LayerMask.GetMask("Hazard", "Hazzard", "Hazards");
    }

    // hazards that are triggers (Is Trigger ticked)
    void OnTriggerStay(Collider other)
    {
        TryHit(other);
    }

    // hazards that are solid (the player bumps into them)
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        TryHit(hit.collider);
    }

    void TryHit(Collider other)
    {
        if (LoadingScreen.IsLoading) return; // no hazard hits while the loading screen is up

        // is the thing we touched on a hazard layer?
        if ((hazardLayers.value & (1 << other.gameObject.layer)) == 0) return;

        // wait for the cooldown so it does not hit every frame
        if (Time.time - lastHitTime < cooldown) return;

        // optional: a Hazard script on the object can use its own numbers
        float penalty = timePenalty;
        float corruption = corruptionAmount;
        Hazard custom = other.GetComponentInParent<Hazard>();
        if (custom != null)
        {
            penalty = custom.timePenalty;
            corruption = custom.corruptionAmount;
        }

        lastHitTime = Time.time;

        if (RunTimer.Instance != null)
            RunTimer.Instance.AddPenalty(penalty);

        if (CorruptionSystem.Instance != null)
            CorruptionSystem.Instance.AddCorruption(corruption);

        if (debugLogs)
            Debug.Log("Hazard hit '" + other.name + "': +" + penalty + "s, +" + corruption + "% corruption");
    }
}