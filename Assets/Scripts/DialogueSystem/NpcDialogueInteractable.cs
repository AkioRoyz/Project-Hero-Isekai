using UnityEngine;
using UnityEngine.Localization;

public class NpcDialogueInteractable : MonoBehaviour, IDialogueSource
{
    [Header("Dialogue")]
    [SerializeField] private DialogueData dialogueData;

    [Header("Speaker")]
    [SerializeField] private DialogueSpeakerData speakerData;

    [Header("NPC Role")]
    [SerializeField] private DialogueNpcRole npcRole = DialogueNpcRole.Regular;

    [Header("Quest")]
    [SerializeField] private string npcId;

    [Tooltip("Если включено, сам старт диалога будет считаться разговором для старой системы TalkToNpc по npcId. Для новой удобной схемы обычно оставляем выключенным.")]
    [SerializeField] private bool notifyQuestTalkOnDialogueStart = false;

    [Header("Interaction")]
    [SerializeField] private GameObject interactionHintObject;

    private bool isPlayerInside;
    private int playerLayer;

    private GameInput gameInput;
    private GameInput subscribedInput;

    public DialogueNpcRole NpcRole => npcRole;
    public string NpcId => npcId;

    private void Awake()
    {
        playerLayer = LayerMask.NameToLayer("Player");
    }

    private void Start()
    {
        RefreshHint();
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeToInput();
        RefreshHint();
    }

    private void OnDisable()
    {
        UnsubscribeFromInput();
    }

    private void Update()
    {
        if (isPlayerInside)
        {
            RefreshHint();
        }
    }

    private void ResolveReferences()
    {
        gameInput = GameInput.Instance != null
            ? GameInput.Instance
            : FindFirstObjectByType<GameInput>();

        if (gameInput == null)
        {
            Debug.LogWarning($"[NpcDialogueInteractable] GameInput not found on '{name}'. NPC interaction will not work until GameInput exists.", this);
        }
    }

    private void SubscribeToInput()
    {
        UnsubscribeFromInput();

        if (gameInput == null)
            return;

        gameInput.OnUse += HandleUsePressed;
        subscribedInput = gameInput;
    }

    private void UnsubscribeFromInput()
    {
        if (subscribedInput == null)
            return;

        subscribedInput.OnUse -= HandleUsePressed;
        subscribedInput = null;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer != playerLayer)
            return;

        isPlayerInside = true;
        RefreshHint();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.layer != playerLayer)
            return;

        isPlayerInside = false;
        RefreshHint();
    }

    private void HandleUsePressed()
    {
        if (!isPlayerInside)
            return;

        if (dialogueData == null)
        {
            Debug.LogWarning($"{name}: dialogueData is missing.", this);
            return;
        }

        if (DialogueManager.Instance == null)
        {
            Debug.LogWarning($"{name}: DialogueManager.Instance is missing.", this);
            return;
        }

        if (!DialogueManager.Instance.CanStartDialogue(dialogueData, this))
        {
            RefreshHint();
            return;
        }

        bool started = DialogueManager.Instance.StartDialogue(dialogueData, this);

        if (!started)
        {
            RefreshHint();
            return;
        }

        if (notifyQuestTalkOnDialogueStart &&
            QuestManager.Instance != null &&
            !string.IsNullOrWhiteSpace(npcId))
        {
            QuestManager.Instance.NotifyNpcTalked(npcId);
        }

        RefreshHint();
    }

    private void RefreshHint()
    {
        if (interactionHintObject == null)
            return;

        bool shouldShow =
            isPlayerInside &&
            dialogueData != null &&
            DialogueManager.Instance != null &&
            DialogueManager.Instance.CanStartDialogue(dialogueData, this);

        interactionHintObject.SetActive(shouldShow);
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