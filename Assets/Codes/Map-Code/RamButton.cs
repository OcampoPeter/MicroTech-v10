using UnityEngine; // Unity basics
using UnityEngine.Events; // lets us use UnityEvent
using UnityEngine.EventSystems; // lets us detect pointer hover and click

public class RamButton : MonoBehaviour, // a button that lifts a RAM stick when hovered
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler // detects pointer enter, exit and click
{
    [Header("The RAM stick that moves up (RamModel)")] // Inspector title
    public Transform moveTarget; // the RAM stick that moves
    public float raiseAmount = 0.3f; // how far it moves up
    public UnityEvent onClick; // runs when the button is clicked

    Vector3 startPos; // the starting position of the RAM stick

    void Awake() // runs when the object loads
    {
        startPos = moveTarget.position; // remember where the stick started
    }

    public void OnPointerEnter(PointerEventData e) // the pointer moved onto the button
    {
        moveTarget.position = startPos + Vector3.up * raiseAmount; // lift the stick up
    }

    public void OnPointerExit(PointerEventData e) // the pointer left the button
    {
        moveTarget.position = startPos; // put the stick back
    }

    public void OnPointerClick(PointerEventData e) // the button was clicked
    {
        onClick.Invoke(); // run the click event
    }
}