using UnityEngine;
using UnityEngine.Localization;

public class DialogueTriggerZone : MonoBehaviour, IDialogueSource
{
    [Header("Dialogue")]
    [SerializeField] private DialogueData dialogueData;

    [Header("Speaker Source (Optional)")]
    [SerializeField] private DialogueSpeakerData speakerData;

    [Header("Trigger Settings")]
    [SerializeField] private bool triggerOnlyOnce = true;

    private bool hasTriggered;
    private int playerLayer;

    private void Awake()
    {
        playerLayer = LayerMask.NameToLayer("Player");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer != playerLayer)
            return;

        if (triggerOnlyOnce && hasTriggered)
            return;

        if (dialogueData == null)
        {
            Debug.LogWarning($"{name}: dialogueData is missing.");
            return;
        }

        if (DialogueManager.Instance == null)
        {
            Debug.LogWarning("DialogueManager.Instance is missing.");
            return;
        }

        if (!DialogueManager.Instance.CanStartDialogue(dialogueData, this))
            return;

        bool started = DialogueManager.Instance.StartDialogue(dialogueData, this);

        if (started)
        {
            hasTriggered = true;
        }
    }

    public LocalizedString GetDialogueSpeakerName()
    {
        if (speakerData == null)
            return null;

        return speakerData.SpeakerName;
    }

    public Sprite GetDialoguePortrait()
    {
        if (speakerData == null)
            return null;

        return speakerData.Portrait;
    }
}