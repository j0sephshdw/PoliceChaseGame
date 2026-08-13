using System.Collections.Generic;
using UnityEngine;

public class HolidayWorldGenerator : MonoBehaviour
{
    [Header("References")]
    public GameObject holidayTilePrefab;
    public Transform player;

    [SerializeField] private Transform tilesParent;

    [Header("Settings")]
    // Unity Plane varsayılan olarak 10x10'dur.
    public int tileSize = 10;

    // Oyuncunun etrafında kaç tile aktif olacak?
    // 4 ise toplam 9x9 = 81 tile oluşturur.
    public int renderSize = 4;

    // Oyuncudan bu mesafeden uzak olan tile'lar silinir.
    public int unloadDistance = 6;

    private Dictionary<Vector2Int, GameObject> spawnedTiles = new();

    private void Start()
    {
        UpdateWorld();
    }

    private void Update()
    {
        UpdateWorld();
    }

    private void UpdateWorld()
    {
        // Player yoksa hiçbir şey yapma
        if (player == null)
            return;

        // Player'ın hangi tile üzerinde olduğunu bul
        int playerTileX = Mathf.FloorToInt(player.position.x / tileSize);
        int playerTileZ = Mathf.FloorToInt(player.position.z / tileSize);

        // Gerekli tile'ları oluştur
        for (int x = playerTileX - renderSize; x <= playerTileX + renderSize; x++)
        {
            for (int z = playerTileZ - renderSize; z <= playerTileZ + renderSize; z++)
            {
                Vector2Int coord = new Vector2Int(x, z);

                // Bu koordinatta tile yoksa oluştur
                if (!spawnedTiles.ContainsKey(coord))
                {
                    Vector3 position = new Vector3(
                        x * tileSize,
                        0f,
                        z * tileSize
                    );

                    GameObject tile = Instantiate(
                        holidayTilePrefab,
                        position,
                        Quaternion.identity,
                        tilesParent
                    );

                    spawnedTiles.Add(coord, tile);
                }
            }
        }

        // Uzak tile'ları belirle
        List<Vector2Int> tilesToRemove = new();

        foreach (var tile in spawnedTiles)
        {
            int distanceX = Mathf.Abs(tile.Key.x - playerTileX);
            int distanceZ = Mathf.Abs(tile.Key.y - playerTileZ);

            if (distanceX > unloadDistance || distanceZ > unloadDistance)
            {
                if (tile.Value != null)
                {
                    Destroy(tile.Value);
                }

                tilesToRemove.Add(tile.Key);
            }
        }

        // Dictionary'den sil
        foreach (Vector2Int coord in tilesToRemove)
        {
            spawnedTiles.Remove(coord);
        }
    }
}