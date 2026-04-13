using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class QuestJournalListUI : MonoBehaviour
{
    public class EntryViewData
    {
        public string QuestId;
        public string Title;
        public string ShortDescription;

        public EntryViewData(string questId, string title, string shortDescription)
        {
            QuestId = questId;
            Title = title;
            ShortDescription = shortDescription;
        }
    }

    [Header("UI References")]
    [SerializeField] private QuestJournalRowUI[] visibleRows = new QuestJournalRowUI[8];
    [SerializeField] private GameObject moreUpIcon;
    [SerializeField] private GameObject moreDownIcon;
    [SerializeField] private GameObject emptyLabelObject;
    [SerializeField] private TextMeshProUGUI emptyLabelText;

    [Header("Visual")]
    [SerializeField] private float inactiveAlpha = 0.6f;

    private List<EntryViewData> currentEntries = new();
    private int firstVisibleIndex = 0;
    private int selectedIndex = 0;
    private bool listIsActive = false;

    public int MaxVisibleRows => visibleRows.Length;
    public int EntryCount => currentEntries != null ? currentEntries.Count : 0;

    public void SetData(List<EntryViewData> entries)
    {
        currentEntries = entries ?? new List<EntryViewData>();

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

    public bool HasEntries()
    {
        return currentEntries != null && currentEntries.Count > 0;
    }

    public EntryViewData GetSelectedEntry()
    {
        if (!HasEntries())
            return null;

        if (selectedIndex < 0 || selectedIndex >= currentEntries.Count)
            return null;

        return currentEntries[selectedIndex];
    }

    public int GetSelectedIndex()
    {
        return selectedIndex;
    }

    public void MoveSelection(int direction)
    {
        if (!HasEntries())
            return;

        selectedIndex += direction;

        if (selectedIndex < 0)
        {
            selectedIndex = currentEntries.Count - 1;
        }
        else if (selectedIndex >= currentEntries.Count)
        {
            selectedIndex = 0;
        }

        AdjustWindowToSelection();
        RefreshUI();
    }

    public void EnsureValidSelection()
    {
        if (currentEntries == null || currentEntries.Count == 0)
        {
            selectedIndex = 0;
            firstVisibleIndex = 0;
            RefreshUI();
            return;
        }

        selectedIndex = Mathf.Clamp(selectedIndex, 0, currentEntries.Count - 1);
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
        bool hasEntries = currentEntries != null && currentEntries.Count > 0;

        if (emptyLabelObject != null)
        {
            emptyLabelObject.SetActive(!hasEntries);
        }

        if (emptyLabelText != null)
        {
            Color color = emptyLabelText.color;
            color.a = listIsActive ? 1f : inactiveAlpha;
            emptyLabelText.color = color;
        }

        if (!hasEntries)
        {
            HideAllRows();
            SetMoreIcons(false, false);
            return;
        }

        int maxVisible = visibleRows.Length;

        for (int rowIndex = 0; rowIndex < visibleRows.Length; rowIndex++)
        {
            QuestJournalRowUI rowUI = visibleRows[rowIndex];

            if (rowUI == null)
                continue;

            int dataIndex = firstVisibleIndex + rowIndex;

            if (dataIndex < currentEntries.Count)
            {
                EntryViewData entry = currentEntries[dataIndex];

                rowUI.gameObject.SetActive(true);
                rowUI.SetData(entry.Title, entry.ShortDescription);
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
}