// WHERE TO PUT THIS FILE:
// Put this script in your Assets folder (for example Assets/Scripts).
// Then drag it onto the PLAYER object (the one with the CharacterController / Rigidbody).
// Only add it ONCE, on the player. Do not add it to hazards.

using UnityEngine;

public class HazardDetector : MonoBehaviour
{
    [Tooltip("Which layers count as hazards. Set this to the Hazard layer.")]
    public LayerMask hazardLayers;

    [Header("Defaults (used when the object has no Hazard script)")]
    public float timePenalty = 3f;
    public float corruptionAmount = 10f;
    public float cooldown = 1f;

    private float lastHitTime = -10f;

    void OnTriggerStay(Collider other)
    {
        // Is the thing we touched on a hazard layer?
        if ((hazardLayers.value & (1 << other.gameObject.layer)) == 0) return;

        // Wait for the cooldown so it does not hit every frame
        if (Time.time - lastHitTime < cooldown) return;

        // Optional: a Hazard script on the object can use its own numbers
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

        Debug.Log("Hazard hit! +" + penalty.ToString() + "s");
    }
}