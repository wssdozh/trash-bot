using System.Collections.Generic;
using UnityEngine;

internal readonly struct LevelTreasureRatio
{
    public readonly int Numerator;
    public readonly int Denominator;

    public LevelTreasureRatio(int numerator, int denominator)
    {
        Numerator = numerator;
        Denominator = denominator;
    }
}

internal readonly struct LevelPlacementSettings
{
    public readonly bool RandomizeRootRotation;

    public readonly bool SnapRoomsToGrid;
    public readonly float RoomGridStepInBlocks;

    public readonly int RoomSpacingPaddingInBlocks;
    public readonly bool DisallowCorridorIntersections;

    public readonly int CorridorMinimumLengthInBlocks;
    public readonly int CorridorMaximumLengthInBlocks;
    public readonly int CorridorLengthJitterInBlocks;
    public readonly int CorridorWidthInBlocks;
    public readonly int CorridorCollisionExtraWidthInBlocks;
    public readonly int CorridorIgnoreEndsInBlocks;

    public LevelPlacementSettings(
        bool randomizeRootRotation,
        bool snapRoomsToGrid,
        float roomGridStepInBlocks,
        int roomSpacingPaddingInBlocks,
        bool disallowCorridorIntersections,
        int corridorMinimumLengthInBlocks,
        int corridorMaximumLengthInBlocks,
        int corridorLengthJitterInBlocks,
        int corridorWidthInBlocks,
        int corridorCollisionExtraWidthInBlocks,
        int corridorIgnoreEndsInBlocks
    )
    {
        RandomizeRootRotation = randomizeRootRotation;

        SnapRoomsToGrid = snapRoomsToGrid;
        RoomGridStepInBlocks = roomGridStepInBlocks;

        RoomSpacingPaddingInBlocks = roomSpacingPaddingInBlocks;
        DisallowCorridorIntersections = disallowCorridorIntersections;

        CorridorMinimumLengthInBlocks = corridorMinimumLengthInBlocks;
        CorridorMaximumLengthInBlocks = corridorMaximumLengthInBlocks;
        CorridorLengthJitterInBlocks = corridorLengthJitterInBlocks;
        CorridorWidthInBlocks = corridorWidthInBlocks;
        CorridorCollisionExtraWidthInBlocks = corridorCollisionExtraWidthInBlocks;
        CorridorIgnoreEndsInBlocks = corridorIgnoreEndsInBlocks;
    }
}

internal sealed class LevelNode
{
    public int NodeId;
    public RoomType RoomType;
    public int Depth;
    public LevelNode Parent;

    public readonly List<LevelNode> Children = new List<LevelNode>(3);

    public RoomGenerator RoomInstance;

    public RoomDoorMarker EntranceMarker;
    public RoomDoorMarker ExitMarker;

    public readonly List<RoomDoorMarker> SideExitMarkers = new List<RoomDoorMarker>(3);
    public readonly List<RoomDoorMarker> UsedOutgoingMarkers = new List<RoomDoorMarker>(3);
}

internal sealed class PlacedRoomInfo
{
    public LevelNode Node;
    public Bounds PaddedBounds;
    public Bounds SolidBounds;
}

internal struct LevelEdge
{
    public LevelNode Parent;
    public LevelNode Child;
    public RoomDoorMarker FromDoor;
    public RoomDoorMarker ToDoor;
    public int CorridorWidthInBlocks;
}

internal readonly struct LevelRoomStreamLink
{
    public readonly RoomRuntimeState FirstRoom;
    public readonly RoomRuntimeState SecondRoom;

    public LevelRoomStreamLink(RoomRuntimeState firstRoom, RoomRuntimeState secondRoom)
    {
        FirstRoom = firstRoom;
        SecondRoom = secondRoom;
    }
}
