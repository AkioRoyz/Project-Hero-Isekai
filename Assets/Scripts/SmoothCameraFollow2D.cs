using UnityEngine;

public class SmoothCameraFollow2D : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool findPlayerByTagOnStart = true;

    [Header("Follow Settings")]
    [SerializeField] private float smoothTime = 0.15f;
    [SerializeField] private Vector2 offset = Vector2.zero;

    [Header("Camera Z")]
    [SerializeField] private float cameraZ = -10f;

    [Header("Options")]
    [SerializeField] private bool snapToTargetOnStart = true;

    private Vector3 velocity;

    private void Start()
    {
        if (target == null && findPlayerByTagOnStart)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);

            if (playerObject != null)
            {
                target = playerObject.transform;
            }
            else
            {
                Debug.LogWarning($"{nameof(SmoothCameraFollow2D)}: Player with tag '{playerTag}' was not found.", this);
            }
        }

        if (target != null && snapToTargetOnStart)
        {
            transform.position = GetTargetCameraPosition();
        }
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 targetPosition = GetTargetCameraPosition();

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref velocity,
            smoothTime
        );
    }

    private Vector3 GetTargetCameraPosition()
    {
        return new Vector3(
            target.position.x + offset.x,
            target.position.y + offset.y,
            cameraZ
        );
    }

    public void SetTarget(Transform newTarget, bool snapImmediately = false)
    {
        target = newTarget;
        velocity = Vector3.zero;

        if (target != null && snapImmediately)
        {
            transform.position = GetTargetCameraPosition();
        }
    }
}