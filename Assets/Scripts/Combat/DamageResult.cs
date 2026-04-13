using UnityEngine;

[System.Serializable]
public struct DamageResult
{
    public bool WasIgnored;
    public bool AppliedDamage;
    public bool WasBlocked;
    public bool WasParried;
    public bool WasKilled;
    public int FinalDamage;
    public CombatHitType HitType;

    public static DamageResult Ignored()
    {
        return new DamageResult
        {
            WasIgnored = true,
            AppliedDamage = false,
            WasBlocked = false,
            WasParried = false,
            WasKilled = false,
            FinalDamage = 0,
            HitType = CombatHitType.None
        };
    }

    public static DamageResult Damaged(int finalDamage, bool wasKilled, CombatHitType hitType)
    {
        return new DamageResult
        {
            WasIgnored = false,
            AppliedDamage = finalDamage > 0,
            WasBlocked = hitType == CombatHitType.Blocked,
            WasParried = hitType == CombatHitType.Parried,
            WasKilled = wasKilled,
            FinalDamage = Mathf.Max(0, finalDamage),
            HitType = hitType
        };
    }
}