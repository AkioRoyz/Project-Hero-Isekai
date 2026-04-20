using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class MerchantMenuUI : MonoBehaviour
{
    private enum MerchantTab
    {
        Buy = 0,
        Sell = 1
    }

    private enum MerchantStatus
    {
        None = 0,
        SoldOut = 1,
        InsufficientGold = 2,
        Unavailable = 3
    }

    private sealed class MerchantViewEntry
    {
        public ItemData Item;
        public MerchantStockEntry StockEntry;
        public int Price;
        public int Amount;
        public bool IsInfinite;
        public bool IsSoldOut;
    }

    public static MerchantMenuUI Instance { get; private set; }

    [Header("Window")]
    [SerializeField] private GameObject windowRoot;

    [Header("List")]
    [SerializeField] private MerchantListRowUI[] visibleRows = new MerchantListRowUI[10];
    [SerializeField] private GameObject moreUpIcon;
    [SerializeField] private GameObject moreDownIcon;
    [SerializeField] private GameObject emptyLabelObject;
    [SerializeField] private TextMeshProUGUI emptyLabelText;

    [Header("Tabs")]
    [SerializeField] private TextMeshProUGUI buyTabText;
    [SerializeField] private TextMeshProUGUI sellTabText;
    [SerializeField] private Color activeTabColor = Color.white;
    [SerializeField] private Color inactiveTabColor = new Color(1f, 1f, 1f, 0.6f);

    [Header("Details")]
    [SerializeField] private TextMeshProUGUI goldHeadingText;
    [SerializeField] private TextMeshProUGUI goldValueText;
    [SerializeField] private TextMeshProUGUI priceHeadingText;
    [SerializeField] private TextMeshProUGUI amountHeadingText;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [SerializeField] private TextMeshProUGUI priceValueText;
    [SerializeField] private TextMeshProUGUI amountValueText;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Localized Strings")]
    [SerializeField] private LocalizedString buyTabLocalized;
    [SerializeField] private LocalizedString sellTabLocalized;
    [SerializeField] private LocalizedString backLocalized;
    [SerializeField] private LocalizedString goldHeadingLocalized;
    [SerializeField] private LocalizedString priceHeadingLocalized;
    [SerializeField] private LocalizedString stockHeadingLocalized;
    [SerializeField] private LocalizedString ownedHeadingLocalized;
    [SerializeField] private LocalizedString emptyBuyLocalized;
    [SerializeField] private LocalizedString emptySellLocalized;
    [SerializeField] private LocalizedString soldOutLocalized;
    [SerializeField] private LocalizedString insufficientGoldLocalized;
    [SerializeField] private LocalizedString unavailableLocalized;
    [SerializeField] private LocalizedString infiniteStockLocalized;

    private readonly List<MerchantViewEntry> currentEntries = new();

    private GameInput gameInput;
    private InventorySystem inventorySystem;
    private GoldSystem goldSystem;

    private MerchantData currentMerchantData;
    private IMerchantSource currentMerchantSource;

    private MerchantTab currentTab = MerchantTab.Buy;
    private MerchantStatus currentStatus = MerchantStatus.None;

    private int selectedIndex;
    private int firstVisibleIndex;

    private bool IsOpen => windowRoot != null && windowRoot.activeSelf;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (windowRoot != null)
            windowRoot.SetActive(false);
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeToEvents();
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        UnsubscribeFromEvents();
    }

    public bool OpenForMerchant(IMerchantSource merchantSource)
    {
        ResolveReferences();

        if (windowRoot == null)
        {
            Debug.LogWarning("MerchantMenuUI: windowRoot is missing.", this);
            return false;
        }

        if (merchantSource == null || merchantSource.MerchantData == null)
        {
            Debug.LogWarning("MerchantMenuUI: merchant source or merchant data is missing.", this);
            return false;
        }

        currentMerchantSource = merchantSource;
        currentMerchantData = merchantSource.MerchantData;

        currentTab = MerchantTab.Buy;
        currentStatus = MerchantStatus.None;
        selectedIndex = 0;
        firstVisibleIndex = 0;

        windowRoot.SetActive(true);

        if (gameInput != null)
            gameInput.SwitchToMenuMode();

        RefreshAll();
        return true;
    }

    public void CloseAndReturnToDialogue()
    {
        currentMerchantSource = null;
        currentMerchantData = null;
        currentEntries.Clear();

        currentTab = MerchantTab.Buy;
        currentStatus = MerchantStatus.None;
        selectedIndex = 0;
        firstVisibleIndex = 0;

        if (windowRoot != null)
            windowRoot.SetActive(false);

        if (gameInput != null)
        {
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
                gameInput.SwitchToDialogueMode();
            else
                gameInput.SwitchToPlayerMode();
        }
    }

    private void ResolveReferences()
    {
        if (gameInput == null)
            gameInput = FindFirstObjectByType<GameInput>();

        inventorySystem = InventorySystem.Instance;

        if (goldSystem == null)
            goldSystem = FindFirstObjectByType<GoldSystem>();
    }

    private void SubscribeToEvents()
    {
        UnsubscribeFromEvents();

        if (gameInput != null)
        {
            gameInput.OnMenuUp += HandleMenuUp;
            gameInput.OnMenuDown += HandleMenuDown;
            gameInput.OnMenuLeft += HandleMenuLeft;
            gameInput.OnMenuRight += HandleMenuRight;
            gameInput.OnMenuSelect += HandleMenuSelect;
            gameInput.OnMenuClose += HandleMenuClose;
        }

        if (inventorySystem != null)
            inventorySystem.OnInventoryChanged += HandleInventoryChanged;
    }

    private void UnsubscribeFromEvents()
    {
        if (gameInput != null)
        {
            gameInput.OnMenuUp -= HandleMenuUp;
            gameInput.OnMenuDown -= HandleMenuDown;
            gameInput.OnMenuLeft -= HandleMenuLeft;
            gameInput.OnMenuRight -= HandleMenuRight;
            gameInput.OnMenuSelect -= HandleMenuSelect;
            gameInput.OnMenuClose -= HandleMenuClose;
        }

        if (inventorySystem != null)
            inventorySystem.OnInventoryChanged -= HandleInventoryChanged;
    }

    private void HandleInventoryChanged()
    {
        if (!IsOpen)
            return;

        RefreshAll();
    }

    private void HandleMenuUp()
    {
        if (!IsOpen)
            return;

        currentStatus = MerchantStatus.None;
        MoveSelection(-1);
    }

    private void HandleMenuDown()
    {
        if (!IsOpen)
            return;

        currentStatus = MerchantStatus.None;
        MoveSelection(1);
    }

    private void HandleMenuLeft()
    {
        if (!IsOpen)
            return;

        SwitchTab(MerchantTab.Buy);
    }

    private void HandleMenuRight()
    {
        if (!IsOpen)
            return;

        SwitchTab(MerchantTab.Sell);
    }

    private void HandleMenuSelect()
    {
        if (!IsOpen)
            return;

        if (IsBackSelected())
        {
            CloseAndReturnToDialogue();
            return;
        }

        if (currentTab == MerchantTab.Buy)
            TryBuySelected();
        else
            TrySellSelected();
    }

    private void HandleMenuClose()
    {
        if (!IsOpen)
            return;

        CloseAndReturnToDialogue();
    }

    private void MoveSelection(int direction)
    {
        int totalCount = GetTotalEntryCount();
        if (totalCount <= 0)
            return;

        selectedIndex += direction;

        if (selectedIndex < 0)
            selectedIndex = totalCount - 1;
        else if (selectedIndex >= totalCount)
            selectedIndex = 0;

        AdjustWindowToSelection();
        RefreshAll();
    }

    private void SwitchTab(MerchantTab targetTab)
    {
        if (currentTab == targetTab)
            return;

        currentTab = targetTab;
        currentStatus = MerchantStatus.None;
        selectedIndex = 0;
        firstVisibleIndex = 0;

        RefreshAll();
    }

    private void TryBuySelected()
    {
        MerchantViewEntry entry = GetSelectedEntry();
        if (entry == null)
            return;

        if (entry.IsSoldOut)
        {
            currentStatus = MerchantStatus.SoldOut;
            RefreshAll();
            return;
        }

        if (inventorySystem == null || goldSystem == null)
        {
            currentStatus = MerchantStatus.Unavailable;
            RefreshAll();
            return;
        }

        if (!goldSystem.HasEnoughGold(entry.Price))
        {
            currentStatus = MerchantStatus.InsufficientGold;
            RefreshAll();
            return;
        }

        bool spent = goldSystem.SpendGold(entry.Price);
        if (!spent)
        {
            currentStatus = MerchantStatus.Unavailable;
            RefreshAll();
            return;
        }

        inventorySystem.AddItem(entry.Item, 1);

        if (entry.StockEntry != null && !entry.StockEntry.IsInfinite)
            MerchantRuntimeState.TryConsumeOne(currentMerchantData, entry.StockEntry);

        currentStatus = MerchantStatus.None;
        RefreshAll();
    }

    private void TrySellSelected()
    {
        MerchantViewEntry entry = GetSelectedEntry();
        if (entry == null)
            return;

        if (inventorySystem == null || goldSystem == null)
        {
            currentStatus = MerchantStatus.Unavailable;
            RefreshAll();
            return;
        }

        int goldBefore = goldSystem.GoldAmount;

        bool removed = inventorySystem.RemoveItem(entry.Item, 1);
        if (!removed)
        {
            currentStatus = MerchantStatus.Unavailable;
            RefreshAll();
            return;
        }

        goldSystem.AddGold(entry.Price);

        if (goldSystem.GoldAmount < goldBefore + entry.Price)
        {
            inventorySystem.AddItem(entry.Item, 1);
            currentStatus = MerchantStatus.Unavailable;
            RefreshAll();
            return;
        }

        currentStatus = MerchantStatus.None;
        RefreshAll();
    }

    private MerchantViewEntry GetSelectedEntry()
    {
        if (selectedIndex < 0 || selectedIndex >= currentEntries.Count)
            return null;

        return currentEntries[selectedIndex];
    }

    private bool IsBackSelected()
    {
        return selectedIndex == currentEntries.Count;
    }

    private int GetTotalEntryCount()
    {
        return currentEntries.Count + 1; // + Back
    }

    private void BuildCurrentEntries()
    {
        currentEntries.Clear();

        if (currentMerchantData == null)
            return;

        if (currentTab == MerchantTab.Buy)
            BuildBuyEntries();
        else
            BuildSellEntries();
    }

    private void BuildBuyEntries()
    {
        IReadOnlyList<MerchantStockEntry> source = currentMerchantData.StockEntries;
        if (source == null)
            return;

        for (int i = 0; i < source.Count; i++)
        {
            MerchantStockEntry stockEntry = source[i];
            if (stockEntry == null || stockEntry.Item == null)
                continue;

            int remaining = MerchantRuntimeState.GetRemainingStock(currentMerchantData, stockEntry);

            MerchantViewEntry entry = new MerchantViewEntry();
            entry.Item = stockEntry.Item;
            entry.StockEntry = stockEntry;
            entry.Price = Mathf.Max(0, stockEntry.Item.BasePrice);
            entry.Amount = stockEntry.IsInfinite ? int.MaxValue : remaining;
            entry.IsInfinite = stockEntry.IsInfinite;
            entry.IsSoldOut = !stockEntry.IsInfinite && remaining <= 0;

            currentEntries.Add(entry);
        }
    }

    private void BuildSellEntries()
    {
        if (inventorySystem == null)
            return;

        Dictionary<string, MerchantViewEntry> entriesByItemId = new Dictionary<string, MerchantViewEntry>();

        AppendSellEntries(inventorySystem.ConsumableItems, entriesByItemId);
        AppendSellEntries(inventorySystem.EquipmentItems, entriesByItemId);
        AppendSellEntries(inventorySystem.QuestItems, entriesByItemId);
    }

    private void AppendSellEntries(
        IReadOnlyList<InventoryEntry> source,
        Dictionary<string, MerchantViewEntry> entriesByItemId)
    {
        if (source == null)
            return;

        for (int i = 0; i < source.Count; i++)
        {
            InventoryEntry inventoryEntry = source[i];
            if (inventoryEntry == null || inventoryEntry.Item == null || inventoryEntry.Amount <= 0)
                continue;

            ItemData item = inventoryEntry.Item;
            if (!item.CanSellToMerchant)
                continue;

            int sellPrice = GetSellPrice(item);
            if (sellPrice <= 0)
                continue;

            string itemId = string.IsNullOrWhiteSpace(item.ItemId) ? item.name : item.ItemId;

            if (entriesByItemId.TryGetValue(itemId, out MerchantViewEntry existingEntry))
            {
                existingEntry.Amount += inventoryEntry.Amount;
            }
            else
            {
                MerchantViewEntry entry = new MerchantViewEntry();
                entry.Item = item;
                entry.StockEntry = null;
                entry.Price = sellPrice;
                entry.Amount = inventoryEntry.Amount;
                entry.IsInfinite = false;
                entry.IsSoldOut = false;

                entriesByItemId.Add(itemId, entry);
                currentEntries.Add(entry);
            }
        }
    }

    private void ClampSelection()
    {
        int totalCount = GetTotalEntryCount();
        selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, totalCount - 1));
    }

    private void AdjustWindowToSelection()
    {
        int maxVisible = Mathf.Max(1, visibleRows.Length);
        int totalCount = GetTotalEntryCount();

        if (totalCount <= maxVisible)
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

        int maxStartIndex = Mathf.Max(0, totalCount - maxVisible);
        firstVisibleIndex = Mathf.Clamp(firstVisibleIndex, 0, maxStartIndex);
    }

    private void RefreshAll()
    {
        if (!IsOpen)
            return;

        ResolveReferences();
        BuildCurrentEntries();
        ClampSelection();
        AdjustWindowToSelection();

        RefreshTabs();
        RefreshHeader();
        RefreshRows();
        RefreshDetails();
    }

    private void RefreshTabs()
    {
        if (buyTabText != null)
        {
            buyTabText.text = ResolveLocalizedString(buyTabLocalized, "Buy");
            buyTabText.color = currentTab == MerchantTab.Buy ? activeTabColor : inactiveTabColor;
        }

        if (sellTabText != null)
        {
            sellTabText.text = ResolveLocalizedString(sellTabLocalized, "Sell");
            sellTabText.color = currentTab == MerchantTab.Sell ? activeTabColor : inactiveTabColor;
        }
    }

    private void RefreshHeader()
    {
        if (goldHeadingText != null)
            goldHeadingText.text = ResolveLocalizedString(goldHeadingLocalized, "Gold");

        if (goldValueText != null)
            goldValueText.text = goldSystem != null ? goldSystem.GoldAmount.ToString() : "0";

        if (priceHeadingText != null)
            priceHeadingText.text = ResolveLocalizedString(priceHeadingLocalized, "Price");

        if (amountHeadingText != null)
        {
            amountHeadingText.text = currentTab == MerchantTab.Buy
                ? ResolveLocalizedString(stockHeadingLocalized, "Stock")
                : ResolveLocalizedString(ownedHeadingLocalized, "Owned");
        }

        if (emptyLabelObject != null)
            emptyLabelObject.SetActive(currentEntries.Count == 0);

        if (emptyLabelText != null)
        {
            emptyLabelText.text = currentTab == MerchantTab.Buy
                ? ResolveLocalizedString(emptyBuyLocalized, "No items available.")
                : ResolveLocalizedString(emptySellLocalized, "Nothing to sell.");
        }
    }

    private void RefreshRows()
    {
        int totalCount = GetTotalEntryCount();
        string infiniteText = ResolveLocalizedString(infiniteStockLocalized, "∞");
        string backText = ResolveLocalizedString(backLocalized, "Back");

        for (int rowIndex = 0; rowIndex < visibleRows.Length; rowIndex++)
        {
            MerchantListRowUI rowUI = visibleRows[rowIndex];
            if (rowUI == null)
                continue;

            int dataIndex = firstVisibleIndex + rowIndex;

            if (dataIndex >= totalCount)
            {
                rowUI.gameObject.SetActive(false);
                continue;
            }

            rowUI.gameObject.SetActive(true);

            if (dataIndex == currentEntries.Count)
            {
                rowUI.SetData(
                    null,
                    false,
                    backText,
                    string.Empty,
                    string.Empty,
                    dataIndex == selectedIndex,
                    false);

                continue;
            }

            MerchantViewEntry entry = currentEntries[dataIndex];
            string amountText = currentTab == MerchantTab.Buy
                ? (entry.IsInfinite ? infiniteText : entry.Amount.ToString())
                : entry.Amount.ToString();

            bool disabled = currentTab == MerchantTab.Buy && entry.IsSoldOut;

            rowUI.SetData(
                entry.Item != null ? entry.Item.Icon : null,
                true,
                GetItemDisplayName(entry.Item),
                entry.Price.ToString(),
                amountText,
                dataIndex == selectedIndex,
                disabled);
        }

        bool showUp = firstVisibleIndex > 0;
        bool showDown = firstVisibleIndex + visibleRows.Length < totalCount;

        if (moreUpIcon != null)
            moreUpIcon.SetActive(showUp);

        if (moreDownIcon != null)
            moreDownIcon.SetActive(showDown);
    }

    private void RefreshDetails()
    {
        string infiniteText = ResolveLocalizedString(infiniteStockLocalized, "∞");
        string backText = ResolveLocalizedString(backLocalized, "Back");

        if (IsBackSelected() || currentEntries.Count == 0)
        {
            if (itemNameText != null)
                itemNameText.text = backText;

            if (itemDescriptionText != null)
                itemDescriptionText.text = string.Empty;

            if (priceValueText != null)
                priceValueText.text = string.Empty;

            if (amountValueText != null)
                amountValueText.text = string.Empty;

            if (statusText != null)
                statusText.text = string.Empty;

            return;
        }

        MerchantViewEntry entry = currentEntries[selectedIndex];

        if (itemNameText != null)
            itemNameText.text = GetItemDisplayName(entry.Item);

        if (itemDescriptionText != null)
            itemDescriptionText.text = GetItemDescription(entry.Item);

        if (priceValueText != null)
            priceValueText.text = entry.Price.ToString();

        if (amountValueText != null)
        {
            amountValueText.text = currentTab == MerchantTab.Buy
                ? (entry.IsInfinite ? infiniteText : entry.Amount.ToString())
                : entry.Amount.ToString();
        }

        if (statusText != null)
            statusText.text = GetStatusText(entry);
    }

    private string GetStatusText(MerchantViewEntry entry)
    {
        if (currentTab == MerchantTab.Buy && entry != null && entry.IsSoldOut)
            return ResolveLocalizedString(soldOutLocalized, "Sold out");

        switch (currentStatus)
        {
            case MerchantStatus.SoldOut:
                return ResolveLocalizedString(soldOutLocalized, "Sold out");

            case MerchantStatus.InsufficientGold:
                return ResolveLocalizedString(insufficientGoldLocalized, "Not enough gold");

            case MerchantStatus.Unavailable:
                return ResolveLocalizedString(unavailableLocalized, "Unavailable");

            default:
                return string.Empty;
        }
    }

    private static int GetSellPrice(ItemData item)
    {
        if (item == null)
            return 0;

        int basePrice = Mathf.Max(0, item.BasePrice);
        if (basePrice <= 0)
            return 0;

        return Mathf.Max(1, Mathf.FloorToInt(basePrice * 0.8f));
    }

    private static string GetItemDisplayName(ItemData item)
    {
        if (item == null)
            return "NULL";

        if (item.ItemName == null || item.ItemName.IsEmpty)
            return item.ItemId;

        string localized = LocalizationSettings.StringDatabase.GetLocalizedString(
            item.ItemName.TableReference,
            item.ItemName.TableEntryReference);

        if (!string.IsNullOrEmpty(localized))
            return localized;

        return item.ItemId;
    }

    private static string GetItemDescription(ItemData item)
    {
        if (item == null)
            return string.Empty;

        if (item.ItemDescription == null || item.ItemDescription.IsEmpty)
            return string.Empty;

        string localized = LocalizationSettings.StringDatabase.GetLocalizedString(
            item.ItemDescription.TableReference,
            item.ItemDescription.TableEntryReference);

        return string.IsNullOrEmpty(localized) ? string.Empty : localized;
    }

    private static string ResolveLocalizedString(LocalizedString localizedString, string fallback)
    {
        if (localizedString == null || localizedString.IsEmpty)
            return fallback;

        string localized = LocalizationSettings.StringDatabase.GetLocalizedString(
            localizedString.TableReference,
            localizedString.TableEntryReference);

        return string.IsNullOrEmpty(localized) ? fallback : localized;
    }
}