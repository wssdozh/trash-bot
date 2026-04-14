using UnityEngine;

[DisallowMultipleComponent]
public sealed class ExcavatorBossProjectile : MonoBehaviour
{
    private const int HitBufferSize = 8;
    private const float DirectionThreshold = 0.0001f;

    private readonly RaycastHit[] _hitBuffer = new RaycastHit[HitBufferSize];

    private Transform _ownerRoot;
    private Vector3 _moveDirection;
    private Vector3 _spinAxis;
    private float _speed;
    private float _damage;
    private float _lifeTime;
    private float _hitRadius;
    private float _spinSpeed;
    private MaterialPropertyBlock _propertyBlock;

    public void Setup(
        Transform ownerRoot,
        Vector3 moveDirection,
        float speed,
        float damage,
        float lifeTime,
        float hitRadius,
        float spinSpeed
    )
    {
        _ownerRoot = ownerRoot;
        _moveDirection = moveDirection;
        _moveDirection.y = 0f;

        if (_moveDirection.sqrMagnitude <= DirectionThreshold)
        {
            _moveDirection = Vector3.forward;
        }
        else
        {
            _moveDirection.Normalize();
        }

        _speed = speed;
        _damage = damage;
        _lifeTime = lifeTime;
        _hitRadius = hitRadius;
        _spinSpeed = spinSpeed;
        _spinAxis = new Vector3(0.8f, 1f, 0.35f).normalized;
    }

    public void SetVisual(Vector3 localScale, Color color)
    {
        transform.localScale = localScale;

        Renderer partRenderer = GetComponent<Renderer>();

        if (partRenderer == null)
        {
            return;
        }

        if (_propertyBlock == null)
        {
            _propertyBlock = new MaterialPropertyBlock();
        }

        _propertyBlock.Clear();
        _propertyBlock.SetColor("_Color", color);
        partRenderer.SetPropertyBlock(_propertyBlock);
    }

    private void Update()
    {
        if (_lifeTime <= 0f)
        {
            Destroy(gameObject);

            return;
        }

        float moveDistance = _speed * Time.deltaTime;
        int hitCount = Physics.SphereCastNonAlloc(
            transform.position,
            _hitRadius,
            _moveDirection,
            _hitBuffer,
            moveDistance,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        int hitIndex = 0;

        while (hitIndex < hitCount)
        {
            RaycastHit hitInfo = _hitBuffer[hitIndex];
            Collider hitCollider = hitInfo.collider;

            if (CanUseHit(hitCollider))
            {
                ApplyHit(hitCollider);
                Destroy(gameObject);

                return;
            }

            hitIndex += 1;
        }

        transform.position += _moveDirection * moveDistance;
        transform.Rotate(_spinAxis, _spinSpeed * Time.deltaTime, Space.Self);
        _lifeTime -= Time.deltaTime;
    }

    private bool CanUseHit(Collider hitCollider)
    {
        if (hitCollider == null)
        {
            return false;
        }

        if (_ownerRoot != null)
        {
            if (hitCollider.transform.IsChildOf(_ownerRoot))
            {
                return false;
            }
        }

        return true;
    }

    private void ApplyHit(Collider hitCollider)
    {
        Player player = hitCollider.GetComponentInParent<Player>();

        if (player == null)
        {
            return;
        }

        Health targetHealth = hitCollider.GetComponentInParent<Health>();

        if (targetHealth == null)
        {
            return;
        }

        targetHealth.Decrease(_damage);
    }
}
