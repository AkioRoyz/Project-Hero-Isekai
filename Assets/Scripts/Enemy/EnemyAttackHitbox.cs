using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemyAttackHitbox : MonoBehaviour
{
    [Header("Hitbox Side")]
    [SerializeField] private bool isLeftSide = false;

    [Header("References")]
    [SerializeField] private EnemyController enemyController;
    [SerializeField] private Collider2D triggerCollider;

    public bool IsLeftSide => isLeftSide;

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

        if (enemyController == null)
            enemyController = GetComponentInParent<EnemyController>();
    }

    private void RegisterWithController()
    {
        if (enemyController != null)
            enemyController.RegisterHitbox(this);
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
        if (enemyController == null)
            return;

        enemyController.NotifyHitboxTriggered(this, other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (enemyController == null)
            return;

        enemyController.NotifyHitboxTriggered(this, other);
    }
}