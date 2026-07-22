using UnityEngine;

public class TreeSpawner : MonoBehaviour
{
    [Header("Ağaç Spawn Noktalarının Parent Objesi")]
    public Transform treeSpawnPointParent;

    [Header("Ağaç Prefabları")]
    public GameObject[] treePrefabs;

    [Header("Ağaç Oluşturma Olasılığı")]
    [Range(0, 100)]
    public int spawnChance = 100;

    [Header("Oluşacak Ağaçların Ölçeği")]
    public Vector3 treeScale = Vector3.one;

    [ContextMenu("Generate Trees")]
    public void GenerateTrees()
    {
        ClearTrees();

        if (treeSpawnPointParent == null)
        {
            Debug.LogError("Tree Spawn Point Parent atanmadı!");
            return;
        }

        if (treePrefabs == null || treePrefabs.Length == 0)
        {
            Debug.LogError("Tree Prefabları atanmadı!");
            return;
        }

        foreach (Transform spawnPoint in treeSpawnPointParent)
        {
            if (Random.Range(0, 100) >= spawnChance)
                continue;

            int randomIndex = Random.Range(0, treePrefabs.Length);

            GameObject tree = Instantiate(
                treePrefabs[randomIndex],
                spawnPoint.position,
                spawnPoint.rotation,
                transform
            );

            tree.transform.localScale = treeScale;
        }

        Debug.Log("Ağaçlar başarıyla oluşturuldu.");
    }

    [ContextMenu("Clear Trees")]
    public void ClearTrees()
    {
        while (transform.childCount > 0)
        {
#if UNITY_EDITOR
            DestroyImmediate(transform.GetChild(0).gameObject);
#else
            Destroy(transform.GetChild(0).gameObject);
#endif
        }

        Debug.Log("Ağaçlar temizlendi.");
    }
}