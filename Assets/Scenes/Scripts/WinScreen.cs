
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class WinScreen : MonoBehaviour
{
    [SerializeField] private string nextLevel;

    public Button nextLevelButton;
    public Button menuButton;

    void Start()
    {
        nextLevelButton.onClick.AddListener(NextLevel);
        menuButton.onClick.AddListener(ShowMenu);

        gameObject.SetActive(false);
    }

    void NextLevel()
    {
        SceneManager.LoadScene(nextLevel);
    }

    void ShowMenu()
    {
        SceneManager.LoadScene("TitleScreen");
    }
}
