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

    public Vector3 MouseWorldPos { get; private set; }
    public Vector3 MouseGroundPos { get; private set; }
    public Vector3 MouseHitPos { get; private set; }
    public bool HasHit { get; private set; }

    public Vector2 MouseScreenPos => Mouse.current.position.ReadValue();

    private void Awake()
    {
        Cursor.visible = false;
    }

    private void Update()
    {
        _rectTransform.position = MouseScreenPos;

        UpdateWorldPositions();
    }

    public bool TryGetHitObject(out RaycastHit hitInfo)
    {
        if (_camera == null)
        {
            hitInfo = default;

            return false;
        }

        Ray ray = _camera.ScreenPointToRay(MouseScreenPos);

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

        Ray ray = _camera.ScreenPointToRay(MouseScreenPos);
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

        if (_player != null)
        {
            if (collider.transform.IsChildOf(_player))
            {
                return true;
            }
        }

        Player player = collider.GetComponentInParent<Player>();

        if (player != null)
        {
            return true;
        }

        return false;
    }
}
