using UnityEngine;

public abstract class ChunkVariantRootComposer : MonoBehaviour
{
    public ChunkVariantSwitcherBase Compose(
        GameObject rootObject,
        GameObject staticVisualObject,
        GameObject notStaticVisualObject,
        bool startWithStaticActive
    )
    {
        ChunkVariantSwitcherBase switcher = CreateSwitcher(rootObject);

        switcher.Initialize(staticVisualObject, notStaticVisualObject);

        switcher.SetUseStatic(startWithStaticActive);

        RoomInteriorChunkDynamicOnDamage chunkDynamicOnDamage = rootObject.GetComponent<RoomInteriorChunkDynamicOnDamage>();

        if (chunkDynamicOnDamage == null)
        {
            chunkDynamicOnDamage = rootObject.AddComponent<RoomInteriorChunkDynamicOnDamage>();
        }

        Health health = rootObject.GetComponent<Health>();

        if (health == null)
        {
            chunkDynamicOnDamage.Initialize(switcher);

            return switcher;
        }

        chunkDynamicOnDamage.Initialize(switcher, staticVisualObject, notStaticVisualObject, health);

        return switcher;
    }

    protected abstract ChunkVariantSwitcherBase CreateSwitcher(GameObject rootObject);
}
