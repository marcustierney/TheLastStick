using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorTrigger : MonoBehaviour
{
    static UpdateHealth FindPlayerHealth()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null)
            {
                continue;
            }

            UpdateHealth health = players[i].GetComponent<UpdateHealth>();
            if (health != null)
            {
                return health;
            }
        }

        return null;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("BossDoor"))
        {
            if (SceneManager.GetActiveScene().name == "LevelOne")
            {
                LevelRunStats.Instance?.EmitLevelCompleted(FindPlayerHealth(), "LevelOne");
                GameAnalytics.FlushIfReady();
                SceneManager.LoadScene("TransitionBossOne");
            }
            else if (SceneManager.GetActiveScene().name == "LevelTwo")
            {
                LevelRunStats.Instance?.EmitLevelCompleted(FindPlayerHealth(), "LevelTwo");
                GameAnalytics.FlushIfReady();
                SceneManager.LoadScene("TransitionLevelTwoBoss");
            }
        }
        if (other.CompareTag("LevelOneDoor"))
        {
            LevelRunStats.Instance?.EmitLevelCompleted(FindPlayerHealth(), AnalyticsKeys.SceneTutorial);
            GameAnalytics.FlushIfReady();
            PlayerPrefs.SetInt("CurrentLevel", 1);
            PlayerPrefs.Save();
            SceneManager.LoadScene("LevelOne");
        }
        if (other.CompareTag("MenuDoor"))
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}
