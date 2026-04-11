using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class SpawnerServiceLocator
{
    private static readonly Dictionary<Type, Dictionary<string, object>> _sourcesByType = new Dictionary<Type, Dictionary<string, object>>();

    public static event Action<Type, string> Registered;
    public static event Action<Type, string> Unregistered;

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

        Type sourceType = typeof(T);
        Dictionary<string, object> sources = GetOrCreateSources(sourceType);
        sources[key] = spawner;

        Action<Type, string> registeredAction = Registered;

        if (registeredAction != null)
        {
            registeredAction.Invoke(sourceType, key);
        }
    }

    public static Spawner<T> Get<T>(string key) where T : MonoBehaviour
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentNullException(nameof(key));
        }

        Type sourceType = typeof(T);

        if (_sourcesByType.TryGetValue(sourceType, out Dictionary<string, object> sources) == false)
        {
            throw new InvalidOperationException(nameof(key));
        }

        if (sources.TryGetValue(key, out object spawnerObject) == false)
        {
            throw new InvalidOperationException(nameof(key));
        }

        if (spawnerObject is Spawner<T> spawner == false)
        {
            throw new InvalidCastException(nameof(spawnerObject));
        }

        return spawner;
    }

    public static Spawner<T> Find<T>(string key) where T : MonoBehaviour
    {
        if (string.IsNullOrEmpty(key))
        {
            return null;
        }

        Type sourceType = typeof(T);

        if (_sourcesByType.TryGetValue(sourceType, out Dictionary<string, object> sources) == false)
        {
            return null;
        }

        if (sources.TryGetValue(key, out object spawnerObject) == false)
        {
            return null;
        }

        if (spawnerObject is Spawner<T> spawner == false)
        {
            return null;
        }

        return spawner;
    }

    public static void Unregister<T>(string key) where T : MonoBehaviour
    {
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        Type sourceType = typeof(T);

        if (_sourcesByType.TryGetValue(sourceType, out Dictionary<string, object> sources) == false)
        {
            return;
        }

        bool isRemoved = sources.Remove(key);

        if (isRemoved == false)
        {
            return;
        }

        Action<Type, string> unregisteredAction = Unregistered;

        if (unregisteredAction != null)
        {
            unregisteredAction.Invoke(sourceType, key);
        }

        if (sources.Count == 0)
        {
            _sourcesByType.Remove(sourceType);
        }
    }

    private static Dictionary<string, object> GetOrCreateSources(Type sourceType)
    {
        if (_sourcesByType.TryGetValue(sourceType, out Dictionary<string, object> sources))
        {
            return sources;
        }

        Dictionary<string, object> createdSources = new Dictionary<string, object>();
        _sourcesByType.Add(sourceType, createdSources);

        return createdSources;
    }
}
