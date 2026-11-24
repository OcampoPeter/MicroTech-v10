using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class TutorialGuideUI : MonoBehaviour
{
    [System.Serializable]
    public class TutorialStep
    {
        public string title;
        [TextArea(3, 6)]
        public string description;

        [Header("Position On Screen (Anchored Position)")]
        public Vector2 anchoredPosition = Vector2.zero;
        public bool overrideSize = false;
        public Vector2 customSize = new Vector2(500f, 180f);

        [Header("Optional HUD Highlight Frame")]
        public bool showHighlight;
        public Vector2 highlightAnchoredPos;
        public Vector2 highlightSize = new Vector2(350f, 150f);
    }

    [Header("UI References")]
    public GameObject tutorialPanel;
    public RectTransform moveTarget; // Drag TutorialPanel (or a container holding TitleText & BodyText) here
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI bodyText;
    public TextMeshProUGUI stepCounterText;
    public Button nextButton;
    public TextMeshProUGUI nextButtonText;
    public RectTransform highlightBox;

    [Header("Disable Player Movement While Open")]
    public Behaviour[] componentsToDisable;

    [Header("Tutorial Slides (Adjust Positions in Inspector)")]
    public List<TutorialStep> steps = new List<TutorialStep>()
    {
        new TutorialStep
        {
            title = "Welcome to Hardware Repair",
            description = "Before you begin repairing this component, let's walk through what each indicator on your screen does. Press any key to continue.",
            anchoredPosition = new Vector2(0f, 0f)
        },
        new TutorialStep
        {
            title = "Timer & Compass",
            description = "The top-left timer tracks how fast you complete the repair, while the top compass shows the distance and direction to task nodes.",
            anchoredPosition = new Vector2(-520f, 340f)
        },
        new TutorialStep
        {
            title = "Corruption & Speed",
            description = "Keep an eye on Corruption at the top-right—if it fills up or you fall into the Killzone, you'll lose progress.",
            anchoredPosition = new Vector2(520f, 340f)
        },
        new TutorialStep
        {
            title = "Objectives (Bent, Missing, Fried)",
            description = "On the left are the damaged elements you must fix or collect before reaching the Finish Line.",
            anchoredPosition = new Vector2(-520f, 0f)
        },
        new TutorialStep
        {
            title = "Ready to Start",
            description = "Complete all objectives and reach the Finish Line to repair the part. Press any key to begin!",
            anchoredPosition = new Vector2(0f, 0f)
        }
    };

    private int currentIndex = 0;
    private bool isTutorialOpen = false;
    private float lastAdvanceTime = -1f;
    private const float AdvanceCooldown = 0.2f;
    private bool textInputReceived = false;

    void Start()
    {
        if (steps == null || steps.Count == 0)
        {
            CloseTutorial();
            return;
        }

        isTutorialOpen = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;

        SetGameplayComponentsEnabled(false);
        RegisterInputListeners();

        if (tutorialPanel != null)
            tutorialPanel.SetActive(true);

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextClicked);
        }

        ShowStep(0);
    }

    void Update()
    {
        if (!isTutorialOpen) return;

        if (Cursor.lockState != CursorLockMode.None)
            Cursor.lockState = CursorLockMode.None;
        if (!Cursor.visible)
            Cursor.visible = true;

        if (WasAnyKeyOrClickPressed())
        {
            OnNextClicked();
        }
    }

    private bool WasAnyKeyOrClickPressed()
    {
        if (textInputReceived)
        {
            textInputReceived = false;
            return true;
        }

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            return true;

        if (Mouse.current != null && (
            Mouse.current.leftButton.wasPressedThisFrame ||
            Mouse.current.rightButton.wasPressedThisFrame ||
            Mouse.current.middleButton.wasPressedThisFrame))
            return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.anyKeyDown)
            return true;
#endif
        return false;
    }

    private void RegisterInputListeners()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            Keyboard.current.onTextInput -= OnTextInput;
            Keyboard.current.onTextInput += OnTextInput;
        }
#endif
    }

    private void UnregisterInputListeners()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            Keyboard.current.onTextInput -= OnTextInput;
        }
#endif
    }

    private void OnTextInput(char _)
    {
        if (isTutorialOpen)
            textInputReceived = true;
    }

    public void OnNextClicked()
    {
        if (!isTutorialOpen) return;

        if (Time.unscaledTime - lastAdvanceTime < AdvanceCooldown)
            return;

        lastAdvanceTime = Time.unscaledTime;
        textInputReceived = false;
        currentIndex++;

        if (currentIndex >= steps.Count)
        {
            CloseTutorial();
        }
        else
        {
            ShowStep(currentIndex);
        }
    }

    private void ShowStep(int index)
    {
        TutorialStep step = steps[index];

        if (titleText != null)
            titleText.text = step.title;

        if (bodyText != null)
            bodyText.text = step.description;

        if (stepCounterText != null)
            stepCounterText.text = $"{index + 1} / {steps.Count}";

        if (nextButtonText != null)
            nextButtonText.text = (index == steps.Count - 1) ? "Start" : "Next (Any Key)";

        RectTransform target = moveTarget != null
            ? moveTarget
            : (tutorialPanel != null ? tutorialPanel.GetComponent<RectTransform>() : null);

        if (target != null)
        {
            target.anchoredPosition = step.anchoredPosition;
            if (step.overrideSize)
            {
                target.sizeDelta = step.customSize;
            }
        }

        if (highlightBox != null)
        {
            highlightBox.gameObject.SetActive(step.showHighlight);
            if (step.showHighlight)
            {
                highlightBox.anchoredPosition = step.highlightAnchoredPos;
                highlightBox.sizeDelta = step.highlightSize;
            }
        }
    }

    private void CloseTutorial()
    {
        isTutorialOpen = false;
        textInputReceived = false;
        UnregisterInputListeners();

        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);

        if (highlightBox != null)
            highlightBox.gameObject.SetActive(false);

        SetGameplayComponentsEnabled(true);

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void SetGameplayComponentsEnabled(bool enabled)
    {
        if (componentsToDisable == null) return;

        foreach (Behaviour comp in componentsToDisable)
        {
            if (comp != null)
                comp.enabled = enabled;
        }
    }

    void OnDestroy()
    {
        UnregisterInputListeners();
        Time.timeScale = 1f;
        SetGameplayComponentsEnabled(true);
    }
}