using UnityEngine;

public class ColorEmissionFeedback : Feedback
{
    [SerializeField] private Renderer _renderer;
    [SerializeField] private Color _flashColor = Color.darkRed;
    [SerializeField] private float _flashDuration = 0.2f;
    [SerializeField] private float _returnDuration = 0.4f;
    [SerializeField] private float _intensity = 3.0f;

    private Color _baseEmission;

    private void Awake()
    {
        _baseEmission = ToonRendererEmissiveColorer.ReadBaseEmission(_renderer);
    }

    public override void Play()
    {
        ToonRendererEmissiveColorer.LerpToEmission(_renderer, _flashColor, _flashDuration, _intensity);
        Invoke(nameof(ReturnBaseEmission), _flashDuration);
    }

    public override void Stop()
    {
        CancelInvoke(nameof(ReturnBaseEmission));
        ToonRendererEmissiveColorer.LerpToEmission(_renderer, _baseEmission, _returnDuration, 1.0f);
    }

    private void ReturnBaseEmission()
    {
        ToonRendererEmissiveColorer.LerpToEmission(_renderer, _baseEmission, _returnDuration, 1.0f);
    }
}
