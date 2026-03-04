using UnityEngine;

public struct ChunkRootContext
{
    public GameObject RootObject;
    public GameObject StaticVisualObject;
    public GameObject NotStaticVisualObject;

    public bool StartWithStaticActive;

    public ChunkVariantSwitcherBase VariantSwitcher;

    public ChunkRootContext(
        GameObject rootObject,
        GameObject staticVisualObject,
        GameObject notStaticVisualObject,
        bool startWithStaticActive
    )
    {
        RootObject = rootObject;
        StaticVisualObject = staticVisualObject;
        NotStaticVisualObject = notStaticVisualObject;

        StartWithStaticActive = startWithStaticActive;

        VariantSwitcher = null;
    }
}
