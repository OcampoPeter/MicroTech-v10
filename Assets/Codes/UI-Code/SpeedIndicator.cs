using UnityEngine; // Unity basics
using TMPro; // lets us use TextMeshPro

public class SpeedIndicator : MonoBehaviour // shows the player's speed on screen
{
    [Header("References")] // Inspector title
    public PlayerMovement player; // the player script
    public TMP_Text speedText; // the text that shows the speed

    [Header("Colors")] // Inspector title
    public Color normalColor = Color.white; // color at normal speed
    public Color fastColor = Color.yellow; // color when fast
    public Color veryFastColor = Color.red; // color when very fast

    [Header("When the colors change")] // Inspector title
    [Tooltip("Speed where it is fully RED, as a multiple of your max sprint speed. 1.6 = red at 16 m/s when max sprint is 10.")] // hint in the Inspector
    public float redAtMultiple = 1.6f; // how fast you must go to be fully red
    [Tooltip("How smoothly the number follows your real speed.")] // hint in the Inspector
    public float numberSmoothing = 12f; // higher = the number follows faster

    private float shownSpeed; // the smoothed speed we show

    void Reset() // runs when the script is first added
    {
        speedText = GetComponent<TMP_Text>(); // grab the text on this object
    }

    void Update() // runs every frame
    {
        if (player == null || speedText == null) return; // missing something, do nothing

        shownSpeed = Mathf.Lerp(shownSpeed, player.HorizontalSpeed, numberSmoothing * Time.unscaledDeltaTime); // move the shown speed toward the real speed

        float sprint = player.maxSprintSpeed; // the normal top sprint speed
        float red = sprint * redAtMultiple; // the speed where the color is fully red

        Color color; // the color we will use
        if (shownSpeed <= sprint) // at or under normal sprint
        {
            color = normalColor; // use the normal color
        }
        else // faster than sprint
        {
            float t = Mathf.InverseLerp(sprint, red, shownSpeed); // 0 at sprint speed, 1 at red speed
            if (t < 0.5f) color = Color.Lerp(normalColor, fastColor, t * 2f); // blend white to yellow
            else color = Color.Lerp(fastColor, veryFastColor, (t - 0.5f) * 2f); // blend yellow to red
        }

        speedText.color = color; // apply the color
        speedText.text = shownSpeed.ToString("F0") + " m/s"; // show the speed as a whole number
    }
}