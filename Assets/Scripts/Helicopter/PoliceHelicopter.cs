using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoliceHelicopter : MonoBehaviour
{
    public static PoliceHelicopter Instance;

    [Header("Ses Ayarları")]
    public AudioClip rotorSound;
    private AudioSource audioSource;

    [Header("Pervane Ayarları")]
    public Transform mainRotor;
    public Transform tailRotor;
    public float mainRotorSpeed = 1200f;
    public float tailRotorSpeed = 1500f;

    [Header("Referanslar")]
    public Transform player;
    public string groundTag = "Ground";

    [Header("Havuz (Pool) Referansları")]
    public GameObject spikeStripPrefab;
    public GameObject tireBurstVFXPrefab;
    public int poolSize = 10;

    [Header("Uçuş Dinamikleri")]
    public float distanceAhead = 7f;
    public float heightAbovePlayer = 4.5f; 
    public float followSpeed = 15f;

    [Header("Gerçekçi Helikopter Fiziği (Eğilmeler)")]
    public float maxForwardTilt = 15f;
    public float maxSideTilt = 25f;

    [Header("Salınım (Hover) Ayarları")]
    public float swayAmount = 3.5f;
    public float swaySpeed = 1.5f;

    [Header("Kapan Atma Ayarları")]
    public float dropInterval = 4f;
    public float groundOffset = 0.02f;

    [Header("Kapan Atma Animasyonu (YENİ)")]
    [Tooltip("Kapan atmadan kaç saniye önce hedef hizasına girip alçalmaya başlasın")]
    public float dropPrepTime = 1.5f;
    [Tooltip("Kapan atarken ne kadar metre alçalsın (Dalış yapssın)")]
    public float dipAmount = 2.5f;

    private float dropTimer;

    // Yumuşak geçiş  için dinamik değerler
    private float activeHeight;
    private float activeSway;

    private Queue<GameObject> spikePool = new Queue<GameObject>();
    private Queue<GameObject> vfxPool = new Queue<GameObject>();

    private void Awake()
    {
        Instance = this;

        // --- Ses Kaynağı Ayarları ---
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;          // Pervane sesi sürekli dönecek
        audioSource.spatialBlend = 0f;    // 2D ses (net ve gür)
        audioSource.playOnAwake = false;

        //  Sesi bağlayıp başlatıyoruz
        if (rotorSound != null)
        {
            audioSource.clip = rotorSound;
            audioSource.Play();
        }

        // Havuz (Pool) döngüsü
        for (int i = 0; i < poolSize; i++)
        {
            GameObject spike = Instantiate(spikeStripPrefab, transform);
            spike.SetActive(false);
            spikePool.Enqueue(spike);

            if (tireBurstVFXPrefab != null)
            {
                GameObject vfx = Instantiate(tireBurstVFXPrefab, transform);
                vfx.SetActive(false);
                vfxPool.Enqueue(vfx);
            }
        }
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) player = pObj.transform;
        }
        dropTimer = dropInterval;

        activeHeight = heightAbovePlayer;
        activeSway = swayAmount;
    }

    private void Update()
    {
        if (player == null) return;
        // --- ANA MENÜ AYARLARINA (SFX SLIDER) BAĞLI SES KONTROLÜ ---
        if (audioSource != null)
        {
            // UIManager.GetSFXVolume() sayesinde Ana Menüdeki "SFX" slider'ı sesi direkt kontrol eder!
            float baseVol = UIManager.GetSFXVolume() * 1.5f;
            audioSource.volume = (Time.timeScale == 0f) ? 0f : Mathf.Clamp01(baseVol);
        }
        // -------------------------------------------------------------

        dropTimer -= Time.deltaTime;

        // ---  Hedef noktada engel bina vb var mı? ---
        bool isTargetClear = true;
        Vector3 targetCheckPos = player.position + (player.forward * distanceAhead);
        targetCheckPos.y += 30f; // Hedefin 30 metre üstünden aşağı doğru tarama yap

        // Eğer ışın aşağı inerken bir "Obstacle" (Bina) etiketine çarpıyorsa orası temiz değildir
        if (Physics.Raycast(targetCheckPos, Vector3.down, out RaycastHit roofHit, 50f))
        {
            if (roofHit.collider.CompareTag("Obstacle"))
            {
                isTargetClear = false; // Dalışı iptal edeceğiz
            }
        }

        // --- SİNAMETİK DALIŞ MATEMATİĞİ ---
        // Sadece hedefe az kaldıysa VE hedef temizse (bina yoksa) dalış yap
        if (dropTimer <= dropPrepTime && isTargetClear)
        {
            // Atışa az kaldı! Helikopter salınımı sıfırlayıp hedefin tam üstüne kilitlensin ve alçalsın
            activeSway = Mathf.Lerp(activeSway, 0f, 5f * Time.deltaTime);
            activeHeight = Mathf.Lerp(activeHeight, heightAbovePlayer - dipAmount, 5f * Time.deltaTime);
        }
        else
        {
            // Atış bitti veya bina var diye dalış iptal edildi; normal devriye uçuşuna geri dön
            activeSway = Mathf.Lerp(activeSway, swayAmount, 2f * Time.deltaTime);
            activeHeight = Mathf.Lerp(activeHeight, heightAbovePlayer, 2f * Time.deltaTime);
        }

        // Pozisyonu Uygula
        Vector3 baseTargetPos = player.position + (player.forward * distanceAhead) + (Vector3.up * activeHeight);
        float sway = Mathf.Sin(Time.time * swaySpeed) * activeSway;
        Vector3 swayOffset = player.right * sway;

        transform.position = Vector3.Lerp(transform.position, baseTargetPos + swayOffset, followSpeed * Time.deltaTime);

        // Rotasyon
        Vector3 lookDir = player.forward;
        lookDir.y = 0f;

        if (lookDir.sqrMagnitude > 0.01f)
        {
            Quaternion baseRotation = Quaternion.LookRotation(lookDir);

            // Hedefe kilitlenip salınımı bıraktığında helikopterin gövdesi düzelsin diye oran hesabı
            float swayRatio = swayAmount > 0f ? (activeSway / swayAmount) : 0f;
            float rollAngle = -Mathf.Cos(Time.time * swaySpeed) * maxSideTilt * swayRatio;
            float pitchAngle = maxForwardTilt;

            Quaternion tiltRotation = Quaternion.Euler(pitchAngle, 0, rollAngle);
            Quaternion finalRotation = baseRotation * tiltRotation;

            transform.rotation = Quaternion.Slerp(transform.rotation, finalRotation, 5f * Time.deltaTime);
        }

        if (dropTimer <= 0f)
        {
            // Sadece hedef temizse (bina yoksa) kapanı yola bırak
            if (isTargetClear)
            {
                DropSpikeStrip();
            }
            dropTimer = dropInterval; // Her iki durumda da süreyi sıfırla ki bir sonraki atış denemesini bekle
        }

        RotateRotors();
    }

    private void RotateRotors()
    {
        if (mainRotor != null)
        {
            mainRotor.Rotate(Vector3.forward * mainRotorSpeed * Time.deltaTime, Space.Self);
        }
        if (tailRotor != null)
        {
            tailRotor.Rotate(Vector3.forward * tailRotorSpeed * Time.deltaTime, Space.Self);
        }
    }

    private void DropSpikeStrip()
    {
        //  Helikopter zaten atış öncesi hedefe kilitlenip pike yaptığı için
        // Raycasti hedef rotadan değil kendi altından yolla
        Vector3 rayStart = transform.position;

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 50f))
        {
            if (hit.collider.CompareTag(groundTag))
            {
                GameObject spike = GetSpike();
                if (spike != null)
                {
                    spike.transform.SetParent(null);
                    spike.transform.position = hit.point + new Vector3(0, groundOffset, 0);

                    Vector3 flatForward = player.forward;
                    flatForward.y = 0f;
                    Quaternion spawnRot = Quaternion.LookRotation(flatForward) * Quaternion.Euler(0, 90f, 0);

                    spike.transform.rotation = spawnRot;
                }
            }
        }
    }

    public GameObject GetSpike()
    {
        if (spikePool.Count > 0)
        {
            GameObject obj = spikePool.Dequeue();
            obj.SetActive(true);
            return obj;
        }
        return Instantiate(spikeStripPrefab);
    }

    public void ReturnSpike(GameObject spike)
    {
        spike.SetActive(false);
        spike.transform.SetParent(transform);
        spikePool.Enqueue(spike);
    }

    public GameObject GetVFX()
    {
        if (vfxPool.Count > 0)
        {
            GameObject vfx = vfxPool.Dequeue();
            vfx.SetActive(true);
            return vfx;
        }
        return tireBurstVFXPrefab != null ? Instantiate(tireBurstVFXPrefab) : null;
    }

    public void ReturnVFX(GameObject vfx, float delay)
    {
        StartCoroutine(VFXDelayRoutine(vfx, delay));
    }

    private IEnumerator VFXDelayRoutine(GameObject vfx, float delay)
    {
        yield return new WaitForSeconds(delay);
        vfx.SetActive(false);
        vfxPool.Enqueue(vfx);
    }
}