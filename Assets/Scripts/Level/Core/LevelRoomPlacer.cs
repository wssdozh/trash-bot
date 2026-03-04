using System.Collections.Generic;
using UnityEngine;

internal sealed class LevelRoomPlacer
{
    public bool PlaceAllRooms(LevelGenerationContext generationContext, LevelNode root, System.Random random, LevelCorridorBuilder corridorBuilder, LevelPlacementSettings placementSettings)
    {
        generationContext.Edges.Clear();
        generationContext.PlacedRooms.Clear();
        generationContext.PlacedCorridorBounds.Clear();

        root.RoomInstance.transform.position = Vector3.zero;
        root.RoomInstance.transform.rotation = Quaternion.identity;

        if (placementSettings.RandomizeRootRotation == true)
            root.RoomInstance.transform.rotation = Quaternion.Euler(0f, random.Next(0, 4) * 90f, 0f);

        SnapRoomPositionToGrid(root.RoomInstance, corridorBuilder, placementSettings);

        RegisterPlacedRoom(generationContext, root, corridorBuilder, placementSettings);

        return PlaceSubtree(generationContext, root, random, corridorBuilder, placementSettings);
    }

    private bool PlaceSubtree(LevelGenerationContext generationContext, LevelNode parentNode, System.Random random, LevelCorridorBuilder corridorBuilder, LevelPlacementSettings placementSettings)
    {
        for (int childIndex = 0; childIndex < parentNode.Children.Count; childIndex++)
        {

            LevelNode childNode = parentNode.Children[childIndex];
            bool isFirstChild = childIndex == 0;

            if (TryPlaceConnection(generationContext, parentNode, childNode, isFirstChild, random, corridorBuilder, placementSettings) == false)
                return false;

            if (PlaceSubtree(generationContext, childNode, random, corridorBuilder, placementSettings) == false)
                return false;

        }

        return true;
    }

    private bool TryPlaceConnection(LevelGenerationContext generationContext, LevelNode parentNode, LevelNode childNode, bool isFirstChild, System.Random random, LevelCorridorBuilder corridorBuilder, LevelPlacementSettings placementSettings)
    {
        RoomDoorMarker childEntrance = childNode.EntranceMarker;

        if (childEntrance == null)
            return false;

        List<RoomDoorMarker> outgoing = BuildOutgoingCandidates(parentNode, isFirstChild, random);

        for (int o = 0; o < outgoing.Count; o++)
        {

            RoomDoorMarker parentDoor = outgoing[o];

            Vector3 direction = LevelDoorAlignmentUtility.GetWorldSideDirection(parentNode.RoomInstance.transform, parentDoor.Side);

            LevelDoorAlignmentUtility.RotateRoomToMatchEntrance(childNode.RoomInstance, childEntrance, -direction);

            int width = Mathf.Max(placementSettings.CorridorWidthInBlocks, parentDoor.WidthInBlocks, childEntrance.WidthInBlocks);

            List<int> lengths = BuildLengthCandidates(random, placementSettings);

            for (int i = 0; i < lengths.Count; i++)
            {

                int lenBlocks = lengths[i];
                float lenUnits = lenBlocks * corridorBuilder.BlockSize;

                Vector3 fromDoor = parentDoor.transform.position;
                Vector3 targetDoor = fromDoor + (direction * lenUnits);

                Vector3 offset = childEntrance.transform.position - childNode.RoomInstance.transform.position;
                Vector3 unsnappedPosition = targetDoor - offset;

                int placementVariantCount = 1;

                if (placementSettings.SnapRoomsToGrid == true)
                    placementVariantCount = 2;

                for (int placementVariant = 0; placementVariant < placementVariantCount; placementVariant++)
                {

                    bool useSnap = placementSettings.SnapRoomsToGrid == true && placementVariant == 0;

                    childNode.RoomInstance.transform.position = unsnappedPosition;

                    if (useSnap == true)
                        SnapRoomPositionToGrid(childNode.RoomInstance, corridorBuilder, placementSettings);

                    Vector3 toDoor = childEntrance.transform.position;

                    if (useSnap == true && LevelCorridorBoundsBuilder.IsStrictlyStraight(fromDoor, toDoor) == false)
                        continue;

                    Bounds padded = LevelRoomBoundsCalculator.CalculateRoomBounds(childNode.RoomInstance, placementSettings.RoomSpacingPaddingInBlocks * corridorBuilder.BlockSize);

                    if (IntersectsAnyRoom(generationContext, padded) == true)
                        continue;

                    Bounds corridorBounds;

                    if (LevelCorridorBoundsBuilder.TryBuildCorridorCollisionBounds(fromDoor, toDoor, width, corridorBuilder, placementSettings, out corridorBounds) == false)
                        continue;

                    if (IntersectsRoomsWithCorridor(generationContext, corridorBounds, parentNode, childNode) == true)
                        continue;

                    if (placementSettings.DisallowCorridorIntersections == true && IntersectsAnyCorridor(generationContext, corridorBounds) == true)
                        continue;

                    parentNode.UsedOutgoingMarkers.Add(parentDoor);

                    RegisterPlacedRoom(generationContext, childNode, corridorBuilder, placementSettings);
                    generationContext.PlacedCorridorBounds.Add(corridorBounds);

                    LevelEdge edge = new LevelEdge();
                    edge.Parent = parentNode;
                    edge.Child = childNode;
                    edge.FromDoor = parentDoor;
                    edge.ToDoor = childEntrance;
                    edge.CorridorWidthInBlocks = width;

                    generationContext.Edges.Add(edge);

                    return true;

                }

            }

        }

        return false;
    }

    private void SnapRoomPositionToGrid(RoomGenerator room, LevelCorridorBuilder corridorBuilder, LevelPlacementSettings placementSettings)
    {
        if (placementSettings.SnapRoomsToGrid == false || room == null)
            return;

        float stepInUnits = GetRoomGridStepInUnits(corridorBuilder, placementSettings);

        if (stepInUnits <= 0.0001f)
            return;

        Transform roomTransform = room.transform;
        Vector3 position = roomTransform.position;

        position.x = SnapValueToGrid(position.x, stepInUnits);
        position.z = SnapValueToGrid(position.z, stepInUnits);

        roomTransform.position = position;
    }

    private float GetRoomGridStepInUnits(LevelCorridorBuilder corridorBuilder, LevelPlacementSettings placementSettings)
    {
        float stepInBlocks = Mathf.Max(0.01f, placementSettings.RoomGridStepInBlocks);

        return stepInBlocks * corridorBuilder.BlockSize;
    }

    private static float SnapValueToGrid(float value, float step)
    {
        return Mathf.Round(value / step) * step;
    }

    private List<RoomDoorMarker> BuildOutgoingCandidates(LevelNode parentNode, bool isFirstChild, System.Random random)
    {
        List<RoomDoorMarker> result = new List<RoomDoorMarker>(4);

        if (isFirstChild == true)
        {

            if (parentNode.ExitMarker != null && IsUsed(parentNode, parentNode.ExitMarker) == false)
                result.Add(parentNode.ExitMarker);

        }

        List<RoomDoorMarker> side = new List<RoomDoorMarker>();

        for (int index = 0; index < parentNode.SideExitMarkers.Count; index++)
        {

            RoomDoorMarker marker = parentNode.SideExitMarkers[index];

            if (IsUsed(parentNode, marker) == false)
                side.Add(marker);

        }

        LevelGeneratorUtility.Shuffle(side, random);
        result.AddRange(side);

        if (isFirstChild == false)
        {

            if (parentNode.ExitMarker != null && IsUsed(parentNode, parentNode.ExitMarker) == false)
                result.Add(parentNode.ExitMarker);

        }

        return result;
    }

    private static bool IsUsed(LevelNode node, RoomDoorMarker marker)
    {
        for (int i = 0; i < node.UsedOutgoingMarkers.Count; i++)
        {

            if (node.UsedOutgoingMarkers[i] == marker)
                return true;

        }

        return false;
    }

    private List<int> BuildLengthCandidates(System.Random random, LevelPlacementSettings placementSettings)
    {
        int min = Mathf.Max(0, placementSettings.CorridorMinimumLengthInBlocks);
        int max = Mathf.Clamp(Mathf.Max(min, placementSettings.CorridorMaximumLengthInBlocks), min, 512);

        List<int> lengths = new List<int>();

        for (int len = min; len <= max; len++)
        {

            int jitter = 0;

            if (placementSettings.CorridorLengthJitterInBlocks > 0)
                jitter = random.Next(-placementSettings.CorridorLengthJitterInBlocks, placementSettings.CorridorLengthJitterInBlocks + 1);

            lengths.Add(Mathf.Clamp(len + jitter, min, max));

        }

        LevelGeneratorUtility.Shuffle(lengths, random);

        return lengths;
    }

    private void RegisterPlacedRoom(LevelGenerationContext generationContext, LevelNode node, LevelCorridorBuilder corridorBuilder, LevelPlacementSettings placementSettings)
    {
        float padding = placementSettings.RoomSpacingPaddingInBlocks * corridorBuilder.BlockSize;

        PlacedRoomInfo info = new PlacedRoomInfo();
        info.Node = node;
        info.PaddedBounds = LevelRoomBoundsCalculator.CalculateRoomBounds(node.RoomInstance, padding);
        info.SolidBounds = LevelRoomBoundsCalculator.CalculateRoomBounds(node.RoomInstance, 0f);

        generationContext.PlacedRooms.Add(info);
    }

    private bool IntersectsAnyRoom(LevelGenerationContext generationContext, Bounds test)
    {
        for (int i = 0; i < generationContext.PlacedRooms.Count; i++)
        {

            if (test.Intersects(generationContext.PlacedRooms[i].PaddedBounds) == true)
                return true;

        }

        return false;
    }

    private bool IntersectsRoomsWithCorridor(LevelGenerationContext generationContext, Bounds corridor, LevelNode parentNode, LevelNode childNode)
    {
        for (int i = 0; i < generationContext.PlacedRooms.Count; i++)
        {

            PlacedRoomInfo info = generationContext.PlacedRooms[i];

            if (info.Node == parentNode || info.Node == childNode)
                continue;

            if (corridor.Intersects(info.SolidBounds) == true)
                return true;

        }

        return false;
    }

    private bool IntersectsAnyCorridor(LevelGenerationContext generationContext, Bounds test)
    {
        for (int i = 0; i < generationContext.PlacedCorridorBounds.Count; i++)
        {

            if (test.Intersects(generationContext.PlacedCorridorBounds[i]) == true)
                return true;

        }

        return false;
    }
}
