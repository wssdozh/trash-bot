using UnityEngine;


class CharacterRotator : MonoBehaviour
{
    [SerializeField] private float _rotationSpeed;
    [SerializeField] private CursorManager _cursorManager;

    public void Rotate()
    {
        Vector3 direction = (_cursorManager.MouseWorldPos - transform.position).normalized;
        direction.y = 0;

        float rotationFactor = 100f;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRot,
                _rotationSpeed * Time.fixedDeltaTime * rotationFactor
            );
        }
    }

    public void RotateTowardsMovement(Vector2 moveInput)
    {
        if (moveInput.sqrMagnitude < 0.001f)
            return;

        float rotationFactor = 100f;

        Vector3 moveDir = new Vector3(moveInput.x, 0, moveInput.y).normalized;

        Quaternion targetRot = Quaternion.LookRotation(moveDir);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRot,
            _rotationSpeed * Time.fixedDeltaTime * rotationFactor
        );
    }
}