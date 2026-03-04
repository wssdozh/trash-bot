using UnityEngine;

public class ColorEmissionFeedback : Feedback
{
    [SerializeField] private Renderer _renderer;
    [SerializeField] private Color _flashColor = Color.darkRed;
    [SerializeField] private float _flashDuration = 0.2f;
    [SerializeField] private float _returnDuration = 0.4f;
    [SerializeField] private float _intensity = 3.0f;

    private readonly ToonRendererEmissiveColorer _toonRendererEmissiveColorer = new ToonRendererEmissiveColorer();
    private Color _baseEmission;

    private void Awake()
    {
        _baseEmission = _toonRendererEmissiveColorer.ReadBaseEmission(_renderer);
    }

    public override void Play()
    {
        _toonRendererEmissiveColorer.LerpToEmission(_renderer, _flashColor, _flashDuration, _intensity);
        Invoke(nameof(ReturnBaseEmission), _flashDuration);
    }

    public override void Stop()
    {
        CancelInvoke(nameof(ReturnBaseEmission));
        _toonRendererEmissiveColorer.LerpToEmission(_renderer, _baseEmission, _returnDuration, 1.0f);
    }

    private void ReturnBaseEmission()
    {
        _toonRendererEmissiveColorer.LerpToEmission(_renderer, _baseEmission, _returnDuration, 1.0f);
    }
}
