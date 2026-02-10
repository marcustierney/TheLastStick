using UnityEngine;
using UnityEngine.SceneManagement;

public class BossDoorTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("BossDoor"))
        {
            SceneManager.LoadScene("LevelOneBoss");
        }
    }
}
