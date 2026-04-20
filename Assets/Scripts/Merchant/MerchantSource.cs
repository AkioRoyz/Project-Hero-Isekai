using UnityEngine;

public class MerchantSource : MonoBehaviour, IMerchantSource
{
    [SerializeField] private MerchantData merchantData;

    public MerchantData MerchantData => merchantData;
}