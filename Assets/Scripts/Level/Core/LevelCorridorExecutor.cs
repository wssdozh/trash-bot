using UnityEngine;

internal sealed class LevelCorridorExecutor
{
    public bool BuildCorridors(LevelGenerationContext generationContext, Transform corridorsRoot, LevelCorridorBuilder corridorBuilder)
    {
        LevelGeneratorUtility.DestroyChildren(corridorsRoot);

        for (int index = 0; index < generationContext.Edges.Count; index++)
        {

            LevelEdge edge = generationContext.Edges[index];

            if (edge.FromDoor == null || edge.ToDoor == null)
                return false;

            if (LevelCorridorBoundsBuilder.IsStrictlyStraight(edge.FromDoor.transform.position, edge.ToDoor.transform.position) == false)
                return false;

            corridorBuilder.BuildBetweenDoors(corridorsRoot, edge.FromDoor, edge.ToDoor, edge.CorridorWidthInBlocks);

        }

        return true;
    }
}
