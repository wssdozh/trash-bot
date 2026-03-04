internal sealed class LevelRoomFinalizer
{
    public void FinalizeInteriors(LevelGenerationContext generationContext)
    {
        for (int index = 0; index < generationContext.PlacedRooms.Count; index++)
        {

            LevelNode node = generationContext.PlacedRooms[index].Node;

            if (node != null && node.RoomInstance != null)
                node.RoomInstance.GenerateInteriorFromShell();

        }
    }
}
