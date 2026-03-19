using UnityEngine;

public class CharacterMover : MonoBehaviour
{
    private const int WallBufferSize = 16;
    private const float ZeroThreshold = 0.0001f;
    private const float WallNormalDot = 0.98f;

    private readonly Vector3[] _wallNormals = new Vector3[WallBufferSize];

    [Header("Зависимости")]
    [SerializeField] private Rigidbody _rigidbody;

    [Header("Настройки")]
    [SerializeField] private float _speed = 3f;
    [SerializeField] private float _speedSprint = 6f;

    [Header("Стена")]
    [SerializeField] private float _wallNormalMaxY = 0.2f;

    private Vector3 _moveDirection;
    private bool _isSprinting;
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

    public void StopMove()
    {
        _moveDirection = Vector3.zero;
    }

    public void ForceStop()
    {
        _moveDirection = Vector3.zero;

        float currentVerticalVelocity = _rigidbody.linearVelocity.y;
        _rigidbody.linearVelocity = new Vector3(0f, currentVerticalVelocity, 0f);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (ShouldTrackWall(collision) == false)
        {
            return;
        }

        int contactCount = collision.contactCount;
        int contactIndex = 0;

        while (contactIndex < contactCount)
        {
            ContactPoint contactPoint = collision.GetContact(contactIndex);
            Vector3 wallNormal = contactPoint.normal;

            if (wallNormal.y <= _wallNormalMaxY)
            {
                AddWallNormal(wallNormal);
            }

            contactIndex += 1;
        }
    }

    private bool ShouldTrackWall(Collision collision)
    {
        Rigidbody collisionRigidbody = collision.rigidbody;

        if (collisionRigidbody == null)
        {
            return true;
        }

        if (collisionRigidbody.isKinematic == false)
        {
            return false;
        }

        return true;
    }

    private void Move()
    {
        float speed = _speed;

        if (_isSprinting)
        {
            speed = _speedSprint;
        }

        Vector3 clampedMoveDirection = Vector3.ClampMagnitude(_moveDirection, 1f);
        Vector3 targetVelocity = clampedMoveDirection * speed;
        Vector3 adjustedVelocity = targetVelocity;

        if (_wallNormalCount > 0)
        {
            adjustedVelocity = GetSlideVelocity(targetVelocity);
        }

        float currentVerticalVelocity = _rigidbody.linearVelocity.y;
        _rigidbody.linearVelocity = new Vector3(adjustedVelocity.x, currentVerticalVelocity, adjustedVelocity.z);
    }

    private Vector3 GetSlideVelocity(Vector3 targetVelocity)
    {
        Vector3 adjustedVelocity = targetVelocity;
        int normalIndex = 0;

        while (normalIndex < _wallNormalCount)
        {
            Vector3 wallNormal = _wallNormals[normalIndex];
            float velocityAlongNormal = Vector3.Dot(adjustedVelocity, wallNormal);

            if (velocityAlongNormal < 0f)
            {
                adjustedVelocity -= wallNormal * velocityAlongNormal;
            }

            normalIndex += 1;
        }

        adjustedVelocity.y = 0f;

        if (adjustedVelocity.sqrMagnitude <= ZeroThreshold)
        {
            return Vector3.zero;
        }

        return adjustedVelocity;
    }

    private void AddWallNormal(Vector3 wallNormal)
    {
        if (wallNormal.sqrMagnitude <= ZeroThreshold)
        {
            return;
        }

        wallNormal.Normalize();

        int normalIndex = 0;

        while (normalIndex < _wallNormalCount)
        {
            float normalDot = Vector3.Dot(_wallNormals[normalIndex], wallNormal);

            if (normalDot >= WallNormalDot)
            {
                return;
            }

            normalIndex += 1;
        }

        if (_wallNormalCount >= _wallNormals.Length)
        {
            return;
        }

        _wallNormals[_wallNormalCount] = wallNormal;
        _wallNormalCount += 1;
    }

    private void ClearWallContacts()
    {
        _wallNormalCount = 0;
    }
}
