using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("BossDoor"))
        {
            if (SceneManager.GetActiveScene().name == "LevelOne")
            {
                SceneManager.LoadScene("TransitionBossOne");
            }
            else if (SceneManager.GetActiveScene().name == "LevelTwo")
            {
                SceneManager.LoadScene("TransitionLevelTwoBoss");
            }
        }
        if (other.CompareTag("LevelOneDoor"))
        {
            SceneManager.LoadScene("LevelOne");
        }
    }
}
