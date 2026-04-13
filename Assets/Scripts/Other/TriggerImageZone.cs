using UnityEngine;

public class TriggerImageZone : MonoBehaviour
{
    [SerializeField] GameObject imageObject;
    [SerializeField] private float floatAmplitude = 0.25f;
    [SerializeField] private float floatSpeed = 2f;

    private int playerLayer;
    private Vector3 startPosition;
    private bool isFloating;

    private void Awake()
    {
        if (imageObject != null)
            imageObject.SetActive(false);
        playerLayer = LayerMask.NameToLayer("Player");
    }

    private void Update()
    {
        if (isFloating && imageObject != null)
        {
            float offsetY = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            imageObject.transform.localPosition = startPosition + new Vector3(0f, offsetY, 0f);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == playerLayer)
        {
            imageObject.SetActive(true);
            isFloating = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.layer == playerLayer)
        {
            isFloating = false;
            imageObject.transform.localPosition = startPosition;
            imageObject.SetActive(false);
        }
    }
}
