using System;
using System.Collections.Generic;
using UnityEngine;

public static class SpawnerServiceLocator
{
    private static readonly Dictionary<string, object> _sources = new Dictionary<string, object>();

    public static void Register<T>(string key, Spawner<T> spawner) where T : MonoBehaviour
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (spawner == null)
        {
            throw new ArgumentNullException(nameof(spawner));
        }

        _sources[key] = spawner;
    }

    public static Spawner<T> Get<T>(string key) where T : MonoBehaviour
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (_sources.ContainsKey(key) == false)
        {
            throw new InvalidOperationException(nameof(key));
        }

        object spawnerObject = _sources[key];

        if (spawnerObject is Spawner<T> spawner == false)
        {
            throw new InvalidCastException(nameof(spawnerObject));
        }

        return (Spawner<T>)spawnerObject;
    }

    public static void Unregister<T>(string key) where T : MonoBehaviour
    {
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        _sources.Remove(key);
    }
}