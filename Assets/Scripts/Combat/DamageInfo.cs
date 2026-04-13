using System;
using UnityEngine;

[Serializable]
public struct DamageInfo
{
    public GameObject Source;
    public CombatTeam SourceTeam;
    public int Damage;
    public Vector2 Direction;
    public Vector3 HitPoint;
    public float KnockbackForce;
    public float StunDuration;
    public bool CanBeBlocked;
    public bool CanBeParried;
    public bool IgnoresInvulnerability;
    public bool IsCritical;
    public bool IsSpecial;
    public HitReactionType HitReactionType;
    public string AttackId;

    public DamageInfo(
        GameObject source,
        CombatTeam sourceTeam,
        int damage,
        Vector2 direction,
        Vector3 hitPoint,
        float knockbackForce = 0f,
        float stunDuration = 0f,
        bool canBeBlocked = true,
        bool canBeParried = true,
        bool ignoresInvulnerability = false,
        bool isCritical = false,
        bool isSpecial = false,
        HitReactionType hitReactionType = HitReactionType.Light,
        string attackId = "")
    {
        Source = source;
        SourceTeam = sourceTeam;
        Damage = Mathf.Max(0, damage);
        Direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.zero;
        HitPoint = hitPoint;
        KnockbackForce = Mathf.Max(0f, knockbackForce);
        StunDuration = Mathf.Max(0f, stunDuration);
        CanBeBlocked = canBeBlocked;
        CanBeParried = canBeParried;
        IgnoresInvulnerability = ignoresInvulnerability;
        IsCritical = isCritical;
        IsSpecial = isSpecial;
        HitReactionType = hitReactionType;
        AttackId = attackId ?? string.Empty;
    }

    public static DamageInfo CreateSimple(
        GameObject source,
        CombatTeam sourceTeam,
        int damage,
        Vector3 hitPoint,
        Vector2 direction)
    {
        return new DamageInfo(
            source,
            sourceTeam,
            damage,
            direction,
            hitPoint
        );
    }
}