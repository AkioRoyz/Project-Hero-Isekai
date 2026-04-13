using UnityEngine;
using UnityEngine.SceneManagement;

public class StatsMenuController : MonoBehaviour
{
    [SerializeField] private GameInput gameInput;
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private EquipmentMenuUI equipmentMenuUI;
    [SerializeField] private StatsControlsHintUI statsControlsHintUI;

    private bool isOpened;
    private GameInput subscribedInput;

    private void Awake()
    {
        ResolveReferences();
        ForceClosedVisual();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;

        ResolveReferences();
        RebindInput();
        ForceClosedVisual();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        UnbindInput();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResolveReferences();
        RebindInput();
        ForceClosedVisual();
    }

    private void ResolveReferences()
    {
        gameInput = GameInput.Instance != null
            ? GameInput.Instance
            : FindFirstObjectByType<GameInput>();
    }

    private void RebindInput()
    {
        UnbindInput();

        if (gameInput == null)
            return;

        gameInput.OnStats += ToggleMenu;
        gameInput.OnMenuClose += CloseMenu;

        subscribedInput = gameInput;
    }

    private void UnbindInput()
    {
        if (subscribedInput == null)
            return;

        subscribedInput.OnStats -= ToggleMenu;
        subscribedInput.OnMenuClose -= CloseMenu;
        subscribedInput = null;
    }

    private void ForceClosedVisual()
    {
        isOpened = false;

        if (menuRoot != null)
            menuRoot.SetActive(false);

        if (equipmentMenuUI != null)
            equipmentMenuUI.CloseMenu();

        if (GameStateManager.Instance != null &&
            GameStateManager.Instance.CurrentState == GameState.Menu)
        {
            GameStateManager.Instance.SetState(GameState.Playing);
        }

        if (gameInput != null)
            gameInput.SwitchToPlayerMode();
    }

    private void ToggleMenu()
    {
        if (GameStateManager.Instance != null)
        {
            if (GameStateManager.Instance.CurrentState == GameState.Dialogue ||
                GameStateManager.Instance.CurrentState == GameState.Pause)
            {
                return;
            }
        }

        if (isOpened)
            CloseMenu();
        else
            OpenMenu();
    }

    public void OpenMenu()
    {
        ResolveReferences();

        if (menuRoot == null || gameInput == null)
            return;

        isOpened = true;
        menuRoot.SetActive(true);

        if (GameStateManager.Instance != null)
            GameStateManager.Instance.SetState(GameState.Menu);

        gameInput.SwitchToMenuMode();

        if (equipmentMenuUI != null)
            equipmentMenuUI.OpenMenu();

        if (statsControlsHintUI != null)
            statsControlsHintUI.Refresh();
    }

    public void CloseMenu()
    {
        ResolveReferences();

        if (menuRoot == null || gameInput == null)
            return;

        isOpened = false;
        menuRoot.SetActive(false);

        if (equipmentMenuUI != null)
            equipmentMenuUI.CloseMenu();

        if (GameStateManager.Instance != null)
            GameStateManager.Instance.SetState(GameState.Playing);

        gameInput.SwitchToPlayerMode();
    }
}