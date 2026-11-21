using System;
using System.Collections;
using UnityEngine;

// Base class for ANY minigame.
// To make a minigame: inherit from Minigame instead of MonoBehaviour, then
//   - override OnBegin()  to reset/set up your minigame (runs every time it starts)
//   - override OnUpdate() for per-frame logic (only runs while the minigame is active)
//   - when it's over call FinishAfterDelay(won) to show the result for a moment first,
//     or Finish(won) to return to the level immediately
// Do NOT write your own Update() or Start() in a minigame. Use OnUpdate() / OnBegin().
public abstract class Minigame : MonoBehaviour
{
    [Header("Minigame (shared settings)")]
    [Tooltip("What gets shown while the minigame runs and hidden otherwise. " +
             "Leave empty to use this object itself (e.g. the minigame's Canvas).")]
    public GameObject content;

    [Tooltip("Optional. Camera to switch to while the minigame runs. The main camera is switched back afterwards.")]
    public Camera minigameCamera;

    [Tooltip("Seconds to keep the result (WIN / LOSE) on screen before returning to the level.")]
    public float resultDelay = 1.5f;

    // The minigame currently being played (null if none). Enemies use this so two can't start at once.
    public static Minigame Current { get; private set; }

    public bool IsRunning { get; private set; }

    Action<bool> onFinished;
    Camera previousCamera;
    bool finishing;

    GameObject Content => content != null ? content : gameObject;

    protected virtual void Awake()
    {
        // Hidden until an enemy starts it. (If Begin() is what woke us up, stay visible.)
        if (!IsRunning)
        {
            if (minigameCamera != null) minigameCamera.enabled = false;
            Content.SetActive(false);
        }
    }

    // Called by Enemy (or anything else). onFinished receives true = player won, false = player lost.
    // Works even if this object is currently inactive.
    public bool Begin(Action<bool> onFinished)
    {
        if (IsRunning || Current != null) return false;

        Current = this;
        IsRunning = true;
        finishing = false;
        this.onFinished = onFinished;

        Content.SetActive(true);

        if (minigameCamera != null)
        {
            previousCamera = Camera.main;
            if (previousCamera != null && previousCamera != minigameCamera) previousCamera.enabled = false;
            minigameCamera.enabled = true;
        }

        OnBegin();
        return true;
    }

    // Shows the result for resultDelay seconds, then ends the minigame.
    protected void FinishAfterDelay(bool playerWon)
    {
        if (!IsRunning || finishing) return;
        finishing = true;
        StartCoroutine(FinishRoutine(playerWon));
    }

    IEnumerator FinishRoutine(bool playerWon)
    {
        yield return new WaitForSeconds(resultDelay);
        Finish(playerWon);
    }

    // Ends the minigame immediately.
    protected void Finish(bool playerWon)
    {
        if (!IsRunning) return;
        IsRunning = false;
        finishing = false;

        OnEnd(playerWon);

        if (minigameCamera != null)
        {
            minigameCamera.enabled = false;
            if (previousCamera != null) previousCamera.enabled = true;
            previousCamera = null;
        }

        Current = null;

        Action<bool> callback = onFinished;
        onFinished = null;
        callback?.Invoke(playerWon);

        Content.SetActive(false); // last, because this may switch off this very object
    }

    protected virtual void Update()
    {
        if (IsRunning && !finishing) OnUpdate();
    }

    // ---- Override these in your minigame ----
    protected abstract void OnBegin();               // reset scores, timers, positions...
    protected virtual void OnUpdate() { }            // per-frame logic while running
    protected virtual void OnEnd(bool playerWon) { } // optional clean-up

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Current = null; }
}