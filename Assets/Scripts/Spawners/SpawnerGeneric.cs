using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public abstract class Spawner<T> : Spawner where T : MonoBehaviour
{
    [Header("РќРµРѕР±С…РѕРґРёРјС‹Рµ РєРѕРјРїРѕРЅРµРЅС‚С‹: ")]
    [SerializeField] protected T Prefab;

    [Header("РќР°СЃС‚СЂРѕР№РєРё РїСѓР»Р°: ")]
    [SerializeField] protected int PoolSize = 5;

    protected List<T> ActiveObjects = new List<T>();
    protected ObjectPool<T> Pool;

    public int CountActiveObjects { get; private set; }

    protected virtual void Awake()
    {
        Debug.Log("Р·Р°СЂРµРіР°РЅ: " + Prefab.name);

        SpawnerServiceLocator.Register(Prefab.name, this);

        CountActiveObjects = 0;
        InitializePool();
    }

    private void OnDestroy()
    {
        SpawnerServiceLocator.Unregister<T>(Prefab.name);
    }

    public abstract T Spawn(Vector3 position);
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
