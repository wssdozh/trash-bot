using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterMover _characterMover;
    [SerializeField] private CameraMover _cameraMover;
    [SerializeField] private CursorManager _cursorManager;

    private void OnEnable()
    {
        if (_characterMover != null)
            _characterMover.enabled = true;

        if (_cameraMover != null)
            _cameraMover.enabled = true;

        if (_cursorManager != null)
            _cursorManager.LockCursor(true);
    }

    private void OnDisable()
    {
        if (_characterMover != null)
            _characterMover.enabled = false;

        if (_cameraMover != null)
            _cameraMover.enabled = false;

        if (_cursorManager != null)
            _cursorManager.LockCursor(false);
    }

    private void Update()
    {
        if (_characterMover != null)
            _characterMover.TickMovement();
    }

    private void LateUpdate()
    {
        if (_cameraMover != null)
            _cameraMover.TickCamera();
    }
}
