using System.Collections; // lets us use IEnumerator
using UnityEngine; // Unity basics

public static class TutorialProgress // remembers tutorial progress between scenes
{
    public static bool PcDelivered; // true once the first PC came out of the chute
    public static bool PartsDelivered; // true once the catalog parts came out of the chute

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] // runs when Play is pressed
    static void ClearOnPlay() { PcDelivered = false; PartsDelivered = false; } // start every Play session clean
}

public class TSRoomFlow : MonoBehaviour // controls what the chute delivers in the TS room
{
    [Header("References")] // Inspector title
    public DeliveryChute chute; // the chute that delivers items
    public PartCatalog catalog; // the list of parts (RAM, GPU, CPU...)
    public GameObject powerSupplyPrefab; // the power supply (empty = taken from the catalog)
    public GameObject pcPrefab; // the PC to deliver

    [Header("Timing")] // Inspector title
    public float startDelay = 2f; // seconds to wait before the first delivery
    public float gapBetweenPackages = 2f; // seconds between two deliveries

    [Header("After the tutorial")] // Inspector title
    [Tooltip("ON: after the PC, the chute sends every part in the catalog that is not fixed yet.")] // hint in the Inspector
    public bool deliverCatalogParts = true; // send the catalog parts after the tutorial

    [Header("Testing only")] // Inspector title
    [Tooltip("Tick this to skip the tutorial: the chute sends the fixed power supply and the PC straight away.")] // hint in the Inspector
    public bool skipTutorial = false; // skip the tutorial for testing

    public const string PowerSupplyId = "PSU"; // the name used for the power supply part

    IEnumerator Start() // runs at the start and can wait between steps
    {
        if (chute == null) // no chute set
        {
            Debug.LogError("TSRoomFlow: drag the ChuteMouth object into the Chute slot."); // show an error
            yield break; // stop here
        }

        if (powerSupplyPrefab == null && catalog != null) // no power supply set but we have a catalog
        {
            PartEntry psu = catalog.Find(PowerSupplyId); // find the power supply in the catalog
            if (psu != null) powerSupplyPrefab = psu.prefab; // use its prefab
        }

        if (skipTutorial) PartRepair.MarkFixed(PowerSupplyId); // mark the power supply as fixed to skip the tutorial

        yield return new WaitForSeconds(startDelay); // wait before delivering

        bool tutorialDone = PartRepair.IsFixed(PowerSupplyId); // true if the power supply is already fixed

        if (!tutorialDone) // the tutorial is not done
        {
            chute.Deliver(powerSupplyPrefab); // send only the broken power supply
            yield break; // stop here
        }

        if (!TutorialProgress.PcDelivered) // the PC has not been delivered yet
        {
            chute.Deliver(powerSupplyPrefab); // send the fixed power supply
            yield return new WaitForSeconds(gapBetweenPackages); // wait between deliveries
            chute.Deliver(pcPrefab); // send the PC
            TutorialProgress.PcDelivered = true; // remember the PC was delivered
        }

        if (deliverCatalogParts && catalog != null && !TutorialProgress.PartsDelivered) // catalog parts not sent yet
        {
            TutorialProgress.PartsDelivered = true; // remember they were sent
            foreach (PartEntry p in catalog.parts) // go through each part in the catalog
            {
                if (p.prefab == null || p.partId == PowerSupplyId) continue; // skip empty rows and the power supply
                if (PartRepair.IsFixed(p.partId)) continue; // skip parts that are already fixed
                yield return new WaitForSeconds(gapBetweenPackages); // wait between deliveries
                chute.Deliver(p.prefab); // send this part
            }
        }
    }
}