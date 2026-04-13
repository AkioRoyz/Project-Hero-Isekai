using UnityEngine;

public class EnemyAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private EnemyController enemyController;

    [Header("Debug")]
    [SerializeField] private bool enableAnimationDebugLogs = true;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Update()
    {
        ResolveReferences();

        UpdateFacingVisual();
        UpdateMovementAnimation();
    }

    private void ResolveReferences()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (enemyController == null)
            enemyController = GetComponent<EnemyController>();
    }

    private void UpdateFacingVisual()
    {
        if (spriteRenderer == null || enemyController == null)
            return;

        spriteRenderer.flipX = enemyController.IsFacingLeft;
    }

    private void UpdateMovementAnimation()
    {
        if (animator == null || enemyController == null)
            return;

        animator.SetBool("IsMoving", enemyController.IsMovingAnimationDesired);
    }

    public void PlayWindup()
    {
        if (animator == null)
            return;

        DebugLog("PlayWindup()");
        animator.SetBool("IsMoving", false);
        animator.ResetTrigger("Hit");
        animator.ResetTrigger("Attack");
        animator.ResetTrigger("Stun");
        animator.SetTrigger("Windup");
    }

    public void PlayAttack()
    {
        if (animator == null)
            return;

        DebugLog("PlayAttack()");
        animator.SetBool("IsMoving", false);
        animator.ResetTrigger("Hit");
        animator.ResetTrigger("Windup");
        animator.ResetTrigger("Stun");
        animator.SetTrigger("Attack");
    }

    public void PlayHit()
    {
        if (animator == null)
            return;

        DebugLog("PlayHit()");
        animator.SetBool("IsMoving", false);
        animator.ResetTrigger("Windup");
        animator.ResetTrigger("Attack");
        animator.ResetTrigger("Stun");
        animator.SetTrigger("Hit");
    }

    public void PlayStun()
    {
        if (animator == null)
            return;

        DebugLog("PlayStun()");
        animator.SetBool("IsMoving", false);
        animator.ResetTrigger("Hit");
        animator.ResetTrigger("Windup");
        animator.ResetTrigger("Attack");
        animator.SetTrigger("Stun");
    }

    public void PlayDeath()
    {
        if (animator == null)
            return;

        DebugLog("PlayDeath()");
        animator.SetBool("IsMoving", false);
        animator.ResetTrigger("Hit");
        animator.ResetTrigger("Windup");
        animator.ResetTrigger("Attack");
        animator.ResetTrigger("Stun");
        animator.SetTrigger("Death");
    }

    public void AnimationEvent_OpenAttackHitbox()
    {
        DebugLog("AnimationEvent_OpenAttackHitbox()");

        if (enemyController != null)
            enemyController.HandleAnimationEvent_OpenAttackHitbox();
    }

    public void AnimationEvent_CloseAttackHitbox()
    {
        DebugLog("AnimationEvent_CloseAttackHitbox()");

        if (enemyController != null)
            enemyController.HandleAnimationEvent_CloseAttackHitbox();
    }

    public void AnimationEvent_EndAttack()
    {
        DebugLog("AnimationEvent_EndAttack()");

        if (enemyController != null)
            enemyController.HandleAnimationEvent_EndAttack();
    }

    private void DebugLog(string message)
    {
        if (!enableAnimationDebugLogs)
            return;

        Debug.Log($"[EnemyAnimation DEBUG] {message}", this);
    }
}