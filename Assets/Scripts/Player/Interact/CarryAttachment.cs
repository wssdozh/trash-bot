using UnityEngine;

public class CarryAttachment : MonoBehaviour
{
    [SerializeField] private Transform _carryPoint;

    public Transform CarryPoint => _carryPoint;
}
