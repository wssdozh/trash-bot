using UnityEngine;

public sealed class AmmoRenderersDisabler : AmmoLifeListener
{
    [Header("Зависимости")]
    [SerializeField] private Renderer[] _renderers;

    protected override void OnAmmoEnabled()
    {
        SetEnabled(true);
    }

    protected override void OnAmmoLifeEnded()
    {
        SetEnabled(false);
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        SetEnabled(false);
    }

    private void SetEnabled(bool isEnabled)
    {
        int index = 0;

        while (index < _renderers.Length)
        {
            _renderers[index].enabled = isEnabled;

            index++;
        }
    }
}
