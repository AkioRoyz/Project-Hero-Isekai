using UnityEngine;
using UnityEngine.SceneManagement;

public class StatsMenuController : MonoBehaviour
{
    [SerializeField] private GameInput gameInput;
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private EquipmentMenuUI equipmentMenuUI;
    [SerializeField] private StatsControlsHintUI statsControlsHintUI;

    [Header("Pause Visual Effect")]
    [SerializeField] private PauseMenuBlackAndWhiteEffect blackAndWhiteEffect;
    [SerializeField] private bool usePauseVisualEffect = true;

    private bool isOpened;
    private bool pauseVisualEffectEnabled;
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
        DisablePauseVisualEffect();
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

        ResolvePauseVisualEffect();
    }

    private void ResolvePauseVisualEffect()
    {
        if (!usePauseVisualEffect || blackAndWhiteEffect != null)
            return;

        PauseMenuBlackAndWhiteEffect[] effects = FindObjectsByType<PauseMenuBlackAndWhiteEffect>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        if (effects != null && effects.Length > 0)
            blackAndWhiteEffect = effects[0];
    }

    private void EnablePauseVisualEffect()
    {
        if (!usePauseVisualEffect || pauseVisualEffectEnabled)
            return;

        ResolvePauseVisualEffect();

        if (blackAndWhiteEffect == null)
            return;

        blackAndWhiteEffect.EnablePauseEffect();
        pauseVisualEffectEnabled = true;
    }

    private void DisablePauseVisualEffect()
    {
        if (!pauseVisualEffectEnabled)
            return;

        if (blackAndWhiteEffect != null)
            blackAndWhiteEffect.DisablePauseEffect();

        pauseVisualEffectEnabled = false;
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

        DisablePauseVisualEffect();

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
        EnablePauseVisualEffect();

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
        DisablePauseVisualEffect();
        menuRoot.SetActive(false);

        if (equipmentMenuUI != null)
            equipmentMenuUI.CloseMenu();

        if (GameStateManager.Instance != null)
            GameStateManager.Instance.SetState(GameState.Playing);

        gameInput.SwitchToPlayerMode();
    }
}