using UnityEngine;
using System.Collections;

public class PoliceSpawner : MonoBehaviour
{
    public GameObject policePrefab;
    public Transform spawnPoint;

    private static bool spawned = false;


    void Start()
    {
        StartCoroutine(WaitForPlayer());
    }


    IEnumerator WaitForPlayer()
    {
        // Player gelene kadar bekle
        GameObject player = null;

        while (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            yield return null;
        }


        // Bir kere spawnla
        if (!spawned)
        {
            Instantiate(
                policePrefab,
                spawnPoint.position,
                spawnPoint.rotation
            );

            spawned = true;

            Debug.Log("Polis spawnlandı");
        }
    }
}