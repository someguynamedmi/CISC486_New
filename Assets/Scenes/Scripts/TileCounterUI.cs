
using TMPro;
using UnityEngine;

public class TileCounterUI : MonoBehaviour
{
    [SerializeField] private TMP_Text counterText;

    void Awake()
    {
        if (counterText == null)
            counterText = GetComponent<TMP_Text>();
    }

    void Update()
    {
        int total = Platform.All.FindAll(
            p => p != null && p.CountAsRegularTile
        ).Count;
        int changed = Platform.ChangedCount;

        if (counterText != null)
        {
            counterText.text =
                "Tiles Changed: " + changed + " / " + total;
        }
    }
}
