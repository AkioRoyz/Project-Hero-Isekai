using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameInput : MonoBehaviour
{
    public static GameInput Instance { get; private set; }

    public enum InputMode
    {
        Player,
        Dialogue,
        Menu,
        QuestJournal,
        PauseMenu
    }

    public enum DeviceGroupType
    {
        KeyboardMouse,
        Gamepad
    }

    private InputSystem_Actions inputActions;

    private InputActionMap playerActionMap;
    private InputAction playerAttackAction;
    private InputAction playerGuardAction;
    private InputAction playerSpecialAction;
    private InputAction playerToggleTargetingAction;

    private bool missingCombatActionsWarningShown;

    public event Action OnAttackStarted;
    public event Action OnAttack;
    public event Action OnAttackCanceled;

    public event Action OnGuardStarted;
    public event Action OnGuardCanceled;
    public event Action OnSpecialPerformed;
    public event Action OnToggleTargeting;

    public event Action OnUse;
    public event Action OnStats;
    public event Action OnQuestJournal;
    public event Action OnPauseToggle;
    public event Action<int> OnQuickSlotPressed;

    public event Action OnDialogueUp;
    public event Action OnDialogueDown;
    public event Action OnDialogueSelect;

    public event Action OnMenuUp;
    public event Action OnMenuDown;
    public event Action OnMenuLeft;
    public event Action OnMenuRight;
    public event Action OnMenuSelect;
    public event Action OnMenuUnequip;
    public event Action OnMenuClose;

    public event Action OnQuestJournalUp;
    public event Action OnQuestJournalDown;
    public event Action OnQuestJournalSelect;
    public event Action OnQuestJournalBack;
    public event Action OnQuestJournalMainTab;
    public event Action OnQuestJournalSideTab;
    public event Action OnQuestJournalPinQuest;
    public event Action OnQuestJournalClose;

    public event Action OnPauseMenuUp;
    public event Action OnPauseMenuDown;
    public event Action OnPauseMenuSelect;

    public event Action OnActiveDeviceGroupChanged;

    public Vector2 MoveVector { get; private set; }
    public InputMode CurrentMode { get; private set; }

    public DeviceGroupType CurrentDeviceGroup { get; private set; } = DeviceGroupType.KeyboardMouse;
    public string CurrentDeviceLayoutName { get; private set; } = "Keyboard";

    public bool HasGuardAction => playerGuardAction != null;
    public bool HasSpecialAction => playerSpecialAction != null;
    public bool HasToggleTargetingAction => playerToggleTargetingAction != null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        EnsureInitialized();
        SwitchToPlayerMode();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            UnsubscribeFromInput();

            if (inputActions != null)
                inputActions.Dispose();

            Instance = null;
        }
    }

    private void Update()
    {
        if (inputActions == null)
        {
            MoveVector = Vector2.zero;
            return;
        }

        if (CurrentMode == InputMode.Player)
            MoveVector = inputActions.Player.Moving.ReadValue<Vector2>().normalized;
        else
            MoveVector = Vector2.zero;
    }

    private void EnsureInitialized()
    {
        if (inputActions != null)
            return;

        inputActions = new InputSystem_Actions();
        CacheActionReferences();
        SubscribeToInput();
    }

    private void CacheActionReferences()
    {
        if (inputActions == null || inputActions.asset == null)
            return;

        playerActionMap = inputActions.asset.FindActionMap("Player", false);

        playerAttackAction = inputActions.Player.Attack;
        playerGuardAction = FindOptionalPlayerAction("Guard");
        playerSpecialAction = FindOptionalPlayerAction("Special");
        playerToggleTargetingAction = FindOptionalPlayerAction("ToggleTargeting");

        ShowMissingCombatActionWarningOnce();
    }

    private InputAction FindOptionalPlayerAction(string actionName)
    {
        if (playerActionMap == null)
            return null;

        return playerActionMap.FindAction(actionName, false);
    }

    private void ShowMissingCombatActionWarningOnce()
    {
        if (missingCombatActionsWarningShown)
            return;

        List<string> missingActions = new List<string>();

        if (playerGuardAction == null)
            missingActions.Add("Player/Guard");

        if (playerSpecialAction == null)
            missingActions.Add("Player/Special");

        if (playerToggleTargetingAction == null)
            missingActions.Add("Player/ToggleTargeting");

        if (missingActions.Count > 0)
        {
            Debug.LogWarning(
                "GameInput: optional combat actions are missing from InputSystem_Actions: " +
                string.Join(", ", missingActions) +
                ". Add them in the Player action map to enable guard, special and targeting toggle."
            );
        }

        missingCombatActionsWarningShown = true;
    }

    public string GetCurrentBindingGroupName()
    {
        return CurrentDeviceGroup == DeviceGroupType.Gamepad ? "Gamepad" : "KeyboardMouse";
    }

    public string GetCurrentDeviceLayoutName()
    {
        return CurrentDeviceLayoutName;
    }

    public InputAction GetMenuAction_Select()
    {
        EnsureInitialized();
        return inputActions.Menu.SelectChoise;
    }

    public InputAction GetMenuAction_Unequip()
    {
        EnsureInitialized();
        return inputActions.Menu.UnequipItem;
    }

    public bool IsAttackPressed()
    {
        return playerAttackAction != null && playerAttackAction.IsPressed();
    }

    public bool IsGuardPressed()
    {
        return playerGuardAction != null && playerGuardAction.IsPressed();
    }

    private void SubscribeToInput()
    {
        if (playerAttackAction != null)
        {
            playerAttackAction.started += OnAttackStartedPerformed;
            playerAttackAction.performed += OnAttackPerformed;
            playerAttackAction.canceled += OnAttackCanceledPerformed;
        }

        if (playerGuardAction != null)
        {
            playerGuardAction.started += OnGuardStartedPerformed;
            playerGuardAction.canceled += OnGuardCanceledPerformed;
        }

        if (playerSpecialAction != null)
            playerSpecialAction.performed += OnSpecialPerformedInternal;

        if (playerToggleTargetingAction != null)
            playerToggleTargetingAction.performed += OnToggleTargetingPerformedInternal;

        // Player
        inputActions.Player.Use.performed += OnUsePerformed;
        inputActions.Player.Stats.performed += OnStatsPerformed;
        inputActions.Player.QuestJournal.performed += OnQuestJournalPerformed;
        inputActions.Player.Pause.performed += OnPausePerformed;
        inputActions.Player.Item1.performed += OnItem1Performed;
        inputActions.Player.Item2.performed += OnItem2Performed;
        inputActions.Player.Item3.performed += OnItem3Performed;
        inputActions.Player.Item4.performed += OnItem4Performed;
        inputActions.Player.Item5.performed += OnItem5Performed;

        // Dialogue
        inputActions.Dialogue.UpSelect.performed += OnDialogueUpPerformed;
        inputActions.Dialogue.DownSelect.performed += OnDialogueDownPerformed;
        inputActions.Dialogue.SelectChoise.performed += OnDialogueSelectPerformed;

        // Menu
        inputActions.Menu.UpSelect.performed += OnMenuUpPerformed;
        inputActions.Menu.DownSelect.performed += OnMenuDownPerformed;
        inputActions.Menu.LeftSelect.performed += OnMenuLeftPerformed;
        inputActions.Menu.RightSelect.performed += OnMenuRightPerformed;
        inputActions.Menu.SelectChoise.performed += OnMenuSelectPerformed;
        inputActions.Menu.UnequipItem.performed += OnMenuUnequipPerformed;
        inputActions.Menu.CloseUI.performed += OnMenuClosePerformed;

        // Quest Journal
        inputActions.QuestJournal.UpSelect.performed += OnQuestJournalUpPerformed;
        inputActions.QuestJournal.DownSelect.performed += OnQuestJournalDownPerformed;
        inputActions.QuestJournal.Select.performed += OnQuestJournalSelectPerformed;
        inputActions.QuestJournal.Back.performed += OnQuestJournalBackPerformed;
        inputActions.QuestJournal.MainTab.performed += OnQuestJournalMainTabPerformed;
        inputActions.QuestJournal.SideTab.performed += OnQuestJournalSideTabPerformed;
        inputActions.QuestJournal.PinQuest.performed += OnQuestJournalPinQuestPerformed;
        inputActions.QuestJournal.CloseUI.performed += OnQuestJournalClosePerformed;

        // Pause Menu
        inputActions.PauseMenu.UpSelect.performed += OnPauseMenuUpPerformed;
        inputActions.PauseMenu.DownSelect.performed += OnPauseMenuDownPerformed;
        inputActions.PauseMenu.SelectChoise.performed += OnPauseMenuSelectPerformed;
        inputActions.PauseMenu.TogglePause.performed += OnPauseMenuTogglePerformed;
    }

    private void UnsubscribeFromInput()
    {
        if (inputActions == null)
            return;

        if (playerAttackAction != null)
        {
            playerAttackAction.started -= OnAttackStartedPerformed;
            playerAttackAction.performed -= OnAttackPerformed;
            playerAttackAction.canceled -= OnAttackCanceledPerformed;
        }

        if (playerGuardAction != null)
        {
            playerGuardAction.started -= OnGuardStartedPerformed;
            playerGuardAction.canceled -= OnGuardCanceledPerformed;
        }

        if (playerSpecialAction != null)
            playerSpecialAction.performed -= OnSpecialPerformedInternal;

        if (playerToggleTargetingAction != null)
            playerToggleTargetingAction.performed -= OnToggleTargetingPerformedInternal;

        // Player
        inputActions.Player.Use.performed -= OnUsePerformed;
        inputActions.Player.Stats.performed -= OnStatsPerformed;
        inputActions.Player.QuestJournal.performed -= OnQuestJournalPerformed;
        inputActions.Player.Pause.performed -= OnPausePerformed;
        inputActions.Player.Item1.performed -= OnItem1Performed;
        inputActions.Player.Item2.performed -= OnItem2Performed;
        inputActions.Player.Item3.performed -= OnItem3Performed;
        inputActions.Player.Item4.performed -= OnItem4Performed;
        inputActions.Player.Item5.performed -= OnItem5Performed;

        // Dialogue
        inputActions.Dialogue.UpSelect.performed -= OnDialogueUpPerformed;
        inputActions.Dialogue.DownSelect.performed -= OnDialogueDownPerformed;
        inputActions.Dialogue.SelectChoise.performed -= OnDialogueSelectPerformed;

        // Menu
        inputActions.Menu.UpSelect.performed -= OnMenuUpPerformed;
        inputActions.Menu.DownSelect.performed -= OnMenuDownPerformed;
        inputActions.Menu.LeftSelect.performed -= OnMenuLeftPerformed;
        inputActions.Menu.RightSelect.performed -= OnMenuRightPerformed;
        inputActions.Menu.SelectChoise.performed -= OnMenuSelectPerformed;
        inputActions.Menu.UnequipItem.performed -= OnMenuUnequipPerformed;
        inputActions.Menu.CloseUI.performed -= OnMenuClosePerformed;

        // Quest Journal
        inputActions.QuestJournal.UpSelect.performed -= OnQuestJournalUpPerformed;
        inputActions.QuestJournal.DownSelect.performed -= OnQuestJournalDownPerformed;
        inputActions.QuestJournal.Select.performed -= OnQuestJournalSelectPerformed;
        inputActions.QuestJournal.Back.performed -= OnQuestJournalBackPerformed;
        inputActions.QuestJournal.MainTab.performed -= OnQuestJournalMainTabPerformed;
        inputActions.QuestJournal.SideTab.performed -= OnQuestJournalSideTabPerformed;
        inputActions.QuestJournal.PinQuest.performed -= OnQuestJournalPinQuestPerformed;
        inputActions.QuestJournal.CloseUI.performed -= OnQuestJournalClosePerformed;

        // Pause Menu
        inputActions.PauseMenu.UpSelect.performed -= OnPauseMenuUpPerformed;
        inputActions.PauseMenu.DownSelect.performed -= OnPauseMenuDownPerformed;
        inputActions.PauseMenu.SelectChoise.performed -= OnPauseMenuSelectPerformed;
        inputActions.PauseMenu.TogglePause.performed -= OnPauseMenuTogglePerformed;
    }

    public void SwitchToPlayerMode()
    {
        EnsureInitialized();

        inputActions.Player.Enable();
        inputActions.Dialogue.Disable();
        inputActions.Menu.Disable();
        inputActions.QuestJournal.Disable();
        inputActions.PauseMenu.Disable();

        MoveVector = Vector2.zero;
        CurrentMode = InputMode.Player;
    }

    public void SwitchToDialogueMode()
    {
        EnsureInitialized();

        inputActions.Player.Disable();
        inputActions.Dialogue.Enable();
        inputActions.Menu.Disable();
        inputActions.QuestJournal.Disable();
        inputActions.PauseMenu.Disable();

        MoveVector = Vector2.zero;
        CurrentMode = InputMode.Dialogue;
    }

    public void SwitchToMenuMode()
    {
        EnsureInitialized();

        inputActions.Player.Disable();
        inputActions.Dialogue.Disable();
        inputActions.Menu.Enable();
        inputActions.QuestJournal.Disable();
        inputActions.PauseMenu.Disable();

        MoveVector = Vector2.zero;
        CurrentMode = InputMode.Menu;
    }

    public void SwitchToQuestJournalMode()
    {
        EnsureInitialized();

        inputActions.Player.Disable();
        inputActions.Dialogue.Disable();
        inputActions.Menu.Disable();
        inputActions.QuestJournal.Enable();
        inputActions.PauseMenu.Disable();

        MoveVector = Vector2.zero;
        CurrentMode = InputMode.QuestJournal;
    }

    public void SwitchToPauseMenuMode()
    {
        EnsureInitialized();

        inputActions.Player.Disable();
        inputActions.Dialogue.Disable();
        inputActions.Menu.Disable();
        inputActions.QuestJournal.Disable();
        inputActions.PauseMenu.Enable();

        MoveVector = Vector2.zero;
        CurrentMode = InputMode.PauseMenu;
    }

    private void UpdateDeviceGroup(InputAction.CallbackContext context)
    {
        if (context.control == null || context.control.device == null)
            return;

        InputDevice device = context.control.device;
        DeviceGroupType newGroup;

        if (device is Keyboard || device is Mouse)
            newGroup = DeviceGroupType.KeyboardMouse;
        else
            newGroup = DeviceGroupType.Gamepad;

        bool groupChanged = newGroup != CurrentDeviceGroup;
        bool layoutChanged = CurrentDeviceLayoutName != device.layout;

        CurrentDeviceGroup = newGroup;
        CurrentDeviceLayoutName = device.layout;

        if (groupChanged || layoutChanged)
            OnActiveDeviceGroupChanged?.Invoke();

        Debug.Log($"Active input device: {device.displayName}, layout: {device.layout}, group: {CurrentDeviceGroup}");
    }

    // -------------------- Player --------------------

    private void OnAttackStartedPerformed(InputAction.CallbackContext context)
    {
        if (CurrentMode != InputMode.Player)
            return;

        UpdateDeviceGroup(context);
        OnAttackStarted?.Invoke();
    }

    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        if (CurrentMode != InputMode.Player)
            return;

        UpdateDeviceGroup(context);
        OnAttack?.Invoke();
    }

    private void OnAttackCanceledPerformed(InputAction.CallbackContext context)
    {
        if (CurrentMode != InputMode.Player)
            return;

        UpdateDeviceGroup(context);
        OnAttackCanceled?.Invoke();
    }

    private void OnGuardStartedPerformed(InputAction.CallbackContext context)
    {
        if (CurrentMode != InputMode.Player)
            return;

        UpdateDeviceGroup(context);
        OnGuardStarted?.Invoke();
    }

    private void OnGuardCanceledPerformed(InputAction.CallbackContext context)
    {
        if (CurrentMode != InputMode.Player)
            return;

        UpdateDeviceGroup(context);
        OnGuardCanceled?.Invoke();
    }

    private void OnSpecialPerformedInternal(InputAction.CallbackContext context)
    {
        if (CurrentMode != InputMode.Player)
            return;

        UpdateDeviceGroup(context);
        OnSpecialPerformed?.Invoke();
    }

    private void OnToggleTargetingPerformedInternal(InputAction.CallbackContext context)
    {
        if (CurrentMode != InputMode.Player)
            return;

        UpdateDeviceGroup(context);
        OnToggleTargeting?.Invoke();
    }

    private void OnUsePerformed(InputAction.CallbackContext context)
    {
        if (CurrentMode != InputMode.Player)
            return;

        UpdateDeviceGroup(context);
        OnUse?.Invoke();
    }

    private void OnStatsPerformed(InputAction.CallbackContext context)
    {
        if (CurrentMode != InputMode.Player)
            return;

        UpdateDeviceGroup(context);
        OnStats?.Invoke();
    }

    private void OnQuestJournalPerformed(InputAction.CallbackContext context)
    {
        if (CurrentMode != InputMode.Player)
            return;

        UpdateDeviceGroup(context);
        OnQuestJournal?.Invoke();
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        if (CurrentMode != InputMode.Player)
            return;

        UpdateDeviceGroup(context);
        OnPauseToggle?.Invoke();
    }

    private void OnItem1Performed(InputAction.CallbackContext context)
    {
        if (CurrentMode != InputMode.Player)
            return;

        UpdateDeviceGroup(context);
        OnQuickSlotPressed?.Invoke(1);
    }

    private void OnItem2Performed(InputAction.CallbackContext context)
    {
        if (CurrentMode != InputMode.Player)
            return;

        UpdateDeviceGroup(context);
        OnQuickSlotPressed?.Invoke(2);
    }

    private void OnItem3Performed(InputAction.CallbackContext context)
    {
        if (CurrentMode != InputMode.Player)
            return;

        UpdateDeviceGroup(context);
        OnQuickSlotPressed?.Invoke(3);
    }

    private void OnItem4Performed(InputAction.CallbackContext context)
    {
        if (CurrentMode != InputMode.Player)
            return;

        UpdateDeviceGroup(context);
        OnQuickSlotPressed?.Invoke(4);
    }

    private void OnItem5Performed(InputAction.CallbackContext context)
    {
        if (CurrentMode != InputMode.Player)
            return;

        UpdateDeviceGroup(context);
        OnQuickSlotPressed?.Invoke(5);
    }

    // -------------------- Dialogue --------------------

    private void OnDialogueUpPerformed(InputAction.CallbackContext context)
    {
        if (CurrentMode != InputMode.Dialogue)
            return;

        UpdateDeviceGroup(context);
        OnDialogueUp?.Invoke();
    }

    private void OnDialogueDownPerformed(InputAction.CallbackContext context)
    {
        if (CurrentMode != InputMode.Dialogue)
            return;

        UpdateDeviceGroup(context);
        OnDialogueDown?.Invoke();
    }

    private void OnDialogueSelectPerformed(InputAction.CallbackContext context)
    {
        if (CurrentMode != InputMode.Dialogue)
            return;

        UpdateDeviceGroup(context);
        OnDialogueSelect?.Invoke();
    }

    // -------------------- Menu --------------------

    private void OnMenuUpPerformed(InputAction.CallbackContext context)
    {
        if (CurrentMode != InputMode.Menu)
            return;

        UpdateDeviceGroup(context);
        OnMenuUp?.Invoke();
    }

    private void OnMenuDownPerformed(InputAction.CallbackContext context)
    {
        if (CurrentMode != InputMode.Menu)
            return;

        UpdateDeviceGroup(context);
        OnMenuDown?.Invoke();
    }

    private void OnMenuLeftPerformed(InputAction.CallbackContext context)
    {
        if (CurrentMode != InputMode.Menu)
            return;

        UpdateDeviceGroup(context);
        OnMenuLeft?.Invoke();
    }

    private void OnMenuRightPerformed(InputAction.CallbackContext context)
    {
        if (CurrentMode != InputMode.Menu)
            return;

        UpdateDeviceGroup(context);
        OnMenuRight?.Invoke();
    }

    private void OnMenuSelectPerformed(InputAction.CallbackContext context)
    {
        if (CurrentMode != InputMode.Menu)
            return;

        UpdateDeviceGroup(context);
        OnMenuSelect?.Invoke();
    }

    private void OnMenuUnequipPerformed(InputAction.CallbackContext context)
    {
        if (CurrentMode != InputMode.Menu)
            return;

        UpdateDeviceGroup(context);
        OnMenuUnequip?.Invoke();
    }

    private void OnMenuClosePerformed(InputAction.CallbackContext context)
    {
        if (CurrentMode != InputMode.Menu)
            return;

        UpdateDeviceGroup(context);
        OnMenuClose?.Invoke();
    }

    // -------------------- QuestJournal --------------------

    private void OnQuestJournalUpPerformed(InputAction.CallbackContext context)
    {
        if (CurrentMode != InputMode.QuestJournal)
            return;

        UpdateDeviceGroup(context);
        OnQuestJournalUp?.Invoke();
    }

    private void OnQuestJournalDownPerformed(InputAction.CallbackContext context)
    {
        if (CurrentMode != InputMode.QuestJournal)
            return;

        UpdateDeviceGroup(context);
        OnQuestJournalDown?.Invoke();
    }

    private void OnQuestJournalSelectPerformed(InputAction.CallbackContext context)
    {
        if (CurrentMode != InputMode.QuestJournal)
            return;

        UpdateDeviceGroup(context);
        OnQuestJournalSelect?.Invoke();
    }

    private void OnQuestJournalBackPerformed(InputAction.CallbackContext context)
    {
        if (CurrentMode != InputMode.QuestJournal)
            return;

        UpdateDeviceGroup(context);
        OnQuestJournalBack?.Invoke();
    }

    private void OnQuestJournalMainTabPerformed(InputAction.CallbackContext context)
    {
        if (CurrentMode != InputMode.QuestJournal)
            return;

        UpdateDeviceGroup(context);
        OnQuestJournalMainTab?.Invoke();
    }

    private void OnQuestJournalSideTabPerformed(InputAction.CallbackContext context)
    {
        if (CurrentMode != InputMode.QuestJournal)
            return;

        UpdateDeviceGroup(context);
        OnQuestJournalSideTab?.Invoke();
    }

    private void OnQuestJournalPinQuestPerformed(InputAction.CallbackContext context)
    {
        if (CurrentMode != InputMode.QuestJournal)
            return;

        UpdateDeviceGroup(context);
        OnQuestJournalPinQuest?.Invoke();
    }

    private void OnQuestJournalClosePerformed(InputAction.CallbackContext context)
    {
        if (CurrentMode != InputMode.QuestJournal)
            return;

        UpdateDeviceGroup(context);
        OnQuestJournalClose?.Invoke();
    }

    // -------------------- PauseMenu --------------------

    private void OnPauseMenuUpPerformed(InputAction.CallbackContext context)
    {
        if (CurrentMode != InputMode.PauseMenu)
            return;

        UpdateDeviceGroup(context);
        OnPauseMenuUp?.Invoke();
    }

    private void OnPauseMenuDownPerformed(InputAction.CallbackContext context)
    {
        if (CurrentMode != InputMode.PauseMenu)
            return;

        UpdateDeviceGroup(context);
        OnPauseMenuDown?.Invoke();
    }

    private void OnPauseMenuSelectPerformed(InputAction.CallbackContext context)
    {
        if (CurrentMode != InputMode.PauseMenu)
            return;

        UpdateDeviceGroup(context);
        OnPauseMenuSelect?.Invoke();
    }

    private void OnPauseMenuTogglePerformed(InputAction.CallbackContext context)
    {
        if (CurrentMode != InputMode.PauseMenu)
            return;

        UpdateDeviceGroup(context);
        OnPauseToggle?.Invoke();
    }
}