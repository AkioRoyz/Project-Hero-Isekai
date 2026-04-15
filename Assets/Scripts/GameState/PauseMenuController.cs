using System.Collections;
using UnityEngine;

public class PauseMenuController : MonoBehaviour
{
    private enum ActiveRoot
    {
        Main,
        Save,
        Load,
        TriggerLoad
    }

    [Header("Main References")]
    [SerializeField] private GameObject pauseCanvasRoot;
    [SerializeField] private GameObject mainRoot;
    [SerializeField] private PauseMenuSaveLoadRootUI saveRootUI;
    [SerializeField] private PauseMenuSaveLoadRootUI loadRootUI;
    [SerializeField] private PauseMenuSaveLoadRootUI triggerLoadRootUI;
    [SerializeField] private PauseMenuSlotViewUI[] mainMenuSlots;

    [Header("Pause Visuals")]
    [SerializeField] private GameObject pauseOverlayRoot;
    [SerializeField] private GameObject quitMessageRoot;
    [SerializeField] private PauseMenuBlackAndWhiteEffect blackAndWhiteEffect;

    [Header("Behaviour")]
    [SerializeField] private bool manageTimeScale = true;
    [SerializeField] private bool switchGameInputMode = true;
    [SerializeField] private bool openMenuOnStart = false;

    [Header("Delays")]
    [SerializeField] private float saveDelayAfterClose = 0.05f;
    [SerializeField] private float loadDelayBeforeSaveLoadCall = 0.05f;
    [SerializeField] private float quitDelay = 0.6f;

    [Header("Debug")]
    [SerializeField] private bool verboseLogs = false;

    private bool isOpen;
    private bool isBusy;
    private bool isSubscribed;
    private int mainSelectionIndex;
    private ActiveRoot activeRoot = ActiveRoot.Main;
    private Coroutine waitForGameInputCoroutine;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        BindRootEvents();
        ForceClosedState();
    }

    private void Start()
    {
        RefreshMainMenuVisuals();

        if (openMenuOnStart)
        {
            OpenPauseMenu();
        }
    }

    private void OnEnable()
    {
        StartWaitingForGameInput();
    }

    private void OnDisable()
    {
        StopWaitingForGameInput();
        UnsubscribeFromGameInput();

        if (manageTimeScale)
        {
            Time.timeScale = 1f;
        }
    }

    private void OnDestroy()
    {
        UnbindRootEvents();
        StopWaitingForGameInput();
        UnsubscribeFromGameInput();
    }

    private void BindRootEvents()
    {
        if (saveRootUI != null)
        {
            saveRootUI.DataSlotChosen += HandleSaveSlotChosen;
            saveRootUI.BackRequested += HandleSaveBackRequested;
        }

        if (loadRootUI != null)
        {
            loadRootUI.DataSlotChosen += HandleLoadSlotChosen;
            loadRootUI.BackRequested += HandleLoadBackRequested;
        }

        if (triggerLoadRootUI != null)
        {
            triggerLoadRootUI.DataSlotChosen += HandleTriggerLoadSlotChosen;
            triggerLoadRootUI.BackRequested += HandleTriggerLoadBackRequested;
        }
    }

    private void UnbindRootEvents()
    {
        if (saveRootUI != null)
        {
            saveRootUI.DataSlotChosen -= HandleSaveSlotChosen;
            saveRootUI.BackRequested -= HandleSaveBackRequested;
        }

        if (loadRootUI != null)
        {
            loadRootUI.DataSlotChosen -= HandleLoadSlotChosen;
            loadRootUI.BackRequested -= HandleLoadBackRequested;
        }

        if (triggerLoadRootUI != null)
        {
            triggerLoadRootUI.DataSlotChosen -= HandleTriggerLoadSlotChosen;
            triggerLoadRootUI.BackRequested -= HandleTriggerLoadBackRequested;
        }
    }

    private void StartWaitingForGameInput()
    {
        StopWaitingForGameInput();
        waitForGameInputCoroutine = StartCoroutine(WaitForGameInputAndSubscribeRoutine());
    }

    private void StopWaitingForGameInput()
    {
        if (waitForGameInputCoroutine != null)
        {
            StopCoroutine(waitForGameInputCoroutine);
            waitForGameInputCoroutine = null;
        }
    }

    private IEnumerator WaitForGameInputAndSubscribeRoutine()
    {
        while (GameInput.Instance == null)
        {
            yield return null;
        }

        SubscribeToGameInput();
        waitForGameInputCoroutine = null;
    }

    private void SubscribeToGameInput()
    {
        if (isSubscribed || GameInput.Instance == null)
        {
            return;
        }

        GameInput.Instance.OnPauseToggle += HandlePauseToggleInput;
        GameInput.Instance.OnPauseMenuUp += HandlePauseMenuUpInput;
        GameInput.Instance.OnPauseMenuDown += HandlePauseMenuDownInput;
        GameInput.Instance.OnPauseMenuSelect += HandlePauseMenuSelectInput;

        isSubscribed = true;

        if (verboseLogs)
        {
            Debug.Log("[PauseMenuController] Connected to GameInput.");
        }
    }

    private void UnsubscribeFromGameInput()
    {
        if (!isSubscribed)
        {
            return;
        }

        if (GameInput.Instance != null)
        {
            GameInput.Instance.OnPauseToggle -= HandlePauseToggleInput;
            GameInput.Instance.OnPauseMenuUp -= HandlePauseMenuUpInput;
            GameInput.Instance.OnPauseMenuDown -= HandlePauseMenuDownInput;
            GameInput.Instance.OnPauseMenuSelect -= HandlePauseMenuSelectInput;
        }

        isSubscribed = false;
    }

    private void HandlePauseToggleInput()
    {
        if (isBusy)
        {
            return;
        }

        if (isOpen)
        {
            ClosePauseMenu();
        }
        else
        {
            OpenPauseMenu();
        }
    }

    private void HandlePauseMenuUpInput()
    {
        if (isBusy || !isOpen)
        {
            return;
        }

        MoveSelection(-1);
    }

    private void HandlePauseMenuDownInput()
    {
        if (isBusy || !isOpen)
        {
            return;
        }

        MoveSelection(+1);
    }

    private void HandlePauseMenuSelectInput()
    {
        if (isBusy || !isOpen)
        {
            return;
        }

        SubmitCurrentSelection();
    }

    public void OpenPauseMenu()
    {
        if (isOpen || GameInput.Instance == null)
        {
            return;
        }

        OpenPauseShell();
        ShowMainRoot();

        if (verboseLogs)
        {
            Debug.Log("[PauseMenuController] Pause opened.");
        }
    }

    public void OpenTriggerLoadMenu()
    {
        if (GameInput.Instance == null)
        {
            return;
        }

        if (!isOpen)
        {
            OpenPauseShell();
        }

        ShowTriggerLoadRoot();

        if (verboseLogs)
        {
            Debug.Log("[PauseMenuController] Trigger load menu opened.");
        }
    }

    private void OpenPauseShell()
    {
        isOpen = true;
        activeRoot = ActiveRoot.Main;

        if (pauseCanvasRoot != null)
        {
            pauseCanvasRoot.SetActive(true);
        }

        if (pauseOverlayRoot != null)
        {
            pauseOverlayRoot.SetActive(true);
        }

        if (quitMessageRoot != null)
        {
            quitMessageRoot.SetActive(false);
        }

        if (manageTimeScale)
        {
            Time.timeScale = 0f;
        }

        if (switchGameInputMode)
        {
            GameInput.Instance.SwitchToPauseMenuMode();
        }

        blackAndWhiteEffect?.EnablePauseEffect();
    }

    public void ClosePauseMenu()
    {
        if (!isOpen)
        {
            return;
        }

        isOpen = false;
        activeRoot = ActiveRoot.Main;

        HideAllRoots();

        if (pauseOverlayRoot != null)
        {
            pauseOverlayRoot.SetActive(false);
        }

        if (pauseCanvasRoot != null)
        {
            pauseCanvasRoot.SetActive(false);
        }

        if (quitMessageRoot != null)
        {
            quitMessageRoot.SetActive(false);
        }

        blackAndWhiteEffect?.DisablePauseEffect();

        if (switchGameInputMode && GameInput.Instance != null)
        {
            GameInput.Instance.SwitchToPlayerMode();
        }

        if (manageTimeScale)
        {
            Time.timeScale = 1f;
        }

        if (verboseLogs)
        {
            Debug.Log("[PauseMenuController] Pause closed.");
        }
    }

    public void TogglePauseMenu()
    {
        if (isOpen)
        {
            ClosePauseMenu();
        }
        else
        {
            OpenPauseMenu();
        }
    }

    public void ResumeGame()
    {
        ClosePauseMenu();
    }

    public void OpenSaveRoot()
    {
        if (!isOpen || saveRootUI == null)
        {
            return;
        }

        activeRoot = ActiveRoot.Save;

        if (mainRoot != null)
        {
            mainRoot.SetActive(false);
        }

        loadRootUI?.HideRoot();
        triggerLoadRootUI?.HideRoot();
        saveRootUI.ShowRoot();
    }

    public void OpenLoadRoot()
    {
        if (!isOpen || loadRootUI == null)
        {
            return;
        }

        activeRoot = ActiveRoot.Load;

        if (mainRoot != null)
        {
            mainRoot.SetActive(false);
        }

        saveRootUI?.HideRoot();
        triggerLoadRootUI?.HideRoot();
        loadRootUI.ShowRoot();
    }

    public void ShowTriggerLoadRoot()
    {
        if (triggerLoadRootUI == null)
        {
            return;
        }

        activeRoot = ActiveRoot.TriggerLoad;

        if (mainRoot != null)
        {
            mainRoot.SetActive(false);
        }

        saveRootUI?.HideRoot();
        loadRootUI?.HideRoot();
        triggerLoadRootUI.ShowRoot();
    }

    public void ShowMainRoot()
    {
        activeRoot = ActiveRoot.Main;

        if (mainRoot != null)
        {
            mainRoot.SetActive(true);
        }

        saveRootUI?.HideRoot();
        loadRootUI?.HideRoot();
        triggerLoadRootUI?.HideRoot();

        RefreshMainMenuVisuals();
    }

    private void HideAllRoots()
    {
        if (mainRoot != null)
        {
            mainRoot.SetActive(false);
        }

        saveRootUI?.HideRoot();
        loadRootUI?.HideRoot();
        triggerLoadRootUI?.HideRoot();
    }

    private void ForceClosedState()
    {
        isOpen = false;
        isBusy = false;
        activeRoot = ActiveRoot.Main;

        if (pauseCanvasRoot != null)
        {
            pauseCanvasRoot.SetActive(false);
        }

        if (pauseOverlayRoot != null)
        {
            pauseOverlayRoot.SetActive(false);
        }

        if (quitMessageRoot != null)
        {
            quitMessageRoot.SetActive(false);
        }

        HideAllRoots();
        blackAndWhiteEffect?.DisablePauseEffectImmediate();

        if (manageTimeScale)
        {
            Time.timeScale = 1f;
        }
    }

    private void MoveSelection(int direction)
    {
        switch (activeRoot)
        {
            case ActiveRoot.Main:
                MoveMainSelection(direction);
                break;

            case ActiveRoot.Save:
                saveRootUI?.MoveSelection(direction);
                break;

            case ActiveRoot.Load:
                loadRootUI?.MoveSelection(direction);
                break;

            case ActiveRoot.TriggerLoad:
                triggerLoadRootUI?.MoveSelection(direction);
                break;
        }
    }

    private void SubmitCurrentSelection()
    {
        switch (activeRoot)
        {
            case ActiveRoot.Main:
                SubmitMainSelection();
                break;

            case ActiveRoot.Save:
                saveRootUI?.SubmitCurrentSelection();
                break;

            case ActiveRoot.Load:
                loadRootUI?.SubmitCurrentSelection();
                break;

            case ActiveRoot.TriggerLoad:
                triggerLoadRootUI?.SubmitCurrentSelection();
                break;
        }
    }

    private void MoveMainSelection(int direction)
    {
        if (mainMenuSlots == null || mainMenuSlots.Length == 0)
        {
            return;
        }

        mainSelectionIndex += direction;

        if (mainSelectionIndex < 0)
        {
            mainSelectionIndex = mainMenuSlots.Length - 1;
        }
        else if (mainSelectionIndex >= mainMenuSlots.Length)
        {
            mainSelectionIndex = 0;
        }

        RefreshMainMenuVisuals();
    }

    private void RefreshMainMenuVisuals()
    {
        if (mainMenuSlots == null || mainMenuSlots.Length == 0)
        {
            return;
        }

        if (mainSelectionIndex < 0 || mainSelectionIndex >= mainMenuSlots.Length)
        {
            mainSelectionIndex = 0;
        }

        for (int i = 0; i < mainMenuSlots.Length; i++)
        {
            if (mainMenuSlots[i] == null)
            {
                continue;
            }

            mainMenuSlots[i].ShowAsMainOption();
            mainMenuSlots[i].SetSelected(i == mainSelectionIndex);
        }
    }

    private void SubmitMainSelection()
    {
        switch (mainSelectionIndex)
        {
            case 0:
                ClosePauseMenu();
                break;

            case 1:
                OpenSaveRoot();
                break;

            case 2:
                OpenLoadRoot();
                break;

            case 3:
                StartCoroutine(QuitRoutine());
                break;
        }
    }

    private void HandleSaveSlotChosen(int slotIndex)
    {
        if (isBusy)
        {
            return;
        }

        StartCoroutine(SaveRoutine(slotIndex));
    }

    private void HandleLoadSlotChosen(int slotIndex)
    {
        if (isBusy)
        {
            return;
        }

        StartCoroutine(LoadRoutine(slotIndex, loadRootUI));
    }

    private void HandleTriggerLoadSlotChosen(int slotIndex)
    {
        if (isBusy)
        {
            return;
        }

        StartCoroutine(LoadRoutine(slotIndex, triggerLoadRootUI));
    }

    private void HandleSaveBackRequested()
    {
        ShowMainRoot();
        mainSelectionIndex = 1;
        RefreshMainMenuVisuals();
    }

    private void HandleLoadBackRequested()
    {
        ShowMainRoot();
        mainSelectionIndex = 2;
        RefreshMainMenuVisuals();
    }

    private void HandleTriggerLoadBackRequested()
    {
        ClosePauseMenu();
    }

    private IEnumerator SaveRoutine(int slotIndex)
    {
        isBusy = true;

        ClosePauseMenu();

        yield return null;
        yield return new WaitForEndOfFrame();

        if (saveDelayAfterClose > 0f)
        {
            yield return new WaitForSecondsRealtime(saveDelayAfterClose);
        }

        if (saveRootUI != null && saveRootUI.SaveAdapter != null)
        {
            saveRootUI.SaveAdapter.SaveToSlot(slotIndex);
        }

        isBusy = false;
    }

    private IEnumerator LoadRoutine(int slotIndex, PauseMenuSaveLoadRootUI sourceRoot)
    {
        isBusy = true;

        ClosePauseMenu();

        if (loadDelayBeforeSaveLoadCall > 0f)
        {
            yield return new WaitForSecondsRealtime(loadDelayBeforeSaveLoadCall);
        }

        if (sourceRoot != null && sourceRoot.SaveAdapter != null)
        {
            sourceRoot.SaveAdapter.LoadFromSlot(slotIndex);
        }

        isBusy = false;
    }

    private IEnumerator QuitRoutine()
    {
        isBusy = true;

        if (quitMessageRoot != null)
        {
            quitMessageRoot.SetActive(true);
        }

        if (quitDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(quitDelay);
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif

        isBusy = false;
    }
}