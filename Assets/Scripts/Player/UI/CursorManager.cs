using UnityEngine;
using UnityEngine.InputSystem;

public class CursorManager : MonoBehaviour
{
    private const int HitBufferSize = 32;
    private const float HitDistance = 100f;

    [Header("Зависимости")]
    [SerializeField] private Camera _camera;
    [SerializeField] private Transform _player;
    [SerializeField] private RectTransform _rectTransform;
    [SerializeField] private LayerMask _interactableMask;

    private readonly RaycastHit[] _hitBuffer = new RaycastHit[HitBufferSize];
    private Vector2 _mouseScreenPos;
    private Transform _playerRoot;

    public Vector3 MouseWorldPos { get; private set; }
    public Vector3 MouseGroundPos { get; private set; }
    public Vector3 MouseHitPos { get; private set; }
    public bool HasHit { get; private set; }

    public Vector2 MouseScreenPos => _mouseScreenPos;

    private void Awake()
    {
        Cursor.visible = false;
        RefreshPlayerRoot();
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

        return TryGetHit(ray, out hitInfo);
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
        }
        else
        {
            HasHit = false;
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
        int hitCount = Physics.RaycastNonAlloc(ray, _hitBuffer, HitDistance, _interactableMask, QueryTriggerInteraction.Ignore);
        float nearestDistance = float.MaxValue;
        int nearestHitIndex = -1;

        for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
        {
            RaycastHit currentHit = _hitBuffer[hitIndex];

            if (IsIgnoredCollider(currentHit.collider))
            {
                continue;
            }

            if (currentHit.distance < nearestDistance)
            {
                nearestDistance = currentHit.distance;
                nearestHitIndex = hitIndex;
            }
        }

        if (nearestHitIndex < 0)
        {
            hitInfo = default;

            return false;
        }

        hitInfo = _hitBuffer[nearestHitIndex];

        return true;
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
