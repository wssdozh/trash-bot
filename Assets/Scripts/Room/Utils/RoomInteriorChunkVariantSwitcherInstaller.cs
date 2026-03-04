using UnityEngine;

[CreateAssetMenu(menuName = "Room/Interior/Chunk Installers/Variant Switcher")]
public sealed class RoomInteriorChunkVariantSwitcherInstaller : ChunkRootInstaller
{
    public override void Install(ref ChunkRootContext context)
    {
        RoomInteriorChunkVariantSwitcher switcher = context.RootObject.GetComponent<RoomInteriorChunkVariantSwitcher>();

        if (switcher == null)
        {
            switcher = context.RootObject.AddComponent<RoomInteriorChunkVariantSwitcher>();
        }

        switcher.Initialize(context.StaticVisualObject, context.NotStaticVisualObject);

        switcher.SetUseStatic(context.StartWithStaticActive);

        context.VariantSwitcher = switcher;
    }
}
