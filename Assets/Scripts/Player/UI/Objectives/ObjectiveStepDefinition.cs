using System;
using UnityEngine;

[Serializable]
public sealed class ObjectiveStepDefinition
{
    [SerializeField] private string _text;
    [SerializeField] private string _description;
    [SerializeField] private ObjectiveTrigger _trigger;
    [SerializeField] private string _targetId;

    public string Text => _text;
    public string Description => _description;
    public ObjectiveTrigger Trigger => _trigger;
    public string TargetId => _targetId;
}
