using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using TMPro;

// Minigame3: press Space to start, then press Right Arrow to cross the platforms without touching an enemy.
public class PlatformMinigame : Minigame
{
    public GameObject player;
    public GameObject enemies_holder;

    public TextMeshProUGUI result;
    public TextMeshProUGUI timer_text;
    public float timer = 4f;
    private float timer_length;
    private bool isTimerActive = false;
    private bool isPlayed = false;

    public float amplitude = 200f;
    public float frequency = 2f;
    private List<Vector2> startPosition;
    private Vector2 playerStartPosition;

    // Runs every time an enemy starts this minigame (was Start).
    protected override void OnBegin()
    {
        RectTransform playerRect = player.GetComponent<RectTransform>();

        if (startPosition == null) // first time only: remember where everything starts
        {
            timer_length = timer;
            playerStartPosition = playerRect.anchoredPosition;
            startPosition = new List<Vector2>();
            foreach (RectTransform child in enemies_holder.transform)
            {
                startPosition.Add(child.anchoredPosition);
            }
        }

        // Reset everything so the minigame can be played again.
        timer = timer_length;
        isTimerActive = false;
        isPlayed = false;
        playerRect.anchoredPosition = playerStartPosition;
        foreach (RectTransform child in enemies_holder.transform)
        {
            child.anchoredPosition = startPosition[child.GetSiblingIndex()];
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

    void mainGame()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            timer = 0;
            isTimerActive = false;
            ShowResult("YOU LOSE!");
            FinishAfterDelay(false);
            return;
        }
        else if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            RectTransform playerRect = player.GetComponent<RectTransform>();
            playerRect.anchoredPosition += new Vector2(256, 0);
        }
        else if (player.transform.position.x >= 1024)
        {
            isTimerActive = false;
            ShowResult("YOU WIN!");
            FinishAfterDelay(true);
            return;
        }
        if (checkCollision(player, enemies_holder))
        {
            isTimerActive = false;
            ShowResult("YOU LOSE!");
            FinishAfterDelay(false);
            return;
        }
        moveEnemies();
    }

    void ShowResult(string text)
    {
        result.gameObject.SetActive(true);
        result.text = text;
    }

    bool checkCollision(GameObject player, GameObject enemy)
    {
        RectTransform playerRect = player.GetComponent<RectTransform>();

        foreach (RectTransform child in enemy.transform)
        {
            if (RectsOverlap(playerRect, child))
            {
                return true;
            }
        }

        return false;
    }

    bool RectsOverlap(RectTransform rect1, RectTransform rect2)
    {
        Vector3[] corners1 = new Vector3[4];
        Vector3[] corners2 = new Vector3[4];
        rect1.GetWorldCorners(corners1);
        rect2.GetWorldCorners(corners2);
        return !(corners1[2].x < corners2[0].x || // rect1 is left of rect2
                 corners1[0].x > corners2[2].x || // rect1 is right of rect2
                 corners1[2].y < corners2[0].y || // rect1 is below rect2
                 corners1[0].y > corners2[2].y);   // rect1 is above rect2
    }

    void moveEnemies()
    {
        foreach (RectTransform child in enemies_holder.transform)
        {
            int index = child.GetSiblingIndex();
            float phaseOffset = index * Mathf.PI / 2f;

            float newY = startPosition[index].y + Mathf.Sin(Time.time * frequency + phaseOffset) * amplitude;

            child.anchoredPosition = new Vector2(
                startPosition[index].x,
                newY
            );
        }
    }
}