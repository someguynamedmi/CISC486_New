using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

// Put this on the player (the object with PlayerHopper).
//
// When the player touches anything tagged "Enemy", the main level is paused and hidden,
// and the minigame scene (the one containing PlatformMinigame) is loaded on top of it.
// When the minigame shows "YOU WIN!" or "YOU LOSE!", it waits a moment, unloads the
// minigame and brings the level back.
//
//   Win  -> the enemy that was touched is removed and play continues.
//   Lose -> depends on "On Lose" (restart the level by default).
//
// Setup:
//   1. Create a tag called "Enemy" and give it to your enemy objects (they need a Collider;
//      a Rigidbody is NOT required, and the collider can be a trigger or not).
//   2. Put PlatformMinigame in its own scene, e.g. "PlatformMinigame".
//   3. Add BOTH your level scene and the minigame scene in File > Build Profiles / Build Settings.
//   4. Type the minigame scene's name into "Minigame Scene Name" on this component.
public class EnemyMinigameTrigger : MonoBehaviour
{
    public enum LoseAction { RestartLevel, RemoveEnemyAndContinue, KeepEnemyAndContinue }

    [Header("Minigame")]
    [Tooltip("Exact name of the scene that holds the PlatformMinigame script.")]
    public string minigameSceneName = "PlatformMinigame";

    [Tooltip("Seconds to keep showing YOU WIN / YOU LOSE before going back to the level.")]
    public float resultDisplayTime = 1.5f;

    [Header("Enemies")]
    [Tooltip("Objects with this tag (or whose parent has it) start the minigame.")]
    public string enemyTag = "Enemy";

    [Tooltip("Makes the touch area a bit bigger or smaller than the player's collider.")]
    public float detectionPadding = 0f;

    [Tooltip("Seconds after returning to the level before another enemy can trigger the minigame.")]
    public float cooldownAfterReturn = 1f;

    [Header("Outcome")]
    public LoseAction onLose = LoseAction.RestartLevel;

    [Tooltip("Called after returning to the level from a won minigame.")]
    public UnityEvent onMinigameWon;

    [Tooltip("Called after returning to the level from a lost minigame (not called if the level restarts).")]
    public UnityEvent onMinigameLost;

    public bool InMinigame { get; private set; }

    Collider playerCollider;
    PlayerHopper hopper;
    float cooldownUntil;
    readonly Collider[] hits = new Collider[32];

    void Awake()
    {
        playerCollider = GetComponent<Collider>();
        hopper = GetComponent<PlayerHopper>();

        if (playerCollider == null)
            Debug.LogWarning("EnemyMinigameTrigger: the player has no Collider, so enemies can't be detected.");
    }

    void Update()
    {
        if (InMinigame || playerCollider == null || Time.time < cooldownUntil) return;

        GameObject enemy = FindTouchedEnemy();
        if (enemy != null)
            StartCoroutine(RunMinigame(enemy));
    }

    // Checks every collider overlapping the player's box, the same way GroundDetector finds platforms.
    // This works even though PlayerHopper moves the player by setting its position directly.
    GameObject FindTouchedEnemy()
    {
        Bounds b = playerCollider.bounds;
        Vector3 halfSize = b.extents + Vector3.one * detectionPadding;

        int count = Physics.OverlapBoxNonAlloc(b.center, halfSize, hits, Quaternion.identity,
                                               ~0, QueryTriggerInteraction.Collide);

        for (int i = 0; i < count; i++)
        {
            Collider c = hits[i];
            if (c == null || c.transform.IsChildOf(transform)) continue; // skip the player itself

            GameObject enemy = FindTaggedParent(c.transform);
            if (enemy != null) return enemy;
        }
        return null;
    }

    // Lets the tag sit on the enemy's root even if the collider is on a child object.
    GameObject FindTaggedParent(Transform t)
    {
        while (t != null)
        {
            if (t.CompareTag(enemyTag)) return t.gameObject;
            t = t.parent;
        }
        return null;
    }

    IEnumerator RunMinigame(GameObject enemy)
    {
        InMinigame = true;
        if (hopper != null) hopper.enabled = false;

        Scene levelScene = gameObject.scene;

        // Hide and pause the level: turning off its root objects stops the camera, timer,
        // enemies, UI, lights and EventSystem so they don't clash with the minigame scene.
        // The player's own root object stays on so this coroutine keeps running.
        List<GameObject> hidden = new List<GameObject>();
        foreach (GameObject root in levelScene.GetRootGameObjects())
        {
            if (root == transform.root.gameObject || !root.activeSelf) continue;
            root.SetActive(false);
            hidden.Add(root);
        }
        SetPlayerVisible(false);

        // Load the minigame on top of the level.
        AsyncOperation load = SceneManager.LoadSceneAsync(minigameSceneName, LoadSceneMode.Additive);
        if (load == null)
        {
            Debug.LogError($"EnemyMinigameTrigger: couldn't load scene '{minigameSceneName}'. " +
                           "Check the name and that it's added to the Build Settings scene list.");
            RestoreLevel(levelScene, hidden);
            yield break;
        }
        while (!load.isDone) yield return null;

        Scene minigameScene = SceneManager.GetSceneByName(minigameSceneName);
        SceneManager.SetActiveScene(minigameScene);

        PlatformMinigame minigame = FindMinigame(minigameScene);
        if (minigame == null)
        {
            Debug.LogError($"EnemyMinigameTrigger: no PlatformMinigame found in '{minigameSceneName}'.");
            yield return SceneManager.UnloadSceneAsync(minigameScene);
            RestoreLevel(levelScene, hidden);
            yield break;
        }

        // Let PlatformMinigame.Start() run (it hides the result text) before watching it.
        yield return null;
        yield return null;

        // PlatformMinigame doesn't report its result, so watch its result text.
        // It turns on and says "YOU WIN!" or "YOU LOSE!" when the round ends.
        bool? won = null;
        while (won == null)
        {
            if (minigame.result != null && minigame.result.gameObject.activeInHierarchy)
            {
                string text = minigame.result.text.ToUpperInvariant();
                if (text.Contains("WIN")) won = true;
                else if (text.Contains("LOSE")) won = false;
            }
            yield return null;
        }

        // Unscaled, so it still works if the minigame or anything else changes Time.timeScale.
        yield return new WaitForSecondsRealtime(resultDisplayTime);

        yield return SceneManager.UnloadSceneAsync(minigameScene);

        if (won == false && onLose == LoseAction.RestartLevel)
        {
            SceneManager.LoadScene(levelScene.name);
            yield break;
        }

        bool removeEnemy = won == true || onLose == LoseAction.RemoveEnemyAndContinue;
        if (removeEnemy && enemy != null)
            Destroy(enemy);

        RestoreLevel(levelScene, hidden);

        if (won == true) onMinigameWon?.Invoke();
        else onMinigameLost?.Invoke();
    }

    PlatformMinigame FindMinigame(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            PlatformMinigame mg = root.GetComponentInChildren<PlatformMinigame>(true);
            if (mg != null) return mg;
        }
        return null;
    }

    void RestoreLevel(Scene levelScene, List<GameObject> hidden)
    {
        SceneManager.SetActiveScene(levelScene);

        foreach (GameObject root in hidden)
            if (root != null) root.SetActive(true);

        SetPlayerVisible(true);
        if (hopper != null) hopper.enabled = true;

        cooldownUntil = Time.time + cooldownAfterReturn;
        InMinigame = false;
    }

    void SetPlayerVisible(bool visible)
    {
        foreach (Renderer r in transform.root.GetComponentsInChildren<Renderer>(true))
            r.enabled = visible;
    }
}
