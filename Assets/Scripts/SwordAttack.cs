using System.Collections;
using UnityEngine;

public class SwordAttack : MonoBehaviour
{
    public GameObject swordHitbox;
    private float attackDuration = 0.2f;
    private Vector2 rightOffset = new Vector2(0.8f, 0f);
    private Vector2 leftOffset = new Vector2(-0.8f, 0f);
    private bool isAttacking;
    private Movement movement;

    private void Awake()
    {
        movement = GetComponent<Movement>();
    }

    void Update()
    {
        if (isAttacking) return;

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            StartCoroutine(Attack(true));
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            StartCoroutine(Attack(false));
        }
    }

    private IEnumerator Attack(bool attackRight)
    {
        isAttacking = true;

        bool facingRight = movement.FacingRight;

        if (attackRight) 
        {
            if (facingRight)
            {
                swordHitbox.transform.localPosition = rightOffset;
            }
            else
            {
                swordHitbox.transform.localPosition = leftOffset;
            }
        }
        else //Left
        {
            if (facingRight)
            {
                swordHitbox.transform.localPosition = leftOffset;
            }
            else
            {
                swordHitbox.transform.localPosition = rightOffset;
            }
        }

        swordHitbox.SetActive(true);

        yield return new WaitForSeconds(attackDuration);

        swordHitbox.SetActive(false);
        isAttacking = false;
    }


    public class SwordHitbox : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Enemy"))
            {
                Debug.Log("hit");
                //collision.GetComponent<Enemy>().TakeDamage(1);
            }
        }
    }
}