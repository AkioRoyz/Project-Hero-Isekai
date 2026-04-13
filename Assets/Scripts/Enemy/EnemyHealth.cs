using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour, ICombatReceiver
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 10;
    [SerializeField] private int currentHealth;

    [Header("Combat State")]
    [SerializeField] private bool isInvulnerable;

    [Header("Quest Identity")]
    [SerializeField] private string enemyTypeId;
    [SerializeField] private string enemyUniqueId;

    [Header("Death Reward")]
    [SerializeField] private int deathGold = 5;
    [SerializeField] private int deathEXP = 10;

    [Header("Damage Numbers")]
    [SerializeField] private bool showDamageNumbers = true;
    [SerializeField] private CombatTarget combatTarget;

    [Header("Death Lifecycle")]
    [SerializeField] private bool destroyImmediatelyOnDeath = true;
    [SerializeField] private float destroyDelayAfterDeath = 0f;
    [SerializeField] private bool disableCombatTargetOnDeath = true;

    private bool isDead;

    public event Action OnEnemyHealthChange;
    public event Action OnDied;
    public event Action<DamageInfo, DamageResult> OnCombatDamageResolved;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public string EnemyTypeId => enemyTypeId;
    public string EnemyUniqueId => enemyUniqueId;

    public CombatTeam Team => CombatTeam.Enemy;
    public bool IsAlive => !isDead && currentHealth > 0;
    public Transform Transform => transform;
    public bool IsInvulnerable => isInvulnerable;
    public bool IsDead => isDead;

    private void Awake()
    {
        ResolveReferences();

        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = maxHealth;
        isDead = false;
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        destroyDelayAfterDeath = Mathf.Max(0f, destroyDelayAfterDeath);

        ResolveReferencesInEditor();
    }

    public void SetInvulnerable(bool value)
    {
        isInvulnerable = value;
    }

    public void SetDestroyImmediatelyOnDeath(bool value)
    {
        destroyImmediatelyOnDeath = value;
    }

    public void SetDestroyDelayAfterDeath(float value)
    {
        destroyDelayAfterDeath = Mathf.Max(0f, value);
    }

    public void DestroyAfterDeath(float delay = -1f)
    {
        if (!isDead)
            return;

        float resolvedDelay = delay >= 0f ? delay : destroyDelayAfterDeath;
        Destroy(gameObject, Mathf.Max(0f, resolvedDelay));
    }

    [ContextMenu("Debug/Take 10 Damage (Legacy)")]
    private void DebugTake10DamageLegacy()
    {
        TakeDamage(10);
    }

    [ContextMenu("Debug/Receive 10 Combat Damage")]
    private void DebugReceive10CombatDamage()
    {
        DamageInfo debugDamage = new DamageInfo(
            source: null,
            sourceTeam: CombatTeam.Player,
            damage: 10,
            direction: Vector2.right,
            hitPoint: transform.position,
            knockbackForce: 0f,
            stunDuration: 0f,
            canBeBlocked: true,
            canBeParried: true,
            ignoresInvulnerability: false,
            isCritical: false,
            isSpecial: false,
            hitReactionType: HitReactionType.Light,
            attackId: "debug_player_hit"
        );

        ReceiveDamage(debugDamage);
    }

    public DamageResult ReceiveDamage(DamageInfo damageInfo)
    {
        if (ShouldIgnoreDamage(damageInfo))
        {
            DamageResult ignored = DamageResult.Ignored();
            OnCombatDamageResolved?.Invoke(damageInfo, ignored);
            return ignored;
        }

        int finalDamage = Mathf.Max(0, damageInfo.Damage);

        if (finalDamage <= 0)
        {
            DamageResult ignored = DamageResult.Ignored();
            OnCombatDamageResolved?.Invoke(damageInfo, ignored);
            return ignored;
        }

        currentHealth -= finalDamage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        CombatHitType hitType = damageInfo.IsSpecial ? CombatHitType.Special : CombatHitType.Normal;
        bool killed = currentHealth <= 0;

        DamageResult result = DamageResult.Damaged(finalDamage, killed, hitType);

        TryShowDamageNumber(damageInfo, result);

        OnEnemyHealthChange?.Invoke();
        OnCombatDamageResolved?.Invoke(damageInfo, result);

        if (killed)
            Die();

        return result;
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0)
            return;

        DamageInfo legacyDamage = DamageInfo.CreateSimple(
            null,
            CombatTeam.Neutral,
            amount,
            transform.position,
            Vector2.zero
        );

        ReceiveDamage(legacyDamage);
    }

    private void TryShowDamageNumber(DamageInfo damageInfo, DamageResult result)
    {
        if (!showDamageNumbers)
            return;

        if (!result.AppliedDamage || result.FinalDamage <= 0)
            return;

        if (damageInfo.SourceTeam != CombatTeam.Player)
            return;

        if (DamageNumberManager.Instance == null)
            return;

        DamageNumberType damageNumberType = ResolveDamageNumberType(damageInfo, result);

        if (combatTarget != null)
            DamageNumberManager.Instance.ShowDamageNumber(combatTarget, result.FinalDamage, damageNumberType);
        else
            DamageNumberManager.Instance.ShowDamageNumber(transform.position, result.FinalDamage, damageNumberType);
    }

    private DamageNumberType ResolveDamageNumberType(DamageInfo damageInfo, DamageResult result)
    {
        if (result.WasParried)
            return DamageNumberType.ParryCounter;

        if (result.WasBlocked)
            return DamageNumberType.Blocked;

        if (damageInfo.IsSpecial || result.HitType == CombatHitType.Special)
            return DamageNumberType.Special;

        if (damageInfo.IsCritical)
            return DamageNumberType.Critical;

        return DamageNumberType.Normal;
    }

    private bool ShouldIgnoreDamage(DamageInfo damageInfo)
    {
        if (!enabled || !gameObject.activeInHierarchy)
            return true;

        if (isDead)
            return true;

        if (damageInfo.Damage <= 0)
            return true;

        if (damageInfo.SourceTeam != CombatTeam.Neutral && damageInfo.SourceTeam == Team)
            return true;

        if (isInvulnerable && !damageInfo.IgnoresInvulnerability)
            return true;

        return false;
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;

        if (disableCombatTargetOnDeath && combatTarget != null)
            combatTarget.SetTargetable(false);

        OnEnemyHealthChange?.Invoke();
        OnDied?.Invoke();

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.NotifyEnemyKilled(enemyTypeId, enemyUniqueId);
        }

        if (RewardSystem.Instance != null)
        {
            RewardData reward = new RewardData(deathEXP, deathGold);
            RewardSystem.Instance.GiveReward(reward);
        }
        else
        {
            Debug.LogWarning("EnemyHealth: RewardSystem.Instance is missing.");
        }

        if (destroyImmediatelyOnDeath)
            DestroyAfterDeath();
    }

    private void ResolveReferences()
    {
        if (combatTarget == null)
            combatTarget = GetComponent<CombatTarget>();
    }

    private void ResolveReferencesInEditor()
    {
        if (combatTarget == null)
            combatTarget = GetComponent<CombatTarget>();
    }
}