using UnityEngine; // Unity basics

public class SpawnPlatform : MonoBehaviour // a platform that moves the spawn point when you step on it
{
    [Header("Spawn point to move (empty = finds an object named SpawnPoint)")] // Inspector title
    public Transform spawnPoint; // the spawn point object to move
    [Tooltip("Where the player appears, measured from the middle of the platform's top.\nRaise Y if the player appears inside the platform.")] // hint in the Inspector
    public Vector3 spawnOffset = new Vector3(0f, 1f, 0f); // how far above the platform the player appears
    [Tooltip("ON: the spawn point also turns the same way as this platform.")] // hint in the Inspector
    public bool copyRotation = false; // copy the platform's rotation to the spawn point

    [Header("Look")] // Inspector title
    public Color offColor = new Color(0.2f, 0.2f, 0.2f); // dark gray when not used
    public Color onColor = Color.white; // white when lit
    [Tooltip("Glow when NOT used. 0 = no glow.")] // hint in the Inspector
    public float offGlow = 0f; // glow when not used
    [Tooltip("Glow when lit. Lower = softer. (Fried uses 2, Bent uses 1.)")] // hint in the Inspector
    public float onGlow = 0.6f; // glow when lit

    [Header("Stepping")] // Inspector title
    [Tooltip("How high above the platform the player's feet can be and still count as standing on it.")] // hint in the Inspector
    public float heightTolerance = 0.4f; // how high the feet can be and still count

    public static SpawnPlatform Current { get; private set; } // the platform that is lit right now

    public bool IsActive { get; private set; } // true if this platform is lit

    Collider platformCol; // the platform's collider
    Transform player; // the player's transform
    CharacterController playerCC; // the player's CharacterController
    Collider playerCol; // the player's collider

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] // runs when Play is pressed
    static void ResetStatics() => Current = null; // clear the old lit platform

    void Start() // runs once at the start
    {
        platformCol = GetComponent<Collider>(); // get the collider on this object
        if (platformCol == null) platformCol = GetComponentInChildren<Collider>(); // or look in the children
        if (platformCol == null) // still no collider
            Debug.LogWarning(name + " is a Spawn Platform but has no Collider.", this); // warn about it

        if (spawnPoint == null) // no spawn point set
        {
            GameObject sp = GameObject.Find("SpawnPoint"); // find the object named SpawnPoint
            if (sp != null) spawnPoint = sp.transform; // use it
            else Debug.LogWarning("Spawn Platform '" + name + "': no object named SpawnPoint found. Drag it into the Spawn Point slot.", this); // warn about it
        }

        SetLook(false); // start dark gray
    }

    void Update() // runs every frame
    {
        if (IsActive || platformCol == null) return; // stop if already lit or no collider

        FindPlayer(); // make sure we know where the player is
        if (player == null) return; // no player, do nothing

        Vector3 feet = player.position; // start with the player's position
        if (playerCC != null) // the player has a CharacterController
            feet = new Vector3(playerCC.bounds.center.x, playerCC.bounds.min.y, playerCC.bounds.center.z); // use the bottom middle of it
        else if (playerCol != null) // the player has a collider
            feet = new Vector3(playerCol.bounds.center.x, playerCol.bounds.min.y, playerCol.bounds.center.z); // use the bottom middle of it

        Bounds b = platformCol.bounds; // the platform's size
        bool inside = feet.x >= b.min.x && feet.x <= b.max.x && // feet are inside the platform's X
                      feet.z >= b.min.z && feet.z <= b.max.z; // and inside the platform's Z
        float height = feet.y - b.max.y; // how high the feet are above the platform top

        if (inside && height > -0.5f && height < heightTolerance) // standing on top of the platform
            Activate(); // light it up
    }

    void Activate() // runs when the player steps on this platform
    {
        if (Current != null && Current != this) Current.SetLook(false); // turn the old lit platform back to gray
        Current = this; // this is now the lit platform

        if (spawnPoint != null) // we have a spawn point
        {
            Bounds b = platformCol.bounds; // the platform's size
            Vector3 top = new Vector3(b.center.x, b.max.y, b.center.z); // the middle of the platform's top
            spawnPoint.position = top + spawnOffset; // move the spawn point there
            if (copyRotation) spawnPoint.rotation = transform.rotation; // copy the rotation if wanted
        }

        SetLook(true); // light up this platform
        Debug.Log("[SpawnPlatform] Spawn point moved to " + name); // show the result in the Console
    }

    void SetLook(bool on) // true = white glow, false = dark gray
    {
        IsActive = on; // remember if it is lit
        Color color = on ? onColor : offColor; // pick the color
        float glow = on ? onGlow : offGlow; // pick the glow

        foreach (Renderer r in GetComponentsInChildren<Renderer>()) // go through every renderer
            TaskVisuals.SetColor(r, color, glow); // apply the color and glow
    }

    void FindPlayer() // finds the player
    {
        if (player != null) return; // already found
        GameObject go = GameObject.FindWithTag("Player"); // find the object tagged Player
        if (go == null) return; // none found
        player = go.transform; // save its transform
        playerCC = go.GetComponent<CharacterController>(); // save its CharacterController
        playerCol = go.GetComponent<Collider>(); // save its collider
    }
}