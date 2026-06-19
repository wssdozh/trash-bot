using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Objectives/Objective Profile", fileName = "ObjectiveProfile")]
public sealed class ObjectiveProfile : ScriptableObject
{
    [SerializeField] private RoomType _roomType;
    [SerializeField] private string _title;
    [SerializeField] private ObjectiveStepDefinition[] _steps;

    public RoomType RoomType => _roomType;
    public string Title => _title;
    public IReadOnlyList<ObjectiveStepDefinition> Steps => _steps;
}
