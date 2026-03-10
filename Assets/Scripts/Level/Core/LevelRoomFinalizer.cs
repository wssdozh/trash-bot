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
            roomRuntimeState.Setup(placedRoomInfo.SolidBounds, enemyBorderGap);
            node.RoomInstance.GenerateInteriorFromShell();
        }
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
