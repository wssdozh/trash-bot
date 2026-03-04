using UnityEngine;

public abstract class ChunkRootInstaller : ScriptableObject
{
    public abstract void Install(ref ChunkRootContext context);
}
