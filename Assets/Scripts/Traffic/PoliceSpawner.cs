using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoliceSpawner : MonoBehaviour
{
    [Header("Polis Araçları (Prefabs)")]
    public GameObject sedanPrefab;
    public GameObject suvPrefab;
    public GameObject musclePrefab;
    public GameObject sportsPrefab;

    [Header("Skor Barajları")]
    public int suvSkorSiniri = 100;
    public int muscleSkorSiniri = 300;
    public int sportsSkorSiniri = 600;

    [Header("Spawn Ayarları")]
    public float spawnInterval = 6f; // Kaç saniyede bir polis eklenecek?
    public int maxPoliceCount = 3;   // Aynı anda sahnede en fazla kaç polis kovalayabilir?
    public float spawnDistanceBehind = 35f; // Oyuncunun ne kadar gerisinde doğacaklar?

    private Transform player;
    private List<GameObject> activePoliceCars = new List<GameObject>();

    void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        // 1. Oyuncuyu bulana kadar bekle (Oyun başlar başlamaz hata vermesini önler)
        while (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            yield return null;
        }

        // 2. Sonsuz Aksiyon Döngüsü
        while (true)
        {
            // Eğer oyuncu öldüyse/silindiyse döngüyü durdur (MissingReference hatasını çözer!)
            if (player == null) yield break;

            // Listeyi temizle (Patlayan, duvara çarpıp yok olan polisleri listeden çıkar)
            activePoliceCars.RemoveAll(p => p == null);

            // Eğer sahnede maksimum polis sayısına henüz ulaşılmadıysa yeni polis üret
            if (activePoliceCars.Count < maxPoliceCount)
            {
                SpawnPolice();
            }

            // Belirlenen süre kadar bekle ve tekrar kontrol et
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnPolice()
    {
        // 3. Skora göre doğru aracı seç
        GameObject selectedPrefab = SecilecekPolisAraci();
        if (selectedPrefab == null) return;

        // 1. Çapraz Kovalama: Biri sağ şeride (+), diğeri sol şeride (-) kaydırılır
        float sideSign = (activePoliceCars.Count % 2 == 0) ? 1f : -1f;
        float offsetRight = sideSign * Random.Range(2.0f, 3.5f);

        // 4. Doğma noktasını ayarla (Oyuncunun arkasında ve sağ/sol şeride kaymış olarak)
        Vector3 spawnPos = player.position - (player.forward * spawnDistanceBehind) + (player.right * offsetRight);
        spawnPos.y = 0.5f;

        Quaternion spawnRot = Quaternion.LookRotation(player.forward);

        // 5. Polisi sahneye ekle
        GameObject newPolice = Instantiate(selectedPrefab, spawnPos, spawnRot);

        // 6. Berat'ın AI koduna hedefin "Player" olduğunu söyle ve takip mesafelerini farklılaştır
        PoliceCarAI ai = newPolice.GetComponent<PoliceCarAI>();
        if (ai != null)
        {
            ai.SetTarget(player);

            // Takip ve frenleme mesafelerini çeşitleyerek üst üste binmelerini önle
            ai.followBufferDistance = Random.Range(0.6f, 2.5f);
            ai.minSafeDistance = Random.Range(0.4f, 1.2f);
        }

        // Polisi takip listesine ekle
        activePoliceCars.Add(newPolice);
    }

    private GameObject SecilecekPolisAraci()
    {
        // Bedirhan'ın yazdığı ScoreManager Singleton olduğu için her yerden direkt erişebiliyoruz
        if (ScoreManager.Instance == null) return sedanPrefab;

        int anlikSkor = ScoreManager.Instance.Score;

        if (anlikSkor >= sportsSkorSiniri) return sportsPrefab;
        if (anlikSkor >= muscleSkorSiniri) return musclePrefab;
        if (anlikSkor >= suvSkorSiniri) return suvPrefab;

        return sedanPrefab; // Skor düşükse standart Sedan
    }
}