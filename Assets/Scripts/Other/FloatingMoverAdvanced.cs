using UnityEngine;

public class FloatingMoverAdvanced : MonoBehaviour
{
    public enum MovementMode
    {
        Sine,
        PingPong
    }

    [Header("Позиция")]
    [SerializeField] private bool useLocalPosition = true;
    [SerializeField] private bool randomStartOffset = false;
    [SerializeField] private MovementMode movementMode = MovementMode.Sine;

    [Header("Амплитуда движения")]
    [Min(0f)][SerializeField] private float moveX = 0f;
    [Min(0f)][SerializeField] private float moveY = 0f;
    [Min(0f)][SerializeField] private float moveZ = 0f;

    [Header("Скорость движения по осям")]
    [Min(0f)][SerializeField] private float speedX = 1f;
    [Min(0f)][SerializeField] private float speedY = 1f;
    [Min(0f)][SerializeField] private float speedZ = 1f;

    [Header("Фаза движения по осям")]
    [SerializeField] private float phaseX = 0f;
    [SerializeField] private float phaseY = 0f;
    [SerializeField] private float phaseZ = 0f;

    [Header("Покачивание вращения")]
    [SerializeField] private bool enableRotationSway = false;
    [SerializeField] private bool useLocalRotation = true;

    [Min(0f)][SerializeField] private float rotateX = 0f;
    [Min(0f)][SerializeField] private float rotateY = 0f;
    [Min(0f)][SerializeField] private float rotateZ = 0f;

    [Min(0f)][SerializeField] private float rotateSpeedX = 1f;
    [Min(0f)][SerializeField] private float rotateSpeedY = 1f;
    [Min(0f)][SerializeField] private float rotateSpeedZ = 1f;

    [SerializeField] private float rotatePhaseX = 0f;
    [SerializeField] private float rotatePhaseY = 0f;
    [SerializeField] private float rotatePhaseZ = 0f;

    private Vector3 startPosition;
    private Vector3 startEulerAngles;

    private float randomOffsetX;
    private float randomOffsetY;
    private float randomOffsetZ;

    private float randomRotOffsetX;
    private float randomRotOffsetY;
    private float randomRotOffsetZ;

    private void Start()
    {
        startPosition = useLocalPosition ? transform.localPosition : transform.position;
        startEulerAngles = useLocalRotation ? transform.localEulerAngles : transform.eulerAngles;

        if (randomStartOffset)
        {
            randomOffsetX = Random.Range(0f, 100f);
            randomOffsetY = Random.Range(0f, 100f);
            randomOffsetZ = Random.Range(0f, 100f);

            randomRotOffsetX = Random.Range(0f, 100f);
            randomRotOffsetY = Random.Range(0f, 100f);
            randomRotOffsetZ = Random.Range(0f, 100f);
        }
    }

    private void Update()
    {
        UpdatePosition();

        if (enableRotationSway)
        {
            UpdateRotation();
        }
    }

    private void UpdatePosition()
    {
        Vector3 offset = new Vector3(
            EvaluateOffset(moveX, speedX, phaseX, randomOffsetX),
            EvaluateOffset(moveY, speedY, phaseY, randomOffsetY),
            EvaluateOffset(moveZ, speedZ, phaseZ, randomOffsetZ)
        );

        if (useLocalPosition)
        {
            transform.localPosition = startPosition + offset;
        }
        else
        {
            transform.position = startPosition + offset;
        }
    }

    private void UpdateRotation()
    {
        Vector3 rotationOffset = new Vector3(
            EvaluateOffset(rotateX, rotateSpeedX, rotatePhaseX, randomRotOffsetX),
            EvaluateOffset(rotateY, rotateSpeedY, rotatePhaseY, randomRotOffsetY),
            EvaluateOffset(rotateZ, rotateSpeedZ, rotatePhaseZ, randomRotOffsetZ)
        );

        Vector3 targetEuler = startEulerAngles + rotationOffset;

        if (useLocalRotation)
        {
            transform.localRotation = Quaternion.Euler(targetEuler);
        }
        else
        {
            transform.rotation = Quaternion.Euler(targetEuler);
        }
    }

    private float EvaluateOffset(float amplitude, float speed, float phase, float randomOffset)
    {
        if (amplitude <= 0f || speed <= 0f)
            return 0f;

        float t = Time.time * speed + phase + randomOffset;

        switch (movementMode)
        {
            case MovementMode.PingPong:
                return Mathf.PingPong(t, amplitude * 2f) - amplitude;

            case MovementMode.Sine:
            default:
                return Mathf.Sin(t) * amplitude;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        moveX = Mathf.Max(0f, moveX);
        moveY = Mathf.Max(0f, moveY);
        moveZ = Mathf.Max(0f, moveZ);

        speedX = Mathf.Max(0f, speedX);
        speedY = Mathf.Max(0f, speedY);
        speedZ = Mathf.Max(0f, speedZ);

        rotateX = Mathf.Max(0f, rotateX);
        rotateY = Mathf.Max(0f, rotateY);
        rotateZ = Mathf.Max(0f, rotateZ);

        rotateSpeedX = Mathf.Max(0f, rotateSpeedX);
        rotateSpeedY = Mathf.Max(0f, rotateSpeedY);
        rotateSpeedZ = Mathf.Max(0f, rotateSpeedZ);
    }
#endif
}