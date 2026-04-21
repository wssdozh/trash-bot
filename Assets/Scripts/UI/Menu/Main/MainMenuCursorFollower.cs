using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class MainMenuCursorFollower : MonoBehaviour
{
    [SerializeField] private RectTransform _cursorRectTransform;

    private void Awake()
    {
        if (_cursorRectTransform == null)
        {
            throw new MissingReferenceException(nameof(_cursorRectTransform));
        }
    }

    private void Update()
    {
        if (Mouse.current == null)
        {
            return;
        }

        _cursorRectTransform.position = Mouse.current.position.ReadValue();
    }
}
