using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

public class StatsUI : MonoBehaviour
{
    [Serializable]
    private class StatTextBlock
    {
        [Header("Localized Name")]
        [SerializeField] private LocalizedString localizedName;
        [SerializeField] private TMP_Text nameText;

        [Header("Value")]
        [SerializeField] private TMP_Text valueText;

        public void Enable()
        {
            if (localizedName != null)
            {
                localizedName.StringChanged += OnNameChanged;
                localizedName.RefreshString();
            }
        }

        public void Disable()
        {
            if (localizedName != null)
            {
                localizedName.StringChanged -= OnNameChanged;
            }
        }

        public void SetValue(string value)
        {
            if (valueText != null)
            {
                valueText.text = value;
            }
        }

        private void OnNameChanged(string value)
        {
            if (nameText != null)
            {
                nameText.text = value;
            }
        }
    }

    [Header("References")]
    [SerializeField] private StatsSystem statsSystem;

    [Header("Stats")]
    [SerializeField] private StatTextBlock strength;
    [SerializeField] private StatTextBlock mana;
    [SerializeField] private StatTextBlock defence;
    [SerializeField] private StatTextBlock dexterity;

    [Header("Critical")]
    [SerializeField] private StatTextBlock criticalChance;
    [SerializeField] private StatTextBlock criticalDamage;

    private void OnEnable()
    {
        EnableLocalization();

        if (statsSystem != null)
        {
            statsSystem.OnStatsUpdate += UpdateStats;
        }

        UpdateStats();
    }

    private void OnDisable()
    {
        DisableLocalization();

        if (statsSystem != null)
        {
            statsSystem.OnStatsUpdate -= UpdateStats;
        }
    }

    private void UpdateStats()
    {
        if (statsSystem == null)
        {
            ClearValues();
            return;
        }

        strength.SetValue(statsSystem.Strength.ToString());
        mana.SetValue(statsSystem.Mana.ToString());
        defence.SetValue(statsSystem.Defence.ToString());
        dexterity.SetValue(statsSystem.Dexterity.ToString());

        criticalChance.SetValue($"{statsSystem.CriticalChancePercent:0.#}%");
        criticalDamage.SetValue($"{statsSystem.CriticalDamagePercent:0.#}%");
    }

    private void ClearValues()
    {
        strength.SetValue("-");
        mana.SetValue("-");
        defence.SetValue("-");
        dexterity.SetValue("-");

        criticalChance.SetValue("-");
        criticalDamage.SetValue("-");
    }

    private void EnableLocalization()
    {
        strength.Enable();
        mana.Enable();
        defence.Enable();
        dexterity.Enable();

        criticalChance.Enable();
        criticalDamage.Enable();
    }

    private void DisableLocalization()
    {
        strength.Disable();
        mana.Disable();
        defence.Disable();
        dexterity.Disable();

        criticalChance.Disable();
        criticalDamage.Disable();
    }
}