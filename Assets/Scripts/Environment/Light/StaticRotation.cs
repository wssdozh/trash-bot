using UnityEngine;

public sealed class StaticRotation : MonoBehaviour
{
    [SerializeField] private Light _light;
    [SerializeField] private Vector3 _rotationAxis = Vector3.right;
    [SerializeField] private float _degreesPerSecond = 10f;
    [SerializeField] private bool _isActive = true;

    public void SetActive(bool isActive)
    {
        _isActive = isActive;
        RefreshUpdateState();
    }

    public void SetDegreesPerSecond(float degreesPerSecond)
    {
        _degreesPerSecond = degreesPerSecond;
        RefreshUpdateState();
    }

    public void SetRotationAxis(Vector3 rotationAxis)
    {
        _rotationAxis = rotationAxis;
    }

    private void Awake()
    {
        RefreshUpdateState();
    }

    private void Update()
    {
        if (_isActive == false)
        {
            return;
        }

        float angle = _degreesPerSecond * Time.deltaTime;
        Quaternion rotation = Quaternion.AngleAxis(angle, _rotationAxis);
        _light.transform.rotation = rotation * _light.transform.rotation;
    }

    private void RefreshUpdateState()
    {
        if (_isActive == false)
        {
            enabled = false;

            return;
        }

        enabled = Mathf.Abs(_degreesPerSecond) > Mathf.Epsilon;
    }
}
