using UnityEngine;
using UnityEngine.UI;

public class ColorFlashImageFeedback : Feedback
{
    [SerializeField] private Image _image;
    [SerializeField] private Color _flashColor = Color.red;
    [SerializeField] private float _flashDuration = 0.2f;
    [SerializeField] private float _returnDuration = 0.2f;

    private Color _baseColor;

    private void Awake()
    {
        _baseColor = _image.color;
    }

    public override void Play()
    {
        ColorerGraphic.Flash(_image, _flashColor, _baseColor, _flashDuration, _returnDuration, true);
    }

    public override void Stop()
    {
        ColorerGraphic.Stop(_image, _baseColor, _returnDuration, true);
    }
}
