using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private GameInput gameInput;
    [SerializeField] private PlayerCombatController playerCombatController;

    [Header("Debug")]
    [SerializeField] private bool enableAnimationDebugLogs = true;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (playerHealth != null)
            playerHealth.OnTakeDamage += TakeDamage;
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnTakeDamage -= TakeDamage;
    }

    private void Update()
    {
        if (gameInput == null || animator == null || spriteRenderer == null)
            return;

        UpdateFacingVisual();
        UpdateRunningAnimation();
    }

    private void ResolveReferences()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (gameInput == null)
            gameInput = FindFirstObjectByType<GameInput>();

        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        if (playerCombatController == null)
            playerCombatController = GetComponent<PlayerCombatController>();
    }

    private void TakeDamage()
    {
        if (animator == null)
            return;

        DebugLog("TakeDamage() -> Trigger Hit");
        animator.SetTrigger("Hit");
    }

    private void UpdateFacingVisual()
    {
        if (playerCombatController != null && playerCombatController.ShouldLockVisualFacing)
        {
            spriteRenderer.flipX = playerCombatController.CurrentAttackSide == PlayerAttackSide.Left;
            return;
        }

        Vector2 move = gameInput.MoveVector;

        if (move.x > 0.01f)
            spriteRenderer.flipX = false;
        else if (move.x < -0.01f)
            spriteRenderer.flipX = true;
    }

    private void UpdateRunningAnimation()
    {
        Vector2 move = gameInput.MoveVector;
        bool isRunning = move.magnitude > 0.01f;

        if (playerCombatController != null && playerCombatController.IsAttackInProgress)
            isRunning = false;

        animator.SetBool("IsRunning", isRunning);
    }

    public void PlayComboAttack(int animationComboIndex, PlayerAttackSide attackSide)
    {
        if (animator == null)
            return;

        if (spriteRenderer != null)
            spriteRenderer.flipX = attackSide == PlayerAttackSide.Left;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        DebugLog(
            $"PlayComboAttack({animationComboIndex}) | side={attackSide} | " +
            $"beforeStateHash={stateInfo.shortNameHash} | normalizedTime={stateInfo.normalizedTime:F2}"
        );

        animator.SetBool("IsRunning", false);
        animator.SetBool("IsHeavyCharging", false);
        animator.ResetTrigger("HeavyAttack");
        animator.ResetTrigger("Attack");
        animator.SetInteger("AttackCombo", Mathf.Max(1, animationComboIndex));
        animator.SetTrigger("Attack");

        DebugLog($"Animator params set | AttackCombo={Mathf.Max(1, animationComboIndex)} | Trigger Attack");
    }

    public void PlayHeavyCharge(PlayerAttackSide attackSide)
    {
        if (animator == null)
            return;

        if (spriteRenderer != null)
            spriteRenderer.flipX = attackSide == PlayerAttackSide.Left;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        DebugLog(
            $"PlayHeavyCharge() | side={attackSide} | " +
            $"beforeStateHash={stateInfo.shortNameHash} | normalizedTime={stateInfo.normalizedTime:F2}"
        );

        animator.SetBool("IsRunning", false);
        animator.ResetTrigger("Attack");
        animator.ResetTrigger("HeavyAttack");
        animator.SetInteger("AttackCombo", 0);
        animator.SetBool("IsHeavyCharging", true);
    }

    public void PlayHeavyAttack(PlayerAttackSide attackSide)
    {
        if (animator == null)
            return;

        if (spriteRenderer != null)
            spriteRenderer.flipX = attackSide == PlayerAttackSide.Left;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        DebugLog(
            $"PlayHeavyAttack() | side={attackSide} | " +
            $"beforeStateHash={stateInfo.shortNameHash} | normalizedTime={stateInfo.normalizedTime:F2}"
        );

        animator.SetBool("IsRunning", false);
        animator.SetBool("IsHeavyCharging", false);
        animator.ResetTrigger("Attack");
        animator.SetInteger("AttackCombo", 0);
        animator.ResetTrigger("HeavyAttack");
        animator.SetTrigger("HeavyAttack");
    }

    public void ResetCombatAnimationState()
    {
        if (animator == null)
            return;

        DebugLog("ResetCombatAnimationState() | reset Attack/HeavyAttack triggers, AttackCombo=0, IsHeavyCharging=false");
        animator.ResetTrigger("Attack");
        animator.ResetTrigger("HeavyAttack");
        animator.SetInteger("AttackCombo", 0);
        animator.SetBool("IsHeavyCharging", false);
    }

    public void PlayBasicAttack(PlayerAttackSide attackSide)
    {
        PlayComboAttack(1, attackSide);
    }

    // Animation Events должны приходить только сюда.
    // Этот компонент форвардит их в PlayerCombatController через методы,
    // которые НЕ начинаются с "AnimationEvent_".
    public void AnimationEvent_OpenCurrentAttackHitbox()
    {
        DebugLog("AnimationEvent_OpenCurrentAttackHitbox()");

        if (playerCombatController != null)
            playerCombatController.HandleAnimationEvent_OpenCurrentAttackHitbox();
    }

    public void AnimationEvent_CloseCurrentAttackHitbox()
    {
        DebugLog("AnimationEvent_CloseCurrentAttackHitbox()");

        if (playerCombatController != null)
            playerCombatController.HandleAnimationEvent_CloseCurrentAttackHitbox();
    }

    public void AnimationEvent_OpenComboInputWindow()
    {
        DebugLog("AnimationEvent_OpenComboInputWindow()");

        if (playerCombatController != null)
            playerCombatController.HandleAnimationEvent_OpenComboInputWindow();
    }

    public void AnimationEvent_CloseComboInputWindow()
    {
        DebugLog("AnimationEvent_CloseComboInputWindow()");

        if (playerCombatController != null)
            playerCombatController.HandleAnimationEvent_CloseComboInputWindow();
    }

    public void AnimationEvent_EndCurrentAttackStep()
    {
        DebugLog("AnimationEvent_EndCurrentAttackStep()");

        if (playerCombatController != null)
            playerCombatController.HandleAnimationEvent_EndCurrentAttackStep();
    }

    public void AnimationEvent_OpenBasicAttackHitbox()
    {
        AnimationEvent_OpenCurrentAttackHitbox();
    }

    public void AnimationEvent_CloseBasicAttackHitbox()
    {
        AnimationEvent_CloseCurrentAttackHitbox();
    }

    public void AnimationEvent_EndBasicAttack()
    {
        AnimationEvent_EndCurrentAttackStep();
    }

    private void DebugLog(string message)
    {
        if (!enableAnimationDebugLogs)
            return;

        Debug.Log($"[PlayerAnimation DEBUG] {message}", this);
    }
}