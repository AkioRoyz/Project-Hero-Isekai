using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class SceneTeleport2D : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string targetSceneName;
    [SerializeField] private string targetEntryPointId;

    [Header("Player Detection")]
    [SerializeField] private string playerTag = "Player";

    [Header("Debug")]
    [SerializeField] private bool showLogs = true;

    private bool isLoading;

    private void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isLoading)
            return;

        GameObject enteredObject = other.attachedRigidbody != null
            ? other.attachedRigidbody.gameObject
            : other.gameObject;

        if (!enteredObject.CompareTag(playerTag))
            return;

        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogWarning("[Teleport] Target scene name is empty.", this);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            Debug.LogError($"[Teleport] Scene '{targetSceneName}' is not in Build Settings.", this);
            return;
        }

        if (SceneTransitionManager.Instance != null)
        {
            if (SceneTransitionManager.Instance.IsLoading)
                return;

            isLoading = true;

            if (showLogs)
                Debug.Log($"[Teleport] Loading scene via SceneTransitionManager: {targetSceneName}, entryPointId = {targetEntryPointId}", this);

            SceneTransitionManager.Instance.LoadScene(targetSceneName, targetEntryPointId);
            return;
        }

        // Fallback, если менеджер не найден
        isLoading = true;
        SceneTransitionState.SetNextEntryPoint(targetEntryPointId);

        if (showLogs)
            Debug.LogWarning($"[Teleport] SceneTransitionManager not found. Fallback direct load: {targetSceneName}", this);

        SceneManager.LoadScene(targetSceneName, LoadSceneMode.Single);
    }
}