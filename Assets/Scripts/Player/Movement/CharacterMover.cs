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
        float speed = _isSprinting ? _speedSprint : _speed;

        Vector3 clampedMoveDirection = Vector3.ClampMagnitude(_moveDirection, 1f);
        Vector3 targetVelocity = clampedMoveDirection * speed;

        _rigidbody.linearVelocity = new Vector3(targetVelocity.x, _rigidbody.linearVelocity.y, targetVelocity.z);
    }
}
