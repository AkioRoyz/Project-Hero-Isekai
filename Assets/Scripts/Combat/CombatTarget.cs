using UnityEngine;

public class CombatTarget : MonoBehaviour
{
    [Header("Fallback Settings")]
    [SerializeField] private CombatTeam fallbackTeam = CombatTeam.Enemy;

    [Header("Aim Point")]
    [SerializeField] private Transform aimPoint;

    [Header("Target State")]
    [SerializeField] private bool isTargetable = true;

    private ICombatReceiver cachedReceiver;

    public CombatTeam Team
    {
        get
        {
            EnsureReceiverCached();
            return cachedReceiver != null ? cachedReceiver.Team : fallbackTeam;
        }
    }

    public Transform AimPoint => aimPoint != null ? aimPoint : transform;

    public bool IsTargetable
    {
        get
        {
            if (!isTargetable)
                return false;

            if (!enabled || !gameObject.activeInHierarchy)
                return false;

            EnsureReceiverCached();

            if (cachedReceiver == null)
                return true;

            return cachedReceiver.IsAlive;
        }
    }

    private void Awake()
    {
        EnsureReceiverCached();
    }

    private void OnEnable()
    {
        EnsureReceiverCached();
    }

    public void SetTargetable(bool value)
    {
        isTargetable = value;
    }

    private void EnsureReceiverCached()
    {
        if (cachedReceiver == null)
            cachedReceiver = GetComponent<ICombatReceiver>();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Transform point = aimPoint != null ? aimPoint : transform;

        if (fallbackTeam == CombatTeam.Player)
            Gizmos.color = Color.green;
        else if (fallbackTeam == CombatTeam.Enemy)
            Gizmos.color = Color.red;
        else
            Gizmos.color = Color.gray;

        Gizmos.DrawWireSphere(point.position, 0.12f);
    }
#endif
}