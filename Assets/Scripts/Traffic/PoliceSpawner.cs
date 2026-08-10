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
    public float spawnInterval = 6f;
    public int maxPoliceCount = 3;
    public float spawnDistanceBehind = 35f;

    // - ÖNDEN DOĞMA MESAFESİ ---
    public float spawnDistanceAhead = 60f;

    [Header("Zorluk (Dinamik Polis Limiti)")]
    public bool dinamikZorlukAktif = true;
    public int baslangicPolisSayisi = 2;
    public int mutlakMaxPolisLimiti = 30;
    public int kacSkordaBirPolisArtsin = 20;

    [Header("Barikat (Roadblock) Ayarları")]
    public bool enableBarricades = true;
    public int barricadeScoreThreshold = 150;
    public float barricadeInterval = 25f;
    public float barricadeDistanceAhead = 80f;

    public static PoliceSpawner Instance;

    private Transform player;
    private List<GameObject> activePoliceCars = new List<GameObject>();

    private static Dictionary<GameObject, Queue<GameObject>> pool = new Dictionary<GameObject, Queue<GameObject>>();
    private static Transform poolHolder;

    private void Awake()
    {
        Instance = this;
    }

    public int GetActivePoliceCount()
    {
        int count = 0;
        foreach (var p in activePoliceCars)
        {
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

        while (true)
        {
            if (player == null) yield break;

            activePoliceCars.RemoveAll(p => p == null || !p.activeInHierarchy);

            int currentLimit = maxPoliceCount;
            if (dinamikZorlukAktif)
            {
                int currentScore = (ScoreManager.Instance != null) ? ScoreManager.Instance.Score : 0;
                currentLimit = Mathf.Clamp(baslangicPolisSayisi + (currentScore / kacSkordaBirPolisArtsin), baslangicPolisSayisi, mutlakMaxPolisLimiti);
            }

            if (activePoliceCars.Count < currentLimit)
            {
                SpawnPolice();
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    IEnumerator BarricadeRoutine()
    {
        while (player == null) yield return null;

        while (true)
        {
            yield return new WaitForSeconds(barricadeInterval);

            if (!enableBarricades || player == null) continue;

            int currentScore = (ScoreManager.Instance != null) ? ScoreManager.Instance.Score : 0;
            if (currentScore >= barricadeScoreThreshold)
            {
                SpawnBarricade();
            }
        }
    }

    private void SpawnPolice()
    {
        GameObject selectedPrefab = SecilecekPolisAraci();
        if (selectedPrefab == null) return;

        float sideSign = (activePoliceCars.Count % 2 == 0) ? 1f : -1f;

        Vector3 spawnPos;
        Quaternion spawnRot;

        // %35 İhtimalle tam önden kafa kafaya gelen polis doğar
        if (Random.value < 0.35f)
        {
            // Önden gelirken tam üstüne sürmesi için yan offseti minimuma çekiyoruz (0 - 1.2m)
            float headOnOffset = sideSign * Random.Range(0f, 1.2f);
            spawnPos = player.position + (player.forward * spawnDistanceAhead) + (player.right * headOnOffset);
            spawnRot = Quaternion.LookRotation(-player.forward);
        }
        else
        {
            // Klasik arkadan takip doğması
            float offsetRight = sideSign * Random.Range(1.5f, 3.5f);
            spawnPos = player.position - (player.forward * spawnDistanceBehind) + (player.right * offsetRight);
            spawnRot = Quaternion.LookRotation(player.forward);
        }

        spawnPos.y = 0.5f;

        GameObject newPolice = GetFromPool(selectedPrefab);
        newPolice.transform.SetPositionAndRotation(spawnPos, spawnRot);
        newPolice.SetActive(true);

        PoliceCarAI ai = newPolice.GetComponent<PoliceCarAI>();
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
        }

        activePoliceCars.Add(newPolice);
    }


    private void SpawnBarricade()
    {
        Vector3 pDir = player.forward;
        bool yolX_Ekseninde = Mathf.Abs(pDir.x) > Mathf.Abs(pDir.z);

        Vector3 cardinalForward;
        if (yolX_Ekseninde)
            cardinalForward = new Vector3(Mathf.Sign(pDir.x), 0, 0);
        else
            cardinalForward = new Vector3(0, 0, Mathf.Sign(pDir.z));

        Vector3 spawnCenter = player.position + (cardinalForward * barricadeDistanceAhead);
        spawnCenter.y = 0.5f;

        Vector3 rayOrigin = spawnCenter + (Vector3.up * 10f);
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 20f))
        {
            if (!hit.collider.gameObject.name.Contains("Road")) return;

            if (yolX_Ekseninde)
                spawnCenter.z = hit.transform.position.z;
            else
                spawnCenter.x = hit.transform.position.x;
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
            Collider[] colliders = Physics.OverlapSphere(pos, 1.5f);
            foreach (Collider col in colliders)
            {
                if (col.CompareTag("Traffic") || col.CompareTag("Player") || col.CompareTag("Police"))
                {
                    alanDolu = true;
                    break;
                }
            }
            if (alanDolu) continue;

            GameObject barricadeCar = GetFromPool(secilenBarikatAraci);

            PoliceCarAI ai = barricadeCar.GetComponent<PoliceCarAI>();
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

            StartCoroutine(ReturnToPoolAfterDelay(secilenBarikatAraci, barricadeCar, 15f));
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

        return Instantiate(prefab, poolHolder);
    }

    public static void ReturnToPool(GameObject prefab, GameObject instance)
    {
        if (instance == null) return;
        instance.SetActive(false);
        instance.transform.SetParent(poolHolder);

        if (!pool.ContainsKey(prefab)) pool[prefab] = new Queue<GameObject>();
        pool[prefab].Enqueue(instance);
    }

    private IEnumerator ReturnToPoolAfterDelay(GameObject prefab, GameObject instance, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (instance != null)
        {
            ReturnToPool(prefab, instance);
        }
    }

    private void OnDestroy()
    {
        pool.Clear();
        poolHolder = null;
    }
}