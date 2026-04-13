using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCombatController : MonoBehaviour
{
    private enum AttackMode
    {
        None = 0,
        LightCombo = 1,
        HeavyCharge = 2,
        HeavyAttack = 3
    }

    [Header("Core References")]
    [SerializeField] private GameInput gameInput;
    [SerializeField] private PlayerAnimation playerAnimation;
    [SerializeField] private PlayerMoving playerMoving;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private StatsSystem statsSystem;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private PlayerTargetingSystem playerTargetingSystem;

    [Header("Attack Hitboxes")]
    [SerializeField] private PlayerCombatHitbox leftHitbox;
    [SerializeField] private PlayerCombatHitbox rightHitbox;

    [Header("Light Combo Settings")]
    [SerializeField] private PlayerComboStepDefinition[] comboSteps;
    [SerializeField] private bool lockMovementDuringAttack = true;
    [SerializeField] private float defaultAttackFailSafeDuration = 0.70f;
    [SerializeField] private float preWindowInputBufferDuration = 0.35f;
    [SerializeField] private bool allowRetargetBetweenComboSteps = true;

    [Header("Heavy Attack Settings")]
    [SerializeField] private float heavyChargeHoldDuration = 0.35f;
    [SerializeField] private float heavyAttackFailSafeDuration = 1.10f;
    [SerializeField] private string heavyAttackId = "player_heavy_01";
    [SerializeField] private float heavyStrengthDamageMultiplier = 1.75f;
    [SerializeField] private int heavyFlatDamageBonus = 3;
    [SerializeField] private float heavyRandomDamageMinMultiplier = 0.95f;
    [SerializeField] private float heavyRandomDamageMaxMultiplier = 1.15f;
    [SerializeField] private float heavyKnockbackForce = 0f;
    [SerializeField] private float heavyStunDuration = 0f;
    [SerializeField] private bool heavyCanBeBlocked = true;
    [SerializeField] private bool heavyCanBeParried = true;
    [SerializeField] private HitReactionType heavyHitReactionType = HitReactionType.Light;

    [Header("Facing")]
    [SerializeField] private Vector2 defaultFacingDirection = Vector2.right;
    [SerializeField] private float minHorizontalInputToChangeFacing = 0.05f;

    [Header("Debug")]
    [SerializeField] private bool enableCombatDebugLogs = true;

    private readonly HashSet<int> hitTargetIds = new HashSet<int>();

    private AttackMode currentAttackMode = AttackMode.None;

    private bool isHitboxOpen;
    private bool comboInputWindowOpen;
    private bool queuedNextComboStep;
    private bool preWindowBufferedInputActive;
    private float preWindowBufferedInputExpireTime;

    private bool neutralAttackPressPending;
    private float neutralAttackPressStartTime = -1f;

    private bool isAttackButtonHeld;
    private float attackButtonHoldStartTime = -1f;

    private bool pendingHeavyFollowUp;

    private int currentComboStepIndex = -1;

    private int lastProcessedEndEventFrame = -1;
    private int lastProcessedOpenHitboxEventFrame = -1;
    private int lastProcessedCloseHitboxEventFrame = -1;
    private int lastProcessedOpenComboWindowEventFrame = -1;
    private int lastProcessedCloseComboWindowEventFrame = -1;

    private Vector2 lastNonZeroMoveDirection = Vector2.right;
    private PlayerAttackSide lastFacingSide = PlayerAttackSide.Right;
    private PlayerAttackSide currentAttackSide = PlayerAttackSide.Right;

    private Coroutine attackFailSafeCoroutine;

    public event Action<int, PlayerAttackSide> OnComboStepStarted;
    public event Action<int, PlayerAttackSide> OnComboStepFinished;
    public event Action OnComboFinished;

    public bool IsAttackInProgress => currentAttackMode != AttackMode.None;
    public bool IsLightComboInProgress => currentAttackMode == AttackMode.LightCombo;
    public bool IsHeavyCharging => currentAttackMode == AttackMode.HeavyCharge;
    public bool IsHeavyAttackInProgress => currentAttackMode == AttackMode.HeavyAttack;

    public bool IsHitboxOpen => isHitboxOpen;
    public bool IsComboInputWindowOpen => comboInputWindowOpen;
    public bool HasQueuedNextComboStep => queuedNextComboStep;
    public bool ShouldLockVisualFacing => currentAttackMode != AttackMode.None;

    public int CurrentComboStepNumber => currentAttackMode == AttackMode.LightCombo && currentComboStepIndex >= 0
        ? currentComboStepIndex + 1
        : 0;

    public int CurrentComboStepIndex => currentAttackMode == AttackMode.LightCombo ? currentComboStepIndex : -1;

    public PlayerAttackSide CurrentAttackSide => currentAttackMode != AttackMode.None ? currentAttackSide : lastFacingSide;

    public Vector2 CurrentAttackDirection
    {
        get
        {
            return CurrentAttackSide == PlayerAttackSide.Left ? Vector2.left : Vector2.right;
        }
    }

    private void Reset()
    {
        EnsureDefaultComboStepsIfNeeded();
        SanitizeComboSteps();
    }

    private void OnValidate()
    {
        EnsureDefaultComboStepsIfNeeded();
        SanitizeComboSteps();

        defaultAttackFailSafeDuration = Mathf.Max(0.05f, defaultAttackFailSafeDuration);
        preWindowInputBufferDuration = Mathf.Max(0.01f, preWindowInputBufferDuration);
        heavyChargeHoldDuration = Mathf.Max(0.05f, heavyChargeHoldDuration);
        heavyAttackFailSafeDuration = Mathf.Max(0.05f, heavyAttackFailSafeDuration);
        minHorizontalInputToChangeFacing = Mathf.Max(0f, minHorizontalInputToChangeFacing);

        heavyRandomDamageMinMultiplier = Mathf.Max(0.01f, heavyRandomDamageMinMultiplier);
        heavyRandomDamageMaxMultiplier = Mathf.Max(heavyRandomDamageMinMultiplier, heavyRandomDamageMaxMultiplier);
    }

    private void Awake()
    {
        ResolveReferences();
        EnsureDefaultComboStepsIfNeeded();
        SanitizeComboSteps();
        DisableAllHitboxesImmediate();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (gameInput != null)
        {
            gameInput.OnAttackStarted += HandleAttackStarted;
            gameInput.OnAttackCanceled += HandleAttackCanceled;
        }

        if (playerHealth != null)
            playerHealth.OnDied += HandlePlayerDied;
    }

    private void OnDisable()
    {
        if (gameInput != null)
        {
            gameInput.OnAttackStarted -= HandleAttackStarted;
            gameInput.OnAttackCanceled -= HandleAttackCanceled;
        }

        if (playerHealth != null)
            playerHealth.OnDied -= HandlePlayerDied;

        ForceStopAllCombatState();
    }

    private void Update()
    {
        UpdateLastFacingFromInput();
        UpdateBufferedInputExpiration();
        UpdateNeutralAttackHoldDecision();
        UpdateHeavyFollowUpDecision();
    }

    private void ResolveReferences()
    {
        if (gameInput == null)
            gameInput = FindFirstObjectByType<GameInput>();

        if (playerAnimation == null)
            playerAnimation = GetComponent<PlayerAnimation>();

        if (playerMoving == null)
            playerMoving = GetComponent<PlayerMoving>();

        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        if (statsSystem == null)
            statsSystem = FindFirstObjectByType<StatsSystem>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (playerTargetingSystem == null)
            playerTargetingSystem = GetComponent<PlayerTargetingSystem>();
    }

    public void RegisterHitbox(PlayerCombatHitbox hitbox)
    {
        if (hitbox == null)
            return;

        if (hitbox.Side == PlayerAttackSide.Left)
            leftHitbox = hitbox;
        else
            rightHitbox = hitbox;
    }

    private void UpdateLastFacingFromInput()
    {
        if (gameInput == null)
            return;

        Vector2 move = gameInput.MoveVector;

        if (move.sqrMagnitude > 0.0001f)
            lastNonZeroMoveDirection = move.normalized;

        if (move.x > minHorizontalInputToChangeFacing)
            lastFacingSide = PlayerAttackSide.Right;
        else if (move.x < -minHorizontalInputToChangeFacing)
            lastFacingSide = PlayerAttackSide.Left;
    }

    private void UpdateBufferedInputExpiration()
    {
        if (!preWindowBufferedInputActive)
            return;

        if (Time.time > preWindowBufferedInputExpireTime)
        {
            DebugLog($"BUFFER EXPIRED | step={CurrentComboStepNumber}");
            ClearBufferedInput();
        }
    }

    private void UpdateNeutralAttackHoldDecision()
    {
        if (!neutralAttackPressPending)
            return;

        if (currentAttackMode != AttackMode.None)
            return;

        if (Time.time - neutralAttackPressStartTime < heavyChargeHoldDuration)
            return;

        DebugLog(
            $"NEUTRAL HOLD REACHED HEAVY THRESHOLD | heldFor={(Time.time - neutralAttackPressStartTime):F2} | " +
            $"threshold={heavyChargeHoldDuration:F2}"
        );

        CancelNeutralAttackPressCandidate();
        StartHeavyChargeFromNeutral();
    }

    private void UpdateHeavyFollowUpDecision()
    {
        TryArmHeavyFollowUpDuringLightCombo();
    }

    private void HandlePlayerDied()
    {
        DebugLog("PLAYER DIED -> ForceStopAllCombatState");
        ForceStopAllCombatState();
    }

    private void HandleAttackStarted()
    {
        BeginAttackButtonHold();

        DebugLog(
            $"INPUT AttackStarted | mode={currentAttackMode} | step={CurrentComboStepNumber} | " +
            $"windowOpen={comboInputWindowOpen} | queued={queuedNextComboStep} | buffered={preWindowBufferedInputActive} | " +
            $"neutralPending={neutralAttackPressPending} | held={isAttackButtonHeld}"
        );

        if (currentAttackMode == AttackMode.None)
        {
            BeginNeutralAttackPressCandidate();
            return;
        }

        if (currentAttackMode == AttackMode.LightCombo)
        {
            TryBufferComboContinuationInput();
            TryArmHeavyFollowUpDuringLightCombo();
            return;
        }

        DebugLog($"INPUT AttackStarted IGNORED | mode={currentAttackMode}");
    }

    private void HandleAttackCanceled()
    {
        float heldDuration = GetCurrentAttackButtonHeldDuration();
        EndAttackButtonHold();

        DebugLog(
            $"INPUT AttackCanceled | mode={currentAttackMode} | neutralPending={neutralAttackPressPending} | " +
            $"step={CurrentComboStepNumber} | heldDuration={heldDuration:F2} | pendingHeavyFollowUp={pendingHeavyFollowUp}"
        );

        if (currentAttackMode == AttackMode.HeavyCharge)
        {
            StartHeavyAttackFromCharge();
            return;
        }

        if (currentAttackMode == AttackMode.LightCombo)
        {
            if (pendingHeavyFollowUp)
            {
                DebugLog("HEAVY FOLLOW-UP DISARMED | button released before combo step ended");
                pendingHeavyFollowUp = false;
            }

            return;
        }

        if (neutralAttackPressPending && currentAttackMode == AttackMode.None)
        {
            CancelNeutralAttackPressCandidate();
            TryStartComboFromBeginning();
            return;
        }

        DebugLog($"INPUT AttackCanceled IGNORED | mode={currentAttackMode}");
    }

    private void BeginAttackButtonHold()
    {
        isAttackButtonHeld = true;
        attackButtonHoldStartTime = Time.time;
    }

    private void EndAttackButtonHold()
    {
        isAttackButtonHeld = false;
        attackButtonHoldStartTime = -1f;
    }

    private float GetCurrentAttackButtonHeldDuration()
    {
        if (!isAttackButtonHeld || attackButtonHoldStartTime < 0f)
            return 0f;

        return Time.time - attackButtonHoldStartTime;
    }

    private void BeginNeutralAttackPressCandidate()
    {
        if (neutralAttackPressPending)
        {
            DebugLog("BeginNeutralAttackPressCandidate IGNORED | already pending");
            return;
        }

        if (!CanStartAttackFromNeutral())
        {
            DebugLog("BeginNeutralAttackPressCandidate FAILED | CanStartAttackFromNeutral=false");
            return;
        }

        neutralAttackPressPending = true;
        neutralAttackPressStartTime = Time.time;

        DebugLog($"NEUTRAL ATTACK PRESS STORED | startTime={neutralAttackPressStartTime:F2}");
    }

    private void CancelNeutralAttackPressCandidate()
    {
        neutralAttackPressPending = false;
        neutralAttackPressStartTime = -1f;
    }

    public bool TryStartComboFromBeginning()
    {
        DebugLog("TryStartComboFromBeginning()");
        CancelNeutralAttackPressCandidate();
        return StartComboStep(0, true);
    }

    private bool StartComboStep(int stepIndex, bool fromNeutral)
    {
        if (!IsValidComboStepIndex(stepIndex))
        {
            DebugLog($"StartComboStep FAILED | invalid stepIndex={stepIndex}");
            return false;
        }

        if (fromNeutral && !CanStartAttackFromNeutral())
        {
            DebugLog($"StartComboStep FAILED | step={stepIndex + 1} | CanStartAttackFromNeutral=false");
            return false;
        }

        PlayerComboStepDefinition step = comboSteps[stepIndex];
        if (step == null)
        {
            DebugLog($"StartComboStep FAILED | step={stepIndex + 1} | step definition is null");
            return false;
        }

        step.Sanitize();

        StopAttackFailSafeTimer();
        CancelNeutralAttackPressCandidate();
        ResetAnimationEventGuards();

        currentAttackMode = AttackMode.LightCombo;
        currentComboStepIndex = stepIndex;

        if (fromNeutral || allowRetargetBetweenComboSteps)
            currentAttackSide = ResolveAttackSideForNewAttack();

        isHitboxOpen = false;
        comboInputWindowOpen = false;
        queuedNextComboStep = false;
        pendingHeavyFollowUp = false;

        ClearBufferedInput();
        hitTargetIds.Clear();
        DisableAllHitboxesImmediate();

        if (lockMovementDuringAttack && playerMoving != null)
            playerMoving.SetExternalMovementBlocked(true);

        float failSafe = GetResolvedFailSafeDuration(step);
        StartAttackFailSafeTimer(failSafe);

        DebugLog(
            $"START LIGHT STEP {stepIndex + 1} | animCombo={step.animationComboIndex} | attackId={step.attackId} | " +
            $"side={currentAttackSide} | failSafe={failSafe:F2}"
        );

        if (playerAnimation != null)
            playerAnimation.PlayComboAttack(step.animationComboIndex, currentAttackSide);

        OnComboStepStarted?.Invoke(currentComboStepIndex, currentAttackSide);
        return true;
    }

    private bool StartHeavyChargeFromNeutral()
    {
        if (!CanStartAttackFromNeutral())
        {
            DebugLog("StartHeavyChargeFromNeutral FAILED | CanStartAttackFromNeutral=false");
            return false;
        }

        return EnterHeavyChargeState(resolveNewSide: true, reason: "neutral hold");
    }

    private bool StartHeavyChargeFromComboFollowUp()
    {
        if (currentAttackMode != AttackMode.LightCombo)
        {
            DebugLog($"StartHeavyChargeFromComboFollowUp FAILED | currentMode={currentAttackMode}");
            return false;
        }

        return EnterHeavyChargeState(resolveNewSide: false, reason: "combo follow-up");
    }

    private bool EnterHeavyChargeState(bool resolveNewSide, string reason)
    {
        StopAttackFailSafeTimer();
        CancelNeutralAttackPressCandidate();
        ResetAnimationEventGuards();

        currentAttackMode = AttackMode.HeavyCharge;
        currentComboStepIndex = -1;

        if (resolveNewSide)
            currentAttackSide = ResolveAttackSideForNewAttack();

        isHitboxOpen = false;
        comboInputWindowOpen = false;
        queuedNextComboStep = false;
        pendingHeavyFollowUp = false;

        ClearBufferedInput();
        hitTargetIds.Clear();
        DisableAllHitboxesImmediate();

        if (lockMovementDuringAttack && playerMoving != null)
            playerMoving.SetExternalMovementBlocked(true);

        DebugLog($"START HEAVY CHARGE | reason={reason} | side={currentAttackSide}");

        if (playerAnimation != null)
            playerAnimation.PlayHeavyCharge(currentAttackSide);

        return true;
    }

    private bool StartHeavyAttackFromCharge()
    {
        if (currentAttackMode != AttackMode.HeavyCharge)
        {
            DebugLog($"StartHeavyAttackFromCharge FAILED | currentMode={currentAttackMode}");
            return false;
        }

        StopAttackFailSafeTimer();
        ResetAnimationEventGuards();

        currentAttackMode = AttackMode.HeavyAttack;
        currentComboStepIndex = -1;

        isHitboxOpen = false;
        comboInputWindowOpen = false;
        queuedNextComboStep = false;
        pendingHeavyFollowUp = false;

        ClearBufferedInput();
        hitTargetIds.Clear();
        DisableAllHitboxesImmediate();

        StartAttackFailSafeTimer(heavyAttackFailSafeDuration);

        DebugLog(
            $"START HEAVY ATTACK | side={currentAttackSide} | attackId={heavyAttackId} | " +
            $"failSafe={heavyAttackFailSafeDuration:F2}"
        );

        if (playerAnimation != null)
            playerAnimation.PlayHeavyAttack(currentAttackSide);

        return true;
    }

    private bool CanStartAttackFromNeutral()
    {
        if (!enabled || !gameObject.activeInHierarchy)
            return false;

        if (currentAttackMode != AttackMode.None)
            return false;

        if (playerHealth != null && !playerHealth.IsAlive)
            return false;

        if (comboSteps == null || comboSteps.Length == 0)
            return false;

        return true;
    }

    private void TryBufferComboContinuationInput()
    {
        if (currentAttackMode != AttackMode.LightCombo)
            return;

        if (!HasNextComboStep())
        {
            DebugLog($"BUFFER INPUT IGNORED | no next combo step after step={CurrentComboStepNumber}");
            return;
        }

        if (comboInputWindowOpen)
        {
            if (!queuedNextComboStep)
            {
                queuedNextComboStep = true;
                DebugLog($"QUEUE NEXT STEP IMMEDIATE | currentStep={CurrentComboStepNumber} -> nextStep={CurrentComboStepNumber + 1}");
            }
            else
            {
                DebugLog($"QUEUE NEXT STEP IGNORED | already queued | currentStep={CurrentComboStepNumber}");
            }

            return;
        }

        preWindowBufferedInputActive = true;
        preWindowBufferedInputExpireTime = Time.time + Mathf.Max(0.01f, preWindowInputBufferDuration);

        DebugLog(
            $"BUFFER INPUT STORED | currentStep={CurrentComboStepNumber} | expiresAt={preWindowBufferedInputExpireTime:F2} | " +
            $"bufferDuration={preWindowInputBufferDuration:F2}"
        );
    }

    private bool TryArmHeavyFollowUpDuringLightCombo()
    {
        if (currentAttackMode != AttackMode.LightCombo)
            return false;

        if (pendingHeavyFollowUp)
            return false;

        if (!isAttackButtonHeld)
            return false;

        if (HasNextComboStep())
            return false;

        float heldDuration = GetCurrentAttackButtonHeldDuration();
        if (heldDuration < heavyChargeHoldDuration)
            return false;

        pendingHeavyFollowUp = true;

        DebugLog(
            $"HEAVY FOLLOW-UP ARMED | step={CurrentComboStepNumber} | heldDuration={heldDuration:F2} | " +
            $"threshold={heavyChargeHoldDuration:F2}"
        );

        return true;
    }

    private PlayerAttackSide ResolveAttackSideForNewAttack()
    {
        PlayerAttackSide targetFallbackSide = lastFacingSide;

        if (playerTargetingSystem != null &&
            playerTargetingSystem.TryGetPreferredAttackSide(targetFallbackSide, out PlayerAttackSide targetedSide))
        {
            DebugLog($"ResolveAttackSide -> targeting picked {targetedSide}");
            return targetedSide;
        }

        if (gameInput != null)
        {
            Vector2 move = gameInput.MoveVector;

            if (move.x > minHorizontalInputToChangeFacing)
                return PlayerAttackSide.Right;

            if (move.x < -minHorizontalInputToChangeFacing)
                return PlayerAttackSide.Left;
        }

        if (lastNonZeroMoveDirection.x > minHorizontalInputToChangeFacing)
            return PlayerAttackSide.Right;

        if (lastNonZeroMoveDirection.x < -minHorizontalInputToChangeFacing)
            return PlayerAttackSide.Left;

        if (spriteRenderer != null)
            return spriteRenderer.flipX ? PlayerAttackSide.Left : PlayerAttackSide.Right;

        return defaultFacingDirection.x < 0f ? PlayerAttackSide.Left : PlayerAttackSide.Right;
    }

    public void HandleAnimationEvent_OpenCurrentAttackHitbox()
    {
        if (currentAttackMode != AttackMode.LightCombo && currentAttackMode != AttackMode.HeavyAttack)
        {
            DebugLog($"EVENT OpenHitbox IGNORED | mode={currentAttackMode}");
            return;
        }

        if (lastProcessedOpenHitboxEventFrame == Time.frameCount)
        {
            DebugLog($"EVENT OpenHitbox IGNORED | duplicate same frame | frame={Time.frameCount}");
            return;
        }

        lastProcessedOpenHitboxEventFrame = Time.frameCount;
        isHitboxOpen = true;

        PlayerCombatHitbox activeHitbox = GetActiveHitboxForCurrentAttack();
        if (activeHitbox != null)
            activeHitbox.SetHitboxActive(true);

        DebugLog($"EVENT OpenHitbox | mode={currentAttackMode} | step={CurrentComboStepNumber} | side={currentAttackSide}");
    }

    public void HandleAnimationEvent_CloseCurrentAttackHitbox()
    {
        if ((currentAttackMode != AttackMode.LightCombo && currentAttackMode != AttackMode.HeavyAttack) && !isHitboxOpen)
        {
            DebugLog($"EVENT CloseHitbox IGNORED | mode={currentAttackMode} | already closed");
            return;
        }

        if (lastProcessedCloseHitboxEventFrame == Time.frameCount)
        {
            DebugLog($"EVENT CloseHitbox IGNORED | duplicate same frame | frame={Time.frameCount}");
            return;
        }

        lastProcessedCloseHitboxEventFrame = Time.frameCount;
        isHitboxOpen = false;
        DisableAllHitboxesImmediate();

        DebugLog($"EVENT CloseHitbox | mode={currentAttackMode} | step={CurrentComboStepNumber}");
    }

    public void HandleAnimationEvent_OpenComboInputWindow()
    {
        if (currentAttackMode != AttackMode.LightCombo)
        {
            DebugLog($"EVENT OpenComboWindow IGNORED | mode={currentAttackMode}");
            return;
        }

        if (lastProcessedOpenComboWindowEventFrame == Time.frameCount)
        {
            DebugLog($"EVENT OpenComboWindow IGNORED | duplicate same frame | frame={Time.frameCount}");
            return;
        }

        lastProcessedOpenComboWindowEventFrame = Time.frameCount;
        comboInputWindowOpen = true;

        DebugLog(
            $"EVENT OpenComboWindow | step={CurrentComboStepNumber} | buffered={preWindowBufferedInputActive} | " +
            $"hasNext={HasNextComboStep()}"
        );

        if (HasNextComboStep() && HasValidBufferedInput())
        {
            queuedNextComboStep = true;
            ClearBufferedInput();

            DebugLog($"BUFFER PROMOTED TO QUEUE | currentStep={CurrentComboStepNumber} -> nextStep={CurrentComboStepNumber + 1}");
        }
    }

    public void HandleAnimationEvent_CloseComboInputWindow()
    {
        if (currentAttackMode != AttackMode.LightCombo)
        {
            DebugLog($"EVENT CloseComboWindow IGNORED | mode={currentAttackMode}");
            return;
        }

        if (lastProcessedCloseComboWindowEventFrame == Time.frameCount)
        {
            DebugLog($"EVENT CloseComboWindow IGNORED | duplicate same frame | frame={Time.frameCount}");
            return;
        }

        lastProcessedCloseComboWindowEventFrame = Time.frameCount;
        comboInputWindowOpen = false;
        ClearBufferedInput();

        DebugLog($"EVENT CloseComboWindow | step={CurrentComboStepNumber} | queued={queuedNextComboStep}");
    }

    public void HandleAnimationEvent_EndCurrentAttackStep()
    {
        if (currentAttackMode == AttackMode.None)
        {
            DebugLog("EVENT EndStep IGNORED | no attack in progress");
            return;
        }

        if (lastProcessedEndEventFrame == Time.frameCount)
        {
            DebugLog($"EVENT EndStep IGNORED | duplicate same frame | frame={Time.frameCount}");
            return;
        }

        lastProcessedEndEventFrame = Time.frameCount;

        StopAttackFailSafeTimer();

        isHitboxOpen = false;
        comboInputWindowOpen = false;
        DisableAllHitboxesImmediate();
        ClearBufferedInput();

        if (currentAttackMode == AttackMode.HeavyAttack)
        {
            DebugLog($"EVENT EndStep | heavy attack finished | attackId={heavyAttackId}");
            FinishHeavyAttack();
            return;
        }

        if (currentAttackMode != AttackMode.LightCombo)
        {
            DebugLog($"EVENT EndStep IGNORED | mode={currentAttackMode}");
            return;
        }

        int finishedStepIndex = currentComboStepIndex;
        PlayerAttackSide finishedSide = currentAttackSide;

        DebugLog(
            $"EVENT EndStep | finishedStep={finishedStepIndex + 1} | queuedNext={queuedNextComboStep} | " +
            $"hasNext={HasNextComboStep()} | pendingHeavyFollowUp={pendingHeavyFollowUp} | buttonHeld={isAttackButtonHeld}"
        );

        OnComboStepFinished?.Invoke(finishedStepIndex, finishedSide);

        if (queuedNextComboStep && HasNextComboStep())
        {
            int nextStepIndex = currentComboStepIndex + 1;
            queuedNextComboStep = false;

            DebugLog($"CHAIN TO NEXT STEP | {finishedStepIndex + 1} -> {nextStepIndex + 1}");

            if (!StartComboStep(nextStepIndex, false))
            {
                DebugLog("CHAIN FAILED -> FinishCombo()");
                FinishCombo();
            }

            return;
        }

        if (pendingHeavyFollowUp && !HasNextComboStep())
        {
            if (isAttackButtonHeld)
            {
                DebugLog($"CHAIN TO HEAVY CHARGE | finishedStep={finishedStepIndex + 1}");

                if (!StartHeavyChargeFromComboFollowUp())
                {
                    DebugLog("HEAVY FOLLOW-UP CHAIN FAILED -> FinishCombo()");
                    FinishCombo();
                }

                return;
            }

            DebugLog("HEAVY FOLLOW-UP DROPPED | button is no longer held");
            pendingHeavyFollowUp = false;
        }

        DebugLog("NO CHAIN -> FinishCombo()");
        FinishCombo();
    }

    public void NotifyHitboxTriggered(PlayerCombatHitbox hitbox, Collider2D other)
    {
        if (currentAttackMode != AttackMode.LightCombo && currentAttackMode != AttackMode.HeavyAttack)
            return;

        if (!isHitboxOpen)
            return;

        if (hitbox == null || other == null)
            return;

        if (hitbox != GetActiveHitboxForCurrentAttack())
            return;

        if (!TryGetCombatReceiver(other, out ICombatReceiver receiver))
            return;

        if (receiver == null || !receiver.IsAlive)
            return;

        if (receiver.Team == CombatTeam.Player)
            return;

        Transform receiverTransform = receiver.Transform;
        if (receiverTransform == null)
            return;

        int targetId = receiverTransform.GetInstanceID();
        if (hitTargetIds.Contains(targetId))
            return;

        hitTargetIds.Add(targetId);

        Vector3 hitPoint = hitbox.GetBestHitPoint(other);
        DamageInfo damageInfo = BuildDamageInfo(hitPoint);

        DebugLog(
            $"HIT CONFIRMED | mode={currentAttackMode} | step={CurrentComboStepNumber} | attackId={damageInfo.AttackId} | " +
            $"damage={damageInfo.Damage} | target={receiverTransform.name}"
        );

        receiver.ReceiveDamage(damageInfo);
    }

    private DamageInfo BuildDamageInfo(Vector3 hitPoint)
    {
        if (currentAttackMode == AttackMode.HeavyAttack)
            return BuildHeavyDamageInfo(hitPoint);

        return BuildLightComboDamageInfo(hitPoint);
    }

    private DamageInfo BuildLightComboDamageInfo(Vector3 hitPoint)
    {
        PlayerComboStepDefinition step = GetCurrentComboStep();
        if (step == null)
        {
            step = CreateFallbackComboStep();
            step.Sanitize();
        }

        int strength = statsSystem != null ? Mathf.Max(1, statsSystem.Strength) : 1;

        float minMultiplier = Mathf.Min(step.randomDamageMinMultiplier, step.randomDamageMaxMultiplier);
        float maxMultiplier = Mathf.Max(step.randomDamageMinMultiplier, step.randomDamageMaxMultiplier);

        float randomizedMultiplier = UnityEngine.Random.Range(minMultiplier, maxMultiplier);
        int calculatedDamage = Mathf.RoundToInt((strength * step.strengthDamageMultiplier + step.flatDamageBonus) * randomizedMultiplier);
        calculatedDamage = Mathf.Max(1, calculatedDamage);

        return new DamageInfo(
            source: gameObject,
            sourceTeam: CombatTeam.Player,
            damage: calculatedDamage,
            direction: CurrentAttackDirection,
            hitPoint: hitPoint,
            knockbackForce: step.knockbackForce,
            stunDuration: step.stunDuration,
            canBeBlocked: step.canBeBlocked,
            canBeParried: step.canBeParried,
            ignoresInvulnerability: false,
            isCritical: false,
            isSpecial: false,
            hitReactionType: step.hitReactionType,
            attackId: step.attackId
        );
    }

    private DamageInfo BuildHeavyDamageInfo(Vector3 hitPoint)
    {
        int strength = statsSystem != null ? Mathf.Max(1, statsSystem.Strength) : 1;

        float minMultiplier = Mathf.Min(heavyRandomDamageMinMultiplier, heavyRandomDamageMaxMultiplier);
        float maxMultiplier = Mathf.Max(heavyRandomDamageMinMultiplier, heavyRandomDamageMaxMultiplier);

        float randomizedMultiplier = UnityEngine.Random.Range(minMultiplier, maxMultiplier);
        int calculatedDamage = Mathf.RoundToInt((strength * heavyStrengthDamageMultiplier + heavyFlatDamageBonus) * randomizedMultiplier);
        calculatedDamage = Mathf.Max(1, calculatedDamage);

        return new DamageInfo(
            source: gameObject,
            sourceTeam: CombatTeam.Player,
            damage: calculatedDamage,
            direction: CurrentAttackDirection,
            hitPoint: hitPoint,
            knockbackForce: heavyKnockbackForce,
            stunDuration: heavyStunDuration,
            canBeBlocked: heavyCanBeBlocked,
            canBeParried: heavyCanBeParried,
            ignoresInvulnerability: false,
            isCritical: false,
            isSpecial: false,
            hitReactionType: heavyHitReactionType,
            attackId: heavyAttackId
        );
    }

    private PlayerCombatHitbox GetActiveHitboxForCurrentAttack()
    {
        return currentAttackSide == PlayerAttackSide.Left ? leftHitbox : rightHitbox;
    }

    private void DisableAllHitboxesImmediate()
    {
        if (leftHitbox != null)
            leftHitbox.ForceDisable();

        if (rightHitbox != null)
            rightHitbox.ForceDisable();
    }

    private void FinishCombo()
    {
        bool comboWasActive = currentAttackMode == AttackMode.LightCombo || currentComboStepIndex >= 0;

        DebugLog($"FinishCombo() | comboWasActive={comboWasActive} | lastStep={CurrentComboStepNumber}");

        ResetToNeutralCombatState();

        if (comboWasActive)
            OnComboFinished?.Invoke();
    }

    private void FinishHeavyAttack()
    {
        DebugLog($"FinishHeavyAttack() | attackId={heavyAttackId}");

        ResetToNeutralCombatState();
    }

    private void ForceStopAllCombatState()
    {
        DebugLog("ForceStopAllCombatState()");
        ResetToNeutralCombatState();
    }

    private void ResetToNeutralCombatState()
    {
        StopAttackFailSafeTimer();

        currentAttackMode = AttackMode.None;
        currentComboStepIndex = -1;

        isHitboxOpen = false;
        comboInputWindowOpen = false;
        queuedNextComboStep = false;
        pendingHeavyFollowUp = false;

        ClearBufferedInput();
        CancelNeutralAttackPressCandidate();
        EndAttackButtonHold();
        ResetAnimationEventGuards();

        hitTargetIds.Clear();
        DisableAllHitboxesImmediate();

        if (playerMoving != null)
            playerMoving.SetExternalMovementBlocked(false);

        if (playerAnimation != null)
            playerAnimation.ResetCombatAnimationState();
    }

    private void ResetAnimationEventGuards()
    {
        lastProcessedEndEventFrame = -1;
        lastProcessedOpenHitboxEventFrame = -1;
        lastProcessedCloseHitboxEventFrame = -1;
        lastProcessedOpenComboWindowEventFrame = -1;
        lastProcessedCloseComboWindowEventFrame = -1;
    }

    private void StartAttackFailSafeTimer(float duration)
    {
        StopAttackFailSafeTimer();
        attackFailSafeCoroutine = StartCoroutine(AttackFailSafeRoutine(duration));

        DebugLog($"StartFailSafeTimer | duration={duration:F2} | mode={currentAttackMode} | step={CurrentComboStepNumber}");
    }

    private void StopAttackFailSafeTimer()
    {
        if (attackFailSafeCoroutine != null)
        {
            StopCoroutine(attackFailSafeCoroutine);
            attackFailSafeCoroutine = null;
        }
    }

    private IEnumerator AttackFailSafeRoutine(float duration)
    {
        yield return new WaitForSeconds(Mathf.Max(0.05f, duration));
        attackFailSafeCoroutine = null;

        DebugLog($"FAILSAFE TRIGGERED | mode={currentAttackMode} | step={CurrentComboStepNumber}");

        if (currentAttackMode == AttackMode.LightCombo || currentAttackMode == AttackMode.HeavyAttack)
            HandleAnimationEvent_EndCurrentAttackStep();
    }

    private bool HasNextComboStep()
    {
        return currentAttackMode == AttackMode.LightCombo && IsValidComboStepIndex(currentComboStepIndex + 1);
    }

    private bool HasValidBufferedInput()
    {
        return preWindowBufferedInputActive && Time.time <= preWindowBufferedInputExpireTime;
    }

    private void ClearBufferedInput()
    {
        preWindowBufferedInputActive = false;
        preWindowBufferedInputExpireTime = -1f;
    }

    private bool IsValidComboStepIndex(int index)
    {
        return comboSteps != null && index >= 0 && index < comboSteps.Length && comboSteps[index] != null;
    }

    private PlayerComboStepDefinition GetCurrentComboStep()
    {
        if (!IsValidComboStepIndex(currentComboStepIndex))
            return null;

        return comboSteps[currentComboStepIndex];
    }

    private float GetResolvedFailSafeDuration(PlayerComboStepDefinition step)
    {
        if (step == null)
            return defaultAttackFailSafeDuration;

        if (step.attackFailSafeDuration <= 0f)
            return defaultAttackFailSafeDuration;

        return step.attackFailSafeDuration;
    }

    private bool TryGetCombatReceiver(Collider2D other, out ICombatReceiver receiver)
    {
        MonoBehaviour[] behaviours = other.GetComponentsInParent<MonoBehaviour>(true);

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is ICombatReceiver combatReceiver)
            {
                receiver = combatReceiver;
                return true;
            }
        }

        receiver = null;
        return false;
    }

    private void EnsureDefaultComboStepsIfNeeded()
    {
        if (comboSteps != null && comboSteps.Length > 0)
            return;

        comboSteps = new PlayerComboStepDefinition[3];
        comboSteps[0] = CreateDefaultComboStep(1, "player_light_01", 1.00f, 0, 0.70f);
        comboSteps[1] = CreateDefaultComboStep(2, "player_light_02", 1.10f, 1, 0.72f);
        comboSteps[2] = CreateDefaultComboStep(3, "player_light_03", 1.25f, 2, 0.78f);
    }

    private void SanitizeComboSteps()
    {
        if (comboSteps == null)
            return;

        for (int i = 0; i < comboSteps.Length; i++)
        {
            if (comboSteps[i] == null)
                comboSteps[i] = CreateDefaultComboStep(i + 1, $"player_light_0{i + 1}", 1.00f + (0.10f * i), i, defaultAttackFailSafeDuration);

            comboSteps[i].Sanitize();
        }
    }

    private PlayerComboStepDefinition CreateDefaultComboStep(int animationComboIndex, string attackId, float strengthMultiplier, int flatBonus, float failSafe)
    {
        PlayerComboStepDefinition step = new PlayerComboStepDefinition();
        step.animationComboIndex = Mathf.Max(1, animationComboIndex);
        step.attackId = attackId;
        step.attackFailSafeDuration = Mathf.Max(0.05f, failSafe);
        step.strengthDamageMultiplier = strengthMultiplier;
        step.flatDamageBonus = flatBonus;
        step.randomDamageMinMultiplier = 0.90f;
        step.randomDamageMaxMultiplier = 1.10f;
        step.knockbackForce = 0f;
        step.stunDuration = 0f;
        step.canBeBlocked = true;
        step.canBeParried = true;
        step.hitReactionType = HitReactionType.Light;
        step.Sanitize();
        return step;
    }

    private PlayerComboStepDefinition CreateFallbackComboStep()
    {
        return CreateDefaultComboStep(1, "player_light_fallback", 1f, 0, defaultAttackFailSafeDuration);
    }

    [ContextMenu("Debug/Start Combo From Beginning")]
    private void DebugStartCombo()
    {
        TryStartComboFromBeginning();
    }

    private void DebugLog(string message)
    {
        if (!enableCombatDebugLogs)
            return;

        Debug.Log($"[PlayerCombat DEBUG] {message}", this);
    }
}