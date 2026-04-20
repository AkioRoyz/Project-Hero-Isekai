using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MerchantListRowUI : MonoBehaviour
{
    [SerializeField] private Image selectedBackground;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI amountText;

    [Header("Optional Extra Tint Targets")]
    [SerializeField] private Graphic[] tintTargets;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color disabledColor = new Color(1f, 1f, 1f, 0.55f);

    public void SetData(
        Sprite icon,
        bool showIcon,
        string displayName,
        string displayPrice,
        string displayAmount,
        bool selected,
        bool disabled)
    {
        if (selectedBackground != null)
            selectedBackground.enabled = selected;

        if (iconImage != null)
        {
            iconImage.gameObject.SetActive(showIcon);
            iconImage.sprite = icon;
            iconImage.enabled = showIcon && icon != null;
        }

        if (nameText != null)
            nameText.text = displayName ?? string.Empty;

        if (priceText != null)
            priceText.text = displayPrice ?? string.Empty;

        if (amountText != null)
            amountText.text = displayAmount ?? string.Empty;

        ApplyTint(disabled ? disabledColor : normalColor);
    }

    private void ApplyTint(Color tint)
    {
        if (iconImage != null)
            iconImage.color = tint;

        if (nameText != null)
            nameText.color = tint;

        if (priceText != null)
            priceText.color = tint;

        if (amountText != null)
            amountText.color = tint;

        if (tintTargets == null)
            return;

        for (int i = 0; i < tintTargets.Length; i++)
        {
            if (tintTargets[i] != null)
                tintTargets[i].color = tint;
        }
    }
}