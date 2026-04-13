using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization.Components;

public class ItemDescriptionPanelUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;

    [Header("Localization Events")]
    [SerializeField] private LocalizeStringEvent itemNameLocalizeEvent;
    [SerializeField] private LocalizeStringEvent itemDescriptionLocalizeEvent;

    [Header("Direct Text References")]
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;

    public void ShowItem(ItemData item)
    {
        if (item == null)
        {
            Clear();
            return;
        }

        if (iconImage != null)
        {
            iconImage.enabled = item.Icon != null;
            iconImage.sprite = item.Icon;
        }

        if (itemNameLocalizeEvent != null)
        {
            itemNameLocalizeEvent.StringReference.TableReference = item.ItemName.TableReference;
            itemNameLocalizeEvent.StringReference.TableEntryReference = item.ItemName.TableEntryReference;
            itemNameLocalizeEvent.RefreshString();
        }

        if (itemDescriptionLocalizeEvent != null)
        {
            itemDescriptionLocalizeEvent.StringReference.TableReference = item.ItemDescription.TableReference;
            itemDescriptionLocalizeEvent.StringReference.TableEntryReference = item.ItemDescription.TableEntryReference;
            itemDescriptionLocalizeEvent.RefreshString();
        }
    }

    public void Clear()
    {
        if (iconImage != null)
        {
            iconImage.enabled = false;
            iconImage.sprite = null;
        }

        // Явно очищаем сами тексты
        if (itemNameText != null)
        {
            itemNameText.text = "";
        }

        if (itemDescriptionText != null)
        {
            itemDescriptionText.text = "";
        }

        // Дополнительно очищаем ссылки локализации
        if (itemNameLocalizeEvent != null)
        {
            itemNameLocalizeEvent.StringReference.Clear();
        }

        if (itemDescriptionLocalizeEvent != null)
        {
            itemDescriptionLocalizeEvent.StringReference.Clear();
        }
    }
}