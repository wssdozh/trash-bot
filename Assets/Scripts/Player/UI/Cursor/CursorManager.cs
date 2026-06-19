using UnityEngine;
using UnityEngine.InputSystem;

public class CursorManager : MonoBehaviour
{
    private const int HitBufferSize = 32;
    private const float HitDistance = 100f;
    private const float DefaultSurfaceLuminance = 0f;

    private static readonly int s_colorPropertyId = Shader.PropertyToID("_Color");

    [Header("Зависимости")]
    [SerializeField] private Camera _camera;
    [SerializeField] private Transform _player;
    [SerializeField] private RectTransform _rectTransform;
    [SerializeField] private LayerMask _interactableMask;

    private readonly RaycastHit[] _hitBuffer = new RaycastHit[HitBufferSize];
    private Vector2 _mouseScreenPos;
    private Transform _playerRoot;
    private Collider _currentHitCollider;

    public static CursorManager Instance { get; private set; }
    public Vector3 MouseWorldPos { get; private set; }
    public Vector3 MouseGroundPos { get; private set; }
    public Vector3 MouseHitPos { get; private set; }
    public bool HasHit { get; private set; }
    public bool HasDamageableHit { get; private set; }
    public Color HitSurfaceColor { get; private set; } = Color.black;

    public Vector2 MouseScreenPos => _mouseScreenPos;

    private void Awake()
    {
        Instance = this;
        Cursor.visible = false;
        RefreshPlayerRoot();
    }

    private void OnDestroy()
    {
        if (ReferenceEquals(Instance, this))
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (Mouse.current == null)
        {
            return;
        }

        _mouseScreenPos = Mouse.current.position.ReadValue();
        _rectTransform.position = _mouseScreenPos;

        UpdateWorldPositions();
    }

    public bool TryGetHitObject(out RaycastHit hitInfo)
    {
        if (_camera == null)
        {
            hitInfo = default;

            return false;
        }

        if (Mouse.current != null)
        {
            _mouseScreenPos = Mouse.current.position.ReadValue();
        }

        Ray ray = _camera.ScreenPointToRay(_mouseScreenPos);

        bool hasHit = TryGetHit(ray, out hitInfo);

        if (hasHit)
        {
            ApplyHitCollider(hitInfo.collider);
        }
        else
        {
            ClearHitCollider();
        }

        return hasHit;
    }

    public void Enable()
    {
        _rectTransform.gameObject.SetActive(true);
    }

    public void Disable()
    {
        _rectTransform.gameObject.SetActive(false);
    }

    private void UpdateWorldPositions()
    {
        if (_camera == null || _player == null)
        {
            return;
        }

        if (_playerRoot == null || _playerRoot != _player.root)
        {
            RefreshPlayerRoot();
        }

        Ray ray = _camera.ScreenPointToRay(_mouseScreenPos);
        RaycastHit hitInfo;

        if (TryGetHit(ray, out hitInfo))
        {
            MouseHitPos = hitInfo.point;
            HasHit = true;
            ApplyHitCollider(hitInfo.collider);
        }
        else
        {
            HasHit = false;
            ClearHitCollider();
        }

        Plane playerPlane = new Plane(Vector3.up, new Vector3(0, _player.position.y, 0));

        if (playerPlane.Raycast(ray, out float distToPlayer))
        {
            MouseWorldPos = ray.GetPoint(distToPlayer);
        }

        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (groundPlane.Raycast(ray, out float distToGround))
        {
            MouseGroundPos = ray.GetPoint(distToGround);
        }
    }

    private bool TryGetHit(Ray ray, out RaycastHit hitInfo)
    {
        int hitCount = Physics.RaycastNonAlloc(ray, _hitBuffer, HitDistance, _interactableMask, QueryTriggerInteraction.Collide);
        RaycastHit nearestAimHit = default;
        RaycastHit nearestHit = default;
        Vector3 nearestAimPoint = Vector3.zero;
        float nearestAimDistance = float.MaxValue;
        float nearestDistance = float.MaxValue;
        bool hasAimHit = false;
        bool hasHit = false;

        for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
        {
            RaycastHit currentHit = _hitBuffer[hitIndex];
            Collider currentCollider = currentHit.collider;

            if (IsIgnoredCollider(currentCollider))
            {
                continue;
            }

            bool isAimHit = IsAimHit(currentCollider);

            if (currentCollider.isTrigger)
            {
                if (isAimHit == false)
                {
                    continue;
                }
            }

            if (isAimHit)
            {
                if (currentHit.distance < nearestAimDistance)
                {
                    nearestAimDistance = currentHit.distance;
                    nearestAimHit = currentHit;
                    nearestAimPoint = GetAimPoint(currentCollider, currentHit);
                    hasAimHit = true;
                }

                continue;
            }

            if (currentHit.distance < nearestDistance)
            {
                nearestDistance = currentHit.distance;
                nearestHit = currentHit;
                hasHit = true;
            }
        }

        if (hasAimHit)
        {
            hitInfo = nearestAimHit;
            hitInfo.point = nearestAimPoint;

            return true;
        }

        if (hasHit == false)
        {
            hitInfo = default;

            return false;
        }

        hitInfo = nearestHit;

        return true;
    }

    private void ApplyHitCollider(Collider collider)
    {
        if (ReferenceEquals(_currentHitCollider, collider))
        {
            return;
        }

        _currentHitCollider = collider;
        HasDamageableHit = IsDamageableCollider(collider);
        HitSurfaceColor = GetSurfaceColor(collider);
    }

    private void ClearHitCollider()
    {
        if (_currentHitCollider == null)
        {
            return;
        }

        _currentHitCollider = null;
        HasDamageableHit = false;
        HitSurfaceColor = new Color(DefaultSurfaceLuminance, DefaultSurfaceLuminance, DefaultSurfaceLuminance, 1f);
    }

    private bool IsDamageableCollider(Collider collider)
    {
        if (collider == null)
        {
            return false;
        }

        EnemyAimCollider enemyAimCollider = collider.GetComponent<EnemyAimCollider>();

        if (enemyAimCollider != null)
        {
            return true;
        }

        DamageableObject damageableObject = collider.GetComponentInParent<DamageableObject>();

        if (damageableObject != null)
        {
            return true;
        }

        Health health = collider.GetComponentInParent<Health>();

        if (health == null)
        {
            return false;
        }

        Player player = health.GetComponentInParent<Player>();

        if (player != null)
        {
            return false;
        }

        return health.Value > health.MinValue;
    }

    private Color GetSurfaceColor(Collider collider)
    {
        if (collider == null)
        {
            return new Color(DefaultSurfaceLuminance, DefaultSurfaceLuminance, DefaultSurfaceLuminance, 1f);
        }

        SpriteRenderer spriteRenderer = collider.GetComponentInParent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            return spriteRenderer.color;
        }

        Renderer renderer = collider.GetComponentInParent<Renderer>();

        if (renderer == null)
        {
            return new Color(DefaultSurfaceLuminance, DefaultSurfaceLuminance, DefaultSurfaceLuminance, 1f);
        }

        Material material = renderer.sharedMaterial;

        if (material == null)
        {
            return new Color(DefaultSurfaceLuminance, DefaultSurfaceLuminance, DefaultSurfaceLuminance, 1f);
        }

        if (material.HasProperty(s_colorPropertyId) == false)
        {
            return new Color(DefaultSurfaceLuminance, DefaultSurfaceLuminance, DefaultSurfaceLuminance, 1f);
        }

        return material.GetColor(s_colorPropertyId);
    }

    private Vector3 GetAimPoint(Collider collider, RaycastHit hitInfo)
    {
        EnemyAimCollider enemyAimCollider = collider.GetComponent<EnemyAimCollider>();

        if (enemyAimCollider != null)
        {
            return enemyAimCollider.AimPoint;
        }

        return hitInfo.point;
    }

    private bool IsAimHit(Collider collider)
    {
        EnemyAimCollider enemyAimCollider = collider.GetComponent<EnemyAimCollider>();

        if (enemyAimCollider != null)
        {
            return true;
        }

        return false;
    }

    private bool IsIgnoredCollider(Collider collider)
    {
        if (collider == null)
        {
            return true;
        }

        Transform colliderTransform = collider.transform;

        if (_player != null)
        {
            if (colliderTransform.IsChildOf(_player))
            {
                return true;
            }
        }

        if (_playerRoot != null)
        {
            if (colliderTransform.IsChildOf(_playerRoot))
            {
                return true;
            }
        }

        return false;
    }

    private void RefreshPlayerRoot()
    {
        if (_player == null)
        {
            _playerRoot = null;

            return;
        }

        _playerRoot = _player.root;
    }
}
