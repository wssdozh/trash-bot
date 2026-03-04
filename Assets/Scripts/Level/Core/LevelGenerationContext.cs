using System.Collections.Generic;
using UnityEngine;

internal sealed class LevelGenerationContext
{
    public readonly List<LevelNode> Nodes;
    public readonly List<LevelEdge> Edges;
    public readonly List<PlacedRoomInfo> PlacedRooms;
    public readonly List<Bounds> PlacedCorridorBounds;

    public LevelGenerationContext(int nodeCapacity, int edgeCapacity, int placedRoomCapacity, int corridorCapacity)
    {
        Nodes = new List<LevelNode>(nodeCapacity);
        Edges = new List<LevelEdge>(edgeCapacity);
        PlacedRooms = new List<PlacedRoomInfo>(placedRoomCapacity);
        PlacedCorridorBounds = new List<Bounds>(corridorCapacity);
    }

    public void ClearData()
    {
        Nodes.Clear();
        Edges.Clear();
        PlacedRooms.Clear();
        PlacedCorridorBounds.Clear();
    }
}
