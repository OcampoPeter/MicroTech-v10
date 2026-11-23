
using UnityEngine;

public class Hazard : MonoBehaviour
{
    [Tooltip("Seconds added to your time when you touch it.")]
    public float timePenalty = 3f;

    [Tooltip("Corruption percent added when you touch it.")]
    public float corruptionAmount = 10f;
}