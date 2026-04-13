using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PauseMenuSlotViewUI : MonoBehaviour
{
    [Header("Visual States")]
    [SerializeField] private GameObject selectedState;
    [SerializeField] private GameObject filledState;
    [SerializeField] private GameObject emptyState;
    [SerializeField] private GameObject backState;

    [Header("Texts")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text sceneText;
    [SerializeField] private TMP_Text dateText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Thumbnail")]
    [SerializeField] private GameObject thumbnailRoot;
    [SerializeField] private Image thumbnailImage;

    public void SetSelected(bool selected)
    {
        if (selectedState != null)
        {
            selectedState.SetActive(selected);
        }
    }

    public void ShowAsMainOption(string title)
    {
        SetState(filled: false, empty: false, back: false);

        SetText(titleText, title);
        SetText(sceneText, string.Empty);
        SetText(dateText, string.Empty);
        SetText(levelText, string.Empty);
        SetText(descriptionText, string.Empty);

        SetThumbnail(null);
    }

    public void ShowAsFilledSaveSlot(
        int visibleSlotNumber,
        string slotPrefix,
        string sceneName,
        string saveDateText,
        int playerLevel,
        string levelPrefix,
        Sprite thumbnail)
    {
        SetState(filled: true, empty: false, back: false);

        SetText(titleText, $"{slotPrefix} {visibleSlotNumber}");
        SetText(sceneText, sceneName);
        SetText(dateText, saveDateText);
        SetText(levelText, $"{levelPrefix}: {playerLevel}");
        SetText(descriptionText, string.Empty);

        SetThumbnail(thumbnail);
    }

    public void ShowAsEmptySaveSlot(int visibleSlotNumber, string slotPrefix, string emptyText)
    {
        SetState(filled: false, empty: true, back: false);

        SetText(titleText, $"{slotPrefix} {visibleSlotNumber}");
        SetText(sceneText, emptyText);
        SetText(dateText, string.Empty);
        SetText(levelText, string.Empty);
        SetText(descriptionText, string.Empty);

        SetThumbnail(null);
    }

    public void ShowAsBackOption(string backText)
    {
        SetState(filled: false, empty: false, back: true);

        SetText(titleText, backText);
        SetText(sceneText, string.Empty);
        SetText(dateText, string.Empty);
        SetText(levelText, string.Empty);
        SetText(descriptionText, string.Empty);

        SetThumbnail(null);
    }

    private void SetState(bool filled, bool empty, bool back)
    {
        if (filledState != null)
        {
            filledState.SetActive(filled);
        }

        if (emptyState != null)
        {
            emptyState.SetActive(empty);
        }

        if (backState != null)
        {
            backState.SetActive(back);
        }
    }

    private void SetText(TMP_Text textField, string value)
    {
        if (textField == null)
        {
            return;
        }

        bool hasText = !string.IsNullOrWhiteSpace(value);
        textField.gameObject.SetActive(hasText);
        textField.text = hasText ? value : string.Empty;
    }

    private void SetThumbnail(Sprite sprite)
    {
        if (thumbnailRoot != null)
        {
            thumbnailRoot.SetActive(sprite != null);
        }

        if (thumbnailImage != null)
        {
            thumbnailImage.sprite = sprite;
            thumbnailImage.enabled = sprite != null;
        }
    }
}