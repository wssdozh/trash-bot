using UnityEngine;
using UnityEngine.UI;

public class ColorFlashImageFeedback : Feedback
{
    [SerializeField] private Image _image;
    [SerializeField] private Color _flashColor = Color.red;
    [SerializeField] private float _flashDuration = 0.2f;
    [SerializeField] private float _returnDuration = 0.2f;

    private readonly ColorerGraphic _colorerGraphic = new ColorerGraphic();
    private Color _baseColor;

    private void Awake()
    {
        _baseColor = _image.color;
    }

    public override void Play()
    {
        _colorerGraphic.Flash(_image, _flashColor, _baseColor, _flashDuration, _returnDuration, true);
    }

    public override void Stop()
    {
        _colorerGraphic.Stop(_image, _baseColor, _returnDuration, true);
    }
}
