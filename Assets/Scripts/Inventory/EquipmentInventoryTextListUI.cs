using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class EquipmentInventoryTextListUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private EquipmentInventoryTextRowUI[] visibleRows = new EquipmentInventoryTextRowUI[13];
    [SerializeField] private GameObject moreUpIcon;
    [SerializeField] private GameObject moreDownIcon;
    [SerializeField] private GameObject emptyLabelObject;
    [SerializeField] private TextMeshProUGUI emptyLabelText;

    private List<InventoryEntry> currentEntries = new();
    private int firstVisibleIndex = 0;
    private int selectedIndex = 0;
    private bool listIsActive = false;

    public int MaxVisibleRows => visibleRows.Length;

    public void SetData(List<InventoryEntry> equipmentEntries)
    {
        currentEntries = equipmentEntries ?? new List<InventoryEntry>();

        // Защита от выхода за границы
        if (selectedIndex >= currentEntries.Count)
        {
            selectedIndex = Mathf.Max(0, currentEntries.Count - 1);
        }

        ClampFirstVisibleIndex();
        RefreshUI();
    }

    public void SetVisualState(bool isActive, int newSelectedIndex)
    {
        listIsActive = isActive;
        selectedIndex = Mathf.Clamp(newSelectedIndex, 0, Mathf.Max(0, currentEntries.Count - 1));

        AdjustWindowToSelection();
        RefreshUI();
    }

    private void AdjustWindowToSelection()
    {
        int maxVisible = visibleRows.Length;

        if (currentEntries.Count <= maxVisible)
        {
            firstVisibleIndex = 0;
            return;
        }

        if (selectedIndex < firstVisibleIndex)
        {
            firstVisibleIndex = selectedIndex;
        }
        else if (selectedIndex >= firstVisibleIndex + maxVisible)
        {
            firstVisibleIndex = selectedIndex - maxVisible + 1;
        }

        ClampFirstVisibleIndex();
    }

    private void ClampFirstVisibleIndex()
    {
        int maxVisible = visibleRows.Length;
        int maxStartIndex = Mathf.Max(0, currentEntries.Count - maxVisible);

        firstVisibleIndex = Mathf.Clamp(firstVisibleIndex, 0, maxStartIndex);
    }

    private void RefreshUI()
    {
        bool hasItems = currentEntries != null && currentEntries.Count > 0;

        if (emptyLabelObject != null)
        {
            emptyLabelObject.SetActive(!hasItems);
        }

        if (emptyLabelText != null)
        {
            Color color = emptyLabelText.color;
            color.a = listIsActive ? 1f : 0.6f;
            emptyLabelText.color = color;
        }

        if (!hasItems)
        {
            HideAllRows();
            SetMoreIcons(false, false);
            return;
        }

        int maxVisible = visibleRows.Length;

        for (int rowIndex = 0; rowIndex < visibleRows.Length; rowIndex++)
        {
            EquipmentInventoryTextRowUI rowUI = visibleRows[rowIndex];

            if (rowUI == null)
                continue;

            int dataIndex = firstVisibleIndex + rowIndex;

            if (dataIndex < currentEntries.Count)
            {
                InventoryEntry entry = currentEntries[dataIndex];
                string localizedName = GetItemDisplayName(entry.Item);

                rowUI.gameObject.SetActive(true);
                rowUI.SetText($"{localizedName} x{entry.Amount}");
                rowUI.SetListActiveVisual(listIsActive);
                rowUI.SetSelected(listIsActive && dataIndex == selectedIndex);
            }
            else
            {
                rowUI.gameObject.SetActive(false);
            }
        }

        bool showUp = firstVisibleIndex > 0;
        bool showDown = firstVisibleIndex + maxVisible < currentEntries.Count;

        SetMoreIcons(showUp, showDown);
    }

    private void HideAllRows()
    {
        for (int i = 0; i < visibleRows.Length; i++)
        {
            if (visibleRows[i] != null)
            {
                visibleRows[i].gameObject.SetActive(false);
            }
        }
    }

    private void SetMoreIcons(bool showUp, bool showDown)
    {
        if (moreUpIcon != null)
        {
            moreUpIcon.SetActive(showUp);
        }

        if (moreDownIcon != null)
        {
            moreDownIcon.SetActive(showDown);
        }
    }

    private string GetItemDisplayName(ItemData item)
    {
        if (item == null)
            return "NULL";

        if (item.ItemName.IsEmpty)
            return item.ItemId;

        string localized = LocalizationSettings.StringDatabase.GetLocalizedString(
            item.ItemName.TableReference,
            item.ItemName.TableEntryReference
        );

        if (!string.IsNullOrEmpty(localized))
        {
            return localized;
        }

        return item.ItemId;
    }
}