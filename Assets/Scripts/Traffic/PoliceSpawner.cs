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

    [Header("Skor Barajları (Araç Tipleri İçin)")]
    public int suvSkorSiniri = 100;
    public int muscleSkorSiniri = 200;
    public int sportsSkorSiniri = 300;

    [Header("Temel Spawn Ayarları")]
    [Tooltip("Polis dalgaları (wave) arasındaki bekleme süresi")]
    public float baseSpawnInterval = 0.5f;
    [Tooltip("Oyun başında sahnede bulunacak minimum polis sayısı")]
    public int baslangicPolisSayisi = 12;

    public float spawnDistanceBehind = 1f;
    public float spawnDistanceAhead = 2f;

    [Tooltip("Polislerin önden (kafa kafaya) gelme ihtimali. 0.5 = %50")]
    [Range(0f, 1f)]
    public float headOnSpawnChance = 0.35f;

    [Header("Gelişmiş Spawn Kontrolü")]
    [Tooltip("Sadece adında 'Road' geçen zeminlerde doğmalarını zorunlu kılar (Tavsiye edilir)")]
    public bool sadeceYoldaDogsun = true;

    [Header("Agresif Zorluk Sistemi (Heat Level)")]
    public bool dinamikZorlukAktif = true;
    public int kacSkordaBirSeviyeArtsin = 5;
    public int seviyeBasinaPolisArtisi = 4;
    public int mutlakMaxPolisLimiti = 80;

    [Header("Barikat (Roadblock) Ayarları")]
    public bool enableBarricades = true;
    public int barricadeScoreThreshold = 15;
    public float barricadeInterval = 20f;
    public float barricadeDistanceAhead = 40f;
    [Tooltip("Barikat aralığının inebileceği en düşük değer")]
    public float minBarricadeInterval = 8f;
    [Tooltip("Her 1 skor puanının barikat aralığını kaç saniye kısaltacağı")]
    public float barricadeIntervalScale = 0.01f;

    public static PoliceSpawner Instance;

    private Transform player;
    private List<GameObject> activePoliceCars = new List<GameObject>();

    // GC Optimizasyonu: Sürekli dizi oluşturmamak için önbelleğe alınmış değişkenler
    private Collider[] overlapResults = new Collider[20];
    private WaitForSeconds spawnDelay = new WaitForSeconds(0.05f);

    // Object Pooling Sistemi
    private static Dictionary<GameObject, Queue<GameObject>> pool = new Dictionary<GameObject, Queue<GameObject>>();
    private static Dictionary<GameObject, GameObject> instancePrefabMap = new Dictionary<GameObject, GameObject>();
    private static Transform poolHolder;

    // Tamamen rastgele yerine merkezden dışa doğru (mantıklı) arama noktaları
    private readonly Vector2[] aramaDesenleri = new Vector2[]
    {
        new Vector2(0, 0), new Vector2(4, 0), new Vector2(-4, 0),
        new Vector2(0, 5), new Vector2(8, 0), new Vector2(-8, 0),
        new Vector2(5, 5), new Vector2(-5, 5), new Vector2(0, -5),
        new Vector2(5, -5), new Vector2(-5, -5), new Vector2(12, 0),
        new Vector2(-12, 0), new Vector2(8, 8), new Vector2(-8, 8)
    };

    private void Awake()
    {
        Instance = this;
    }

    public int GetActivePoliceCount()
    {
        int count = 0;
        for (int i = 0; i < activePoliceCars.Count; i++)
        {
            GameObject p = activePoliceCars[i];
            if (p != null && p.activeInHierarchy) count++;
        }
        return count;
    }

    void Start()
    {
        if (poolHolder == null) poolHolder = new GameObject("PolicePool").transform;

        StartCoroutine(SpawnRoutine());
        StartCoroutine(BarricadeRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
            yield return null;
        }

        float timer = 0f;

        while (true)
        {
            if (player == null) yield break;

            // Ters for ile inaktif polisleri listeden temizle
            for (int i = activePoliceCars.Count - 1; i >= 0; i--)
            {
                GameObject p = activePoliceCars[i];
                if (p == null)
                {
                    activePoliceCars.RemoveAt(i);
                }
                else if (!p.activeInHierarchy)
                {
                    ReturnToPool(p);
                    activePoliceCars.RemoveAt(i);
                }
            }

            int currentLimit = baslangicPolisSayisi;
            float currentInterval = baseSpawnInterval;

            if (dinamikZorlukAktif && ScoreManager.Instance != null)
            {
                int currentScore = ScoreManager.Instance.Score;
                int heatLevel = currentScore / kacSkordaBirSeviyeArtsin;

                int calculatedLimit = baslangicPolisSayisi + (heatLevel * seviyeBasinaPolisArtisi);
                currentLimit = Mathf.Clamp(calculatedLimit, baslangicPolisSayisi, mutlakMaxPolisLimiti);
                currentInterval = Mathf.Max(1.0f, baseSpawnInterval - (heatLevel * 0.35f));
            }

            int eksikPolisSayisi = currentLimit - activePoliceCars.Count;

            if (eksikPolisSayisi > 0)
            {
                int maxDeneme = eksikPolisSayisi * 3;
                int basariliSpawn = 0;
                int deneme = 0;

                while (basariliSpawn < eksikPolisSayisi && deneme < maxDeneme)
                {
                    if (SpawnPolice())
                    {
                        basariliSpawn++;
                        yield return spawnDelay;
                    }
                    else
                    {
                        yield return null;
                    }
                    deneme++;
                }
            }

            // GC (Garbage Collector) sızıntısını önlemek için while döngüsü ile bekleme
            timer = 0f;
            while (timer < currentInterval)
            {
                timer += Time.deltaTime;
                yield return null;
            }
        }
    }

    IEnumerator BarricadeRoutine()
    {
        while (player == null) yield return null;

        float timer = 0f;

        while (true)
        {
            timer += Time.deltaTime;

            int currentScore = (ScoreManager.Instance != null) ? ScoreManager.Instance.Score : 0;

            // Skor arttıkça barikatlar sıklaşıyor. Polis sayısı tavana ulaştıktan sonra
            // baskıyı artırmaya devam eden iki sistemden biri bu.
            float currentInterval = Mathf.Max(minBarricadeInterval, barricadeInterval - (currentScore * barricadeIntervalScale));

            if (timer >= currentInterval)
            {
                timer = 0f;

                if (enableBarricades && player != null && currentScore >= barricadeScoreThreshold)
                {
                    SpawnBarricade();
                }
            }
            yield return null;
        }
    }

    private bool SpawnPolice()
    {
        GameObject selectedPrefab = SecilecekPolisAraci();
        if (selectedPrefab == null) return false;

        bool isHeadOn = Random.value < headOnSpawnChance;

        // Merkez referans mesafesi
        float baseDistance = isHeadOn
            ? Random.Range(spawnDistanceAhead - 5f, spawnDistanceAhead + 15f)
            : Random.Range(spawnDistanceBehind - 5f, spawnDistanceBehind + 15f);

        baseDistance = Mathf.Max(baseDistance, 10f); // Inspector'daki değer ne olursa olsun negatif/çok küçük mesafeyi engelle

        Vector3 forwardDir = isHeadOn ? -player.forward : player.forward;
        Vector3 centerSpawnPos = player.position + (player.forward * (isHeadOn ? baseDistance : -baseDistance));
        Quaternion spawnRot = Quaternion.LookRotation(forwardDir);

        // Karışık ama rastgele olmayan 15 nokta dener (Merkezden dışa doğru)
        for (int i = 0; i < aramaDesenleri.Length; i++)
        {
            Vector3 rawPos = centerSpawnPos + (player.right * aramaDesenleri[i].x) + (player.forward * aramaDesenleri[i].y);
            Vector3 rayOrigin = new Vector3(rawPos.x, player.position.y + 30f, rawPos.z);
            Vector3 finalSpawnPos = rawPos;

            // 1. KONTROL: Lazer (Raycast)
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 60f))
            {
                if (hit.collider.CompareTag("Obstacle") || hit.collider.CompareTag("Traffic") || hit.collider.CompareTag("Police"))
                {
                    continue; // Engel veya araç gördü, pas geç.
                }

                if (sadeceYoldaDogsun && !hit.collider.gameObject.name.Contains("Road"))
                {
                    continue; // Eğer yolda doğsun dediysek ve objenin adında Road yoksa pas geç.
                }

                finalSpawnPos = hit.point;
                finalSpawnPos.y += 0.5f;
            }
            else
            {
                continue; // Lazer boşluğa düştü.
            }

            // 2. KONTROL: Etraf Boş Mu?
            bool alanDolu = false;
            int hitCount = Physics.OverlapSphereNonAlloc(finalSpawnPos, 2.5f, overlapResults);

            for (int j = 0; j < hitCount; j++)
            {
                Collider col = overlapResults[j];
                if (col.CompareTag("Obstacle") || col.CompareTag("Police") || col.CompareTag("Traffic") || col.CompareTag("Player"))
                {
                    alanDolu = true;
                    break;
                }
            }

            if (alanDolu) continue;

            // --- MUTLU SON ---
            GameObject newPolice = GetFromPool(selectedPrefab);
            newPolice.transform.SetPositionAndRotation(finalSpawnPos, spawnRot);
            newPolice.SetActive(true);

            PoliceCarAI ai = newPolice.GetComponent<PoliceCarAI>();

            if (ai != null) ai.poolGeneration++;

            if (ai != null)
            {
                ai.enabled = true;
                ai.SetTarget(player);
            }

            Rigidbody rb = newPolice.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.mass = ai != null ? ai.collisionMass : 400f;
                rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            activePoliceCars.Add(newPolice);

            return true;
        }

        return false;
    }

    private void SpawnBarricade()
    {
        Vector3 pDir = player.forward;
        bool yolX_Ekseninde = Mathf.Abs(pDir.x) > Mathf.Abs(pDir.z);

        Vector3 cardinalForward = yolX_Ekseninde
            ? new Vector3(Mathf.Sign(pDir.x), 0, 0)
            : new Vector3(0, 0, Mathf.Sign(pDir.z));

        Vector3 spawnCenter = player.position + (cardinalForward * barricadeDistanceAhead);
        spawnCenter.y = 0.5f;

        Vector3 rayOrigin = spawnCenter + (Vector3.up * 10f);
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 20f))
        {
            if (!hit.collider.gameObject.name.Contains("Road")) return;

            if (yolX_Ekseninde) spawnCenter.z = hit.transform.position.z;
            else spawnCenter.x = hit.transform.position.x;
        }
        else return;

        Quaternion fixedRotation = Quaternion.Euler(0f, 0f, 0f);
        float arabaGenisligi = 0.35f;

        int arabaSayisi = Random.Range(2, 5);
        int toplamSlot = arabaSayisi + 1;
        int emptySlot = Random.Range(0, toplamSlot);

        GameObject secilenBarikatAraci = SecilecekPolisAraci();

        for (int i = 0; i < toplamSlot; i++)
        {
            if (i == emptySlot) continue;

            Vector3 pos = spawnCenter;
            float offsetZ = (i - (toplamSlot - 1) / 2f) * arabaGenisligi;
            pos.z += offsetZ;

            bool alanDolu = false;
            int hitCount = Physics.OverlapSphereNonAlloc(pos, 0.15f, overlapResults);

            for (int j = 0; j < hitCount; j++)
            {
                Collider col = overlapResults[j];
                if (col.CompareTag("Traffic") || col.CompareTag("Player") || col.CompareTag("Police"))
                {
                    alanDolu = true;
                    break;
                }
            }
            if (alanDolu) continue;

            GameObject barricadeCar = GetFromPool(secilenBarikatAraci);
            PoliceCarAI ai = barricadeCar.GetComponent<PoliceCarAI>();
            
            if (ai != null) ai.poolGeneration++;

            if (ai != null) ai.enabled = false;

            Rigidbody rb = barricadeCar.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.position = pos;
                rb.rotation = fixedRotation;
                rb.mass = 5000f;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            barricadeCar.transform.SetPositionAndRotation(pos, fixedRotation);
            barricadeCar.SetActive(true);

            StartCoroutine(ReturnToPoolAfterDelay(secilenBarikatAraci, barricadeCar, 15f, ai, ai != null ? ai.poolGeneration : 0));
        }
    }

    private GameObject SecilecekPolisAraci()
    {
        if (ScoreManager.Instance == null) return sedanPrefab;

        int anlikSkor = ScoreManager.Instance.Score;

        if (anlikSkor >= sportsSkorSiniri) return sportsPrefab;
        if (anlikSkor >= muscleSkorSiniri) return musclePrefab;
        if (anlikSkor >= suvSkorSiniri) return suvPrefab;

        return sedanPrefab;
    }

    private static GameObject GetFromPool(GameObject prefab)
    {
        if (!pool.ContainsKey(prefab)) pool[prefab] = new Queue<GameObject>();

        Queue<GameObject> queue = pool[prefab];
        while (queue.Count > 0)
        {
            GameObject obj = queue.Dequeue();
            if (obj != null) return obj;
        }

        GameObject newInstance = Instantiate(prefab, poolHolder);
        instancePrefabMap[newInstance] = prefab;
        return newInstance;
    }

    public static void ReturnToPool(GameObject instance)
    {
        if (instance == null) return;

        if (instancePrefabMap.TryGetValue(instance, out GameObject prefab))
        {
            ReturnToPool(prefab, instance);
        }
        else
        {
            instance.SetActive(false);
            Destroy(instance);
        }
    }

    public static void ReturnToPool(GameObject prefab, GameObject instance)
    {
        if (instance == null) return;
        instance.SetActive(false);
        instance.transform.SetParent(poolHolder);

        if (!pool.ContainsKey(prefab)) pool[prefab] = new Queue<GameObject>();
        pool[prefab].Enqueue(instance);

        if (!instancePrefabMap.ContainsKey(instance))
        {
            instancePrefabMap[instance] = prefab;
        }
    }

    private IEnumerator ReturnToPoolAfterDelay(GameObject prefab, GameObject instance, float delay, PoliceCarAI ai, int expectedGeneration)
    {
        float timer = 0f;
        while (timer < delay)
        {
            timer += Time.deltaTime;
            yield return null;
        }
        // Araç bu süre içinde başka bir amaçla tekrar kullanıldıysa (nesil değiştiyse) havuza geri döndürme
        if (instance != null && (ai == null || ai.poolGeneration == expectedGeneration))
        {
            ReturnToPool(prefab, instance);
        }
    }

    private void OnDestroy()
    {
        pool.Clear();
        instancePrefabMap.Clear();
        poolHolder = null;
    }
}