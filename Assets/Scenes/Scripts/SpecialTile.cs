
using UnityEngine;
using UnityEngine.SceneManagement;

public class SpecialTile : MonoBehaviour
{
    public enum TileType
    {
        Restart,
        Finish
    }

    [Header("Tile Settings")]
    [SerializeField] private TileType tileType;

    [Header("Finish Settings")]
    [SerializeField] private GameObject completionPanel;

    private bool activated = false;

    public void Activate()
    {
        if (activated)
            return;

        activated = true;

        if (tileType == TileType.Restart)
        {
            RestartLevel();
        }
        else if (tileType == TileType.Finish)
        {
            CompleteLevel();
        }
    }

    private void RestartLevel()
    {
        Debug.Log("Restarting Level!");

        Platform.ResetChangedCount();
        
        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }

    private void CompleteLevel()
    {
        Debug.Log("Level Completed!");

        if (completionPanel != null)
        {
            completionPanel.SetActive(true);
        }
    }
}
