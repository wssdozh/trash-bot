using UnityEngine;

internal static class LevelRoomBoundsCalculator
{
    public static Bounds CalculateRoomBounds(RoomGenerator room, float padding)
    {
        Renderer[] renderers = room.GetComponentsInChildren<Renderer>();

        if (renderers == null || renderers.Length == 0)
        {

            Bounds fallback = new Bounds(room.transform.position, Vector3.one);

            fallback.Expand(new Vector3(padding * 2f, 0f, padding * 2f));

            fallback.center = new Vector3(fallback.center.x, 0f, fallback.center.z);
            fallback.size = new Vector3(fallback.size.x, 1f, fallback.size.z);

            return fallback;

        }

        Bounds bounds = renderers[0].bounds;

        for (int index = 1; index < renderers.Length; index++)
            bounds.Encapsulate(renderers[index].bounds);

        if (padding > 0f)
            bounds.Expand(new Vector3(padding * 2f, 0f, padding * 2f));

        bounds.center = new Vector3(bounds.center.x, 0f, bounds.center.z);
        bounds.size = new Vector3(bounds.size.x, 1f, bounds.size.z);

        return bounds;
    }
}
