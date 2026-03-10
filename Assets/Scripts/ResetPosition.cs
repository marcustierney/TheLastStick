using UnityEngine;

public class ResetPosition : MonoBehaviour
{
    public FlyingSwordEnemy swordOne;
    public FlyingSwordEnemy swordTwo;
    public FlyingSwordEnemy swordThree;
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.gameObject.transform.position = new Vector3(-0, -2, 0);
            swordOne.transform.position = new Vector3(-12, -2, 0);
            swordTwo.transform.position = new Vector3(-6, -2, 0);
            swordThree.transform.position = new Vector3(-7, 1, 0);
            print("fall");
            Rigidbody2D rb = collision.gameObject.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }
    }
}
