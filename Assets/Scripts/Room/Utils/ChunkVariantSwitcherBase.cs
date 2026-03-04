using UnityEngine;

public abstract class ChunkVariantSwitcherBase : MonoBehaviour
{
    protected GameObject StaticObject;
    protected GameObject NotStaticObject;

    public void Initialize(GameObject staticObject, GameObject notStaticObject)
    {
        StaticObject = staticObject;
        NotStaticObject = notStaticObject;
    }

    public abstract void SetUseStatic(bool useStatic);
}
