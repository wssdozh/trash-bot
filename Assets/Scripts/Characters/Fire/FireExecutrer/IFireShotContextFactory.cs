using UnityEngine;

public interface IFireShotContextFactory
{
    FireShotContext Create(float timeSeconds, Transform muzzle, LayerMask targetLayers);
}
