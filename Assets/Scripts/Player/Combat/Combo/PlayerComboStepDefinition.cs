using UnityEngine;

[System.Serializable]
public class PlayerComboStepDefinition
{
    [Header("Animation")]
    [Min(1)] public int animationComboIndex = 1;
    public string attackId = "player_light_01";

    [Header("Fail Safe")]
    [Min(0f)] public float attackFailSafeDuration = 0.70f;

    [Header("Damage")]
    public float strengthDamageMultiplier = 1.00f;
    public int flatDamageBonus = 0;
    public float randomDamageMinMultiplier = 0.90f;
    public float randomDamageMaxMultiplier = 1.10f;

    [Header("Combat Metadata")]
    public float knockbackForce = 0f;
    public float stunDuration = 0f;
    public bool canBeBlocked = true;
    public bool canBeParried = true;
    public HitReactionType hitReactionType = HitReactionType.Light;

    public void Sanitize()
    {
        animationComboIndex = Mathf.Max(1, animationComboIndex);
        attackFailSafeDuration = Mathf.Max(0f, attackFailSafeDuration);

        randomDamageMinMultiplier = Mathf.Max(0f, randomDamageMinMultiplier);
        randomDamageMaxMultiplier = Mathf.Max(0f, randomDamageMaxMultiplier);

        if (randomDamageMaxMultiplier < randomDamageMinMultiplier)
        {
            float temp = randomDamageMaxMultiplier;
            randomDamageMaxMultiplier = randomDamageMinMultiplier;
            randomDamageMinMultiplier = temp;
        }

        if (attackId == null)
            attackId = string.Empty;
    }
}