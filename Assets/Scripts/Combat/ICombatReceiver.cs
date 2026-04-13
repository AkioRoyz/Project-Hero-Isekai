using UnityEngine;

public interface ICombatReceiver
{
    CombatTeam Team { get; }
    bool IsAlive { get; }
    Transform Transform { get; }

    DamageResult ReceiveDamage(DamageInfo damageInfo);
}