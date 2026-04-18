using UnityEngine;

public class CharacterMover : MonoBehaviour
{
    private const int WallBufferSize = 16;
    private const float ZeroThreshold = 0.0001f;
    private const float WallNormalDot = 0.98f;

    private readonly Vector3[] _wallNormals = new Vector3[WallBufferSize];

    [Header("Р—Р°РІРёСЃРёРјРѕСЃС‚Рё")]
    [SerializeField] private Rigidbody _rigidbody;

    [Header("РќР°СЃС‚СЂРѕР№РєРё")]
    [SerializeField] private float _speed = 3f;
    [SerializeField] private float _speedSprint = 6f;

    [Header("РЎС‚РµРЅР°")]
    [SerializeField] private bool _isWallSlideEnabled = true;
    [SerializeField] private float _wallNormalMaxY = 0.2f;

    private Vector3 _moveDirection;
    private Vector3 _knockbackVelocity;
    private bool _isSprinting;
    private float _knockbackTimer;
    private float _knockbackDuration;
    private int _wallNormalCount;

    public float Speed => _speed;

    public float SprintSpeed => _speedSprint;

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

    public void ApplyKnockback(Vector3 direction, float speed, float duration, float lift)
    {
        if (_rigidbody.isKinematic)
        {
            return;
        }

        direction.y = 0f;

        if (direction.sqrMagnitude <= ZeroThreshold)
        {
            return;
        }

        if (speed <= 0f)
        {
            return;
        }

        if (duration <= 0f)
        {
            return;
        }

        direction.Normalize();
        _moveDirection = Vector3.zero;
        _knockbackVelocity = direction * speed;
        _knockbackTimer = duration;
        _knockbackDuration = duration;

        float currentVerticalVelocity = _rigidbody.linearVelocity.y;
        float nextVerticalVelocity = currentVerticalVelocity;

        if (lift > nextVerticalVelocity)
        {
            nextVerticalVelocity = lift;
        }

        _rigidbody.linearVelocity = new Vector3(_knockbackVelocity.x, nextVerticalVelocity, _knockbackVelocity.z);
    }

    public void ForceStop()
    {
        _moveDirection = Vector3.zero;
        ClearKnockback();

        if (_rigidbody.isKinematic)
        {
            return;
        }

        float currentVerticalVelocity = _rigidbody.linearVelocity.y;
        _rigidbody.linearVelocity = new Vector3(0f, currentVerticalVelocity, 0f);
    }

    public void ApplySpeed(float speed, float sprintSpeed)
    {
        _speed = Mathf.Max(0f, speed);
        _speedSprint = Mathf.Max(_speed, sprintSpeed);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (_isWallSlideEnabled == false)
        {
            return;
        }

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
        if (_rigidbody.isKinematic)
        {
            return;
        }

        float speed = _speed;

        if (_isSprinting)
        {
            speed = _speedSprint;
        }

        Vector3 clampedMoveDirection = Vector3.ClampMagnitude(_moveDirection, 1f);
        Vector3 targetVelocity = clampedMoveDirection * speed;
        Vector3 adjustedVelocity = targetVelocity;
        Vector3 knockbackVelocity;

        if (TryGetKnockbackVelocity(out knockbackVelocity))
        {
            adjustedVelocity = knockbackVelocity;
        }

        if (_isWallSlideEnabled && _wallNormalCount > 0)
        {
            adjustedVelocity = GetSlideVelocity(adjustedVelocity);
        }

        float currentVerticalVelocity = _rigidbody.linearVelocity.y;
        _rigidbody.linearVelocity = new Vector3(adjustedVelocity.x, currentVerticalVelocity, adjustedVelocity.z);
        TickKnockback();
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

    private bool TryGetKnockbackVelocity(out Vector3 knockbackVelocity)
    {
        knockbackVelocity = Vector3.zero;

        if (_knockbackTimer <= 0f)
        {
            return false;
        }

        float knockbackFactor = 1f;

        if (_knockbackDuration > 0f)
        {
            knockbackFactor = Mathf.Clamp01(_knockbackTimer / _knockbackDuration);
        }

        knockbackVelocity = _knockbackVelocity * knockbackFactor;

        return true;
    }

    private void TickKnockback()
    {
        if (_knockbackTimer <= 0f)
        {
            return;
        }

        _knockbackTimer = Mathf.Max(0f, _knockbackTimer - Time.fixedDeltaTime);

        if (_knockbackTimer > 0f)
        {
            return;
        }

        ClearKnockback();
    }

    private void ClearKnockback()
    {
        _knockbackVelocity = Vector3.zero;
        _knockbackTimer = 0f;
        _knockbackDuration = 0f;
    }
}
