using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SwordAttack : MonoBehaviour
{
    private InputSystem_Actions inputActions;
    public GameObject swordHitbox;
    [SerializeField] private GameObject swordStandHitbox;
    [SerializeField] private GameObject dashAttackHitbox;
    private float attackDuration = 0.2f;
    private Vector2 rightOffset = new Vector2(0.8f, 0f);
    private Vector2 leftOffset = new Vector2(-0.8f, 0f);
    [SerializeField] private Vector2 dashAttackRightOffset = new Vector2(1.2f, 0f);
    private Vector2 downOffset = new Vector2(0f, -.8f);
    private bool isAttacking;
    private Movement movement;
    private bool isSwordStanding;
    public bool swordStandTouchGround;
    private bool usedSwordStand;
    private Animator animator;
    private bool isDashAttacking;
    private GameObject activeAttackHitbox;
    [SerializeField] private string dashAttackStateName = "Dash_Attack";
    [SerializeField] private string swordStandBoolName = "isSwordStanding";
    [SerializeField] private AudioSource attackAudioSource;
    [SerializeField] private int comboLength = 4;
    [SerializeField] private float comboResetDelay = 0.7f;
    [SerializeField] private string comboStepIntName = "attackComboStep";
    [SerializeField] private string comboTriggerName = "attackCombo";

    private int comboStepIndex;
    private float lastAttackInputTime = float.NegativeInfinity;
    private bool queuedComboAttack;
    private readonly HashSet<string> animatorParameterNames = new HashSet<string>();
    private bool wasDownInputHeld;

    public bool IsAttacking => isAttacking;
    public bool IsSwordStanding => isSwordStanding;


    private void Awake()
    {
        inputActions = new InputSystem_Actions();
        InputBindingOverrides.ApplySavedOverrides(inputActions.asset);
        movement = GetComponent<Movement>();
        animator = GetComponent<Animator>();
        CacheAnimatorParameters();

        comboLength = Mathf.Max(1, comboLength);
        comboResetDelay = Mathf.Max(0.01f, comboResetDelay);
    }

    private void OnEnable()
    {
        inputActions?.Gameplay.Enable();
    }

    private void OnDisable()
    {
        inputActions?.Gameplay.Disable();
    }

    void Update()
    {
        if (GameplayInputGate.BlocksGameplayActions)
        {
            return;
        }

        bool attackKeyPressed = inputActions.Gameplay.Attack.WasPressedThisFrame();
        if (attackKeyPressed)
        {
            lastAttackInputTime = Time.time;
        }

        if (!isAttacking && !isDashAttacking && Time.time - lastAttackInputTime > comboResetDelay)
        {
            comboStepIndex = 0;
        }

        if (isAttacking)
        {
            if (!isSwordStanding && !movement.IsDashing && attackKeyPressed)
            {
                queuedComboAttack = true;
            }
            return;
        }

        Vector2 moveInput = inputActions.Gameplay.Move.ReadValue<Vector2>();
        bool downInputPressed = IsDownAttackPressedThisFrame(moveInput);
        bool crouchInputPressed = inputActions.Gameplay.Crouch.WasPressedThisFrame();

        if (!movement.IsGrounded() && (downInputPressed || crouchInputPressed))
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

        if (movement.IsDashing && attackKeyPressed)
        {
            StartCoroutine(Attack());
            return;
        }

        bool facingRight = movement.FacingRight;
        if ((facingRight && attackKeyPressed) ||
            (!facingRight && attackKeyPressed))
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
        comboStepIndex = 0;
        queuedComboAttack = false;

        GameObject standHitbox = GetSwordStandHitbox();
        if (standHitbox == null)
        {
            yield break;
        }

        swordStandTouchGround = false;
        usedSwordStand = true;
        isAttacking = true;
        isSwordStanding = true;
        SetSwordStandAnimation(true);
        movement.CanMoveHorizontally = false;
        //move sword below player
        standHitbox.transform.localPosition = downOffset;
        //standHitbox.transform.localEulerAngles = new Vector3(0, 0, 90);
        //make sword standable
        standHitbox.SetActive(true);
        standHitbox.GetComponent<SwordHitbox>().EnablePlatform();
        Vector2 initialMoveInput = inputActions.Gameplay.Move.ReadValue<Vector2>();
        bool wasMoveCancelHeld = Mathf.Abs(initialMoveInput.x) > 0.5f;
        bool wasJumpHeld = initialMoveInput.y > 0.5f;

        while (isSwordStanding)
        {
            if (GameplayInputGate.BlocksGameplayActions)
            {
                yield return null;
                continue;
            }

            Vector2 moveInput = inputActions.Gameplay.Move.ReadValue<Vector2>();
            bool moveCancelHeld = Mathf.Abs(moveInput.x) > 0.5f;
            bool jumpHeld = moveInput.y > 0.5f;
            bool moveCancelPressed = moveCancelHeld && !wasMoveCancelHeld;
            bool jumpOffPressed = jumpHeld && !wasJumpHeld && swordStandTouchGround;
            wasMoveCancelHeld = moveCancelHeld;
            wasJumpHeld = jumpHeld;

            if (moveCancelPressed || jumpOffPressed)
            {
                ExitSwordStand();
            }

            yield return null;
        }
        //remove sword platform
        standHitbox.GetComponent<SwordHitbox>().Disable();
        standHitbox.SetActive(false);
        standHitbox.transform.localEulerAngles = Vector3.zero;
        SetSwordStandAnimation(false);
        isAttacking = false;
    }

    private void ExitSwordStand()
    {
        GameObject standHitbox = GetSwordStandHitbox();

        isSwordStanding = false;
        SetSwordStandAnimation(false);
        if (swordStandTouchGround)
        {
            movement.SwordJump();
        }
        if (standHitbox != null)
        {
            standHitbox.GetComponent<SwordHitbox>().Disable();
            standHitbox.SetActive(false);
            standHitbox.transform.localEulerAngles = Vector3.zero;
        }
        movement.CanMoveHorizontally = true;
        isAttacking = false;
    }

    private IEnumerator Attack()
    {
        isAttacking = true;
        PlayAttackSound();
        bool dashAttack = movement.IsDashing;
        isDashAttacking = dashAttack;
        queuedComboAttack = false;

        if (!dashAttack && Time.time - lastAttackInputTime > comboResetDelay)
        {
            comboStepIndex = 0;
        }

        int currentComboStep = comboStepIndex;
        if (!dashAttack)
        {
            comboStepIndex = (comboStepIndex + 1) % comboLength;
        }

        bool facingRight = movement.FacingRight;
        activeAttackHitbox = GetAttackHitbox(dashAttack);

        ApplyFacingToVisual(facingRight);

        if (!dashAttack)
        {
            movement.CanMoveHorizontally = false;
            Rigidbody2D rb = movement.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
        }
        
        // Trigger attack animation
        if (animator != null)
        {
            animator.SetBool("isAttacking", true);
            if (dashAttack && !string.IsNullOrEmpty(dashAttackStateName))
            {
                animator.CrossFadeInFixedTime(dashAttackStateName, 0.02f, 0, 0f);
            }
            else
            {
                TriggerComboAnimation(currentComboStep);
            }
        }

        DisableAttackHitbox(swordHitbox);
        DisableAttackHitbox(swordStandHitbox);
        DisableAttackHitbox(dashAttackHitbox);

        if (activeAttackHitbox != null)
        {
            PositionAttackHitbox(activeAttackHitbox, facingRight, dashAttack);
            activeAttackHitbox.SetActive(true);
            SwordHitbox hitbox = activeAttackHitbox.GetComponent<SwordHitbox>();
            if (hitbox != null)
            {
                hitbox.EnableAttack();
            }
        }

        if (dashAttack)
        {
            while (movement.IsDashing)
            {
                ApplyFacingToVisual(movement.FacingRight);
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(attackDuration);
        }

        DisableAttackHitbox(activeAttackHitbox);
        if (activeAttackHitbox != null)
        {
            activeAttackHitbox.transform.localPosition = Vector3.zero;
        }
        activeAttackHitbox = null;
        // End attack animation
        if (animator != null)
        {
            animator.SetBool("isAttacking", false);
        }

        if (!dashAttack)
        {
            movement.CanMoveHorizontally = true;
        }

        isDashAttacking = false;
        
        isAttacking = false;

        if (!dashAttack && queuedComboAttack && Time.time - lastAttackInputTime <= comboResetDelay)
        {
            StartCoroutine(Attack());
        }
    }

    private void LateUpdate()
    {
        if (!isDashAttacking)
        {
            return;
        }

        bool facingRight = movement.FacingRight;
        ApplyFacingToVisual(facingRight);

        if (activeAttackHitbox != null)
        {
            PositionAttackHitbox(activeAttackHitbox, facingRight, true);
        }
    }

    private GameObject GetAttackHitbox(bool dashAttack)
    {
        if (dashAttack && dashAttackHitbox != null)
        {
            return dashAttackHitbox;
        }

        return swordHitbox;
    }

    private void PositionAttackHitbox(GameObject hitboxObject, bool facingRight, bool dashAttack)
    {
        if (hitboxObject == null)
        {
            return;
        }

        Vector2 baseOffset = dashAttack ? dashAttackRightOffset : rightOffset;
        float facingSign = facingRight ? 1f : -1f;
        float scaleSign = Mathf.Sign(transform.localScale.x);
        if (scaleSign == 0f)
        {
            scaleSign = 1f;
        }

        float localX = Mathf.Abs(baseOffset.x) * facingSign * scaleSign;
        hitboxObject.transform.localPosition = new Vector2(localX, baseOffset.y);
    }

    private void DisableAttackHitbox(GameObject hitboxObject)
    {
        if (hitboxObject == null)
        {
            return;
        }

        SwordHitbox hitbox = hitboxObject.GetComponent<SwordHitbox>();
        if (hitbox != null)
        {
            hitbox.Disable();
        }
        else
        {
            hitboxObject.SetActive(false);
        }
    }

    private void ApplyFacingToVisual(bool facingRight)
    {
        Vector3 localScale = transform.localScale;
        float absX = Mathf.Abs(localScale.x);
        if (absX == 0f)
        {
            absX = 1f;
        }

        localScale.x = facingRight ? absX : -absX;
        transform.localScale = localScale;
    }

    public void ForceExitSwordStand()
    {
        if (!isSwordStanding) return;

        ExitSwordStand();
    }

    public void ForceExitSwordStandWithBounce()
    {
        if (!isSwordStanding) return;

        swordStandTouchGround = true;
        ExitSwordStand();
    }

    private void SetSwordStandAnimation(bool value)
    {
        if (animator == null || string.IsNullOrEmpty(swordStandBoolName))
        {
            return;
        }

        animator.SetBool(swordStandBoolName, value);
    }

    private GameObject GetSwordStandHitbox()
    {
        if (swordStandHitbox != null)
        {
            return swordStandHitbox;
        }

        return swordHitbox;
    }

    private void PlayAttackSound()
    {
        if (attackAudioSource == null)
        {
            return;
        }

        attackAudioSource.Play();
    }

    private void CacheAnimatorParameters()
    {
        animatorParameterNames.Clear();

        if (animator == null)
        {
            return;
        }

        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            animatorParameterNames.Add(parameters[i].name);
        }
    }

    private bool HasAnimatorParameter(string parameterName)
    {
        if (string.IsNullOrEmpty(parameterName))
        {
            return false;
        }

        return animatorParameterNames.Contains(parameterName);
    }

    private void TriggerComboAnimation(int comboStep)
    {
        if (animator == null)
        {
            return;
        }

        if (HasAnimatorParameter(comboStepIntName))
        {
            animator.SetInteger(comboStepIntName, comboStep + 1);
        }

        if (HasAnimatorParameter(comboTriggerName))
        {
            animator.SetTrigger(comboTriggerName);
        }
    }

    private bool IsDownAttackPressedThisFrame(Vector2 moveInput)
    {
        bool downInputHeld = moveInput.y < -0.5f;
        bool downInputPressed = downInputHeld && !wasDownInputHeld;
        wasDownInputHeld = downInputHeld;
        return downInputPressed;
    }
}