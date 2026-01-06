using System.Collections.Generic;
using Unity.Collections;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Pool;

public abstract class Spawner<T> : MonoBehaviour where T : MonoBehaviour
{
    [Header("Необходимые компоненты: ")]
    [SerializeField] protected T Prefab;

    [Header("Настройки пула: ")]
    [SerializeField] protected int PoolSize = 5;
    [SerializeField] protected int WarmingUp = 20;

    [Header("Статистика: ")]
    [SerializeField] private int _activeObjectsCount;
    [SerializeField] private int _inactiveObjectsCount;
    [SerializeField] private int _totalObjectsCount;

    protected List<T> ActiveObjects = new();
    protected ObjectPool<T> Pool;

    public int CountActiveObjects { get; private set; }

    protected virtual void Awake()
    {
        CountActiveObjects = 0;

        InitializePool();
        UpdateStatistics();
    }

    private void Start()
    {
        for (int i = 0; i < WarmingUp; i++)
        {
            CreateInstance();
        }
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

        _totalObjectsCount++;
        UpdateStatistics();

        return instance;
    }

    private void OnGet(T prefab)
    {
        CountActiveObjects++;
        ActionOnGet(prefab);
        ActiveObjects.Add(prefab);
        UpdateStatistics();
    }

    private void OnRelease(T prefab)
    {
        CountActiveObjects--;
        ActionOnRelease(prefab);
        ActiveObjects.Remove(prefab);
        UpdateStatistics();
    }

    private void OnDestroyObject(T prefab)
    {
        _totalObjectsCount--;
        UpdateStatistics();
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

    private void UpdateStatistics()
    {
        _activeObjectsCount = CountActiveObjects;
        _inactiveObjectsCount = _totalObjectsCount - _activeObjectsCount;
    }
}
