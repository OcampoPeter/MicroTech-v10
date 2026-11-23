using UnityEngine;

public class FinishDetector : MonoBehaviour
{
    [Tooltip("Which layers count as a finish line. Set this to the Finish layer.")]
    public LayerMask finishLayers;

    private bool finished = false;

    void OnTriggerEnter(Collider other)
    {
        if (finished) return;

        // Is the thing we touched on a finish layer?
        if ((finishLayers.value & (1 << other.gameObject.layer)) == 0) return;

        finished = true;

        if (ScoreSystem.Instance != null)
            ScoreSystem.Instance.Finish();
    }
}