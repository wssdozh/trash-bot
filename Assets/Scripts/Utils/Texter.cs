using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class Texter : MonoBehaviour
{
    [SerializeField] private Text _text;
    [SerializeField] private float _fadeDuration = 0.25f;

    public void Show(string message)
    {
        DOTween.Kill(_text.gameObject);

        if (_text.gameObject.activeSelf == false)
        {
            _text.gameObject.SetActive(true);
            Color color = _text.color;
            color.a = 0f;
            _text.color = color;
        }

        _text.text = message;

        _text
            .DOFade(1f, _fadeDuration)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true)
            .SetLink(_text.gameObject);
    }

    public void Hide()
    {
        DOTween.Kill(_text.gameObject);

        if (_text.gameObject.activeSelf == false)
            return;

        Color color = _text.color;

        if (color.a <= 0f)
        {
            _text.gameObject.SetActive(false);

            return;
        }

        _text
            .DOFade(0f, _fadeDuration)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true)
            .SetLink(_text.gameObject)
            .OnComplete(() =>
            {
                _text.gameObject.SetActive(false);
            });
    }

    public void Clear()
    {
        if (_text == null)
            return;

        _text.text = string.Empty;
        _text.gameObject.SetActive(false);
        DOTween.Kill(_text.gameObject);
    }
}
