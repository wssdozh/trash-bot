using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Room/Interior/Chunk Installers/Dynamic On Damage")]
public sealed class RoomInteriorChunkDynamicOnDamageInstaller : ChunkRootInstaller
{
    public override void Install(ref ChunkRootContext context)
    {
        if (context.VariantSwitcher == null)
        {
            throw new InvalidOperationException(nameof(context.VariantSwitcher));
        }

        RoomInteriorChunkDynamicOnDamage chunkDynamicOnDamage = context.RootObject.GetComponent<RoomInteriorChunkDynamicOnDamage>();

        if (chunkDynamicOnDamage == null)
        {
            chunkDynamicOnDamage = context.RootObject.AddComponent<RoomInteriorChunkDynamicOnDamage>();
        }

        chunkDynamicOnDamage.Initialize(context.VariantSwitcher);
    }
}
