using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public abstract class Spawner<T> : MonoBehaviour where T : MonoBehaviour
{
    [Header("Необходимые компоненты: ")]
    [SerializeField] protected T Prefab;

    [Header("Настройки пула: ")]
    [SerializeField] protected int PoolSize = 5;

    protected List<T> ActiveObjects = new();
    protected ObjectPool<T> Pool;

    public int CountActiveObjects { get; private set; }

    protected virtual void Awake()
    {
        CountActiveObjects = 0;

        InitializePool();
    }

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
        ActiveObjects.Add(prefab);
    }

    private void OnRelease(T prefab)
    {
        CountActiveObjects--;
        ActionOnRelease(prefab);
        ActiveObjects.Remove(prefab);
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