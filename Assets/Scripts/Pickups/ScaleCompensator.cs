using UnityEngine;

public sealed class ParentScaleCompensator : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private Vector3 _targetWorldScale = Vector3.one;

    private void OnEnable()
    {
        Apply();
    }

    private void Apply()
    {
        Transform parentTransform = transform.parent;

        if (parentTransform == null)
            return;

        Vector3 parentWorldScale = parentTransform.lossyScale;

        if (Mathf.Abs(parentWorldScale.x) <= Mathf.Epsilon)
            return;

        if (Mathf.Abs(parentWorldScale.y) <= Mathf.Epsilon)
            return;

        if (Mathf.Abs(parentWorldScale.z) <= Mathf.Epsilon)
            return;

        Vector3 localScale = new Vector3(
            _targetWorldScale.x / parentWorldScale.x,
            _targetWorldScale.y / parentWorldScale.y,
            _targetWorldScale.z / parentWorldScale.z
        );

        transform.localScale = localScale;
    }
}
