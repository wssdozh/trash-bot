using UnityEngine;

public class Cursor : MonoBehaviour
{
    private bool _cursorLocked = true;

    private void Start()
    {
        LockCursor(true);
    }

    public void LockCursor(bool isLocked)
    {
        _cursorLocked = isLocked;
        UnityEngine.Cursor.visible = isLocked == false;
        UnityEngine.Cursor.lockState = isLocked == true ? CursorLockMode.Locked : CursorLockMode.None;
    }

    public bool IsCursorLocked()
    {
        return _cursorLocked;
    }
}
