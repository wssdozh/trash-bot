using UnityEngine;

internal static class LevelDoorAlignmentUtility
{
    public static void RotateRoomToMatchEntrance(RoomGenerator room, RoomDoorMarker entrance, Vector3 desiredOut)
    {
        int localIndex = SideIndex(entrance.Side);
        int targetIndex = DirectionIndex(desiredOut);

        int steps = targetIndex - localIndex;

        while (steps < 0)
            steps += 4;

        while (steps >= 4)
            steps -= 4;

        room.transform.rotation = Quaternion.Euler(0f, steps * 90f, 0f) * room.transform.rotation;
    }

    public static Vector3 GetWorldSideDirection(Transform roomTransform, DoorSide side)
    {
        Vector3 local = Vector3.left;

        if (side == DoorSide.North)
            local = Vector3.forward;

        if (side == DoorSide.East)
            local = Vector3.right;

        if (side == DoorSide.South)
            local = Vector3.back;

        Vector3 world = roomTransform.rotation * local;
        world.y = 0f;

        if (world.sqrMagnitude > 0.0001f)
            world.Normalize();

        return world;
    }

    private static int SideIndex(DoorSide side)
    {
        if (side == DoorSide.North)
            return 0;

        if (side == DoorSide.East)
            return 1;

        if (side == DoorSide.South)
            return 2;

        return 3;
    }

    private static int DirectionIndex(Vector3 worldDirection)
    {
        Vector3 direction = worldDirection;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.0001f)
            direction.Normalize();

        float dotNorth = Vector3.Dot(direction, Vector3.forward);
        float dotEast = Vector3.Dot(direction, Vector3.right);
        float dotSouth = Vector3.Dot(direction, Vector3.back);
        float dotWest = Vector3.Dot(direction, Vector3.left);

        float best = dotNorth;
        int bestIndex = 0;

        if (dotEast > best)
        {
            best = dotEast;
            bestIndex = 1;
        }

        if (dotSouth > best)
        {
            best = dotSouth;
            bestIndex = 2;
        }

        if (dotWest > best)
        {
            bestIndex = 3;
        }

        return bestIndex;
    }
}
