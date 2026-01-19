using UnityEngine;

public class CharacterMover : MonoBehaviour
{
    [Header("Зависимости")]
    [SerializeField] private Rigidbody _rigidbody;

    [Header("Настройки")]
    [SerializeField] private float _speed = 3f;
    [SerializeField] private float _speedSprint = 6f;

    [Header("Стена")]
    [SerializeField] private float _wallNormalMaxY = 0.2f;

    private Vector3 _moveDirection;
    private bool _isSprinting;

    private Vector3 _wallNormalSum;
    private int _wallNormalCount;

    private void FixedUpdate()
    {
        Move();
        ClearWallContacts();
    }

    public void OnMove(Vector2 input)
    {
        _moveDirection = new Vector3(input.x, 0f, input.y);
    }

    public void OnSprint(bool sprinting)
    {
        _isSprinting = sprinting;
    }

    private void OnCollisionStay(Collision collision)
    {
        int contactCount = collision.contactCount;
        int contactIndex = 0;

        while (contactIndex < contactCount)
        {
            ContactPoint contactPoint = collision.GetContact(contactIndex);
            Vector3 normal = contactPoint.normal;

            if (normal.y <= _wallNormalMaxY)
            {
                _wallNormalSum += normal;
                _wallNormalCount += 1;
            }

            contactIndex += 1;
        }
    }

    private void Move()
    {
        float speed = _speed;

        if (_isSprinting == true)
        {
            speed = _speedSprint;
        }

        Vector3 clampedMoveDirection = Vector3.ClampMagnitude(_moveDirection, 1f);
        Vector3 targetVelocity = clampedMoveDirection * speed;

        Vector3 adjustedVelocity = targetVelocity;

        if (_wallNormalCount > 0)
        {
            Vector3 averageWallNormal = _wallNormalSum / (float)_wallNormalCount;

            if (averageWallNormal.sqrMagnitude > 0.0001f)
            {
                averageWallNormal.Normalize();

                float velocityAlongNormal = Vector3.Dot(targetVelocity, averageWallNormal);

                if (velocityAlongNormal < 0f)
                {
                    adjustedVelocity = targetVelocity - (averageWallNormal * velocityAlongNormal);
                }
            }
        }

        float currentVerticalVelocity = _rigidbody.linearVelocity.y;
        _rigidbody.linearVelocity = new Vector3(adjustedVelocity.x, currentVerticalVelocity, adjustedVelocity.z);
    }

    private void ClearWallContacts()
    {
        _wallNormalSum = Vector3.zero;
        _wallNormalCount = 0;
    }
}
