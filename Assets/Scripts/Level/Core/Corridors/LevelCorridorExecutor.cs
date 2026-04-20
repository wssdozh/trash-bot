using UnityEngine;

internal sealed class LevelCorridorExecutor
{
    public bool BuildCorridors(LevelGenerationContext generationContext, Transform corridorsRoot, LevelCorridorBuilder corridorBuilder)
    {
        ClearCorridors(corridorsRoot);

        for (int index = 0; index < generationContext.Edges.Count; index++)
        {
            if (BuildCorridor(generationContext.Edges[index], corridorsRoot, corridorBuilder) == false)
            {
                return false;
            }

        }

        return true;
    }

    public void ClearCorridors(Transform corridorsRoot)
    {
        LevelGeneratorUtility.DestroyChildren(corridorsRoot);
    }

    public bool BuildCorridor(LevelEdge edge, Transform corridorsRoot, LevelCorridorBuilder corridorBuilder)
    {
        if (edge.FromDoor == null || edge.ToDoor == null)
        {
            return false;
        }

        if (LevelCorridorBoundsBuilder.IsStrictlyStraight(edge.FromDoor.transform.position, edge.ToDoor.transform.position) == false)
        {
            return false;
        }

        corridorBuilder.BuildBetweenDoors(corridorsRoot, edge.FromDoor, edge.ToDoor, edge.CorridorWidthInBlocks);

        return true;
    }
}
