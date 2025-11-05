using UnityEngine;

public class RopeAnchor : MonoBehaviour
{
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private Transform _anchorPoint;

    public Rigidbody Rigidbody => _rigidbody;
    public Transform AnchorPoint => _anchorPoint;
}
