using UnityEngine; // Unity basics

public enum POVBlockAction // what the block does to the camera
{
    Toggle, // switch to the other view
    FirstPerson, // go to first person
    ThirdPerson // go to third person
}

public class POVSwitchBlock : MonoBehaviour // a trigger block that changes the camera view
{
    public POVBlockAction action = POVBlockAction.Toggle; // what this block does

    void OnTriggerEnter(Collider other) // runs when something enters the block
    {
        if (!other.CompareTag("Player")) return; // only the player counts

        CameraModeSwitcher switcher = other.GetComponentInParent<CameraModeSwitcher>(); // get the camera switcher on the player
        if (switcher == null) return; // no switcher, do nothing

        if (action == POVBlockAction.Toggle) switcher.Toggle(); // switch to the other view
        else if (action == POVBlockAction.FirstPerson) switcher.SetMode(CameraMode.FirstPerson); // go to first person
        else switcher.SetMode(CameraMode.ThirdPerson); // go to third person
    }
}