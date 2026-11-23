using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TitleScreen : MonoBehaviour
{
    public Button playButton;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        playButton.onClick.AddListener(PlayGame);
    }

    void PlayGame()
    {
        // Load the game scene
        SceneManager.LoadScene("Level 1");
    }
}
