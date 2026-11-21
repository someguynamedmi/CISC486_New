using UnityEngine;
using UnityEngine.InputSystem;

// Example minigame: press Space enough times before the timer runs out.
// Use it as a template — copy this file, rename the class, and change OnBegin / OnUpdate.
public class MashMinigame : Minigame
{
    public int pressesNeeded = 15;
    public float timeLimit = 5f;

    int presses;
    float timeLeft;

    protected override void OnBegin()
    {
        presses = 0;
        timeLeft = timeLimit;
    }

    protected override void OnUpdate()
    {
        timeLeft -= Time.deltaTime;

        Keyboard kb = Keyboard.current;
        if (kb != null && kb.spaceKey.wasPressedThisFrame) presses++;

        if (presses >= pressesNeeded) Finish(true);
        else if (timeLeft <= 0f)      Finish(false);
    }

    // Quick on-screen display so the example works without building any UI.
    void OnGUI()
    {
        if (!IsRunning) return;

        GUIStyle style = new GUIStyle(GUI.skin.label) { fontSize = 32, alignment = TextAnchor.MiddleCenter };
        style.normal.textColor = Color.white;
        GUI.Label(new Rect(0, Screen.height * 0.35f, Screen.width, 60), "MASH SPACE!", style);
        GUI.Label(new Rect(0, Screen.height * 0.35f + 60, Screen.width, 60),
                  $"{presses} / {pressesNeeded}    {Mathf.Max(0f, timeLeft):F1}s", style);
    }
}