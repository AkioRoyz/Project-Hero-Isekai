using UnityEngine;

public class SceneEntryPoint : MonoBehaviour
{
    [SerializeField] private string entryPointId = "Default";

    public string EntryPointId => entryPointId;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.25f);

        Vector3 dir = transform.right * 0.75f;
        Gizmos.DrawLine(transform.position, transform.position + dir);
        Gizmos.DrawWireSphere(transform.position + dir, 0.08f);
    }
#endif
}