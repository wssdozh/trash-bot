using UnityEngine;

public sealed class RoomInteriorChunkVariantRootComposer : ChunkVariantRootComposer
{
    protected override ChunkVariantSwitcherBase CreateSwitcher(GameObject rootObject)
    {
        RoomInteriorChunkVariantSwitcher switcher = rootObject.GetComponent<RoomInteriorChunkVariantSwitcher>();

        if (switcher == null)
        {
            switcher = rootObject.AddComponent<RoomInteriorChunkVariantSwitcher>();
        }

        return switcher;
    }
}
