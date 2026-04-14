using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;

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

    [Header("Localization")]
    [SerializeField] private LocalizedString mainOptionLocalized;
    [SerializeField] private LocalizedString slotTitleLocalized;
    [SerializeField] private LocalizedString emptySlotLocalized;
    [SerializeField] private LocalizedString backLocalized;
    [SerializeField] private LocalizedString levelLocalized;

    [Header("Fallback Texts")]
    [SerializeField] private string mainOptionFallback = "";
    [SerializeField] private string slotTitleFallback = "Слот {0}";
    [SerializeField] private string emptySlotFallback = "Пустой слот";
    [SerializeField] private string backFallback = "Назад";
    [SerializeField] private string levelFallback = "Уровень: {0}";

    public void SetSelected(bool selected)
    {
        if (selectedState != null)
        {
            selectedState.SetActive(selected);
        }
    }

    public void ShowAsMainOption()
    {
        SetState(filled: false, empty: false, back: false);

        SetText(titleText, GetLocalized(mainOptionLocalized, mainOptionFallback));
        SetText(sceneText, string.Empty);
        SetText(dateText, string.Empty);
        SetText(levelText, string.Empty);
        SetText(descriptionText, string.Empty);

        SetThumbnail(null);
    }

    public void ShowAsFilledSaveSlot(
        int visibleSlotNumber,
        string sceneName,
        string saveDateText,
        int playerLevel,
        Sprite thumbnail)
    {
        SetState(filled: true, empty: false, back: false);

        SetText(titleText, GetLocalized(slotTitleLocalized, slotTitleFallback, visibleSlotNumber));
        SetText(sceneText, sceneName);
        SetText(dateText, saveDateText);
        SetText(levelText, GetLocalized(levelLocalized, levelFallback, playerLevel));
        SetText(descriptionText, string.Empty);

        SetThumbnail(thumbnail);
    }

    public void ShowAsEmptySaveSlot(int visibleSlotNumber)
    {
        SetState(filled: false, empty: true, back: false);

        SetText(titleText, GetLocalized(slotTitleLocalized, slotTitleFallback, visibleSlotNumber));
        SetText(sceneText, GetLocalized(emptySlotLocalized, emptySlotFallback));
        SetText(dateText, string.Empty);
        SetText(levelText, string.Empty);
        SetText(descriptionText, string.Empty);

        SetThumbnail(null);
    }

    public void ShowAsBackOption()
    {
        SetState(filled: false, empty: false, back: true);

        SetText(titleText, GetLocalized(backLocalized, backFallback));
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

    private string GetLocalized(LocalizedString localizedString, string fallback, params object[] arguments)
    {
        if (localizedString != null && !localizedString.IsEmpty)
        {
            try
            {
                if (arguments != null && arguments.Length > 0)
                {
                    return localizedString.GetLocalizedString(arguments);
                }

                return localizedString.GetLocalizedString();
            }
            catch
            {
                // Если таблица ещё не готова или строка не найдена — используем fallback ниже.
            }
        }

        if (string.IsNullOrWhiteSpace(fallback))
        {
            return string.Empty;
        }

        if (arguments != null && arguments.Length > 0)
        {
            return string.Format(fallback, arguments);
        }

        return fallback;
    }
}