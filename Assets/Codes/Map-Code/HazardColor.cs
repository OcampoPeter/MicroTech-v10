// WHERE TO PUT THIS FILE: Assets > Codes > Map-Code  (replace the old HazardColor.cs)
// WHERE TO USE IT: on ONE empty object in each scene (name it "HazardColorManager")
// Anything on the Hazard layer gets painted a flat red color.

using System.Collections.Generic;
using UnityEngine;

public class HazardColor : MonoBehaviour
{
    [Header("Find")]
    [SerializeField] LayerMask hazardLayer;                                  // pick "Hazard" (empty = auto)
    [SerializeField] bool includeChildren = true;                            // also color the children of a Hazard object

    [Header("Look")]
    [SerializeField] Color hazardColor = new Color32(237, 89, 89, 255);      // #ED5959
    [SerializeField] bool removeTextures = true;                             // flat color, no texture

    [Header("New objects")]
    [SerializeField] float rescanSeconds = 2f;                               // checks for new Hazard objects (0 = only at start)

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");     // URP
    static readonly int ColorId = Shader.PropertyToID("_Color");             // Built-in
    static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    static readonly int MainTexId = Shader.PropertyToID("_MainTex");

    readonly Dictionary<Material, Material> hazardMaterials = new Dictionary<Material, Material>();
    readonly HashSet<Renderer> done = new HashSet<Renderer>();
    float timer;

    void Start()
    {
        // if the field was left on "Nothing", look for a layer called Hazard
        if (hazardLayer.value == 0)
            hazardLayer = LayerMask.GetMask("Hazard", "Hazzard", "Hazards");

        if (hazardLayer.value == 0)
            Debug.LogWarning("HazardColor: no layer called 'Hazard' exists. Create it in Layers > Add Layer.");

        int count = ApplyColor();
        Debug.Log("HazardColor: colored " + count + " object(s) on the Hazard layer");
        if (count == 0)
            Debug.LogWarning("HazardColor: nothing is on the Hazard layer yet (set the Layer on the object that has the Mesh Renderer).");
    }

    void Update()
    {
        if (rescanSeconds <= 0f) return;

        timer += Time.deltaTime;
        if (timer < rescanSeconds) return;
        timer = 0f;

        ApplyColor();
    }

    [ContextMenu("Apply Color Now")]
    public int ApplyColor()
    {
        int count = 0;
        Renderer[] all = FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Renderer r in all)
        {
            if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer)) continue;
            if (done.Contains(r)) continue;
            if (!IsHazard(r)) continue;

            Material[] mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
                mats[i] = GetHazardMaterial(mats[i]);
            r.sharedMaterials = mats;

            done.Add(r);
            count++;
        }
        return count;
    }

    bool IsHazard(Renderer r)
    {
        Transform t = r.transform;
        while (t != null)
        {
            if ((hazardLayer.value & (1 << t.gameObject.layer)) != 0) return true;
            if (!includeChildren) break;
            t = t.parent;
        }
        return false;
    }

    // one red copy per original material (so the maze doesn't make thousands of materials)
    Material GetHazardMaterial(Material original)
    {
        if (original == null) return null;

        if (!hazardMaterials.TryGetValue(original, out Material copy))
        {
            copy = new Material(original);
            if (copy.HasProperty(BaseColorId)) copy.SetColor(BaseColorId, hazardColor);
            if (copy.HasProperty(ColorId)) copy.SetColor(ColorId, hazardColor);

            if (removeTextures)
            {
                if (copy.HasProperty(BaseMapId)) copy.SetTexture(BaseMapId, null);
                if (copy.HasProperty(MainTexId)) copy.SetTexture(MainTexId, null);
            }
            hazardMaterials[original] = copy;
        }
        return copy;
    }
}