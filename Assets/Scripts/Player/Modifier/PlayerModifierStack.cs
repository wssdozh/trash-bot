using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PlayerModifierStack : MonoBehaviour
{
    [SerializeField] private List<PlayerModifier> _modifiers = new List<PlayerModifier>();

    public event Action Changed;

    public IReadOnlyList<PlayerModifier> Modifiers
    {
        get { return _modifiers; }
    }

    public void Add(PlayerModifier modifier)
    {
        if (modifier == null)
        {
            throw new ArgumentNullException(nameof(modifier));
        }

        _modifiers.Add(modifier);

        if (Changed != null)
        {
            Changed.Invoke();
        }
    }

    public void ClearAll()
    {
        _modifiers.Clear();

        if (Changed != null)
        {
            Changed.Invoke();
        }
    }
}
