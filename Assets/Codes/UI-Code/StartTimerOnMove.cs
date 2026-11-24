using UnityEngine;

public class StartTimerOnMove : MonoBehaviour
{
    PlayerInputHandler input; // your new Input System script on the Player
    bool done = false;        // becomes true after the timer has started

    void Start()
    {
        input = GetComponent<PlayerInputHandler>(); // finds it on the same object (the Player)
    }

    void Update()
    {
        if (done || input == null || LoadingScreen.IsLoading) return; // do nothing while loading

        if (input.MoveInput.sqrMagnitude > 0.01f || // any move input (WASD / stick)
            input.JumpTriggered)                    // jump counts as moving too
        {
            if (RunTimer.Instance != null) RunTimer.Instance.StartTimer(); // start the timer on the first move
            done = true;
        }
    }
}