using UnityEngine;

public sealed class UiUnscaledTimeProvider : MonoBehaviour
{
    [SerializeField] private string _globalTimePropertyName = "_UiUnscaledTime";

    private int _globalTimePropertyId;

    private void Awake()
    {
        _globalTimePropertyId = Shader.PropertyToID(_globalTimePropertyName);
    }

    private void Update()
    {
        Shader.SetGlobalFloat(_globalTimePropertyId, Time.unscaledTime);
    }
}
