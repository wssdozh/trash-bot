using Unity.AI.Navigation;
using UnityEngine;

[CreateAssetMenu(menuName = "Room/Interior/Chunk Installers/Variant Switcher")]
public sealed class RoomInteriorChunkVariantSwitcherInstaller : ChunkRootInstaller
{
    private const int NotWalkableArea = 1;

    public override void Install(ref ChunkRootContext context)
    {
        RoomInteriorChunkVariantSwitcher switcher = context.RootObject.GetComponent<RoomInteriorChunkVariantSwitcher>();

        if (switcher == null)
        {
            switcher = context.RootObject.AddComponent<RoomInteriorChunkVariantSwitcher>();
        }

        switcher.Initialize(context.StaticVisualObject, context.NotStaticVisualObject);

        switcher.SetUseStatic(context.StartWithStaticActive);
        AddNavModifier(context.RootObject);

        context.VariantSwitcher = switcher;
    }

    private void AddNavModifier(GameObject targetObject)
    {
        NavMeshModifier navMeshModifier = targetObject.GetComponent<NavMeshModifier>();

        if (navMeshModifier == null)
        {
            navMeshModifier = targetObject.AddComponent<NavMeshModifier>();
        }

        navMeshModifier.overrideArea = true;
        navMeshModifier.area = NotWalkableArea;
        navMeshModifier.ignoreFromBuild = false;
        navMeshModifier.applyToChildren = true;
    }
}
