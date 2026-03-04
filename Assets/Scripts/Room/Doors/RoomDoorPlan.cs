using System;
using UnityEngine;

[Serializable]
public sealed class RoomDoorPlan
{
    [SerializeField] private DoorSide _side;
    [SerializeField] private DoorRole _role;
    [SerializeField] private int _openingOffset;
    [SerializeField] private int _openingWidthInBlocks;
    [SerializeField] private int _openingHeightInBlocks;

    public DoorSide Side => _side;
    public DoorRole Role => _role;
    public int OpeningOffset => _openingOffset;
    public int OpeningWidthInBlocks => _openingWidthInBlocks;
    public int OpeningHeightInBlocks => _openingHeightInBlocks;

    public RoomDoorPlan(DoorSide side, DoorRole role, int openingOffset, int openingWidthInBlocks, int openingHeightInBlocks)
    {
        _side = side;
        _role = role;
        _openingOffset = openingOffset;
        _openingWidthInBlocks = openingWidthInBlocks;
        _openingHeightInBlocks = openingHeightInBlocks;
    }
}
