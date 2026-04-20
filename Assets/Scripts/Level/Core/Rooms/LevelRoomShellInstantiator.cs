using System.Collections.Generic;
using UnityEngine;

internal sealed class LevelRoomShellInstantiator
{
    public bool InstantiateRoomsShellOnly(
        LevelGenerationContext generationContext,
        System.Random random,
        Transform roomsRoot,
        LevelRoomPrefabLibrary roomPrefabLibrary,
        LevelSequenceProfile levelSequenceProfile,
        int maximumRoomRegenerateAttempts
    )
    {
        for (int index = 0; index < generationContext.Nodes.Count; index++)
        {
            if (InstantiateRoomShell(
                generationContext,
                index,
                random,
                roomsRoot,
                roomPrefabLibrary,
                levelSequenceProfile,
                maximumRoomRegenerateAttempts
            ) == false)
            {
                return false;
            }
        }

        return true;
    }

    public bool InstantiateRoomShell(
        LevelGenerationContext generationContext,
        int nodeIndex,
        System.Random random,
        Transform roomsRoot,
        LevelRoomPrefabLibrary roomPrefabLibrary,
        LevelSequenceProfile levelSequenceProfile,
        int maximumRoomRegenerateAttempts
    )
    {
        LevelNode node = generationContext.Nodes[nodeIndex];
        RoomGenerator prefab = roomPrefabLibrary.Pick(node.RoomType, random);
        RoomGenerator instance = UnityEngine.Object.Instantiate(prefab, roomsRoot);
        RoomTypeProfile roomTypeProfile = GetProfileOverride(node, levelSequenceProfile);

        bool needsEntrance = node.Parent != null;
        bool needsExit = node.Children.Count > 0;

        int requiredDoors = 0;

        if (needsEntrance == true)
        {
            requiredDoors += 1;
        }

        if (needsExit == true)
        {
            requiredDoors += 1;
        }

        requiredDoors += Mathf.Max(0, node.Children.Count - 1);
        requiredDoors = Mathf.Clamp(requiredDoors, 0, 4);

        node.RoomInstance = instance;

        if (roomTypeProfile != null)
        {
            instance.SetRuntimeProfile(roomTypeProfile);
        }
        else
        {
            instance.ClearRuntimeProfile();
        }

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

        return ok;
    }

    private static RoomTypeProfile GetProfileOverride(LevelNode node, LevelSequenceProfile levelSequenceProfile)
    {
        if (levelSequenceProfile == null)
        {
            return null;
        }

        if (node.RoomType != RoomType.Combat)
        {
            return null;
        }

        if (GetCombatIndex(node) != 1)
        {
            return null;
        }

        return levelSequenceProfile.FirstCombatProfile;
    }

    private static int GetCombatIndex(LevelNode node)
    {
        int combatIndex = 0;
        LevelNode currentNode = node;

        while (currentNode != null)
        {
            if (currentNode.RoomType == RoomType.Combat)
            {
                combatIndex += 1;
            }

            currentNode = currentNode.Parent;
        }

        return combatIndex;
    }

    private static bool ValidateMarkers(LevelNode node, bool needsEntrance, bool needsExit, int sideExits)
    {
        if (needsEntrance == true && node.EntranceMarker == null)
        {
            return false;
        }

        if (needsExit == true && node.ExitMarker == null)
        {
            return false;
        }

        if (sideExits > 0 && node.SideExitMarkers.Count < sideExits)
        {
            return false;
        }

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
            {
                continue;
            }

            if (marker.Role == DoorRole.Entrance)
            {
                node.EntranceMarker = marker;
            }

            if (marker.Role == DoorRole.Exit)
            {
                node.ExitMarker = marker;
            }

            if (marker.Role == DoorRole.SideExit)
            {
                node.SideExitMarkers.Add(marker);
            }
        }
    }
}
