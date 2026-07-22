using UnityEngine;

public class CarSpawner : MonoBehaviour
{
    [Header("Araba Spawn Noktalarının Parent Objesi")]
    public Transform carSpawnPointParent;

    [Header("Araba Prefabları")]
    public GameObject[] carPrefabs;

    [Header("Araba Oluşturma Olasılığı")]
    [Range(0, 100)]
    public int spawnChance = 100;

    [Header("Oluşacak Arabaların Ölçeği")]
    public Vector3 carScale = new Vector3(0.1f, 0.1f, 0.1f);

    [ContextMenu("Generate Cars")]
    public void GenerateCars()
    {
        ClearCars();

        if (carSpawnPointParent == null)
        {
            Debug.LogError("Car Spawn Point Parent atanmadı!");
            return;
        }

        if (carPrefabs == null || carPrefabs.Length == 0)
        {
            Debug.LogError("Car Prefabları atanmadı!");
            return;
        }

        foreach (Transform spawnPoint in carSpawnPointParent)
        {
            if (Random.Range(0, 100) >= spawnChance)
                continue;

            int randomIndex = Random.Range(0, carPrefabs.Length);

            GameObject car = Instantiate(
                carPrefabs[randomIndex],
                spawnPoint.position,
                spawnPoint.rotation,
                transform
            );

            car.transform.localScale = carScale;
        }

        Debug.Log("Arabalar başarıyla oluşturuldu.");
    }

    [ContextMenu("Clear Cars")]
    public void ClearCars()
    {
        while (transform.childCount > 0)
        {
#if UNITY_EDITOR
            DestroyImmediate(transform.GetChild(0).gameObject);
#else
            Destroy(transform.GetChild(0).gameObject);
#endif
        }

        Debug.Log("Arabalar temizlendi.");
    }
}