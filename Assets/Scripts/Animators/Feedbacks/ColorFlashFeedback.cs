using UnityEngine;

public class ColorFlashFeedback : Feedback
{
    [SerializeField] private Renderer _renderer;
    [SerializeField] private Color _flashColor = Color.red;
    [SerializeField] private float _flashDuration = 0.2f;
    [SerializeField] private float _returnDuration = 0.2f;

    private readonly ColorerRenderer _colorerRenderer = new ColorerRenderer();
    private Color _baseColor;

    private void Awake()
    {
        _baseColor = _renderer.material.color;
    }

    public override void Play()
    {
        _colorerRenderer.LerpToColor(_renderer, _flashColor, _flashDuration);
        Invoke(nameof(ReturnBaseColor), _flashDuration);
    }

    public override void Stop()
    {
        CancelInvoke(nameof(ReturnBaseColor));
        _colorerRenderer.LerpToColor(_renderer, _baseColor, _returnDuration);
    }

    private void ReturnBaseColor()
    {
        _colorerRenderer.LerpToColor(_renderer, _baseColor, _returnDuration);
    }
}
