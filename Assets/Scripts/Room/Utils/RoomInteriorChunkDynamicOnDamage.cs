using System;
using UnityEngine;

public sealed class RoomInteriorChunkDynamicOnDamage : MonoBehaviour
{
    private ChunkVariantSwitcherBase _chunkVariantSwitcher;

    private bool _dynamicEnabled;

    public void Initialize(ChunkVariantSwitcherBase chunkVariantSwitcher)
    {
        _chunkVariantSwitcher = chunkVariantSwitcher;
    }

    public void ApplyDamage()
    {
        EnableDynamic();
    }

    public void ApplyDamage(int damage)
    {
        EnableDynamic();
    }

    public void ApplyDamage(float damage)
    {
        EnableDynamic();
    }

    private void EnableDynamic()
    {
        if (_chunkVariantSwitcher == null)
        {
            throw new MissingReferenceException(nameof(_chunkVariantSwitcher));
        }

        if (_dynamicEnabled == true)
        {
            return;
        }

        _chunkVariantSwitcher.SetUseStatic(false);

        gameObject.isStatic = false;

        _dynamicEnabled = true;
    }
}
