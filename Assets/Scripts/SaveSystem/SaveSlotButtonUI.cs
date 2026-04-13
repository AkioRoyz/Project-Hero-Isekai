using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

public class SaveSlotButtonUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private Image thumbnailImage;
    [SerializeField] private Sprite fallbackThumbnail;
    [SerializeField] private LocalizedString filledSlotTemplate;
    [SerializeField] private LocalizedString emptySlotTemplate;
    [SerializeField] private SceneDisplayNameCatalog sceneDisplayNameCatalog;

    private Texture2D runtimeTexture;
    private Sprite runtimeSprite;
    private Coroutine activeRoutine;

    public Button Button => button;

    public void Apply(SaveSlotMetadata metadata, Texture2D thumbnail, bool interactableWhenEmpty)
    {
        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        ReleaseRuntimeObjects();

        bool hasSave = metadata != null && metadata.exists;

        if (button != null)
            button.interactable = hasSave || interactableWhenEmpty;

        if (thumbnailImage != null)
        {
            if (hasSave && thumbnail != null)
            {
                runtimeTexture = thumbnail;
                runtimeSprite = Sprite.Create(
                    runtimeTexture,
                    new Rect(0f, 0f, runtimeTexture.width, runtimeTexture.height),
                    new Vector2(0.5f, 0.5f));

                thumbnailImage.sprite = runtimeSprite;
                thumbnailImage.enabled = true;
            }
            else
            {
                if (thumbnail != null)
                    Destroy(thumbnail);

                thumbnailImage.sprite = fallbackThumbnail;
                thumbnailImage.enabled = fallbackThumbnail != null;
            }
        }

        activeRoutine = StartCoroutine(UpdateTextRoutine(metadata));
    }

    public void Select()
    {
        if (button != null)
            button.Select();
    }

    private IEnumerator UpdateTextRoutine(SaveSlotMetadata metadata)
    {
        if (infoText == null)
            yield break;

        if (metadata == null || !metadata.exists)
        {
            if (emptySlotTemplate != null)
            {
                var emptyHandle = emptySlotTemplate.GetLocalizedStringAsync();
                yield return emptyHandle;
                infoText.text = emptyHandle.Result;
            }
            else
            {
                infoText.text = string.Empty;
            }

            yield break;
        }

        string sceneDisplayName = metadata.sceneName;

        if (sceneDisplayNameCatalog != null &&
            sceneDisplayNameCatalog.TryGetDisplayName(metadata.sceneName, out LocalizedString localizedSceneName))
        {
            var sceneHandle = localizedSceneName.GetLocalizedStringAsync();
            yield return sceneHandle;

            if (!string.IsNullOrWhiteSpace(sceneHandle.Result))
                sceneDisplayName = sceneHandle.Result;
        }

        if (filledSlotTemplate != null)
        {
            filledSlotTemplate.Arguments = new object[]
            {
                sceneDisplayName,
                metadata.GetLocalTimeText(),
                metadata.playerLevel
            };

            var filledHandle = filledSlotTemplate.GetLocalizedStringAsync();
            yield return filledHandle;
            infoText.text = filledHandle.Result;
        }
        else
        {
            infoText.text = sceneDisplayName + "\n" + metadata.GetLocalTimeText() + "\nLvl " + metadata.playerLevel;
        }
    }

    private void OnDestroy()
    {
        ReleaseRuntimeObjects();
    }

    private void ReleaseRuntimeObjects()
    {
        if (runtimeSprite != null)
        {
            Destroy(runtimeSprite);
            runtimeSprite = null;
        }

        if (runtimeTexture != null)
        {
            Destroy(runtimeTexture);
            runtimeTexture = null;
        }
    }
}