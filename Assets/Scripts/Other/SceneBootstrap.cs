using UnityEngine;

public class SceneBootstrap : MonoBehaviour
{
    [Header("Player Spawn")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private bool moveExistingPlayerToSpawn = true;
    [SerializeField] private bool resetPlayerVelocityOnTeleport = true;

    [Header("Debug")]
    [SerializeField] private bool showLogs = true;

    private void Awake()
    {
        EnsurePlayerPlaced();
    }

    private void EnsurePlayerPlaced()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        if (players.Length == 0)
        {
            Debug.LogError("[SceneBootstrap] Player with tag 'Player' was not found. Make sure the player is created before loading this scene.", this);
            return;
        }

        if (players.Length > 1)
        {
            Debug.LogError($"[SceneBootstrap] Found {players.Length} objects with Player tag. There must be exactly one player.", this);
        }

        GameObject player = players[0];

        if (!moveExistingPlayerToSpawn)
        {
            if (showLogs)
                Debug.Log($"[SceneBootstrap] Player found: {player.name}, but moving is disabled.", this);

            return;
        }

        Transform resolvedSpawn = ResolveSpawnPoint();
        if (resolvedSpawn == null)
        {
            Debug.LogWarning("[SceneBootstrap] Spawn point was not resolved.", this);
            return;
        }

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            if (resetPlayerVelocityOnTeleport)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            rb.position = resolvedSpawn.position;
            rb.rotation = resolvedSpawn.eulerAngles.z;
        }
        else
        {
            player.transform.SetPositionAndRotation(resolvedSpawn.position, resolvedSpawn.rotation);
        }

        if (showLogs)
            Debug.Log($"[SceneBootstrap] Existing player moved to spawn: {resolvedSpawn.name}", this);
    }

    private Transform ResolveSpawnPoint()
    {
        string pendingEntryPointId = SceneTransitionState.ConsumePendingEntryPoint();

        if (!string.IsNullOrWhiteSpace(pendingEntryPointId))
        {
            SceneEntryPoint[] entryPoints = FindObjectsByType<SceneEntryPoint>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            for (int i = 0; i < entryPoints.Length; i++)
            {
                SceneEntryPoint entryPoint = entryPoints[i];
                if (entryPoint != null && entryPoint.EntryPointId == pendingEntryPointId)
                {
                    if (showLogs)
                        Debug.Log($"[SceneBootstrap] Entry point resolved: {pendingEntryPointId}", this);

                    return entryPoint.transform;
                }
            }

            Debug.LogWarning($"[SceneBootstrap] Entry point '{pendingEntryPointId}' not found. Fallback to default spawnPoint.", this);
        }

        return spawnPoint;
    }
}