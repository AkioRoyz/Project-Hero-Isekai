using System;
using UnityEngine;
using UnityEngine.UI;

public class SaveLoadPanelUI : MonoBehaviour
{
    public enum PanelMode
    {
        Save,
        Load
    }

    [SerializeField] private SaveManager saveManager;
    [SerializeField] private PanelMode panelMode;
    [SerializeField] private SaveSlotButtonUI[] slotButtons = Array.Empty<SaveSlotButtonUI>();
    [SerializeField] private Button backButton;

    private int currentIndex;

    public event Action BackRequested;

    private void Awake()
    {
        if (saveManager == null)
            saveManager = SaveManager.Instance;

        BindButtons();
    }

    private void OnEnable()
    {
        if (saveManager == null)
            saveManager = SaveManager.Instance;

        if (saveManager != null)
            saveManager.OnSlotChanged += HandleSlotChanged;

        RefreshSlots();
        SelectFirstValid();
    }

    private void OnDisable()
    {
        if (saveManager != null)
            saveManager.OnSlotChanged -= HandleSlotChanged;
    }

    public void Show()
    {
        gameObject.SetActive(true);
        RefreshSlots();
        SelectFirstValid();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void MoveSelection(int direction)
    {
        int total = slotButtons.Length + (backButton != null ? 1 : 0);
        if (total == 0)
            return;

        int startIndex = currentIndex;

        do
        {
            currentIndex = (currentIndex + direction + total) % total;

            if (IsInteractable(currentIndex))
            {
                ApplySelection();
                return;
            }
        }
        while (currentIndex != startIndex);

        ApplySelection();
    }

    public void Submit()
    {
        if (currentIndex < slotButtons.Length)
            OnSlotPressed(currentIndex);
        else
            HandleBackRequested();
    }

    public void RefreshSlots()
    {
        int managerSlotCount = saveManager != null ? saveManager.SlotCount : 0;

        for (int i = 0; i < slotButtons.Length; i++)
        {
            SaveSlotButtonUI slotButton = slotButtons[i];
            if (slotButton == null)
                continue;

            if (i >= managerSlotCount)
            {
                slotButton.Apply(new SaveSlotMetadata(), null, false);

                if (slotButton.Button != null)
                    slotButton.Button.interactable = false;

                continue;
            }

            SaveSlotMetadata metadata = saveManager.GetSlotMetadata(i);
            Texture2D thumbnail = metadata.exists ? saveManager.LoadThumbnail(i) : null;
            bool interactableWhenEmpty = panelMode == PanelMode.Save;

            slotButton.Apply(metadata, thumbnail, interactableWhenEmpty);

            if (panelMode == PanelMode.Load && slotButton.Button != null && !metadata.exists)
                slotButton.Button.interactable = false;
        }

        if (backButton != null)
            backButton.interactable = true;
    }

    private void BindButtons()
    {
        for (int i = 0; i < slotButtons.Length; i++)
        {
            SaveSlotButtonUI slotButton = slotButtons[i];
            if (slotButton == null || slotButton.Button == null)
                continue;

            int capturedIndex = i;
            slotButton.Button.onClick.AddListener(delegate { OnSlotPressed(capturedIndex); });
        }

        if (backButton != null)
            backButton.onClick.AddListener(HandleBackRequested);
    }

    private void SelectFirstValid()
    {
        int total = slotButtons.Length + (backButton != null ? 1 : 0);
        if (total == 0)
            return;

        for (int i = 0; i < total; i++)
        {
            if (IsInteractable(i))
            {
                currentIndex = i;
                ApplySelection();
                return;
            }
        }

        currentIndex = slotButtons.Length;
        ApplySelection();
    }

    private void ApplySelection()
    {
        if (currentIndex < slotButtons.Length)
        {
            SaveSlotButtonUI slotButton = slotButtons[currentIndex];
            if (slotButton != null)
                slotButton.Select();
        }
        else if (backButton != null)
        {
            backButton.Select();
        }
    }

    private bool IsInteractable(int index)
    {
        if (index < slotButtons.Length)
        {
            SaveSlotButtonUI slotButton = slotButtons[index];
            return slotButton != null && slotButton.Button != null && slotButton.Button.interactable;
        }

        return backButton != null && backButton.interactable;
    }

    private void OnSlotPressed(int slotIndex)
    {
        if (saveManager == null)
            return;

        if (panelMode == PanelMode.Save)
        {
            if (saveManager.SaveToSlot(slotIndex))
            {
                RefreshSlots();
                currentIndex = slotIndex;
                ApplySelection();
            }

            return;
        }

        if (!saveManager.HasSave(slotIndex))
            return;

        Hide();
        BackRequested?.Invoke();
        saveManager.LoadFromSlot(slotIndex);
    }

    private void HandleBackRequested()
    {
        BackRequested?.Invoke();
    }

    private void HandleSlotChanged(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < slotButtons.Length)
            RefreshSlots();
    }
}