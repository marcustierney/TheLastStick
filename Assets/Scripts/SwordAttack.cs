using System.Collections;
using UnityEngine;

public class SwordAttack : MonoBehaviour
{
    public GameObject swordHitbox;
    private float attackDuration = 0.2f;
    private Vector2 rightOffset = new Vector2(0.8f, 0f);
    private Vector2 leftOffset = new Vector2(-0.8f, 0f);
    private Vector2 downOffset = new Vector2(0f, -.9f);
    private bool isAttacking;
    private Movement movement;
    private bool isSwordStanding;
    public bool swordStandTouchGround;
    private bool usedSwordStand;
    private Animator animator;

    public bool IsAttacking => isAttacking;
    public bool IsSwordStanding => isSwordStanding;


    private void Awake()
    {
        movement = GetComponent<Movement>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (isAttacking) return;

        if (!movement.IsGrounded() && Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (!usedSwordStand)
            {
                StartCoroutine(SwordStand());
                return;
            }
            else if (usedSwordStand && swordStandTouchGround)
            {
                swordStandTouchGround = false;
                StartCoroutine(SwordStand());
                return;
            }
        }
        bool facingRight = movement.FacingRight;
        if ((facingRight && Input.GetKeyDown(KeyCode.RightArrow)) ||
            (!facingRight && Input.GetKeyDown(KeyCode.LeftArrow)))
        {
            StartCoroutine(Attack());
        }
        if (movement.IsGrounded())
        {
            usedSwordStand = false;
        }

    }

    private IEnumerator SwordStand()
    {
        usedSwordStand = true;
        isAttacking = true;
        isSwordStanding = true;
        movement.CanMoveHorizontally = false;
        //move sword below player
        swordHitbox.transform.localPosition = downOffset;
        swordHitbox.transform.localEulerAngles = new Vector3(0, 0, 90);
        //make sword standable
        swordHitbox.SetActive(true);
        swordHitbox.GetComponent<SwordHitbox>().EnablePlatform();

        while (isSwordStanding)
        {
            if (Input.GetKeyDown(KeyCode.Space)) 
            {
                ExitSwordStand();
            }

            yield return null;
        }
        //remove sword platform
        swordHitbox.GetComponent<SwordHitbox>().Disable();
        swordHitbox.SetActive(false);
        swordHitbox.transform.localEulerAngles = Vector3.zero;
        isAttacking = false;
    }

    private void ExitSwordStand()
    {
        isSwordStanding = false;
        movement.SwordJump();
        swordHitbox.GetComponent<SwordHitbox>().Disable();
        swordHitbox.SetActive(false);
        swordHitbox.transform.localEulerAngles = Vector3.zero;
        movement.CanMoveHorizontally = true;
        isAttacking = false;
    }

    private IEnumerator Attack()
    {
        isAttacking = true;
        movement.CanMoveHorizontally = false;
        Rigidbody2D rb = movement.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
        
        // Trigger attack animation
        if (animator != null)
        {
            animator.SetBool("isAttacking", true);
        }
        
        bool facingRight = movement.FacingRight;
        float facingSign = facingRight ? 1f : -1f;
        float scaleSign = Mathf.Sign(transform.localScale.x);
        if (scaleSign == 0f)
        {
            scaleSign = 1f;
        }
        float localX = Mathf.Abs(rightOffset.x) * facingSign * scaleSign;
        swordHitbox.transform.localPosition = new Vector2(localX, rightOffset.y);
        swordHitbox.SetActive(true);
        swordHitbox.GetComponent<SwordHitbox>().EnableAttack();

        yield return new WaitForSeconds(attackDuration);

        swordHitbox.GetComponent<SwordHitbox>().Disable();
        swordHitbox.SetActive(false);
        swordHitbox.transform.localPosition = Vector3.zero;
        // End attack animation
        if (animator != null)
        {
            animator.SetBool("isAttacking", false);
        }

        movement.CanMoveHorizontally = true;
        
        isAttacking = false;
    }
    public void ForceExitSwordStand()
    {
        if (!isSwordStanding) return;

        ExitSwordStand();
    }
}