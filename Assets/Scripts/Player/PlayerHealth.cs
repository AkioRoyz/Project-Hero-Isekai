using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, ICombatReceiver
{
    [SerializeField] private int maxHealth;
    [SerializeField] private int currentHealth;
    [SerializeField] private StatsSystem statsSystem;

    [Header("Combat State")]
    [SerializeField] private bool isInvulnerable;

    private bool isDead;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;

    public CombatTeam Team => CombatTeam.Player;
    public bool IsAlive => !isDead && currentHealth > 0;
    public Transform Transform => transform;
    public bool IsInvulnerable => isInvulnerable;

    public event Action OnHealthChange;
    public event Action OnTakeDamage;
    public event Action OnDied;
    public event Action<DamageInfo, DamageResult> OnCombatDamageResolved;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Start()
    {
        RefreshHealthFromStats(true);
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (statsSystem != null)
        {
            statsSystem.OnStatsUpdate += OnStatsUpdated;
            statsSystem.OnLevelStatsUpdated += OnLevelStatsUpdated;
        }
    }

    private void OnDisable()
    {
        if (statsSystem != null)
        {
            statsSystem.OnStatsUpdate -= OnStatsUpdated;
            statsSystem.OnLevelStatsUpdated -= OnLevelStatsUpdated;
        }
    }

    private void ResolveReferences()
    {
        if (statsSystem == null)
            statsSystem = FindFirstObjectByType<StatsSystem>();
    }

    public void SetInvulnerable(bool value)
    {
        isInvulnerable = value;
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

        OnHealthChange?.Invoke();
        OnTakeDamage?.Invoke();
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

    public void Heal(int amount)
    {
        if (amount <= 0)
            return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (currentHealth > 0)
            isDead = false;

        OnHealthChange?.Invoke();
    }

    public void RestoreToFull()
    {
        currentHealth = maxHealth;
        isDead = false;
        OnHealthChange?.Invoke();
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
        OnDied?.Invoke();
    }

    private void OnStatsUpdated()
    {
        RefreshHealthFromStats(false);
    }

    private void OnLevelStatsUpdated()
    {
        currentHealth = maxHealth;
        isDead = false;
        OnHealthChange?.Invoke();
    }

    private void RefreshHealthFromStats(bool firstInit)
    {
        if (statsSystem == null)
        {
            maxHealth = Mathf.Max(maxHealth, 1);
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
            isDead = currentHealth <= 0;
            OnHealthChange?.Invoke();
            return;
        }

        int oldMaxHealth = maxHealth;
        maxHealth = Mathf.Max(1, statsSystem.Strength * 10);

        if (firstInit || oldMaxHealth <= 0)
        {
            currentHealth = maxHealth;
        }
        else
        {
            int difference = maxHealth - oldMaxHealth;
            currentHealth += difference;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        }

        isDead = currentHealth <= 0;
        OnHealthChange?.Invoke();
    }

    [ContextMenu("Debug/Receive 10 Combat Damage")]
    private void DebugReceive10CombatDamage()
    {
        DamageInfo debugDamage = new DamageInfo(
            source: null,
            sourceTeam: CombatTeam.Enemy,
            damage: 10,
            direction: Vector2.left,
            hitPoint: transform.position,
            knockbackForce: 0f,
            stunDuration: 0f,
            canBeBlocked: true,
            canBeParried: true,
            ignoresInvulnerability: false,
            isCritical: false,
            isSpecial: false,
            hitReactionType: HitReactionType.Light,
            attackId: "debug_enemy_hit"
        );

        ReceiveDamage(debugDamage);
    }

    [ContextMenu("Debug/Restore To Full")]
    private void DebugRestoreToFull()
    {
        RestoreToFull();
    }
}