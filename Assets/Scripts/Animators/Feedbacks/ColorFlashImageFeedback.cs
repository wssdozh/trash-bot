using UnityEngine;
using UnityEngine.UI;

public class ColorFlashImageFeedback : Feedback
{
    [SerializeField] private Image _image;
    [SerializeField] private Color _flashColor = Color.red;
    [SerializeField] private float _flashDuration = 0.2f;
    [SerializeField] private float _returnDuration = 0.2f;

    private Color _baseColor;
    private Coroutine _routine;

    private void Awake()
    {
        _baseColor = _image.color;
    }

    public override void Play()
    {
        StopRunningRoutine();
        _routine = StartCoroutine(FlashRoutine());
    }

    public override void Stop()
    {
        StopRunningRoutine();
        _routine = StartCoroutine(LerpColorRoutine(_image.color, _baseColor, _returnDuration));
    }

    private System.Collections.IEnumerator FlashRoutine()
    {
        yield return LerpColorRoutine(_baseColor, _flashColor, _flashDuration);
        yield return LerpColorRoutine(_flashColor, _baseColor, _returnDuration);
        _routine = null;
    }

    private System.Collections.IEnumerator LerpColorRoutine(Color fromColor, Color toColor, float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float t = duration > 0f ? elapsedTime / duration : 1f;
            _image.color = Color.Lerp(fromColor, toColor, t);
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        _image.color = toColor;
    }

    private void StopRunningRoutine()
    {
        if (_routine == null)
        {
            return;
        }

        StopCoroutine(_routine);
        _routine = null;
    }
}
