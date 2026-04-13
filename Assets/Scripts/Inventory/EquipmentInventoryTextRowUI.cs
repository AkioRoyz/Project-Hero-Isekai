using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EquipmentInventoryTextRowUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI itemText;
    [SerializeField] private Image selectedBackground;

    public void SetText(string text)
    {
        if (itemText != null)
        {
            itemText.text = text;
        }
    }

    public void SetSelected(bool selected)
    {
        if (selectedBackground != null)
        {
            selectedBackground.enabled = selected;
        }
    }

    public void SetListActiveVisual(bool isActive)
    {
        if (itemText != null)
        {
            Color textColor = itemText.color;
            textColor.a = isActive ? 1f : 0.6f;
            itemText.color = textColor;
        }

        if (!isActive && selectedBackground != null)
        {
            selectedBackground.enabled = false;
        }
    }
}