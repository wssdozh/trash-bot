using UnityEngine;

public class CharacterJump : MonoBehaviour
{
    [Header("Зависимости")]
    [SerializeField] private Rigidbody _rigidbody;

    [Header("Настройки")]
    [SerializeField] private float _jumpForce = 5f;
    [SerializeField] private float _rayLengthGround = 0.2f;
    [SerializeField] private LayerMask _groundLayer = 1;

    private bool IsGrounded => Physics.Raycast(transform.position, Vector3.down, _rayLengthGround, _groundLayer);

    public void OnJump()
    {
        if (IsGrounded)
        {
            _rigidbody.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * _rayLengthGround);
    }
}
