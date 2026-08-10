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

    // UI için Singleton bağlantısı
    public static PoliceSpawner Instance;

    private Transform player;
    private List<GameObject> activePoliceCars = new List<GameObject>();

    // OBJECT POOLING DEĞİŞKENLERİ
    private static Dictionary<GameObject, Queue<GameObject>> pool = new Dictionary<GameObject, Queue<GameObject>>();
    private static Transform poolHolder;
    private void Awake()
    {
        Instance = this;
    }

    // UI'ın polis sayısını çekeceği fonksiyon
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

            // Geride kalıp kendini kapatan (veya patlayan) polisleri listeden temizle ki yer açılsın
            activePoliceCars.RemoveAll(p => p == null || !p.activeInHierarchy);

            int currentLimit = maxPoliceCount;
            if (dinamikZorlukAktif)
            {
                int currentScore = (ScoreManager.Instance != null) ? ScoreManager.Instance.Score : 0;
                currentLimit = Mathf.Clamp(baslangicPolisSayisi + (currentScore / kacSkordaBirPolisArtsin), baslangicPolisSayisi, mutlakMaxPolisLimiti);
            }

            // ---  PEŞ PEŞE SPAWN ---
            // Limitte boşluk varsa 6 saniye beklemek yerine 0.5 saniyede bir polisleri art arda spawnla!
            if (activePoliceCars.Count < currentLimit)
            {
                SpawnPolice();
                yield return new WaitForSeconds(0.5f);
                continue; // Döngüyü başa sar ki limit dolana kadar peş peşe çağırsın
            }

            // Limit doluysa normal aralığı (6 saniye) bekle
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
        float offsetRight = sideSign * Random.Range(2.0f, 3.5f);

        Vector3 spawnPos = player.position - (player.forward * spawnDistanceBehind) + (player.right * offsetRight);
        spawnPos.y = 0.5f;

        Quaternion spawnRot = Quaternion.LookRotation(player.forward);

        GameObject newPolice = GetFromPool(selectedPrefab);
        newPolice.transform.SetPositionAndRotation(spawnPos, spawnRot);
        newPolice.SetActive(true);

        PoliceCarAI ai = newPolice.GetComponent<PoliceCarAI>();
        if (ai != null)
        {
            ai.enabled = true;
            ai.SetTarget(player);
            ai.followBufferDistance = Random.Range(0.6f, 2.5f);
            ai.minSafeDistance = Random.Range(0.4f, 1.2f);
        }

        Rigidbody rb = newPolice.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.mass = ai != null ? ai.collisionMass : 400f;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;//Havuzdan çıkan polislerin virajlarda devrilmemesi için takla kilitlerini geri ver
        }

        activePoliceCars.Add(newPolice);
    }

    // ==========================================
    //  BARİKAT FONKSİYONU
    // ==========================================
    private void SpawnBarricade()
    {
        Vector3 pDir = player.forward;

        // Yolun hangi eksende uzandığını buluyoruz (X mi, Z mi?)
        bool yolX_Ekseninde = Mathf.Abs(pDir.x) > Mathf.Abs(pDir.z);

        // İleriye doğru barikat merkez noktasını belirliyoruz
        Vector3 cardinalForward;
        if (yolX_Ekseninde)
            cardinalForward = new Vector3(Mathf.Sign(pDir.x), 0, 0);
        else
            cardinalForward = new Vector3(0, 0, Mathf.Sign(pDir.z));

        Vector3 spawnCenter = player.position + (cardinalForward * barricadeDistanceAhead);
        spawnCenter.y = 0.5f;

        // Sensör ile yolu bul ve tam merkeze hizala
        Vector3 rayOrigin = spawnCenter + (Vector3.up * 10f);
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 20f))
        {
            if (!hit.collider.gameObject.name.Contains("Road")) return;

            // Hangi eksende ilerliyorsak, diğer ekseni yolun merkezine kilitliyoruz
            if (yolX_Ekseninde)
                spawnCenter.z = hit.transform.position.z;
            else
                spawnCenter.x = hit.transform.position.x;
        }
        else return;

        // KESİN ROTASYON (İstediğin gibi sadece 0, 0, 0)
        Quaternion fixedRotation = Quaternion.Euler(0f, 0f, 0f);

        // KRİTİK DÜZELTME: Yolun toplam genişliği (Z ekseninde) sadece 1.31 birim!
        // 0.35f değeri, araçların yola tampon tampona sığmasını sağlayacaktır.
        float arabaGenisligi = 0.35f;

        // --- 2 İLE 4 ARAÇ ARASINDA DEĞİŞEN SİSTEM ---
        int arabaSayisi = Random.Range(2, 5); // 2, 3 veya 4 araba seçer
        int toplamSlot = arabaSayisi + 1;     // 1 tane de kaçış boşluğu ekliyoruz
        int emptySlot = Random.Range(0, toplamSlot);

        GameObject secilenBarikatAraci = SecilecekPolisAraci();

        for (int i = 0; i < toplamSlot; i++)
        {
            if (i == emptySlot) continue;

            Vector3 pos = spawnCenter;
            float offsetZ = (i - (toplamSlot - 1) / 2f) * arabaGenisligi;
            pos.z += offsetZ;

            // 3. DÜZELTME: O noktada başka bir araba (sivil veya polis) var mı kontrol et!
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
            // Eğer orada zaten bir araba varsa, o barikat slotunu boş bırak ve diğerine geç
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

        Debug.Log("<color=green>🚨 BARİKAT YOLA SIĞACAK ÖLÇEKTE KURULDU! 🚨</color>");
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

    // ==========================================
    // OBJECT POOLING FONKSİYONLARI
    // ==========================================
    private static GameObject GetFromPool(GameObject prefab)
    {
        if (!pool.ContainsKey(prefab)) pool[prefab] = new Queue<GameObject>();

        Queue<GameObject> queue = pool[prefab];

        // GÜVENLİK: Silinmiş (Missing) objeleri atla!
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
        if (!pool.ContainsKey(prefab)) pool[prefab] = new Queue<GameObject>();
        pool[prefab].Enqueue(instance);
    }

    private IEnumerator ReturnToPoolAfterDelay(GameObject prefab, GameObject instance, float delay)
    {
        yield return new WaitForSeconds(delay);
        // GÜVENLİK: Obje o 15 saniye içinde başka bir sebeple silindiyse havuza atmaya çalışma
        if (instance != null)
        {
            ReturnToPool(prefab, instance);
        }
    }
    private void OnDestroy()//sahne yuklenmeden statk havuzu temızledm
    {
        pool.Clear();
        poolHolder = null;
    }
}