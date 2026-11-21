using UnityEngine;
using UnityEngine.Events;

// Put this on an enemy. On Start it stands itself on the nearest block (feet centred on top).
// When the player lands on that block, the player is frozen and the assigned minigame starts. Works with ANY script that inherits from Minigame —
// just drag the object holding the minigame into the "Minigame" slot.
public class Enemy : MonoBehaviour
{
    [Header("Minigame")]
    [Tooltip("Drag in the object that has a Minigame script on it (any kind).")]
    public Minigame minigame;

    [Header("Placement")]
    [Tooltip("On Start, move the enemy so its feet stand on the centre of the nearest block.")]
    public bool snapToNearestBlock = true;

    [Header("Jumping")]
    [Tooltip("Make the enemy jump up and down on its block.")]
    public bool jump = true;
    [Tooltip("How high the enemy jumps (world units). Must be higher than the player for them to pass under.")]
    public float jumpHeight = 2.5f;
    [Tooltip("Seconds the enemy spends in the air per jump. Bigger = slower.")]
    public float airTime = 2.5f;
    [Tooltip("Seconds the enemy waits on its block between jumps.")]
    public float groundTime = 1.5f;
    [Tooltip("Start each enemy at a random point in its jump so they don't all move together.")]
    public bool randomStartTime = true;
    [Tooltip("When the enemy is higher than this above its block, the player can pass under it safely. " +
             "0 = use the player's height automatically.")]
    public float passUnderHeight = 0f;

    [Header("Contact")]
    [Tooltip("How close (horizontally) the player's feet must be to count as touching.")]
    public float contactRadius = 0.5f;
    [Tooltip("How far apart vertically they can be and still count as touching.")]
    public float contactHeight = 1.5f;
    [Tooltip("Only start the minigame once the player has landed, not while flying over.")]
    public bool onlyWhenLanded = true;

    [Header("Debug")]
    [Tooltip("Print what the enemy is doing to the Console.")]
    public bool debugLog = true;

    [Header("Result")]
    [Tooltip("Delete the enemy when the player wins the minigame.")]
    public bool removeEnemyOnWin = true;
    [Tooltip("Restart the level when the player loses. If off, the player must step away and come back to retry.")]
    public bool restartLevelOnLose = true;
    [Tooltip("Keep the level timer running through the restart instead of resetting it to 0.")]
    public bool keepTimerOnRestart = true;
    [Tooltip("Extra things to do when the player wins (play a sound, add score...).")]
    public UnityEvent onPlayerWon;
    [Tooltip("Extra things to do when the player loses.")]
    public UnityEvent onPlayerLost;

    PlayerHopper player;
    Platform myBlock;
    Vector3 groundPosition;  // where the enemy stands on its block
    float jumpClock;         // only advances while no minigame is running
    float currentHeight;     // how high above its block the enemy is right now
    bool waitForPlayerToLeave;

    void Start()
    {
        if (snapToNearestBlock) SnapToNearestBlock();

        // If a prefab from the Project window was dragged into the slot (instead of an object
        // in the scene), make a copy of it in the scene so it can actually be shown.
        if (minigame != null && !minigame.gameObject.scene.IsValid())
        {
            minigame = Instantiate(minigame);
            if (debugLog) Debug.Log($"[Enemy] '{name}': Minigame slot held a prefab, so a copy was added to the scene.", this);
        }

        player = FindAnyObjectByType<PlayerHopper>();

        groundPosition = transform.position;
        if (randomStartTime) jumpClock = Random.Range(0f, groundTime + airTime);

        // Pass-under height defaults to the player's height.
        if (passUnderHeight <= 0f && player != null)
        {
            Renderer pr = player.GetComponentInChildren<Renderer>();
            passUnderHeight = pr != null ? pr.bounds.size.y : 1f;
        }
        if (jump && jumpHeight <= passUnderHeight)
            Debug.LogWarning($"[Enemy] '{name}': Jump Height ({jumpHeight}) isn't above the player's height ({passUnderHeight:F2}), so the player can never pass under.", this);
        if (player == null) Debug.LogWarning($"[Enemy] '{name}': no PlayerHopper found in the scene.", this);
        if (minigame == null) Debug.LogWarning($"[Enemy] '{name}': no Minigame assigned.", this);
    }

    void Update()
    {
        UpdateJump();

        if (player == null || minigame == null) return;
        if (Minigame.Current != null) return; // a minigame is already running

        bool touching = IsTouchingPlayer();

        // After a loss, don't instantly restart: wait until the player steps off.
        if (waitForPlayerToLeave)
        {
            if (!touching) waitForPlayerToLeave = false;
            return;
        }

        if (!touching) return;
        if (onlyWhenLanded && player.IsHopping) return;

        StartMinigame();
    }

    bool IsTouchingPlayer()
    {
        // Measure from the top of the enemy's block if it has one, otherwise from the enemy itself.
        Vector3 centre = myBlock != null ? myBlock.TopCenter : transform.position;
        // Enemy is up in the air: the player can pass underneath.
        if (currentHeight > passUnderHeight) return false;

        Vector3 d = player.FeetWorldPosition() - centre;
        float vertical = Mathf.Abs(d.y);
        d.y = 0f;
        return d.magnitude <= contactRadius && vertical <= contactHeight;
    }

    // Waits on the block, then does a smooth slow jump, and repeats.
    void UpdateJump()
    {
        if (!jump || airTime <= 0f) { currentHeight = 0f; return; }

        // Freeze in place while a minigame is on screen.
        if (Minigame.Current == null) jumpClock += Time.deltaTime;

        float t = jumpClock % (groundTime + airTime);
        currentHeight = t < groundTime
            ? 0f
            : Mathf.Sin((t - groundTime) / airTime * Mathf.PI) * jumpHeight;

        transform.position = groundPosition + Vector3.up * currentHeight;
    }

    void StartMinigame()
    {
        if (debugLog) Debug.Log($"[Enemy] '{name}': player landed on my block, starting '{minigame.name}'.", this);
        player.enabled = false; // freeze player controls
        if (!minigame.Begin(OnMinigameFinished))
            player.enabled = true; // couldn't start, give control back
    }

    void OnMinigameFinished(bool playerWon)
    {
        player.enabled = true;

        if (playerWon)
        {
            onPlayerWon?.Invoke();
            if (removeEnemyOnWin) Destroy(gameObject);
        }
        else
        {
            onPlayerLost?.Invoke();
            if (restartLevelOnLose)
                LevelRestarter.Restart(keepTimerOnRestart);
            else
                waitForPlayerToLeave = true;
        }
    }

    // ---------- Standing on a block ----------

    void SnapToNearestBlock()
    {
        // Feet offset from the pivot (works for models with the pivot at the feet, centre, or anywhere).
        float footHeight = GetFootHeight();
        Vector3 feet = GetFeetOffsetWorld();

        // Nearest block, measured from the feet.
        Vector3 feetPos = transform.position + feet;
        float best = float.MaxValue;
        foreach (Platform p in Platform.All)
        {
            Vector3 o = p.TopCenter - feetPos;
            o.y = 0f;
            if (o.sqrMagnitude < best) { best = o.sqrMagnitude; myBlock = p; }
        }

        if (debugLog) Debug.Log($"[Enemy] '{name}': standing on block '{(myBlock != null ? myBlock.name : "none")}'.", this);

        if (myBlock == null)
        {
            Debug.LogWarning($"[Enemy] '{name}': no blocks found to stand on.", this);
            return;
        }

        transform.position = myBlock.TopCenter + Vector3.up * footHeight - feet;
    }

    float GetFootHeight()
    {
        Collider col = GetComponentInChildren<Collider>();
        if (col != null) return transform.position.y - col.bounds.min.y;

        Renderer rend = GetComponentInChildren<Renderer>();
        if (rend != null) return transform.position.y - rend.bounds.min.y;

        return 0f;
    }

    // Horizontal world-space vector from the pivot to the centre of the lowest vertices (the feet).
    Vector3 GetFeetOffsetWorld()
    {
        MeshFilter mf = GetComponentInChildren<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return Vector3.zero;
        if (!mf.sharedMesh.isReadable)
        {
            Debug.LogWarning($"[{GetType().Name}] '{name}': can't find the feet because Read/Write is off on mesh '{mf.sharedMesh.name}'. " +
                             "Select the model in the Project window > Model tab > tick Read/Write > Apply.", this);
            return Vector3.zero;
        }
        Vector3[] verts = mf.sharedMesh.vertices;
        if (verts.Length == 0) return Vector3.zero;

        float minY = float.MaxValue, maxY = float.MinValue;
        foreach (Vector3 v in verts) { minY = Mathf.Min(minY, v.y); maxY = Mathf.Max(maxY, v.y); }
        float band = minY + (maxY - minY) * 0.05f;

        Vector3 lo = Vector3.one * float.MaxValue, hi = Vector3.one * float.MinValue;
        foreach (Vector3 v in verts)
        {
            if (v.y > band) continue;
            lo = Vector3.Min(lo, v);
            hi = Vector3.Max(hi, v);
        }

        Vector3 world = mf.transform.TransformPoint((lo + hi) * 0.5f) - transform.position;
        world.y = 0f;
        return world;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, contactRadius);
    }
#endif
}