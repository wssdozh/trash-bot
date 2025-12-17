using UnityEngine;

public class HeldMode : MonoBehaviour
{
    [SerializeField] private Collider[] _colliders;
    [SerializeField] private MonoBehaviour[] _componentsToDisable;
    [SerializeField] private Rigidbody[] _rigidbodies;

    private void OnEnable()
    {
        SetHeld(false);
    }

    public void SetHeld(bool isHeld)
    {
        for (int i = 0; i < _colliders.Length; i++)
        {
            _colliders[i].enabled = isHeld == false;
        }

        for (int i = 0; i < _componentsToDisable.Length; i++)
        {
            _componentsToDisable[i].enabled = isHeld == false;
        }

        for (int i = 0; i < _rigidbodies.Length; i++)
        {
            Rigidbody rigidbody = _rigidbodies[i];

            rigidbody.linearVelocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;

            rigidbody.isKinematic = isHeld == true;
            rigidbody.detectCollisions = isHeld == false;
        }
    }
}
