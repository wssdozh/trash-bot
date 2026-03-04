using System.Collections.Generic;
using UnityEngine;


class CharacterDied : MonoBehaviour
{
    [SerializeField] private Collider _mainCollider;
    [SerializeField] private Rigidbody _mainRigidbody;
    [SerializeField] private Animator _animator;

    [SerializeField] private List<Rigidbody> _rigidbodies;
    [SerializeField] private List<Collider> _colliders;

    [ContextMenu("Включить регдолл")]
    public void EnableRegdoll()
    {
        _mainCollider.enabled = false;
        _mainRigidbody.useGravity = false;
        _animator.enabled = false;

        _rigidbodies.ForEach(
            rb =>
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }
        );

        _colliders.ForEach(
            collider =>
            {
                collider.enabled = true;
            }
        );
    }

    [ContextMenu("Выключить регдолл")]
    public void DisableRegdoll()
    {
        _mainCollider.enabled = true;
        _mainRigidbody.useGravity = true;
        _animator.enabled = true;

        _rigidbodies.ForEach(
            rb =>
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        );

        _colliders.ForEach(
            collider =>
            {
                collider.enabled = false;
            }
        );
    }

    [ContextMenu("Получить дочерние регдолл")]
    public void GetRigidbodiesAndColliders()
    {
        GetColliders();
        GetRigidbodies();
    }

    private void GetRigidbodies()
    {
        _rigidbodies = new List<Rigidbody>(GetComponentsInChildren<Rigidbody>());
    }

    private void GetColliders()
    {
        _colliders = new List<Collider>(GetComponentsInChildren<Collider>());
    }
}