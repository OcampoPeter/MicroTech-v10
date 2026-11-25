using System.Collections; // lets us use IEnumerator
using System.Collections.Generic; // lets us use HashSet
using UnityEngine; // Unity basics

public static class TutorialProgress // remembers tutorial progress between scenes
{
    public static bool PcDelivered; // true once the RAM, GPU and CPU came out of the chute
    public static bool PartsDelivered; // true once the catalog parts came out of the chute
    public static readonly HashSet<string> Sent = new HashSet<string>(); // the ids of everything the chute already sent

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] // runs when Play is pressed
    static void ClearOnPlay() { PcDelivered = false; PartsDelivered = false; Sent.Clear(); } // start every Play session clean
}

public class TSRoomFlow : MonoBehaviour // controls what the chute delivers in the TS room
{
    [Header("References")] // Inspector title
    public DeliveryChute chute; // the chute that delivers items
    public PartCatalog catalog; // the list of parts (RAM, GPU, CPU...)
    public GameObject powerSupplyPrefab; // the power supply (empty = taken from the catalog)

    [Header("Parts after the power supply")] // Inspector title
    public GameObject ramPrefab; // the RAM (empty = taken from the catalog)
    public GameObject gpuPrefab; // the GPU (empty = taken from the catalog)
    public GameObject cpuPrefab; // the CPU (empty = taken from the catalog)
    public string ramId = "RAM"; // must match the RAM Part Id in the catalog
    public string gpuId = "GPU"; // must match the GPU Part Id in the catalog
    public string cpuId = "CPU"; // must match the CPU Part Id in the catalog

    [Header("Timing")] // Inspector title
    public float startDelay = 2f; // seconds to wait before the first delivery
    public float gapBetweenPackages = 2f; // seconds between two deliveries

    [Header("After the tutorial")] // Inspector title
    [Tooltip("ON: after RAM, GPU and CPU, the chute sends every other part in the catalog that is not fixed yet.")] // hint in the Inspector
    public bool deliverCatalogParts = true; // send the other catalog parts too

    [Header("Testing only")] // Inspector title
    [Tooltip("Tick this to skip the tutorial: the chute sends the fixed power supply and the parts straight away.")] // hint in the Inspector
    public bool skipTutorial = false; // skip the tutorial for testing

    public const string PowerSupplyId = "PowerSupply-Broken"; // the name used for the power supply part (must match its Part Id in the catalog)

    void OnEnable() // runs when the TS room is switched on, including when we come back from a maze
    {
        StartCoroutine(Flow()); // start (or restart) the delivery flow
    }

    bool Send(string id, GameObject prefab) // sends one package, but only once per id
    {
        if (prefab == null) return false; // nothing to send
        if (!TutorialProgress.Sent.Add(id)) return false; // this id was already sent
        chute.Deliver(prefab); // send it down the chute
        return true; // it was sent now
    }

    GameObject FromCatalog(string id, GameObject current) // uses the slot prefab, or the catalog prefab if the slot is empty
    {
        if (current != null) return current; // the slot is filled, use it
        if (catalog == null) return null; // no catalog to look in
        PartEntry entry = catalog.Find(id); // find the part in the catalog
        return entry != null ? entry.prefab : null; // use its prefab if found
    }

    IEnumerator SendWithGap(string id, GameObject prefab) // sends a package and waits only if something was really sent
    {
        if (Send(id, prefab)) // it was sent now
            yield return new WaitForSeconds(gapBetweenPackages); // wait between deliveries
    }

    IEnumerator Flow() // the delivery flow, safe to run again and again
    {
        if (chute == null) // no chute set
        {
            Debug.LogError("TSRoomFlow: drag the ChuteMouth object into the Chute slot."); // show an error
            yield break; // stop here
        }

        powerSupplyPrefab = FromCatalog(PowerSupplyId, powerSupplyPrefab); // fill the power supply from the catalog if empty
        ramPrefab = FromCatalog(ramId, ramPrefab); // fill the RAM from the catalog if empty
        gpuPrefab = FromCatalog(gpuId, gpuPrefab); // fill the GPU from the catalog if empty
        cpuPrefab = FromCatalog(cpuId, cpuPrefab); // fill the CPU from the catalog if empty

        if (skipTutorial) PartRepair.MarkFixed(PowerSupplyId); // mark the power supply as fixed to skip the tutorial

        yield return new WaitForSeconds(startDelay); // wait before delivering

        if (!PartRepair.IsFixed(PowerSupplyId)) // the tutorial is not done
        {
            Send(PowerSupplyId, powerSupplyPrefab); // send only the broken power supply (once)
            yield break; // stop here
        }

        yield return SendWithGap(PowerSupplyId, powerSupplyPrefab); // the power supply was never sent (for example when skipping the tutorial)
        yield return SendWithGap(ramId, ramPrefab); // send the RAM (once)
        yield return SendWithGap(gpuId, gpuPrefab); // send the GPU (once)
        yield return SendWithGap(cpuId, cpuPrefab); // send the CPU (once)

        TutorialProgress.PcDelivered = true; // remember the main parts were delivered

        if (deliverCatalogParts && catalog != null) // we should send the other catalog parts
        {
            foreach (PartEntry p in catalog.parts) // go through each part in the catalog
            {
                if (p.prefab == null || p.partId == PowerSupplyId) continue; // skip empty rows and the power supply
                if (p.partId == ramId || p.partId == gpuId || p.partId == cpuId) continue; // skip RAM, GPU and CPU, they were already handled
                if (PartRepair.IsFixed(p.partId)) continue; // skip parts that are already fixed
                if (TutorialProgress.Sent.Contains(p.partId)) continue; // skip parts that were already sent
                yield return new WaitForSeconds(gapBetweenPackages); // wait between deliveries
                Send(p.partId, p.prefab); // send this part (once)
            }
            TutorialProgress.PartsDelivered = true; // remember the parts were sent
        }
    }
}