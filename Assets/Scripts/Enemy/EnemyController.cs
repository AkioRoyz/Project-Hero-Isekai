using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyHealth))]
public class EnemyController : MonoBehaviour
{
    private enum EnemyState
    {
        Idle = 0,
        Chase = 1,
        Windup = 2,
        Attack = 3,
        Recovery = 4,
        Hit = 5,
        Stun = 6,
        Death = 7
    }

    private enum IncomingReactionType
    {
        None = 0,
        Hit = 1,
        Stun = 2
    }

    [Header("References")]
    [SerializeField] private EnemyHealth enemyHealth;
    [SerializeField] private EnemyAnimation enemyAnimation;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private CombatTarget combatTarget;

    [SerializeField] private Collider2D bodyCollider;

    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerMoving playerMoving;
    [SerializeField] private Transform playerTargetTransform;
    [SerializeField] private Collider2D playerBodyCollider;

    [Header("Attack Hitboxes")]
    [SerializeField] private EnemyAttackHitbox leftHitbox;
    [SerializeField] private EnemyAttackHitbox rightHitbox;

    [Header("Perception")]
    [SerializeField] private float detectionRadius = 4.5f;
    [SerializeField] private float loseTargetRadius = 6f;

    [Header("Combat Distance")]
    [SerializeField] private float attackRange = 0.20f;
    [SerializeField] private float movementStopDistance = 0.08f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;

    [Header("Attack Timing")]
    [SerializeField] private float windupDuration = 0.30f;
    [SerializeField] private float recoveryDuration = 0.45f;
    [SerializeField] private float attackFailSafeDuration = 0.90f;

    [Header("Hit Reaction")]
    [SerializeField] private bool enableLightHitReaction = true;
    [SerializeField] private float lightHitDuration = 0.10f;
    [SerializeField] private bool lightHitInterruptsWindup = false;
    [SerializeField] private bool lightHitInterruptsAttack = false;
    [SerializeField] private bool lightHitInterruptsRecovery = true;
    [SerializeField] private bool heavyHitInterruptsWindup = true;
    [SerializeField] private bool heavyHitInterruptsAttack = true;
    [SerializeField] private bool heavyHitInterruptsRecovery = true;

    [Header("Attack Damage")]
    [SerializeField] private string attackId = "enemy_melee_01";
    [SerializeField] private int attackDamage = 5;
    [SerializeField] private float attackRandomDamageMinMultiplier = 0.95f;
    [SerializeField] private float attackRandomDamageMaxMultiplier = 1.05f;
    [SerializeField] private float attackKnockbackForce = 0f;
    [SerializeField] private float attackStunDuration = 0f;
    [SerializeField] private bool attackCanBeBlocked = true;
    [SerializeField] private bool attackCanBeParried = true;
    [SerializeField] private HitReactionType attackHitReactionType = HitReactionType.Light;

    [Header("Stun")]
    [SerializeField] private float fallbackHeavyStunDuration = 0.20f;
    [SerializeField] private float fallbackExplicitStunDuration = 0.45f;

    [Header("Death")]
    [SerializeField] private float deathDestroyDelay = 1.25f;
    [SerializeField] private bool disableAllCollidersOnDeath = true;

    [Header("Facing")]
    [SerializeField] private Vector2 defaultFacingDirection = Vector2.right;

    [Header("Debug")]
    [SerializeField] private bool enableEnemyDebugLogs = true;
    [SerializeField] private bool logTargetResolution = true;

    private readonly HashSet<int> hitTargetIds = new HashSet<int>();

    private EnemyState currentState = EnemyState.Idle;
    private bool isFacingLeft;
    private bool isHitboxOpen;
    private float stateTimer;

    private int lastProcessedOpenHitboxEventFrame = -1;
    private int lastProcessedCloseHitboxEventFrame = -1;
    private int lastProcessedEndAttackEventFrame = -1;

    private Coroutine attackFailSafeCoroutine;

    public bool IsFacingLeft => isFacingLeft;

    public bool IsMovingAnimationDesired =>
        currentState == EnemyState.Chase &&
        HasValidPlayerTarget() &&
        GetCombatDistanceToPlayer() > movementStopDistance + 0.01f;

    public bool ShouldLockVisualFacing =>
        currentState == EnemyState.Windup ||
        currentState == EnemyState.Attack ||
        currentState == EnemyState.Hit ||
        currentState == EnemyState.Stun ||
        currentState == EnemyState.Death;

    private void Reset()
    {
        ResolveReferences();
    }

    private void OnValidate()
    {
        detectionRadius = Mathf.Max(0.1f, detectionRadius);
        loseTargetRadius = Mathf.Max(detectionRadius, loseTargetRadius);
        attackRange = Mathf.Max(0.01f, attackRange);
        movementStopDistance = Mathf.Max(0f, movementStopDistance);
        moveSpeed = Mathf.Max(0f, moveSpeed);
        windupDuration = Mathf.Max(0f, windupDuration);
        recoveryDuration = Mathf.Max(0f, recoveryDuration);
        attackFailSafeDuration = Mathf.Max(0.05f, attackFailSafeDuration);

        enableLightHitReaction = enableLightHitReaction;
        lightHitDuration = Mathf.Max(0.03f, lightHitDuration);

        attackDamage = Mathf.Max(0, attackDamage);
        attackRandomDamageMinMultiplier = Mathf.Max(0.01f, attackRandomDamageMinMultiplier);
        attackRandomDamageMaxMultiplier = Mathf.Max(attackRandomDamageMinMultiplier, attackRandomDamageMaxMultiplier);

        fallbackHeavyStunDuration = Mathf.Max(0f, fallbackHeavyStunDuration);
        fallbackExplicitStunDuration = Mathf.Max(0f, fallbackExplicitStunDuration);

        deathDestroyDelay = Mathf.Max(0f, deathDestroyDelay);

        ResolveReferences();
    }

    private void Awake()
    {
        ResolveReferences();

        isFacingLeft = defaultFacingDirection.x < 0f;
        DisableAllHitboxesImmediate();

        if (enemyHealth != null)
            enemyHealth.SetDestroyImmediatelyOnDeath(false);

        currentState = EnemyState.Idle;
    }

    private void Start()
    {
        RefreshPlayerReferences(forceRefresh: true);
        InitializeRuntimeStateAfterAllAwake();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (enemyHealth != null)
        {
            enemyHealth.SetDestroyImmediatelyOnDeath(false);
            enemyHealth.OnDied += HandleEnemyDied;
            enemyHealth.OnCombatDamageResolved += HandleCombatDamageResolved;
        }
    }

    private void OnDisable()
    {
        if (enemyHealth != null)
        {
            enemyHealth.OnDied -= HandleEnemyDied;
            enemyHealth.OnCombatDamageResolved -= HandleCombatDamageResolved;
        }

        StopAttackFailSafeTimer();
        DisableAllHitboxesImmediate();
    }

    private void Update()
    {
        RefreshPlayerReferences(forceRefresh: false);

        if (enemyHealth != null && enemyHealth.IsDead)
        {
            if (currentState != EnemyState.Death)
                EnterDeath();

            return;
        }

        if (currentState == EnemyState.Death)
            return;

        UpdateFacingFromTarget();

        switch (currentState)
        {
            case EnemyState.Idle:
                TickIdle();
                break;

            case EnemyState.Chase:
                TickChase();
                break;

            case EnemyState.Windup:
                TickWindup();
                break;

            case EnemyState.Recovery:
                TickRecovery();
                break;

            case EnemyState.Hit:
                TickHit();
                break;

            case EnemyState.Stun:
                TickStun();
                break;

            case EnemyState.Attack:
            case EnemyState.Death:
            default:
                break;
        }
    }

    private void FixedUpdate()
    {
        if (currentState != EnemyState.Chase)
            return;

        if (!HasValidPlayerTarget())
            return;

        if (rb == null)
            return;

        float combatDistance = GetCombatDistanceToPlayer();
        float stopDistance = Mathf.Max(0f, movementStopDistance);

        if (combatDistance <= stopDistance)
            return;

        Vector2 direction = GetDirectionToPlayer();
        if (direction.sqrMagnitude <= 0.0001f)
            return;

        float maxStep = moveSpeed * Time.fixedDeltaTime;
        float allowedStep = Mathf.Max(0f, combatDistance - stopDistance);
        float finalStep = Mathf.Min(maxStep, allowedStep);

        rb.MovePosition(rb.position + direction * finalStep);
    }

    private void InitializeRuntimeStateAfterAllAwake()
    {
        if (enemyHealth != null && enemyHealth.IsDead)
        {
            EnterDeath();
            return;
        }

        EnterIdle();
    }

    private void ResolveReferences()
    {
        if (enemyHealth == null)
            enemyHealth = GetComponent<EnemyHealth>();

        if (enemyAnimation == null)
            enemyAnimation = GetComponent<EnemyAnimation>();

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (combatTarget == null)
            combatTarget = GetComponent<CombatTarget>();

        if (bodyCollider == null)
            bodyCollider = GetComponent<Collider2D>();
    }

    private void RefreshPlayerReferences(bool forceRefresh)
    {
        bool needHealthRefresh = forceRefresh || !IsPlayerHealthUsable(playerHealth);
        bool needMovingRefresh = forceRefresh || !IsPlayerMovingUsable(playerMoving);
        bool needTransformRefresh = forceRefresh || !IsPlayerTargetTransformUsable(playerTargetTransform);
        bool needBodyColliderRefresh = forceRefresh || !IsBodyColliderUsable(playerBodyCollider);

        if (!needHealthRefresh && !needMovingRefresh && !needTransformRefresh && !needBodyColliderRefresh)
            return;

        PlayerHealth oldHealth = playerHealth;
        PlayerMoving oldMoving = playerMoving;
        Transform oldTargetTransform = playerTargetTransform;
        Collider2D oldPlayerBodyCollider = playerBodyCollider;

        if (needHealthRefresh)
            playerHealth = FindBestPlayerHealth();

        if (needMovingRefresh)
            playerMoving = FindBestPlayerMoving();

        if (needTransformRefresh)
            playerTargetTransform = ResolveBestPlayerTargetTransform(playerHealth, playerMoving);

        if (needBodyColliderRefresh)
            playerBodyCollider = ResolveBestPlayerBodyCollider(playerHealth, playerMoving, playerTargetTransform);

        if (logTargetResolution)
        {
            string oldHealthName = oldHealth != null ? oldHealth.name : "null";
            string newHealthName = playerHealth != null ? playerHealth.name : "null";

            string oldMovingName = oldMoving != null ? oldMoving.name : "null";
            string newMovingName = playerMoving != null ? playerMoving.name : "null";

            string oldTargetName = oldTargetTransform != null ? oldTargetTransform.name : "null";
            string newTargetName = playerTargetTransform != null ? playerTargetTransform.name : "null";

            string oldColliderName = oldPlayerBodyCollider != null ? oldPlayerBodyCollider.name : "null";
            string newColliderName = playerBodyCollider != null ? playerBodyCollider.name : "null";

            DebugLog(
                $"TARGET REFRESH | health: {oldHealthName} -> {newHealthName} | " +
                $"moving: {oldMovingName} -> {newMovingName} | " +
                $"targetTransform: {oldTargetName} -> {newTargetName} | " +
                $"bodyCollider: {oldColliderName} -> {newColliderName}"
            );
        }
    }

    private PlayerHealth FindBestPlayerHealth()
    {
        PlayerHealth[] candidates = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);

        PlayerHealth best = null;
        float bestDistanceSqr = float.MaxValue;
        Vector3 selfPosition = transform.position;

        for (int i = 0; i < candidates.Length; i++)
        {
            PlayerHealth candidate = candidates[i];
            if (!IsPlayerHealthUsable(candidate))
                continue;

            float distanceSqr = (candidate.transform.position - selfPosition).sqrMagnitude;
            if (distanceSqr < bestDistanceSqr)
            {
                bestDistanceSqr = distanceSqr;
                best = candidate;
            }
        }

        return best;
    }

    private PlayerMoving FindBestPlayerMoving()
    {
        PlayerMoving[] candidates = FindObjectsByType<PlayerMoving>(FindObjectsSortMode.None);

        PlayerMoving best = null;
        float bestDistanceSqr = float.MaxValue;
        Vector3 selfPosition = transform.position;

        for (int i = 0; i < candidates.Length; i++)
        {
            PlayerMoving candidate = candidates[i];
            if (!IsPlayerMovingUsable(candidate))
                continue;

            float distanceSqr = (candidate.transform.position - selfPosition).sqrMagnitude;
            if (distanceSqr < bestDistanceSqr)
            {
                bestDistanceSqr = distanceSqr;
                best = candidate;
            }
        }

        return best;
    }

    private Transform ResolveBestPlayerTargetTransform(PlayerHealth health, PlayerMoving moving)
    {
        if (IsPlayerMovingUsable(moving))
            return moving.transform;

        if (IsPlayerHealthUsable(health))
        {
            PlayerMoving movingFromParent = health.GetComponentInParent<PlayerMoving>();
            if (IsPlayerMovingUsable(movingFromParent))
                return movingFromParent.transform;

            return health.transform;
        }

        return null;
    }

    private Collider2D ResolveBestPlayerBodyCollider(PlayerHealth health, PlayerMoving moving, Transform targetTransform)
    {
        if (IsPlayerMovingUsable(moving))
        {
            Collider2D fromMoving = moving.GetComponent<Collider2D>();
            if (IsBodyColliderUsable(fromMoving))
                return fromMoving;

            Collider2D fromMovingChild = moving.GetComponentInChildren<Collider2D>();
            if (IsBodyColliderUsable(fromMovingChild))
                return fromMovingChild;
        }

        if (IsPlayerTargetTransformUsable(targetTransform))
        {
            Collider2D fromTarget = targetTransform.GetComponent<Collider2D>();
            if (IsBodyColliderUsable(fromTarget))
                return fromTarget;

            Collider2D fromTargetChild = targetTransform.GetComponentInChildren<Collider2D>();
            if (IsBodyColliderUsable(fromTargetChild))
                return fromTargetChild;
        }

        if (IsPlayerHealthUsable(health))
        {
            Collider2D fromHealthParent = health.GetComponentInParent<Collider2D>();
            if (IsBodyColliderUsable(fromHealthParent))
                return fromHealthParent;
        }

        return null;
    }

    private bool IsPlayerHealthUsable(PlayerHealth candidate)
    {
        return candidate != null &&
               candidate.isActiveAndEnabled &&
               candidate.gameObject.activeInHierarchy &&
               candidate.IsAlive;
    }

    private bool IsPlayerMovingUsable(PlayerMoving candidate)
    {
        return candidate != null &&
               candidate.isActiveAndEnabled &&
               candidate.gameObject.activeInHierarchy;
    }

    private bool IsPlayerTargetTransformUsable(Transform candidate)
    {
        return candidate != null &&
               candidate.gameObject.activeInHierarchy;
    }

    private bool IsBodyColliderUsable(Collider2D candidate)
    {
        return candidate != null &&
               candidate.enabled &&
               candidate.gameObject.activeInHierarchy &&
               !candidate.isTrigger;
    }

    public void RegisterHitbox(EnemyAttackHitbox hitbox)
    {
        if (hitbox == null)
            return;

        if (hitbox.IsLeftSide)
            leftHitbox = hitbox;
        else
            rightHitbox = hitbox;
    }

    private void TickIdle()
    {
        if (CanDetectPlayer())
            EnterChase();
    }

    private void TickChase()
    {
        if (!CanKeepPlayerTarget())
        {
            EnterIdle();
            return;
        }

        if (IsPlayerWithinAttackRange())
        {
            EnterWindup();
            return;
        }
    }

    private void TickWindup()
    {
        if (!CanKeepPlayerTarget())
        {
            EnterIdle();
            return;
        }

        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f)
            EnterAttack();
    }

    private void TickRecovery()
    {
        stateTimer -= Time.deltaTime;

        if (stateTimer > 0f)
            return;

        ResumeAfterReactionOrRecovery();
    }

    private void TickHit()
    {
        stateTimer -= Time.deltaTime;

        if (stateTimer > 0f)
            return;

        ResumeAfterReactionOrRecovery();
    }

    private void TickStun()
    {
        stateTimer -= Time.deltaTime;

        if (stateTimer > 0f)
            return;

        ResumeAfterReactionOrRecovery();
    }

    private void ResumeAfterReactionOrRecovery()
    {
        if (!CanKeepPlayerTarget())
        {
            EnterIdle();
            return;
        }

        if (IsPlayerWithinAttackRange())
            EnterWindup();
        else
            EnterChase();
    }

    private void EnterIdle()
    {
        if (currentState == EnemyState.Death)
            return;

        DebugLog("STATE -> Idle");
        currentState = EnemyState.Idle;
        stateTimer = 0f;

        StopAttackFailSafeTimer();
        DisableAllHitboxesImmediate();
    }

    private void EnterChase()
    {
        if (currentState == EnemyState.Death)
            return;

        DebugLog("STATE -> Chase");
        currentState = EnemyState.Chase;
        stateTimer = 0f;

        StopAttackFailSafeTimer();
        DisableAllHitboxesImmediate();
    }

    private void EnterWindup()
    {
        if (currentState == EnemyState.Death)
            return;

        if (!HasValidPlayerTarget())
            return;

        FaceTowardsPlayer(true);

        DebugLog("STATE -> Windup");
        currentState = EnemyState.Windup;
        stateTimer = windupDuration;

        StopAttackFailSafeTimer();
        DisableAllHitboxesImmediate();

        if (enemyAnimation != null)
            enemyAnimation.PlayWindup();
    }

    private void EnterAttack()
    {
        if (currentState == EnemyState.Death)
            return;

        FaceTowardsPlayer(true);

        DebugLog("STATE -> Attack");
        currentState = EnemyState.Attack;
        stateTimer = 0f;

        hitTargetIds.Clear();
        ResetAttackEventGuards();
        DisableAllHitboxesImmediate();
        StartAttackFailSafeTimer(attackFailSafeDuration);

        if (enemyAnimation != null)
            enemyAnimation.PlayAttack();
    }

    private void EnterRecovery()
    {
        if (currentState == EnemyState.Death)
            return;

        DebugLog("STATE -> Recovery");
        currentState = EnemyState.Recovery;
        stateTimer = recoveryDuration;

        StopAttackFailSafeTimer();
        DisableAllHitboxesImmediate();
    }

    private void EnterHit(float duration)
    {
        if (currentState == EnemyState.Death)
            return;

        float resolvedDuration = Mathf.Max(0.03f, duration);

        DebugLog($"STATE -> Hit | duration={resolvedDuration:F2}");
        currentState = EnemyState.Hit;
        stateTimer = resolvedDuration;

        StopAttackFailSafeTimer();
        DisableAllHitboxesImmediate();

        if (enemyAnimation != null)
            enemyAnimation.PlayHit();
    }

    private void EnterStun(float duration)
    {
        if (currentState == EnemyState.Death)
            return;

        float resolvedDuration = Mathf.Max(0.05f, duration);

        DebugLog($"STATE -> Stun | duration={resolvedDuration:F2}");
        currentState = EnemyState.Stun;
        stateTimer = resolvedDuration;

        StopAttackFailSafeTimer();
        DisableAllHitboxesImmediate();

        if (enemyAnimation != null)
            enemyAnimation.PlayStun();
    }

    private void EnterDeath()
    {
        if (currentState == EnemyState.Death)
            return;

        DebugLog("STATE -> Death");
        currentState = EnemyState.Death;
        stateTimer = 0f;

        StopAttackFailSafeTimer();
        DisableAllHitboxesImmediate();

        if (combatTarget != null)
            combatTarget.SetTargetable(false);

        if (disableAllCollidersOnDeath)
            DisableAllCollidersForDeath();

        if (rb != null)
            rb.simulated = false;

        if (enemyAnimation != null)
            enemyAnimation.PlayDeath();

        if (enemyHealth != null)
            enemyHealth.DestroyAfterDeath(deathDestroyDelay);
    }

    private void UpdateFacingFromTarget()
    {
        if (ShouldLockVisualFacing)
            return;

        if (!HasValidPlayerTarget())
            return;

        Vector3 toPlayer = playerTargetTransform.position - transform.position;

        if (toPlayer.x > 0.01f)
            isFacingLeft = false;
        else if (toPlayer.x < -0.01f)
            isFacingLeft = true;
    }

    private void FaceTowardsPlayer(bool force)
    {
        if (!HasValidPlayerTarget())
            return;

        Vector3 toPlayer = playerTargetTransform.position - transform.position;

        if (force)
        {
            isFacingLeft = toPlayer.x < 0f;
            return;
        }

        if (toPlayer.x > 0.01f)
            isFacingLeft = false;
        else if (toPlayer.x < -0.01f)
            isFacingLeft = true;
    }

    private bool HasValidPlayerTarget()
    {
        return IsPlayerHealthUsable(playerHealth) &&
               IsPlayerTargetTransformUsable(playerTargetTransform);
    }

    private bool CanDetectPlayer()
    {
        if (!HasValidPlayerTarget())
            return false;

        return GetCenterDistanceToPlayer() <= detectionRadius;
    }

    private bool CanKeepPlayerTarget()
    {
        if (!HasValidPlayerTarget())
            return false;

        return GetCenterDistanceToPlayer() <= loseTargetRadius;
    }

    private bool IsPlayerWithinAttackRange()
    {
        if (!HasValidPlayerTarget())
            return false;

        return GetCombatDistanceToPlayer() <= attackRange;
    }

    private float GetCenterDistanceToPlayer()
    {
        if (!HasValidPlayerTarget())
            return float.MaxValue;

        return Vector2.Distance(transform.position, playerTargetTransform.position);
    }

    private float GetCombatDistanceToPlayer()
    {
        if (!HasValidPlayerTarget())
            return float.MaxValue;

        if (IsBodyColliderUsable(bodyCollider) && IsBodyColliderUsable(playerBodyCollider))
        {
            Vector2 ownPoint = bodyCollider.bounds.ClosestPoint(playerBodyCollider.bounds.center);
            Vector2 targetPoint = playerBodyCollider.bounds.ClosestPoint(bodyCollider.bounds.center);
            return Vector2.Distance(ownPoint, targetPoint);
        }

        return Vector2.Distance(transform.position, playerTargetTransform.position);
    }

    private Vector2 GetDirectionToPlayer()
    {
        if (!HasValidPlayerTarget())
            return Vector2.zero;

        Vector2 from = IsBodyColliderUsable(bodyCollider)
            ? (Vector2)bodyCollider.bounds.center
            : (Vector2)transform.position;

        Vector2 to = IsBodyColliderUsable(playerBodyCollider)
            ? (Vector2)playerBodyCollider.bounds.center
            : (Vector2)playerTargetTransform.position;

        Vector2 direction = to - from;

        if (direction.sqrMagnitude <= 0.0001f)
            return Vector2.zero;

        return direction.normalized;
    }

    private void HandleEnemyDied()
    {
        EnterDeath();
    }

    private void HandleCombatDamageResolved(DamageInfo damageInfo, DamageResult result)
    {
        if (currentState == EnemyState.Death)
            return;

        if (result.WasIgnored || !result.AppliedDamage)
            return;

        if (result.WasKilled)
            return;

        float resolvedStun = ResolveIncomingStunDuration(damageInfo);
        IncomingReactionType reactionType = ResolveIncomingReactionType(damageInfo, resolvedStun);

        DebugLog(
            $"RECEIVED DAMAGE | finalDamage={result.FinalDamage} | " +
            $"reaction={damageInfo.HitReactionType} | incomingStun={damageInfo.StunDuration:F2} | " +
            $"resolvedStun={resolvedStun:F2} | currentState={currentState} | resolvedReaction={reactionType}"
        );

        switch (reactionType)
        {
            case IncomingReactionType.Hit:
                EnterHit(lightHitDuration);
                break;

            case IncomingReactionType.Stun:
                EnterStun(resolvedStun);
                break;

            case IncomingReactionType.None:
            default:
                break;
        }
    }

    private IncomingReactionType ResolveIncomingReactionType(DamageInfo damageInfo, float resolvedStun)
    {
        bool isHeavyOrStronger =
            damageInfo.HitReactionType == HitReactionType.Heavy ||
            damageInfo.HitReactionType == HitReactionType.Stun ||
            resolvedStun > 0f;

        bool isLight =
            damageInfo.HitReactionType == HitReactionType.Light &&
            !isHeavyOrStronger &&
            enableLightHitReaction;

        switch (currentState)
        {
            case EnemyState.Idle:
            case EnemyState.Chase:
                if (isHeavyOrStronger && resolvedStun > 0f)
                    return IncomingReactionType.Stun;

                if (isLight)
                    return IncomingReactionType.Hit;

                return IncomingReactionType.None;

            case EnemyState.Windup:
                if (isHeavyOrStronger && resolvedStun > 0f && heavyHitInterruptsWindup)
                    return IncomingReactionType.Stun;

                if (isLight && lightHitInterruptsWindup)
                    return IncomingReactionType.Hit;

                return IncomingReactionType.None;

            case EnemyState.Attack:
                if (isHeavyOrStronger && resolvedStun > 0f && heavyHitInterruptsAttack)
                    return IncomingReactionType.Stun;

                if (isLight && lightHitInterruptsAttack)
                    return IncomingReactionType.Hit;

                return IncomingReactionType.None;

            case EnemyState.Recovery:
                if (isHeavyOrStronger && resolvedStun > 0f && heavyHitInterruptsRecovery)
                    return IncomingReactionType.Stun;

                if (isLight && lightHitInterruptsRecovery)
                    return IncomingReactionType.Hit;

                return IncomingReactionType.None;

            case EnemyState.Hit:
                if (isHeavyOrStronger && resolvedStun > 0f)
                    return IncomingReactionType.Stun;

                if (isLight)
                    return IncomingReactionType.Hit;

                return IncomingReactionType.None;

            case EnemyState.Stun:
                if (isHeavyOrStronger && resolvedStun > stateTimer)
                    return IncomingReactionType.Stun;

                return IncomingReactionType.None;

            case EnemyState.Death:
            default:
                return IncomingReactionType.None;
        }
    }

    private float ResolveIncomingStunDuration(DamageInfo damageInfo)
    {
        float resolved = Mathf.Max(0f, damageInfo.StunDuration);

        if (damageInfo.HitReactionType == HitReactionType.Heavy)
            resolved = Mathf.Max(resolved, fallbackHeavyStunDuration);

        if (damageInfo.HitReactionType == HitReactionType.Stun)
            resolved = Mathf.Max(resolved, fallbackExplicitStunDuration);

        return resolved;
    }

    public void HandleAnimationEvent_OpenAttackHitbox()
    {
        if (currentState != EnemyState.Attack)
        {
            DebugLog($"EVENT OpenAttackHitbox IGNORED | state={currentState}");
            return;
        }

        if (lastProcessedOpenHitboxEventFrame == Time.frameCount)
        {
            DebugLog($"EVENT OpenAttackHitbox IGNORED | duplicate same frame={Time.frameCount}");
            return;
        }

        lastProcessedOpenHitboxEventFrame = Time.frameCount;
        isHitboxOpen = true;
        hitTargetIds.Clear();

        EnemyAttackHitbox activeHitbox = GetActiveHitboxForCurrentFacing();
        if (activeHitbox != null)
            activeHitbox.SetHitboxActive(true);

        DebugLog($"EVENT OpenAttackHitbox | facingLeft={isFacingLeft}");
    }

    public void HandleAnimationEvent_CloseAttackHitbox()
    {
        if (currentState != EnemyState.Attack && !isHitboxOpen)
        {
            DebugLog($"EVENT CloseAttackHitbox IGNORED | state={currentState}");
            return;
        }

        if (lastProcessedCloseHitboxEventFrame == Time.frameCount)
        {
            DebugLog($"EVENT CloseAttackHitbox IGNORED | duplicate same frame={Time.frameCount}");
            return;
        }

        lastProcessedCloseHitboxEventFrame = Time.frameCount;
        DisableAllHitboxesImmediate();

        DebugLog("EVENT CloseAttackHitbox");
    }

    public void HandleAnimationEvent_EndAttack()
    {
        if (currentState != EnemyState.Attack)
        {
            DebugLog($"EVENT EndAttack IGNORED | state={currentState}");
            return;
        }

        if (lastProcessedEndAttackEventFrame == Time.frameCount)
        {
            DebugLog($"EVENT EndAttack IGNORED | duplicate same frame={Time.frameCount}");
            return;
        }

        lastProcessedEndAttackEventFrame = Time.frameCount;

        DebugLog("EVENT EndAttack");
        EnterRecovery();
    }

    public void NotifyHitboxTriggered(EnemyAttackHitbox hitbox, Collider2D other)
    {
        if (currentState != EnemyState.Attack)
            return;

        if (!isHitboxOpen)
            return;

        if (hitbox == null || other == null)
            return;

        if (hitbox != GetActiveHitboxForCurrentFacing())
            return;

        if (!TryGetCombatReceiver(other, out ICombatReceiver receiver))
        {
            DebugLog(
                $"HITBOX OVERLAP BUT NO ICombatReceiver | other={other.name} | root={other.transform.root.name}"
            );
            return;
        }

        if (receiver == null || !receiver.IsAlive)
            return;

        if (receiver.Team == CombatTeam.Enemy)
            return;

        Transform receiverTransform = receiver.Transform;
        if (receiverTransform == null)
            return;

        int targetId = receiverTransform.GetInstanceID();
        if (hitTargetIds.Contains(targetId))
            return;

        hitTargetIds.Add(targetId);

        Vector3 hitPoint = hitbox.GetBestHitPoint(other);
        DamageInfo damageInfo = BuildAttackDamageInfo(hitPoint);

        DebugLog(
            $"HIT CONFIRMED | attackId={damageInfo.AttackId} | damage={damageInfo.Damage} | " +
            $"target={receiverTransform.name}"
        );

        receiver.ReceiveDamage(damageInfo);
    }

    private DamageInfo BuildAttackDamageInfo(Vector3 hitPoint)
    {
        float minMultiplier = Mathf.Min(attackRandomDamageMinMultiplier, attackRandomDamageMaxMultiplier);
        float maxMultiplier = Mathf.Max(attackRandomDamageMinMultiplier, attackRandomDamageMaxMultiplier);
        float randomizedMultiplier = Random.Range(minMultiplier, maxMultiplier);

        int finalDamage = Mathf.RoundToInt(Mathf.Max(0, attackDamage) * randomizedMultiplier);
        finalDamage = Mathf.Max(1, finalDamage);

        Vector2 direction = isFacingLeft ? Vector2.left : Vector2.right;

        return new DamageInfo(
            source: gameObject,
            sourceTeam: CombatTeam.Enemy,
            damage: finalDamage,
            direction: direction,
            hitPoint: hitPoint,
            knockbackForce: attackKnockbackForce,
            stunDuration: attackStunDuration,
            canBeBlocked: attackCanBeBlocked,
            canBeParried: attackCanBeParried,
            ignoresInvulnerability: false,
            isCritical: false,
            isSpecial: false,
            hitReactionType: attackHitReactionType,
            attackId: attackId
        );
    }

    private EnemyAttackHitbox GetActiveHitboxForCurrentFacing()
    {
        return isFacingLeft ? leftHitbox : rightHitbox;
    }

    private void DisableAllHitboxesImmediate()
    {
        if (leftHitbox != null)
            leftHitbox.ForceDisable();

        if (rightHitbox != null)
            rightHitbox.ForceDisable();

        isHitboxOpen = false;
    }

    private void DisableAllCollidersForDeath()
    {
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = false;
        }
    }

    private void ResetAttackEventGuards()
    {
        lastProcessedOpenHitboxEventFrame = -1;
        lastProcessedCloseHitboxEventFrame = -1;
        lastProcessedEndAttackEventFrame = -1;
    }

    private void StartAttackFailSafeTimer(float duration)
    {
        StopAttackFailSafeTimer();
        attackFailSafeCoroutine = StartCoroutine(AttackFailSafeRoutine(duration));
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

        if (currentState == EnemyState.Attack)
        {
            DebugLog("FAILSAFE EndAttack TRIGGERED");
            HandleAnimationEvent_EndAttack();
        }
    }

    private bool TryGetCombatReceiver(Collider2D other, out ICombatReceiver receiver)
    {
        if (TryFindCombatReceiverOnGameObject(other.gameObject, out receiver))
            return true;

        if (other.attachedRigidbody != null &&
            TryFindCombatReceiverOnGameObject(other.attachedRigidbody.gameObject, out receiver))
            return true;

        MonoBehaviour[] parentBehaviours = other.GetComponentsInParent<MonoBehaviour>(true);
        for (int i = 0; i < parentBehaviours.Length; i++)
        {
            if (parentBehaviours[i] is ICombatReceiver parentReceiver)
            {
                receiver = parentReceiver;
                return true;
            }
        }

        MonoBehaviour[] childBehaviours = other.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < childBehaviours.Length; i++)
        {
            if (childBehaviours[i] is ICombatReceiver childReceiver)
            {
                receiver = childReceiver;
                return true;
            }
        }

        if (other.attachedRigidbody != null)
        {
            MonoBehaviour[] rigidbodyChildBehaviours =
                other.attachedRigidbody.GetComponentsInChildren<MonoBehaviour>(true);

            for (int i = 0; i < rigidbodyChildBehaviours.Length; i++)
            {
                if (rigidbodyChildBehaviours[i] is ICombatReceiver rigidbodyChildReceiver)
                {
                    receiver = rigidbodyChildReceiver;
                    return true;
                }
            }
        }

        receiver = null;
        return false;
    }

    private bool TryFindCombatReceiverOnGameObject(GameObject target, out ICombatReceiver receiver)
    {
        MonoBehaviour[] behaviours = target.GetComponents<MonoBehaviour>();

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

    private void DebugLog(string message)
    {
        if (!enableEnemyDebugLogs)
            return;

        Debug.Log($"[EnemyController DEBUG] {message}", this);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, loseTargetRadius);
    }
#endif
}