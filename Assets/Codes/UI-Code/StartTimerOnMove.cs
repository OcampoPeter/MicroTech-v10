using UnityEngine; // Unity basics

public class StartTimerOnMove : MonoBehaviour // starts the timer when the player first moves
{
    PlayerInputHandler input; // the player's input script
    bool done = false; // true after the timer has started

    void Start() // runs once at the start
    {
        input = GetComponent<PlayerInputHandler>(); // get the input script on this object
    }

    void Update() // runs every frame
    {
        if (done || input == null || LoadingScreen.IsLoading) return; // stop if already started, missing input, or still loading

        if (input.MoveInput.sqrMagnitude > 0.01f || // the player is pressing a move key
            input.JumpTriggered) // or the player jumped
        {
            if (RunTimer.Instance != null) RunTimer.Instance.StartTimer(); // start the timer
            done = true; // never do this again
        }
    }
}