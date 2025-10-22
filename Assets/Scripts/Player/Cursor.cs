using UnityEngine;
using UnityEngine.InputSystem;


public class CursorManager : MonoBehaviour
{
    [Header("Зависимости")]
    [SerializeField] private Camera _cam;
    [SerializeField] private Transform _player;
    [SerializeField] private RectTransform _rectTransform;

    public Vector3 MouseWorldPos { get; private set; }
    public Vector3 MouseGroundPos { get; private set; }

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
        if (_cam == null || _player == null)
            return;

        Ray ray = _cam.ScreenPointToRay(MouseScreenPos);

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
}
