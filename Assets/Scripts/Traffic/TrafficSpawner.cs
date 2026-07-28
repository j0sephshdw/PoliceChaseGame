using System.Collections;
using UnityEngine;

public class TrafficSpawner : MonoBehaviour
{
    [Header("Spawn Noktaları")]
    public Transform spawnPointsParent;

    [Header("Araç Prefabları")]
    public GameObject[] trafficCars;

    [Header("Spawn Ayarları")]
    public float spawnInterval = 3f;

    private void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            SpawnCar();

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnCar()
    {
        if (trafficCars.Length == 0)
            return;

        if (spawnPointsParent.childCount == 0)
            return;

        Transform spawnPoint =
            spawnPointsParent.GetChild(Random.Range(0, spawnPointsParent.childCount));

        GameObject randomCar =
            trafficCars[Random.Range(0, trafficCars.Length)];

        Instantiate(
            randomCar,
            spawnPoint.position,
            spawnPoint.rotation);
    }
}