using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FootstepSet", menuName = "Audio/FootstepSet")]
public class FootstepSet : ScriptableObject
{
    [SerializeField] private List<AudioClip> _clips;
    [SerializeField] private float _pitchMin = 0.9f;
    [SerializeField] private float _pitchMax = 1.1f;

    public IReadOnlyList<AudioClip> Clips
    {
        get { return _clips; }
    }

    public float PitchMin
    {
        get { return _pitchMin; }
    }

    public float PitchMax
    {
        get { return _pitchMax; }
    }
}
