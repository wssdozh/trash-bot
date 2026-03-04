using UnityEngine;

[CreateAssetMenu(menuName = "Room/Interior/Chunk Root Composition Profile")]
public sealed class RoomInteriorChunkRootCompositionProfile : ScriptableObject
{
    [SerializeField] private ChunkRootInstaller[] _installers;

    public void Compose(
        GameObject rootObject,
        GameObject staticVisualObject,
        GameObject notStaticVisualObject,
        bool startWithStaticActive
    )
    {
        ChunkRootContext context = new ChunkRootContext(rootObject, staticVisualObject, notStaticVisualObject, startWithStaticActive);

        for (int index = 0; index < _installers.Length; index++)
        {
            ChunkRootInstaller installer = _installers[index];

            installer.Install(ref context);
        }
    }
}
