using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PlayerCombatHitbox : MonoBehaviour
{
    [Header("Hitbox Settings")]
    [SerializeField] private PlayerAttackSide side = PlayerAttackSide.Right;

    [Header("References")]
    [SerializeField] private PlayerCombatController combatController;
    [SerializeField] private Collider2D triggerCollider;

    public PlayerAttackSide Side => side;

    private void Reset()
    {
        triggerCollider = GetComponent<Collider2D>();

        if (triggerCollider != null)
            triggerCollider.isTrigger = true;
    }

    private void Awake()
    {
        ResolveReferences();
        ForceDisable();
        RegisterWithController();
    }

    private void OnEnable()
    {
        ResolveReferences();
        ForceDisable();
        RegisterWithController();
    }

    private void ResolveReferences()
    {
        if (triggerCollider == null)
            triggerCollider = GetComponent<Collider2D>();

        if (combatController == null)
            combatController = GetComponentInParent<PlayerCombatController>();
    }

    private void RegisterWithController()
    {
        if (combatController != null)
            combatController.RegisterHitbox(this);
    }

    public void SetHitboxActive(bool value)
    {
        if (triggerCollider == null)
            return;

        triggerCollider.enabled = value;
    }

    public void ForceDisable()
    {
        if (triggerCollider == null)
            return;

        triggerCollider.enabled = false;
    }

    public Vector3 GetBestHitPoint(Collider2D other)
    {
        if (other != null)
            return other.bounds.ClosestPoint(transform.position);

        return transform.position;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (combatController == null)
            return;

        combatController.NotifyHitboxTriggered(this, other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (combatController == null)
            return;

        combatController.NotifyHitboxTriggered(this, other);
    }
}