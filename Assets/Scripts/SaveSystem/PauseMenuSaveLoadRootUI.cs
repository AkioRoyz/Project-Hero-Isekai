using System;
using UnityEngine;

public class PauseMenuSaveLoadRootUI : MonoBehaviour
{
    public enum RootMode
    {
        Save,
        Load
    }

    [Header("Root Setup")]
    [SerializeField] private RootMode rootMode = RootMode.Save;
    [SerializeField] private GameObject rootObject;
    [SerializeField] private PauseMenuSaveAdapter saveAdapter;
    [SerializeField] private PauseMenuSlotViewUI[] slotViews;

    [Header("Layout")]
    [SerializeField] private int dataSlotCount = 5;

    private int selectedIndex;
    private bool[] cachedHasData = new bool[0];

    public event Action<int> DataSlotChosen;
    public event Action BackRequested;

    public PauseMenuSaveAdapter SaveAdapter => saveAdapter;
    public bool IsVisible => rootObject != null ? rootObject.activeSelf : gameObject.activeSelf;

    private int UsableDataSlotCount
    {
        get
        {
            int maxByViews = Mathf.Max(0, slotViews.Length - 1);
            return Mathf.Min(dataSlotCount, maxByViews);
        }
    }

    private int BackSlotIndex => UsableDataSlotCount;

    private void Awake()
    {
        cachedHasData = new bool[Mathf.Max(UsableDataSlotCount, 1)];
    }

    public void ShowRoot()
    {
        if (rootObject != null)
        {
            rootObject.SetActive(true);
        }
        else
        {
            gameObject.SetActive(true);
        }

        selectedIndex = 0;
        Refresh();
    }

    public void HideRoot()
    {
        if (rootObject != null)
        {
            rootObject.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void MoveSelection(int direction)
    {
        int totalSelectable = UsableDataSlotCount + 1;
        if (totalSelectable <= 0)
        {
            return;
        }

        selectedIndex += direction;

        if (selectedIndex < 0)
        {
            selectedIndex = totalSelectable - 1;
        }
        else if (selectedIndex >= totalSelectable)
        {
            selectedIndex = 0;
        }

        RefreshSelectionVisuals();
    }

    public void SubmitCurrentSelection()
    {
        if (selectedIndex == BackSlotIndex)
        {
            BackRequested?.Invoke();
            return;
        }

        if (selectedIndex < 0 || selectedIndex >= UsableDataSlotCount)
        {
            return;
        }

        if (rootMode == RootMode.Load && !cachedHasData[selectedIndex])
        {
            return;
        }

        DataSlotChosen?.Invoke(selectedIndex);
    }

    public void Refresh()
    {
        int usableDataSlots = UsableDataSlotCount;

        if (cachedHasData == null || cachedHasData.Length != Mathf.Max(usableDataSlots, 1))
        {
            cachedHasData = new bool[Mathf.Max(usableDataSlots, 1)];
        }

        if (selectedIndex < 0 || selectedIndex > BackSlotIndex)
        {
            selectedIndex = 0;
        }

        for (int i = 0; i < usableDataSlots; i++)
        {
            if (slotViews[i] == null)
            {
                continue;
            }

            PauseMenuSaveAdapter.SlotPresentationData data = new PauseMenuSaveAdapter.SlotPresentationData();
            bool hasPresentation = false;

            if (saveAdapter != null)
            {
                hasPresentation = saveAdapter.TryGetSlotPresentation(i, out data);
            }

            if (!hasPresentation || !data.HasData)
            {
                cachedHasData[i] = false;
                slotViews[i].ShowAsEmptySaveSlot(i + 1);
                continue;
            }

            cachedHasData[i] = true;
            slotViews[i].ShowAsFilledSaveSlot(
                i + 1,
                data.DisplaySceneName,
                data.SaveDateText,
                data.PlayerLevel,
                data.Thumbnail);
        }

        if (slotViews.Length > BackSlotIndex && slotViews[BackSlotIndex] != null)
        {
            slotViews[BackSlotIndex].ShowAsBackOption();
        }

        for (int i = BackSlotIndex + 1; i < slotViews.Length; i++)
        {
            if (slotViews[i] != null)
            {
                slotViews[i].gameObject.SetActive(false);
            }
        }

        RefreshSelectionVisuals();
    }

    private void RefreshSelectionVisuals()
    {
        for (int i = 0; i < slotViews.Length; i++)
        {
            if (slotViews[i] == null)
            {
                continue;
            }

            bool shouldBeVisible = i <= BackSlotIndex;
            slotViews[i].gameObject.SetActive(shouldBeVisible);

            if (!shouldBeVisible)
            {
                continue;
            }

            slotViews[i].SetSelected(i == selectedIndex);
        }
    }
}