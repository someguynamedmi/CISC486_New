using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

// Minigame1: press Space to start, release, then press Space again when the crosshair is on the target.
public class ShootMinigame : Minigame
{
    public GameObject key;
    public GameObject crosshair;
    public GameObject target;

    public TextMeshProUGUI result;

    public float timer = 4f;
    private float timer_length;
    private float precision = 0.15f;
    private bool isTimerActive = false;
    private bool isPlayed = false;
    private bool canShoot = false;
    private bool shot = false;
    private bool initialised = false;

    // Runs every time an enemy starts this minigame (was Start).
    protected override void OnBegin()
    {
        if (!initialised)
        {
            timer_length = timer;
            initialised = true;
        }

        // Reset everything so the minigame can be played again.
        timer = timer_length;
        isTimerActive = false;
        isPlayed = false;
        canShoot = false;
        shot = false;

        key.transform.position = new Vector2(960, 340);
        crosshair.transform.position = new Vector2(360, 710);
        target.transform.position = new Vector2(1260, 710);
        result.gameObject.SetActive(false);
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
    }

    void mainGame()
    {
        if (Keyboard.current.spaceKey.wasReleasedThisFrame)
        {
            canShoot = true;
        }
        timer -= Time.deltaTime;

        crosshair.transform.position = new Vector2(360 + ((timer_length - timer) * 300), 710);
        if (timer <= 0)
        {
            isTimerActive = false;
            if (!shot)
            {
                ShowResult("You Didn't Even Try?!");
                FinishAfterDelay(false);
            }
            return;
        }

        if (Keyboard.current.spaceKey.wasPressedThisFrame && !shot && canShoot)
        {
            shot = true;
            isTimerActive = false; // freeze the crosshair where the player shot

            if (timer >= 1f + precision)
            {
                ShowResult("Too Early!");
                FinishAfterDelay(false);
            }
            else if (timer > 1f - precision)
            {
                Vector2 crosshairBounds = new Vector2(crosshair.transform.position.x - 50f, crosshair.transform.position.x + 50f);
                Vector2 targetBounds = new Vector2(target.transform.position.x - 50f, target.transform.position.x + 50f);
                float intersection = Mathf.Min(crosshairBounds.y, targetBounds.y) - Mathf.Max(crosshairBounds.x, targetBounds.x);
                float union = Mathf.Max(crosshairBounds.y, targetBounds.y) - Mathf.Min(crosshairBounds.x, targetBounds.x);
                float score = Mathf.Round((intersection / union) * 10000f) / 100f;
                ShowResult("WIN! \n score = " + score);
                FinishAfterDelay(true);
            }
            else
            {
                ShowResult("Too Late!");
                FinishAfterDelay(false);
            }
        }
    }

    void ShowResult(string text)
    {
        result.gameObject.SetActive(true);
        result.text = text;
    }
}