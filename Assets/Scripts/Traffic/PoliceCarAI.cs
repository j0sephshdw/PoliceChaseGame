using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PoliceCarAI : MonoBehaviour
{
    // Polisin oyuncunun tam arkasında değil, sağında veya solunda durmasını sağlayacak sapma değeri
    private float randomLateralOffset;
    [Header("Hedef (Oyuncu)")]
    public Transform target; // Boş bırakılırsa "Player" tag'ine sahip objeyi otomatik bulur

    [Header("Yakın Takip Ayarları")]
    public float followBufferDistance = 2.2f;  // Polisin oyuncunun tam arkasında tutmaya çalışacağı mesafe ("dibinde")
    public float catchUpGain = 2f;             // Mesafe hatasını hıza çeviren kazanç
    public float maxCatchUpBrake = 6f;         // Mesafe aniden kısalırsa (ör. oyuncu keskin dönünce) hız düşüşünü bu değerle sınırlar -> ani "durma" hissini engeller

    [Header("Tahmin (sadece uzaktayken kullanılır)")]
    public float predictionTime = 0.5f;        // Uzaktayken oyuncunun ilerideki noktasını kesmeye çalışır
    public float predictionFadeStart = 6f;     // Bu mesafenin altında tahmin tamamen kapanır (dolanmayı engeller)
    public float predictionFadeEnd = 16f;      // Bu mesafenin üstünde tahmin tam güçte çalışır

    [Header("Hız Ayarları")]
    public float maxSpeed = 20f;               // Sadece güvenlik tavanı; normalde hedef hız oyuncunun hızına göre belirlenir
    public float acceleration = 9f;

    [Header("Çarpışmayı Önleme (dibine gelsin ama çarpmasın)")]
    public float minSafeDistance = 3f;         // Bu mesafenin altında hız kısılmaya başlar
    public float emergencyStopDistance = 1.4f; // Bu kadar yakınsa oyuncunun hızının biraz altına düşer, mesafeyi kapatmaz

    [Header("Çarpışma Sağlamlığı (player çarpınca polis İTİLMESİN)")]
    [Tooltip("Polisin fizik kütlesi. Player'ın kütlesinden çok yüksek tutulursa çarpışma impulsu polisi neredeyse hiç itmez; player kendini polise çarpmış gibi hisseder, polis olduğu yerde/rotada kalır.")]
    public float collisionMass = 400f;         // Player'dan (genelde ~1-10) çok daha ağır -> impuls etkisi ihmal edilebilir düzeyde kalır

    [Header("Dönüş Ayarları (savrulmayı ve dolanmayı engellemek için)")]
    public float turnSpeed = 55f;              // Maksimum dönüş hızı (derece/saniye)
    public float turnResponsiveness = 3f;      // Açı hatasını dönüş hızına çeviren kazanç
    public float turnSmoothing = 8f;           // Normal (drift dışı) dönüş yumuşatması
    public float driftEntrySmoothing = 30f;    // Drift SIRASINDA (sadece giriş karesinde değil, sürekli) kullanılan çok daha sert/hızlı yumuşatma
    public float fullTurnSpeedThreshold = 3f;  // Bu hızın üzerindeyken dönüş hızı tam güçte çalışır

    [Header("Drift Ayarları (ani dönüşlerde geniş yay çizmeyi engeller)")]
    public float driftAngleThreshold = 20f;    // Açı hatası bunu geçerse ve yeterli hızdaysa drift moduna girer
    public float driftMinSpeed = 6f;           // Bu hızın altında drift tetiklenmez (yerinde savrulmayı engeller)
    public float normalGrip = 6f;              // Normal durumda hareket yönü, aracın baktığı yöne ne kadar hızlı yapışır
    public float driftGrip = 1.2f;             // Drift sırasında grip (düşük = daha çok kayar)
    public float driftTurnSpeedMultiplier = 1.7f; // Drift sırasında dönüş hızı çarpanı (daha keskin dönebilsin)
    public float driftTurnResponsivenessMultiplier = 1.4f; // Drift sırasında açıya tepki çarpanı

    [Header("Oyuncu Drift Senkronu (polis SENİNLE BİRLİKTE drift atsın)")]
    [Tooltip("AÇIK: Oyuncunun kendi controller scripti PoliceCarAI.SetPlayerDriftInput(bool) fonksiyonunu el freni tuşuna basılınca/bırakılınca çağırmalı. Bu, fizik tabanlı algılamadaki gecikmeyi TAMAMEN ortadan kaldırır ve polis oyuncuyla AYNI KAREDE drifte girer. BU BAĞLI DEĞİLSE (player controller'da çağrılmıyorsa) polis her zaman fallback algılamayı kullanır ve gecikme YAŞAR.")]
    public bool usePlayerDriftInputOverride = true;
    private bool playerDriftInputActive = false; // Dışarıdan (PlayerController) SetPlayerDriftInput ile set edilir

    public float playerDriftDetectAngle = 15f; // Fallback: oyuncunun baktığı yön ile GİTTİĞİ yön arasındaki fark bunu geçerse "oyuncu drift atıyor" sayılır (sadece override kapalıysa / player rb yoksa kullanılır)
    public float playerDriftMinSpeed = 3f;     // Oyuncu bu hızın altındaysa drift algılanmaz (fallback için)
    [Range(0f, 1f)]
    public float driftSpeedFloorFactor = 0.75f; // Drift sırasında polis hızı, oyuncu hızının bu oranının altına düşmez (polisin "durmasını" engeller)
    public float driftAccelerationMultiplier = 2.2f; // Drift sırasında hız değişimi (hızlanma/yavaşlama) daha çabuk tepki versin

    [Header("Engel Algılama (opsiyonel)")]
    public float obstacleCheckDistance = 4f;
    public LayerMask obstacleLayerMask;

    [Header("Sesler / Efektler (opsiyonel)")]
    public AudioClip sirenSound;
    private AudioSource audioSource;

    private Rigidbody rb;
    private Rigidbody targetRb;
    private float currentSpeed = 0f;
    private float currentTurnRate = 0f;

    // Drift için: aracın GERÇEK hareket yönü, transform.forward'dan bağımsız takip edilir
    private Vector3 currentMoveDir;
    private bool isDrifting = false;
    private bool isStunned = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.centerOfMass = new Vector3(0f, -0.4f, 0f);
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.mass = collisionMass; // Player çarpınca polisi itmesin diye kütleyi ağır tutuyoruz

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;

        currentMoveDir = transform.forward;
    }

    private void Start()
    {
        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) target = playerObj.transform;
        }

        if (target != null) targetRb = target.GetComponent<Rigidbody>();

        if (sirenSound != null)
        {
            audioSource.clip = sirenSound;
            audioSource.volume = GameUIManager.GetGameVolume();
            audioSource.Play();
        }
        // Her polise oyuncunun -2.5 (sol) ile +2.5 (sağ) birim arasında rastgele bir hedef offset veriyoruz
        randomLateralOffset = Random.Range(-2.5f, 2.5f);
    }

    void Update()
    {
        if (audioSource != null)
        {
            audioSource.volume = GameUIManager.GetGameVolume();
        }
    }

    private void FixedUpdate()
    {
        if (target == null) return;

        // Çarpışma impulsundan (ör. player'ın polise çarpması) kalan yatay hızı her karede
        // sıfırlıyoruz. Böylece bir önceki fizik adımında oluşan itme birikip polisi kaydırmaz;
        // polisin konumu tamamen bu scriptin MovePosition/MoveRotation çağrılarıyla belirlenir.
        // Dikey (y) hız dokunulmadan kalır, yerçekimi/yere yapışma normal çalışmaya devam eder.
        Vector3 vel = rb.linearVelocity;
        vel.x = 0f;
        vel.z = 0f;
        rb.linearVelocity = vel;

        if (isStunned) return;
        ChaseTarget();
    }

    /// <summary>
    /// Oyuncunun controller scripti el frenine basınca true, bırakınca false çağırmalı.
    /// Örnek: policeCarAI.SetPlayerDriftInput(Input.GetKey(KeyCode.Space));
    /// Bu sayede polis, fiziğin kaymayı üretmesini BEKLEMEDEN, oyuncuyla aynı anda drifte girer.
    /// ÖNEMLİ: Bu fonksiyon her karede (Update/FixedUpdate) çağrılmalı, sadece basılma/bırakılma
    /// anında değil - aksi halde durum senkron kalmaz.
    /// </summary>
    public void SetPlayerDriftInput(bool active)
    {
        playerDriftInputActive = active;
    }

    private void ChaseTarget()
    {
        float actualDistanceToPlayer = Vector3.Distance(transform.position, target.position);

        // --- 1. GERİDE KALANLARI SİL ---
        if (actualDistanceToPlayer > 120f)
        {
            gameObject.SetActive(false);
            return;
        }

        float playerSpeed = 0f;
        Vector3 flatTargetVel = Vector3.zero;
        if (targetRb != null)
        {
            flatTargetVel = targetRb.linearVelocity;
            flatTargetVel.y = 0f;
            playerSpeed = flatTargetVel.magnitude;
        }

        // 1. HEDEF NOKTA
        float predictionBlend = Mathf.InverseLerp(predictionFadeStart, predictionFadeEnd, actualDistanceToPlayer);
        Vector3 aimPoint = target.position + flatTargetVel * (predictionTime * predictionBlend);

        // 2. YAN YANA YAYILMA 
        aimPoint += target.right * randomLateralOffset;

        Vector3 toAimPoint = aimPoint - transform.position;
        toAimPoint.y = 0f;
        float distanceToAimPoint = toAimPoint.magnitude;
        Vector3 dirToAimPoint = distanceToAimPoint > 0.05f ? toAimPoint.normalized : transform.forward;

        // 2. Açı hatası
        float angleToAimPoint = Vector3.SignedAngle(transform.forward, dirToAimPoint, Vector3.up);

        // --- DRIFT KARARI ---
        bool selfDrift = Mathf.Abs(angleToAimPoint) > driftAngleThreshold && currentSpeed > driftMinSpeed;

        bool playerDrift;
        if (usePlayerDriftInputOverride)
        {
            playerDrift = playerDriftInputActive;
        }
        else
        {
            playerDrift = false;
            if (playerSpeed > playerDriftMinSpeed)
            {
                Vector3 playerForwardFlat = target.forward;
                playerForwardFlat.y = 0f;
                if (playerForwardFlat.sqrMagnitude > 0.0001f)
                {
                    float playerSlipAngle = Vector3.Angle(playerForwardFlat.normalized, flatTargetVel.normalized);
                    playerDrift = playerSlipAngle > playerDriftDetectAngle;
                }
            }
        }

        bool wasDrifting = isDrifting;
        isDrifting = selfDrift || playerDrift;
        bool driftJustStarted = isDrifting && !wasDrifting;
        bool driftJustEnded = !isDrifting && wasDrifting;

        float effectiveTurnSpeed = turnSpeed;
        float effectiveTurnResponsiveness = turnResponsiveness;
        if (isDrifting)
        {
            effectiveTurnSpeed *= driftTurnSpeedMultiplier;
            effectiveTurnResponsiveness *= driftTurnResponsivenessMultiplier;
        }

        float desiredTurnRate = Mathf.Clamp(angleToAimPoint * effectiveTurnResponsiveness, -effectiveTurnSpeed, effectiveTurnSpeed);
        desiredTurnRate += ObstacleAvoidanceSteer();

        float appliedSmoothing = isDrifting ? driftEntrySmoothing : turnSmoothing;
        currentTurnRate = Mathf.Lerp(currentTurnRate, desiredTurnRate, appliedSmoothing * Time.fixedDeltaTime);

        float speedFactor = Mathf.Clamp01(Mathf.Abs(currentSpeed) / fullTurnSpeedThreshold);
        float appliedTurnRate = currentTurnRate * Mathf.Lerp(0.35f, 1f, speedFactor);

        Quaternion turnRotation = Quaternion.Euler(0f, appliedTurnRate * Time.fixedDeltaTime, 0f);
        rb.MoveRotation(rb.rotation * turnRotation);

        // --- 3. HIZ HESAPLAMASI ---
        float distanceError = actualDistanceToPlayer - followBufferDistance;
        float clampedDistanceError = Mathf.Max(distanceError, -maxCatchUpBrake / Mathf.Max(catchUpGain, 0.0001f));
        float targetSpeed = playerSpeed + clampedDistanceError * catchUpGain;
        targetSpeed = Mathf.Clamp(targetSpeed, 0f, maxSpeed);

        // --- 4. HİBRİD ÇARPIŞMAYI ÖNLEME SİSTEMİ (AKILLI POLİSLER) ---
        if (actualDistanceToPlayer < minSafeDistance)
        {
            // Eğer oyuncu düzgünce kaçıyorsa (hızı 5'ten büyükse) saygılı mesafeyi koru
            if (playerSpeed > 5f)
            {
                float safeFactor = Mathf.InverseLerp(emergencyStopDistance, minSafeDistance, actualDistanceToPlayer);
                float cappedSpeed = Mathf.Lerp(playerSpeed * 0.9f, targetSpeed, safeFactor);
                targetSpeed = Mathf.Min(targetSpeed, cappedSpeed);
            }
            else
            {
                // Eğer oyuncu durmuşsa, kaza yapmışsa veya kendi etrafında dönüyorsa acımadan çarp!
                targetSpeed = Mathf.Max(targetSpeed, 12f); // En az 12 hızla vur
            }
        }

        // 5. DRIFT HIZ TABANI
        if (isDrifting && actualDistanceToPlayer > emergencyStopDistance)
        {
            targetSpeed = Mathf.Max(targetSpeed, playerSpeed * driftSpeedFloorFactor);
        }

        float effectiveAcceleration = (isDrifting || driftJustEnded) ? acceleration * driftAccelerationMultiplier : acceleration;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, effectiveAcceleration * Time.fixedDeltaTime);

        MoveForward();
    }

    public void Stun(float duration)
    {
        if (isStunned) return; // zaten durduysa tekrar tetiklenmesin
        StartCoroutine(StunRoutine(duration));
    }

    private IEnumerator StunRoutine(float duration)
    {
        isStunned = true;
        currentSpeed = 0f; // aniden dursun
        yield return new WaitForSeconds(duration);
        isStunned = false;
    }

    private void MoveForward()
    {
        // --- DRIFT HAREKETİ ---
        // Normalde currentMoveDir hızlıca transform.forward'a yapışır (grip yüksek).
        // Drift sırasında grip düşer: araç burnu hedefe dönerken, GERÇEK hareket yönü
        // geriden yetişir -> bu da klasik "kayarak dönüş" (drift) görüntüsünü yaratır.
        float grip = isDrifting ? driftGrip : normalGrip;
        currentMoveDir = Vector3.Slerp(currentMoveDir, transform.forward, grip * Time.fixedDeltaTime).normalized;

        Vector3 movement = currentMoveDir * currentSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movement);
    }

    private float ObstacleAvoidanceSteer()//SphereCast ile geniş bir tarama yap
    {
        RaycastHit hit;
        Vector3 origin = transform.position + Vector3.up * 0.5f;

        // 1.2f yarıçapında bir sphereCast  fırlat
        if (Physics.SphereCast(origin, 1.2f, transform.forward, out hit, obstacleCheckDistance, obstacleLayerMask))
        {
            Vector3 localHit = transform.InverseTransformPoint(hit.point);
            // Engel sağdaysa sola dön -60 soldaysa sağa dön 60
            return localHit.x < 0f ? 60f : -60f;
        }

        return 0f;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            currentSpeed *= 0.4f;
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        targetRb = newTarget != null ? newTarget.GetComponent<Rigidbody>() : null;
    }

    public float GetCurrentSpeed()
    {
        return currentSpeed;
    }

    public bool IsDrifting()
    {
        return isDrifting;
    }
}