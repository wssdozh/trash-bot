using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Levels/Level Generation Profile", fileName = "LevelGenerationProfile")]
public sealed class LevelGenerationProfile : ScriptableObject
{
    [Header("Main Path")]
    [SerializeField] private List<RoomType> _mainPathRoomTypes = new List<RoomType>()
    {
        RoomType.Start,
        RoomType.Combat,
        RoomType.Combat,
        RoomType.Boss
    };

    [Header("Branches")]
    [SerializeField] private Vector2Int _branchCountRange = new Vector2Int(0, 2);
    [SerializeField] private Vector2Int _branchLengthRange = new Vector2Int(1, 2);

    [SerializeField] private RoomType _branchRoomType = RoomType.Combat;
    [SerializeField] private RoomType _branchTerminalRoomType = RoomType.Treasure;

    [SerializeField] private bool _allowBranchesFromStart = true;
    [SerializeField] private bool _allowBranchesFromBoss = false;

    public IReadOnlyList<RoomType> MainPathRoomTypes => _mainPathRoomTypes;

    public Vector2Int BranchCountRange => _branchCountRange;
    public Vector2Int BranchLengthRange => _branchLengthRange;

    public RoomType BranchRoomType => _branchRoomType;
    public RoomType BranchTerminalRoomType => _branchTerminalRoomType;

    public bool AllowBranchesFromStart => _allowBranchesFromStart;
    public bool AllowBranchesFromBoss => _allowBranchesFromBoss;
}
