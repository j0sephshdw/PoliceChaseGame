using System.Collections.Generic;
using UnityEngine;

public class PickupSpawner : MonoBehaviour
{
    [Header("Pickup Prefabları")]
    public GameObject[] pickupPrefabs; // Kalkan, Hızlanma, Can prefabları buraya sürüklenecek

    [Range(0, 100)]
    public int pickupSpawnChance = 20; // Her spawn noktasında pickup çıkma ihtimali (%)

    [Header("Spawn Noktaları")]
    [SerializeField] private Transform pickupSpawnPointsParent; // Inspector'dan PickupSpawnPoints objesini buraya sürükle

    // "static" olduğu için TÜM PickupSpawner örnekleri (yani her tile) aynı havuzu paylaşıyor
    private static Dictionary<GameObject, Queue<GameObject>> pool = new Dictionary<GameObject, Queue<GameObject>>();
    private static Transform poolHolder;

    private void Start()
    {
        SpawnPickups();
    }

    private void SpawnPickups()
    {
        if (pickupSpawnPointsParent == null)
            return;

        foreach (Transform spawnPoint in pickupSpawnPointsParent)
        {
            if (Random.Range(0, 100) >= pickupSpawnChance)
                continue;

            GameObject prefab = pickupPrefabs[Random.Range(0, pickupPrefabs.Length)];
            GameObject pickup = GetFromPool(prefab);

            Vector3 spawnPosition = new Vector3(spawnPoint.position.x, prefab.transform.position.y, spawnPoint.position.z);
            pickup.transform.SetPositionAndRotation(spawnPosition, prefab.transform.rotation);
            pickup.SetActive(true);

            PickupItem item = pickup.GetComponent<PickupItem>();
            if (item != null) item.SourcePrefab = prefab; // toplanınca hangi havuza döneceğini bilsin diye işaretliyoruz
        }
    }

    // Havuzdan boşta bekleyen bir obje alır, yoksa yenisini üretir
    private static GameObject GetFromPool(GameObject prefab)
    {
        if (poolHolder == null)
        {
            poolHolder = new GameObject("PickupPool").transform; // tüm havuzun toplandığı, hiç silinmeyen kutu
        }

        if (!pool.ContainsKey(prefab))
        {
            pool[prefab] = new Queue<GameObject>();
        }

        Queue<GameObject> queue = pool[prefab];

        if (queue.Count > 0)
        {
            return queue.Dequeue(); // havuzda bekleyen varsa onu geri ver
        }

        return Instantiate(prefab, poolHolder); // havuzda yoksa ilk defa üret
    }

    // PickupItem toplandığında çağırır: yok etmek yerine havuza geri koyar
    public static void ReturnToPool(GameObject prefab, GameObject instance)
    {
        instance.SetActive(false);
        instance.transform.SetParent(poolHolder);

        if (!pool.ContainsKey(prefab))
        {
            pool[prefab] = new Queue<GameObject>();
        }

        pool[prefab].Enqueue(instance);
    }
}