using UnityEngine;

// Put this on ONE object in the scene (an empty GameObject is fine).
// When the scene starts, every object on the Hazard layer is tinted red.
public class HazardColor : MonoBehaviour
{
    [SerializeField] LayerMask hazardLayer;                                    // pick "Hazard" in the Inspector
    [SerializeField] Color hazardColor = new Color32(237, 89, 89, 255);        // #ED5959

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");       // URP / HDRP
    static readonly int ColorId = Shader.PropertyToID("_Color");               // Built-in pipeline

    void Start()
    {
        ApplyColor();
    }

    [ContextMenu("Apply Color Now")]
    public void ApplyColor()
    {
        var block = new MaterialPropertyBlock();
        Renderer[] all = FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Renderer r in all)
        {
            if ((hazardLayer.value & (1 << r.gameObject.layer)) == 0) continue;

            r.GetPropertyBlock(block);
            block.SetColor(BaseColorId, hazardColor);
            block.SetColor(ColorId, hazardColor);
            r.SetPropertyBlock(block);
        }
    }
}