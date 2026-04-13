using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(fileName = "ItemData_", menuName = "Game/Items/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Base")]
    [SerializeField] private string itemId;
    [SerializeField] private ItemType itemType;
    [SerializeField] private Sprite icon;

    [Header("Localized UI")]
    [SerializeField] private LocalizedString itemName;
    [SerializeField] private LocalizedString itemDescription;

    [Header("Stack Settings")]
    [SerializeField] private bool stackable = true;

    [Header("Consumable Settings")]
    [SerializeField] private ConsumableEffectType consumableEffectType = ConsumableEffectType.None;
    [SerializeField] private int consumableValue = 0;
    [SerializeField] private float consumableDuration = 0f;

    [Header("Equipment Bonuses")]
    [SerializeField] private int equipmentStrengthBonus = 0;
    [SerializeField] private int equipmentManaBonus = 0;
    [SerializeField] private int equipmentDefenceBonus = 0;

    public string ItemId => itemId;
    public ItemType ItemType => itemType;
    public Sprite Icon => icon;
    public LocalizedString ItemName => itemName;
    public LocalizedString ItemDescription => itemDescription;
    public bool Stackable => stackable;

    public ConsumableEffectType ConsumableEffectType => consumableEffectType;
    public int ConsumableValue => consumableValue;
    public float ConsumableDuration => consumableDuration;

    public int EquipmentStrengthBonus => equipmentStrengthBonus;
    public int EquipmentManaBonus => equipmentManaBonus;
    public int EquipmentDefenceBonus => equipmentDefenceBonus;
}