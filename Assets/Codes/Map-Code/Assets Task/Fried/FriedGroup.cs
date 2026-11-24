using UnityEngine; // Gives access to Unity classes like MonoBehaviour

public class FriedGroup : MonoBehaviour // Put this on a set of Fried platforms to control that set
{
    [Tooltip("How many of this set's platforms are used (-1 = all of them). The others go back to the normal layer.")] // Hover text in the Inspector
    public int nodeCount = -1; // How many platforms of this set are used (-1 = all)

    [Tooltip("ON: the flash order is shuffled every run.\nOFF: the order follows the Hierarchy, top to bottom.")] // Hover text in the Inspector
    public bool randomizeOrder = true; // True = the flash order is shuffled every run
}