using UnityEngine;

public sealed class RoomInteriorChunkVariantSwitcher : ChunkVariantSwitcherBase
{
    public override void SetUseStatic(bool useStatic)
    {
        if (StaticObject != null)
        {
            StaticObject.SetActive(useStatic);
        }

        if (NotStaticObject != null)
        {
            NotStaticObject.SetActive(useStatic == false);
        }
    }
}
