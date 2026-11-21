using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

// Minigame2: press Space to start, then type the letters shown before time runs out.
public class TypingMinigame : Minigame
{
    public GameObject button_holder;

    public TextMeshProUGUI result;
    public TextMeshProUGUI timer_text;

    private List<GameObject> buttons;
    private List<Color> buttonStartColors;
    private List<char> letters = new List<char>();
    public float timer = 4f;
    private float timer_length;
    private bool isTimerActive = false;
    private bool isPlayed = false;
    private int currentButtonIndex = 0;

    // Runs every time an enemy starts this minigame (was Start).
    protected override void OnBegin()
    {
        if (buttons == null) // first time only
        {
            buttons = new List<GameObject>();
            buttonStartColors = new List<Color>();
            foreach (Transform child in button_holder.transform)
            {
                buttons.Add(child.gameObject);
                Image img = child.GetComponent<Image>();
                buttonStartColors.Add(img != null ? img.color : Color.white);
            }
            timer_length = timer;
        }

        // Reset everything so the minigame can be played again.
        timer = timer_length;
        isTimerActive = false;
        isPlayed = false;
        currentButtonIndex = 0;
        letters.Clear();
        for (int i = 0; i < buttons.Count; i++)
        {
            Image img = buttons[i].GetComponent<Image>();
            if (img != null) img.color = buttonStartColors[i];
        }
        result.gameObject.SetActive(false);
        UpdateTimerText();
    }

    // Was Update.
    protected override void OnUpdate()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame && !isPlayed)
        {
            isTimerActive = true;
            isPlayed = true;
            setupGame();
        }
        if (isTimerActive)
        {
            mainGame();
        }
        UpdateTimerText();
    }

    void UpdateTimerText()
    {
        timer_text.text = "Time: " + (Mathf.Round(timer * 10) / 10).ToString();
    }

    void setupGame()
    {
        foreach (GameObject button in buttons)
        {
            TextMeshProUGUI childText = button.GetComponentInChildren<TextMeshProUGUI>();
            char randomChar = (char)('a' + Random.Range(0, 26));
            letters.Add(randomChar);
            childText.text = randomChar.ToString().ToUpper();
        }
    }

    void mainGame()
    {
        if (currentButtonIndex >= letters.Count)
        {
            isTimerActive = false;
            result.gameObject.SetActive(true);
            result.text = "WIN!";
            FinishAfterDelay(true);
            return;
        }
        var keyboard = Keyboard.current;
        var keyControl = keyboard.FindKeyOnCurrentKeyboardLayout(letters[currentButtonIndex].ToString());
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            timer = 0;
            isTimerActive = false;
            result.gameObject.SetActive(true);
            result.text = "You failed!";
            FinishAfterDelay(false);
            return;
        }
        else if (keyControl != null && keyControl.wasPressedThisFrame)
        {
            Image buttonImage = buttons[currentButtonIndex].GetComponent<Image>();
            buttonImage.color = Color.green;
            currentButtonIndex++;
        }
    }
}