using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ResetPosition : MonoBehaviour
{
    public FlyingSwordEnemy swordOne;
    public FlyingSwordEnemy swordTwo;
    public FlyingSwordEnemy swordThree;
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (string.Equals(SceneManager.GetActiveScene().name, AnalyticsKeys.SceneLevelTwo, StringComparison.Ordinal))
            {
                LevelRunStats.Instance?.RegisterLevelTwoFallReset();
            }

            collision.gameObject.transform.position = new Vector3(-0, -2, 0);
            if (swordOne != null)
            {
                swordOne.ForceDespawn();
            }

            if (swordTwo != null)
            {
                swordTwo.ForceDespawn();
            }

            if (swordThree != null)
            {
                swordThree.ForceDespawn();
            }

            print("fall");
            Rigidbody2D rb = collision.gameObject.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }
    }
}
