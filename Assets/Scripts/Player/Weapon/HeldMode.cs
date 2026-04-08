using UnityEngine;

public class HeldMode : MonoBehaviour
{
    [SerializeField] private Collider[] _colliders;
    [SerializeField] private MonoBehaviour[] _componentsToDisable;
    [SerializeField] private Rigidbody[] _rigidbodies;

    private bool _isHeld;

    private void OnEnable()
    {
        ApplyHeldState();
    }

    public void SetHeld(bool isHeld)
    {
        _isHeld = isHeld;

        ApplyHeldState();
    }

    private void ApplyHeldState()
    {
        for (int i = 0; i < _colliders.Length; i++)
        {
            _colliders[i].enabled = _isHeld == false;
        }

        for (int i = 0; i < _componentsToDisable.Length; i++)
        {
            _componentsToDisable[i].enabled = _isHeld == false;
        }

        for (int i = 0; i < _rigidbodies.Length; i++)
        {
            Rigidbody rigidbody = _rigidbodies[i];

            if (rigidbody.isKinematic == false)
            {
                rigidbody.linearVelocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
            }

            rigidbody.isKinematic = _isHeld;
            rigidbody.detectCollisions = _isHeld == false;
        }
    }
}
