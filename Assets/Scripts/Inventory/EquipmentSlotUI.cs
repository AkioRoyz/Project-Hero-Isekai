using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EquipmentSlotUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject selectionFrame;
    [SerializeField] private TextMeshProUGUI slotIndexText;

    private int slotIndex;

    public void Setup(int index)
    {
        slotIndex = index;

        if (slotIndexText != null)
        {
            slotIndexText.text = (slotIndex + 1).ToString();
        }
    }

    public void Refresh(ItemData item, bool selected)
    {
        if (iconImage != null)
        {
            if (item != null && item.Icon != null)
            {
                iconImage.enabled = true;
                iconImage.sprite = item.Icon;

                Color color = iconImage.color;
                color.a = 1f;
                iconImage.color = color;
            }
            else
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }
        }

        if (selectionFrame != null)
        {
            selectionFrame.SetActive(selected);
        }
    }
}