using UnityEngine;
using UnityEngine.SceneManagement;

// Restarts the current level. By default the GameTimer keeps its time across the restart.
// Use from any script:  LevelRestarter.Restart();
public static class LevelRestarter
{
    public static void Restart(bool keepTimer = true)
    {
        if (keepTimer)
        {
            GameTimer timer = Object.FindAnyObjectByType<GameTimer>();
            if (timer != null) GameTimer.CarryOverTime(timer.Elapsed);
        }

        Platform.ResetChangedCount();

        Scene scene = SceneManager.GetActiveScene();
        if (scene.buildIndex >= 0) SceneManager.LoadScene(scene.buildIndex);
        else SceneManager.LoadScene(scene.name);
    }
}
