using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Put this on the player object (the cube or the Crested_Walker model).
// W / Up    = hop to the nearest platform away from the camera
// S / Down  = hop to the nearest platform toward the camera
// A / Left  = hop left,  D / Right = hop right
// After every hop the player's FEET are snapped to the exact top-center of the platform,
// no matter where the model's pivot is.
public class PlayerHopper : MonoBehaviour
{
    [Header("Hop")]
    [Tooltip("Seconds the hop takes. Set to 0 for an instant teleport.")]
    public float hopDuration = 0.25f;
    [Tooltip("How high the arc goes above the straight line between platforms.")]
    public float hopHeight = 1.5f;
    [Tooltip("Platforms farther than this are ignored.")]
    public float maxHopDistance = 10f;
    [Tooltip("How far off the pressed direction a platform can be and still count (degrees).")]
    [Range(1f, 89f)] public float searchConeAngle = 45f;

    [Header("Direction")]
    [Tooltip("If on, W/S/A/D are relative to the camera's view. If off, they use world +Z/-Z/-X/+X.")]
    public bool cameraRelative = true;
    public Transform cameraTransform;

    [Header("Model alignment")]
    [Tooltip("Find the feet automatically from the lowest vertices of the mesh.")]
    public bool autoDetectFeet = true;
    [Tooltip("Extra nudge in the model's local space, added on top of the auto-detected feet position. Leave at 0 normally.")]
    public Vector3 feetNudge = Vector3.zero;
    [Header("Facing")]
    [Tooltip("Turn the model to face the direction it hops.")]
    public bool faceHopDirection = true;
    [Tooltip("If a key is pressed but there's no platform that way, still turn to face it.")]
    public bool turnWhenBlocked = true;
    [Tooltip("Seconds a turn-in-place takes (when blocked).")]
    public float turnDuration = 0.1f;
    [Tooltip("If the model ends up facing sideways or backwards, set this to 90, -90 or 180.")]
    public float modelYawOffset = 0f;

    [Header("Debug")]
    [Tooltip("Print the detected values to the Console on Start.")]
    public bool debugLog = true;

    // Fires every time the player starts a hop. Other scripts (like GameTimer) can listen to it.
    public event System.Action Hopped;

    Platform current;
    bool isHopping;
    float footHeight;   // distance from pivot down to the lowest point of the model
    Vector3 feetOffset; // local-space horizontal offset from pivot to the centre of the feet

    void Start()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        // Physics would fight the hop and push the player off-center, so make it kinematic.
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        footHeight = GetFootHeight();
        feetOffset = (autoDetectFeet ? ComputeFeetOffset() : Vector3.zero) + feetNudge;

        if (debugLog)
            Debug.Log($"[PlayerHopper] on '{name}': footHeight={footHeight:F3}, feetOffset(local)={feetOffset:F3}", this);

        // Start centered on whichever platform the player is closest to.
        current = FindNearestPlatform();
        if (current != null) SnapTo(current);
    }

    void Update()
    {
        if (isHopping) return;

        Keyboard kb = Keyboard.current;
        if (kb == null) return; // no keyboard connected

        Vector3 input = Vector3.zero;
        if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)         input = Vector3.forward;
        else if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame)  input = Vector3.back;
        else if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame)  input = Vector3.left;
        else if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) input = Vector3.right;

        if (input == Vector3.zero) return;

        Vector3 dir = ToWorldDirection(input);
        Platform target = FindPlatformInDirection(dir);
        if (target != null)
        {
            Hopped?.Invoke();
            StartCoroutine(HopTo(target));
        }
        else if (faceHopDirection && turnWhenBlocked)
        {
            StartCoroutine(TurnInPlace(FacingRotation(dir)));
        }
    }

    // Converts a WASD direction into a flat world direction (optionally relative to the camera).
    Vector3 ToWorldDirection(Vector3 input)
    {
        if (!cameraRelative || cameraTransform == null) return input;

        Vector3 forward = cameraTransform.forward; forward.y = 0f;
        Vector3 right   = cameraTransform.right;   right.y = 0f;

        // Camera looking straight down: its forward flattens to nothing, so use its up instead.
        if (forward.sqrMagnitude < 0.001f) { forward = cameraTransform.up; forward.y = 0f; }

        forward.Normalize();
        right.Normalize();
        return (forward * input.z + right * input.x).normalized;
    }

    // Rotation that makes the model look along a flat world direction.
    Quaternion FacingRotation(Vector3 dir)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return transform.rotation;
        return Quaternion.LookRotation(dir.normalized, Vector3.up) * Quaternion.Euler(0f, modelYawOffset, 0f);
    }

    // Horizontal world-space vector from the pivot to the feet, for a given rotation.
    Vector3 FeetVector(Quaternion rot)
    {
        Vector3 f = rot * Vector3.Scale(feetOffset, transform.lossyScale);
        f.y = 0f;
        return f;
    }

    // True while a hop or turn is in progress (used by Enemy).
    public bool IsHopping => isHopping;

    // Where the feet are in the world right now (used by Enemy).
    public Vector3 FeetWorldPosition()
    {
        return transform.position + FeetVector(transform.rotation);
    }

    // Closest platform that lies roughly in the pressed direction.
    Platform FindPlatformInDirection(Vector3 dir)
    {
        Vector3 from = current != null ? current.TopCenter : FeetWorldPosition();
        float minDot = Mathf.Cos(searchConeAngle * Mathf.Deg2Rad);

        Platform best = null;
        float bestDist = float.MaxValue;

        foreach (Platform p in Platform.All)
        {
            if (p == current) continue;

            Vector3 offset = p.TopCenter - from;
            offset.y = 0f; // compare on the ground plane only, so stack height doesn't matter
            float dist = offset.magnitude;
            if (dist < 0.01f || dist > maxHopDistance) continue;

            if (Vector3.Dot(offset / dist, dir) < minDot) continue; // not in that direction

            if (dist < bestDist)
            {
                bestDist = dist;
                best = p;
            }
        }
        return best;
    }

    Platform FindNearestPlatform()
    {
        Vector3 feet = FeetWorldPosition();
        Platform best = null;
        float bestDist = float.MaxValue;
        foreach (Platform p in Platform.All)
        {
            Vector3 offset = p.TopCenter - feet;
            offset.y = 0f;
            if (offset.sqrMagnitude < bestDist)
            {
                bestDist = offset.sqrMagnitude;
                best = p;
            }
        }
        return best;
    }

    IEnumerator HopTo(Platform target)
    {
        isHopping = true;

        // Face the actual hop direction (tile to tile), not just the key pressed.
        Vector3 from = current != null ? current.TopCenter : FeetWorldPosition();
        Quaternion startRot = transform.rotation;
        Quaternion endRot = faceHopDirection ? FacingRotation(target.TopCenter - from) : startRot;

        Vector3 start = transform.position;
        Vector3 end = LandingPoint(target, endRot); // computed with the FINAL rotation so the feet land centred
        float t = 0f;

        while (t < hopDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / hopDuration);

            // Finish turning in the first half of the hop.
            transform.rotation = Quaternion.Slerp(startRot, endRot, Mathf.Clamp01(k * 2f));

            Vector3 pos = Vector3.Lerp(start, end, k);
            pos.y += Mathf.Sin(k * Mathf.PI) * hopHeight; // arc
            transform.position = pos;

            yield return null;
        }

        // Final snap guarantees perfect centering and facing, even if a frame overshot.
        current = target;
        transform.rotation = endRot;
        SnapTo(target);
        isHopping = false;
    }

    // Turns on the spot, pivoting around the feet so they stay on the tile centre.
    IEnumerator TurnInPlace(Quaternion endRot)
    {
        isHopping = true;

        Vector3 feet = FeetWorldPosition();
        float y = transform.position.y;
        Quaternion startRot = transform.rotation;
        float t = 0f;

        while (t < turnDuration)
        {
            t += Time.deltaTime;
            Quaternion rot = Quaternion.Slerp(startRot, endRot, Mathf.Clamp01(t / turnDuration));
            transform.rotation = rot;
            Vector3 pos = feet - FeetVector(rot);
            pos.y = y;
            transform.position = pos;
            yield return null;
        }

        transform.rotation = endRot;
        Vector3 final = feet - FeetVector(endRot);
        final.y = y;
        transform.position = final;
        isHopping = false;
    }

    void SnapTo(Platform p)
    {
        transform.position = LandingPoint(p, transform.rotation);
    }

    // Pivot position that puts the FEET on the top-center of the platform, for a given rotation.
    Vector3 LandingPoint(Platform p, Quaternion rot)
    {
        return p.TopCenter + Vector3.up * footHeight - FeetVector(rot);
    }

    // Distance from the pivot down to the lowest point of the model.
    // Cube (centre pivot) -> half its height. Crested_Walker (feet pivot) -> ~0.
    float GetFootHeight()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) return transform.position.y - col.bounds.min.y;

        Renderer rend = GetComponentInChildren<Renderer>();
        if (rend != null) return transform.position.y - rend.bounds.min.y;

        return transform.lossyScale.y * 0.5f;
    }

    // Finds the centre of the model's lowest vertices (the feet), relative to the pivot,
    // in this object's local space so it follows rotation and scale.
    // In a build, the model needs Read/Write enabled in its import settings for this to work.
    Vector3 ComputeFeetOffset()
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
        float band = minY + (maxY - minY) * 0.05f; // bottom 5% of the model = feet

        Vector3 lo = Vector3.one * float.MaxValue, hi = Vector3.one * float.MinValue;
        foreach (Vector3 v in verts)
        {
            if (v.y > band) continue;
            lo = Vector3.Min(lo, v);
            hi = Vector3.Max(hi, v);
        }
        Vector3 feetMeshSpace = (lo + hi) * 0.5f;

        // mesh space -> world -> this object's local space
        Vector3 world = mf.transform.TransformPoint(feetMeshSpace) - transform.position;
        world.y = 0f;
        return transform.InverseTransformVector(world);
    }

#if UNITY_EDITOR
    // Red sphere = where the script thinks the feet are. It should sit on the yellow tile marker.
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = Color.red;
        Vector3 f = FeetWorldPosition();
        f.y = transform.position.y - footHeight;
        Gizmos.DrawSphere(f, 0.08f);
    }
#endif
}