using System;
using UnityEngine;
using UnityEngine.Pool;

public abstract class Spawner<T> : Spawner where T : MonoBehaviour
{
    [Header("Required Components:")]
    [SerializeField] protected T Prefab;

    [Header("Pool Settings:")]
    [SerializeField] protected int PoolSize = 5;

    protected ObjectPool<T> Pool;

    public int CountActiveObjects { get; private set; }

    protected virtual void Awake()
    {
        if (Prefab == null)
        {
            throw new InvalidOperationException(nameof(Prefab));
        }

        CountActiveObjects = 0;
        InitializePool();
        SpawnerServiceLocator.Register(Prefab.name, this);
    }

    private void OnDestroy()
    {
        SpawnerServiceLocator.Unregister<T>(Prefab.name);
    }

    public abstract T Spawn(Vector3 position);

    public virtual T Spawn(Vector3 position, Transform parent)
    {
        T instance = Spawn(position);
        instance.transform.SetParent(parent, true);

        return instance;
    }

    public abstract void Despawn(T instance);

    private void InitializePool()
    {
        Pool = new ObjectPool<T>(
            createFunc: CreateInstance,
            actionOnGet: OnGet,
            actionOnRelease: OnRelease,
            actionOnDestroy: OnDestroyObject,
            collectionCheck: true,
            defaultCapacity: PoolSize,
            maxSize: PoolSize
        );
    }

    private T CreateInstance()
    {
        T instance = Instantiate(Prefab);
        instance.gameObject.SetActive(false);

        return instance;
    }

    private void OnGet(T prefab)
    {
        CountActiveObjects++;
        ActionOnGet(prefab);
    }

    private void OnRelease(T prefab)
    {
        CountActiveObjects--;
        ActionOnRelease(prefab);
    }

    private void OnDestroyObject(T prefab)
    {
        Destroy(prefab.gameObject);
    }

    protected virtual void ActionOnGet(T prefab)
    {
        prefab.gameObject.SetActive(true);
    }

    protected virtual void ActionOnRelease(T prefab)
    {
        prefab.gameObject.SetActive(false);
    }
}
