using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class WeaponModifierStack : MonoBehaviour
{
    [SerializeField] private List<WeaponModifier> _modifiers = new List<WeaponModifier>();

    public event Action Changed;

    public IReadOnlyList<WeaponModifier> Modifiers
    {
        get { return _modifiers; }
    }

    public void Add(WeaponModifier modifier)
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
