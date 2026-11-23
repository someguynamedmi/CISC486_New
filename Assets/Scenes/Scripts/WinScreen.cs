using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class WinScreen : MonoBehaviour
{
    [SerializeField] private string nextLevel;
    public Button nextLevelButton;
    public Button menuButton;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        nextLevelButton.onClick.AddListener(NextLevel);
        menuButton.onClick.AddListener(ShowMenu);
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
