using UnityEngine;
using UnityEngine.UI;

public class QuestMarkerDisplayUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Icon")]
    [SerializeField] private Image markerImage;

    [Header("State Sprites")]
    [SerializeField] private Sprite availableSprite;
    [SerializeField] private Sprite inProgressSprite;
    [SerializeField] private Sprite readyToTurnInSprite;

    [Header("Quest Type Colors")]
    [SerializeField] private Color mainQuestColor = Color.yellow;
    [SerializeField] private Color sideQuestColor = Color.white;

    public void Hide()
    {
        if (root != null)
        {
            root.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void Show(QuestMarkerState state, QuestType questType)
    {
        if (state == QuestMarkerState.None)
        {
            Hide();
            return;
        }

        if (root != null)
        {
            root.SetActive(true);
        }
        else
        {
            gameObject.SetActive(true);
        }

        if (markerImage == null)
            return;

        markerImage.sprite = GetSpriteForState(state);
        markerImage.color = questType == QuestType.Main ? mainQuestColor : sideQuestColor;
        markerImage.enabled = markerImage.sprite != null;
    }

    private Sprite GetSpriteForState(QuestMarkerState state)
    {
        switch (state)
        {
            case QuestMarkerState.Available:
                return availableSprite;

            case QuestMarkerState.InProgress:
                return inProgressSprite;

            case QuestMarkerState.ReadyToTurnIn:
                return readyToTurnInSprite;

            case QuestMarkerState.None:
            default:
                return null;
        }
    }
}