using UnityEngine;


public class CharacterMover : MonoBehaviour
{
    [Header("Зависимости")]
    [SerializeField] private Rigidbody _rigidbody;

    [Header("Настройки")]
    [SerializeField] private float _speed = 3f;
    [SerializeField] private float _speedSprint = 6f;

    private Vector3 _moveDirection;
    private bool _isSprinting;

    private void FixedUpdate()
    {
        Move();
    }

    public void OnMove(Vector2 input)
    {
        _moveDirection = new Vector3(input.x, 0f, input.y);
    }

    public void OnSprint(bool sprinting)
    {
        _isSprinting = sprinting;
    }

    private void Move()
    {
        if (_moveDirection.sqrMagnitude < 0.01f)
            return;

        float speed = _isSprinting ? _speedSprint : _speed;
        Vector3 targetVelocity = _moveDirection.normalized * speed;

        _rigidbody.linearVelocity = new Vector3(targetVelocity.x, _rigidbody.linearVelocity.y, targetVelocity.z);
    }
}
