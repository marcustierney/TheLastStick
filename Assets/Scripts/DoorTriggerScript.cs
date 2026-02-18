using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("BossDoor"))
        {
            SceneManager.LoadScene("LevelOneBoss");
        }
        if (other.CompareTag("LevelOneDoor"))
        {
            SceneManager.LoadScene("LevelOne");
        }
    }
}
