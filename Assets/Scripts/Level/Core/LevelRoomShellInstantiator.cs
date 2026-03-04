using System.Collections.Generic;
using UnityEngine;

internal sealed class LevelRoomShellInstantiator
{
    public bool InstantiateRoomsShellOnly(LevelGenerationContext generationContext, System.Random random, Transform roomsRoot, LevelRoomPrefabLibrary roomPrefabLibrary, int maximumRoomRegenerateAttempts)
    {
        for (int index = 0; index < generationContext.Nodes.Count; index++)
        {

            LevelNode node = generationContext.Nodes[index];

            RoomGenerator prefab = roomPrefabLibrary.Pick(node.RoomType, random);
            RoomGenerator instance = UnityEngine.Object.Instantiate(prefab, roomsRoot);

            bool needsEntrance = node.Parent != null;
            bool needsExit = node.Children.Count > 0;

            int requiredDoors = 0;

            if (needsEntrance == true)
                requiredDoors++;

            if (needsExit == true)
                requiredDoors++;

            requiredDoors += Mathf.Max(0, node.Children.Count - 1);
            requiredDoors = Mathf.Clamp(requiredDoors, 0, 4);

            node.RoomInstance = instance;

            bool ok = false;

            for (int regen = 0; regen <= maximumRoomRegenerateAttempts; regen++)
            {

                instance.SetRuntimeSeed(LevelGeneratorUtility.NextAnyInt(random));
                instance.SetDoorRolesEnabled(needsEntrance, needsExit);
                instance.SetRuntimeDoorCountRange(requiredDoors, requiredDoors);
                instance.GenerateShellOnly();

                CacheDoorMarkers(node);

                int requiredSideExits = Mathf.Max(0, node.Children.Count - 1);

                if (ValidateMarkers(node, needsEntrance, needsExit, requiredSideExits) == true)
                {
                    ok = true;
                    break;
                }

                instance.Clear();

            }

            if (ok == false)
                return false;

        }

        return true;
    }

    private static bool ValidateMarkers(LevelNode node, bool needsEntrance, bool needsExit, int sideExits)
    {
        if (needsEntrance == true && node.EntranceMarker == null)
            return false;

        if (needsExit == true && node.ExitMarker == null)
            return false;

        if (sideExits > 0 && node.SideExitMarkers.Count < sideExits)
            return false;

        return true;
    }

    private void CacheDoorMarkers(LevelNode node)
    {
        node.EntranceMarker = null;
        node.ExitMarker = null;

        node.SideExitMarkers.Clear();
        node.UsedOutgoingMarkers.Clear();

        IReadOnlyList<RoomDoorMarker> markers = node.RoomInstance.DoorMarkers;

        for (int i = 0; i < markers.Count; i++)
        {

            RoomDoorMarker marker = markers[i];

            if (marker == null)
                continue;

            if (marker.Role == DoorRole.Entrance)
                node.EntranceMarker = marker;

            if (marker.Role == DoorRole.Exit)
                node.ExitMarker = marker;

            if (marker.Role == DoorRole.SideExit)
                node.SideExitMarkers.Add(marker);

        }
    }
}
