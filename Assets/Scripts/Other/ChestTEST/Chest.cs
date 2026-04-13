using UnityEngine;

public class Chest : MonoBehaviour
{
    private bool isOpened;
    private bool isPlayerCanOpen;
    private int playerLayer;

    [Header("Basic Rewards")]
    [SerializeField] private int chestEXP = 10;
    [SerializeField] private int chestGold = 10;

    [Header("Item Rewards")]
    [SerializeField] private RewardItemData[] itemRewards;

    [Header("References")]
    [SerializeField] private Animator chestAnimator;
    [SerializeField] private GameObject triggerImageZone;

    private GameInput gameInput;
    private GameInput subscribedInput;

    private void Awake()
    {
        playerLayer = LayerMask.NameToLayer("Player");

        if (chestAnimator == null)
            chestAnimator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeToInput();
    }

    private void OnDisable()
    {
        UnsubscribeFromInput();
    }

    private void ResolveReferences()
    {
        gameInput = GameInput.Instance != null
            ? GameInput.Instance
            : FindFirstObjectByType<GameInput>();

        if (gameInput == null)
        {
            Debug.LogWarning($"[Chest] GameInput not found on object '{name}'. Chest will not react to Use input.", this);
        }
    }

    private void SubscribeToInput()
    {
        UnsubscribeFromInput();

        if (gameInput == null)
            return;

        gameInput.OnUse += ChestOpened;
        subscribedInput = gameInput;
    }

    private void UnsubscribeFromInput()
    {
        if (subscribedInput == null)
            return;

        subscribedInput.OnUse -= ChestOpened;
        subscribedInput = null;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer != playerLayer)
            return;

        isPlayerCanOpen = true;

        if (!isOpened && triggerImageZone != null)
            triggerImageZone.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.layer != playerLayer)
            return;

        isPlayerCanOpen = false;

        if (triggerImageZone != null)
            triggerImageZone.SetActive(false);
    }

    private void ChestOpened()
    {
        if (!isPlayerCanOpen || isOpened)
            return;

        if (RewardSystem.Instance == null)
        {
            Debug.LogError($"[Chest] RewardSystem.Instance is null on object '{name}'. Reward cannot be given.", this);
            return;
        }

        RewardData reward = new RewardData(chestEXP, chestGold, itemRewards);
        RewardSystem.Instance.GiveReward(reward);

        isOpened = true;

        if (chestAnimator != null)
            chestAnimator.SetBool("IsOpened", true);
        else
            Debug.LogWarning($"[Chest] Animator is not assigned on object '{name}'. Chest opened without animation.", this);

        if (triggerImageZone != null)
            triggerImageZone.SetActive(false);
    }
}