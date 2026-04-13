using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Debug")]
    [SerializeField] private bool showLogs = true;

    private void Awake()
    {
        if (showLogs)
            Debug.Log($"[GameManager Awake] {name}, scene = {gameObject.scene.name}", gameObject);

        if (Instance != null && Instance != this)
        {
            if (showLogs)
                Debug.LogWarning("[GameManager] Duplicate instance destroyed.", gameObject);

            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (showLogs)
            Debug.Log("[GameManager] Instance initialized. Persistence is handled by PersistentRoot.", gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}