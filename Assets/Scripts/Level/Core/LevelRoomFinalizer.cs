internal sealed class LevelRoomFinalizer
{
    public void FinalizeInteriors(LevelGenerationContext generationContext, float enemyBorderGap)
    {
        for (int index = 0; index < generationContext.PlacedRooms.Count; index++)
        {
            PlacedRoomInfo placedRoomInfo = generationContext.PlacedRooms[index];

            if (placedRoomInfo == null)
            {
                continue;
            }

            LevelNode node = placedRoomInfo.Node;

            if (node == null)
            {
                continue;
            }

            if (node.RoomInstance == null)
            {
                continue;
            }

            RoomRuntimeState roomRuntimeState = GetRoomRuntimeState(node.RoomInstance);
            int combatRoomIndex = GetCombatRoomIndex(node);
            node.RoomInstance.SetRuntimeCombatRoomIndex(combatRoomIndex);
            roomRuntimeState.Setup(placedRoomInfo.SolidBounds, enemyBorderGap);
            node.RoomInstance.GenerateInteriorFromShell();
        }
    }

    private int GetCombatRoomIndex(LevelNode node)
    {
        if (node.RoomType != RoomType.Combat)
        {
            return 0;
        }

        int combatRoomIndex = 0;
        LevelNode currentNode = node;

        while (currentNode != null)
        {
            if (currentNode.RoomType == RoomType.Combat)
            {
                combatRoomIndex += 1;
            }

            currentNode = currentNode.Parent;
        }

        return combatRoomIndex;
    }

    private RoomRuntimeState GetRoomRuntimeState(RoomGenerator roomGenerator)
    {
        RoomRuntimeState roomRuntimeState = roomGenerator.GetComponent<RoomRuntimeState>();

        if (roomRuntimeState != null)
        {
            return roomRuntimeState;
        }

        return roomGenerator.gameObject.AddComponent<RoomRuntimeState>();
    }
}
