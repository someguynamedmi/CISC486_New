
using System.Collections.Generic;
using UnityEngine;

public class Platform : MonoBehaviour
{
    // Registry of every active platform.
    public static readonly List<Platform> All = new List<Platform>();

    // Tracks how many platforms have changed colour.
    public static int ChangedCount = 0;

    // Whether this platform should count toward the total.
    public bool CountAsRegularTile = true;

    private bool hasChanged = false;

    void OnEnable()
    {
        All.Add(this);
    }

    void OnDisable()
    {
        All.Remove(this);
    }

    // Center of the top face, in world space.
    public Vector3 TopCenter
    {
        get
        {
            Collider[] cols = GetComponentsInChildren<Collider>();

            if (cols.Length == 0)
                return transform.position;

            Bounds b = cols[0].bounds;

            for (int i = 1; i < cols.Length; i++)
                b.Encapsulate(cols[i].bounds);

            return new Vector3(b.center.x, b.max.y, b.center.z);
        }
    }

    // Turns the platform black when touched.
    public void TurnBlack()
    {
        if (hasChanged)
            return;

        hasChanged = true;

        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        foreach (Renderer rend in renderers)
        {
            rend.material.color = Color.black;
        }

        if (CountAsRegularTile)
        {
            ChangedCount++;
        }
    }

    // Resets the changed tile counter.
    public static void ResetChangedCount()
    {
        ChangedCount = 0;

        foreach (Platform platform in All)
        {
            if (platform != null)
                platform.hasChanged = false;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(TopCenter, 0.1f);
    }
#endif
}
