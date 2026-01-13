using Mirror;
using UnityEngine;

public class ItemSpawnPooler : MonoBehaviour
{
    public static ItemSpawnPooler singleton;
    Pool<GameObject> goldPool;
    void Start()
    {
        if (singleton != null && singleton != this)
        {
            Destroy(this);
            return;
        }
        singleton = this;

        DontDestroyOnLoad(gameObject);

        NetworkClient.RegisterPrefab(AetherdaleData.GetAetherdaleData().goldCoinsItem.GetLootEquivalentGameObject(), SpawnHandler, UnspawnHandler);

        InitializePools();
    }


    void InitializePools()
    {
        goldPool = new Pool<GameObject>(CreateNewGold, 200);
    }

    GameObject CreateNewGold()
    {
        GameObject next = Instantiate(AetherdaleData.GetAetherdaleData().goldCoinsItem.GetLootEquivalentGameObject(), transform);

        next.SetActive(false);

        return next;
    }

    GameObject SpawnHandler(SpawnMessage msg) => GetGold(msg.position, msg.rotation);
    void UnspawnHandler(GameObject spawned) => Return(spawned);

    public GameObject GetGold(Vector3 position, Quaternion rotation)
    {
        GameObject next = goldPool.Get();
        
        next.GetComponent<Pickup>().spawnTime = Time.time;
        next.transform.SetPositionAndRotation(position, rotation);
        next.SetActive(true);

        return next;
    }

    public void Return(GameObject spawned)
    {
        spawned.SetActive(false);
        goldPool.Return(spawned);
    }
}
