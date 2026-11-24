using UnityEngine; // Gives access to Unity classes like Renderer, Color, Bounds

public static class TaskVisuals // Small helpers shared by the Bent, Missing and Fried tasks
{
    static readonly int BaseColor = Shader.PropertyToID("_BaseColor"); // The color name used by URP/HDRP shaders
    static readonly int Color_ = Shader.PropertyToID("_Color"); // The color name used by built-in shaders
    static readonly int Emission = Shader.PropertyToID("_EmissionColor"); // The glow color name

    public static void SetColor(Renderer r, Color color, float emission = 0f) // Colors a renderer, and makes it glow if emission is above 0
    {
        if (r == null) return; // Stop if there is no renderer
        var block = new MaterialPropertyBlock(); // A small color override, so no new material copy is made
        r.GetPropertyBlock(block); // Load the renderer's current override
        block.SetColor(BaseColor, color); // Set the color for URP/HDRP shaders
        block.SetColor(Color_, color); // Set the color for built-in shaders
        if (emission > 0f) // If the color should glow
        {
            block.SetColor(Emission, color * emission); // Set the glow color
            r.sharedMaterial.EnableKeyword("_EMISSION"); // Turn the glow feature on
        }
        else // No glow wanted
        {
            block.SetColor(Emission, Color.black); // Turn the glow off
        }
        r.SetPropertyBlock(block); // Apply the override to the renderer
    }

    public static Bounds GetBounds(Renderer[] renderers, Collider[] colliders) // Gets the size and position of a platform in the world
    {
        bool has = false; // True once we have a first bounds
        Bounds b = new Bounds(Vector3.zero, Vector3.one); // The bounds we return

        foreach (var r in renderers) // Go through each renderer
        {
            if (!has) { b = r.bounds; has = true; } // The first one starts the bounds
            else b.Encapsulate(r.bounds); // The others make the bounds bigger
        }

        if (!has) // No renderers, so use the colliders instead
        {
            foreach (var c in colliders) // Go through each collider
            {
                if (!has) { b = c.bounds; has = true; } // The first one starts the bounds
                else b.Encapsulate(c.bounds); // The others make the bounds bigger
            }
        }
        return b; // Give back the bounds
    }

    public static float FitFactor(GameObject modelPrefab, Bounds platformBounds) // Works out how much to scale a model so it fits a platform
    {
        GameObject temp = Object.Instantiate(modelPrefab, Vector3.zero, Quaternion.identity); // Make a temporary copy to measure
        Renderer[] renderers = temp.GetComponentsInChildren<Renderer>(); // Get the copy's renderers

        float factor = 1f; // 1 = the model is already the right size
        if (renderers.Length > 0) // If the model has any renderer
        {
            Bounds b = renderers[0].bounds; // Start with the first renderer's bounds
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds); // Grow the bounds to include the others

            float fx = b.size.x > 0.0001f ? platformBounds.size.x / b.size.x : 1f; // Scale needed to match the width
            float fz = b.size.z > 0.0001f ? platformBounds.size.z / b.size.z : 1f; // Scale needed to match the depth
            factor = Mathf.Min(fx, fz); // Use the smaller one, so the model never sticks out
        }

        temp.SetActive(false); // Switch the copy off right away
        Object.Destroy(temp); // Let Unity remove the copy at the end of the frame
        return factor; // Give back the scale
    }
}