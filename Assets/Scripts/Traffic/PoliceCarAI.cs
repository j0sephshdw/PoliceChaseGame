using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PoliceCarAI : MonoBehaviour
{
    [Header("Hedef (Oyuncu)")]
    public Transform target; // Boş bırakılırsa "Player" tag'ine sahip objeyi otomatik bulur

    [Header("Yakın Takip Ayarları")]
    public float followBufferDistance = 2.2f;  // Polisin oyuncunun tam arkasında tutmaya çalışacağı mesafe ("dibinde")
    public float catchUpGain = 2f;             // Mesafe hatasını hıza çeviren kazanç

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

    [Header("Dönüş Ayarları (savrulmayı ve dolanmayı engellemek için)")]
    public float turnSpeed = 55f;              // Maksimum dönüş hızı (derece/saniye) - düşürüldü
    public float turnResponsiveness = 3f;      // Açı hatasını dönüş hızına çeviren kazanç - düşürüldü
    public float turnSmoothing = 8f;
    public float fullTurnSpeedThreshold = 3f;  // Bu hızın üzerindeyken dönüş hızı tam güçte çalışır

    [Header("Drift Ayarları (ani dönüşlerde geniş yay çizmeyi engeller)")]
    public float driftAngleThreshold = 20f;    // Açı hatası bunu geçerse ve yeterli hızdaysa drift moduna girer
    public float driftMinSpeed = 6f;           // Bu hızın altında drift tetiklenmez (yerinde savrulmayı engeller)
    public float normalGrip = 6f;              // Normal durumda hareket yönü, aracın baktığı yöne ne kadar hızlı yapışır
    public float driftGrip = 1.2f;             // Drift sırasında grip (düşük = daha çok kayar)
    public float driftTurnSpeedMultiplier = 1.7f; // Drift sırasında dönüş hızı çarpanı (daha keskin dönebilsin)
    public float driftTurnResponsivenessMultiplier = 1.4f; // Drift sırasında açıya tepki çarpanı

    [Header("Oyuncu Drift Senkronu (polis SENİNLE BİRLİKTE drift atsın)")]
    public float playerDriftDetectAngle = 15f; // Oyuncunun baktığı yön ile GİTTİĞİ yön arasındaki fark bunu geçerse "oyuncu drift atıyor" sayılır
    public float playerDriftMinSpeed = 3f;     // Oyuncu bu hızın altındaysa drift algılanmaz
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

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.centerOfMass = new Vector3(0f, -0.4f, 0f);
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

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
            audioSource.Play();
        }
    }

    private void FixedUpdate()
    {
        if (target == null) return;
        ChaseTarget();
    }

    private void ChaseTarget()
    {
        float actualDistanceToPlayer = Vector3.Distance(transform.position, target.position);

        // Oyuncunun düz hızı
        float playerSpeed = 0f;
        Vector3 flatTargetVel = Vector3.zero;
        if (targetRb != null)
        {
            flatTargetVel = targetRb.linearVelocity;
            flatTargetVel.y = 0f;
            playerSpeed = flatTargetVel.magnitude;
        }

        // 1. HEDEF NOKTA: uzaktayken hafif tahmin (lead pursuit) kullan,
        //    yakınlaştıkça tahmini SÖNDÜR ve direkt oyuncuyu hedefle.
        float predictionBlend = Mathf.InverseLerp(predictionFadeStart, predictionFadeEnd, actualDistanceToPlayer);
        Vector3 aimPoint = target.position + flatTargetVel * (predictionTime * predictionBlend);

        Vector3 toAimPoint = aimPoint - transform.position;
        toAimPoint.y = 0f;
        float distanceToAimPoint = toAimPoint.magnitude;
        Vector3 dirToAimPoint = distanceToAimPoint > 0.05f ? toAimPoint.normalized : transform.forward;

        // 2. Açı hatası
        float angleToAimPoint = Vector3.SignedAngle(transform.forward, dirToAimPoint, Vector3.up);

        // --- DRIFT KARARI ---
        // A) Polisin kendi açı hatası büyükse VE yeterince hızlıysa -> kendi başına drift.
        bool selfDrift = Mathf.Abs(angleToAimPoint) > driftAngleThreshold && currentSpeed > driftMinSpeed;

        // B) OYUNCU şu an drift atıyor mu? Oyuncunun baktığı yön ile GERÇEKTEN gittiği yön
        //    (rigidbody velocity) arasındaki açı büyükse oyuncu kayıyor demektir.
        //    Bu, polisin KENDİ hızından/açısından TAMAMEN BAĞIMSIZ çalışır -> polis
        //    henüz yavaşlamamış/tam hizalanmamış olsa bile SENİNLE AYNI ANDA drift moduna girer.
        bool playerDrift = false;
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

        isDrifting = selfDrift || playerDrift;

        float effectiveTurnSpeed = turnSpeed;
        float effectiveTurnResponsiveness = turnResponsiveness;
        if (isDrifting)
        {
            // Drift sırasında araç daha keskin/hızlı döner (kaymayla desteklendiği için savrulmaz)
            effectiveTurnSpeed *= driftTurnSpeedMultiplier;
            effectiveTurnResponsiveness *= driftTurnResponsivenessMultiplier;
        }

        float desiredTurnRate = Mathf.Clamp(angleToAimPoint * effectiveTurnResponsiveness, -effectiveTurnSpeed, effectiveTurnSpeed);
        desiredTurnRate += ObstacleAvoidanceSteer();

        currentTurnRate = Mathf.Lerp(currentTurnRate, desiredTurnRate, turnSmoothing * Time.fixedDeltaTime);

        // Neredeyse dururken sert dönemesin (yerinde dönme bugını engeller)
        float speedFactor = Mathf.Clamp01(Mathf.Abs(currentSpeed) / fullTurnSpeedThreshold);
        float appliedTurnRate = currentTurnRate * Mathf.Lerp(0.35f, 1f, speedFactor);

        Quaternion turnRotation = Quaternion.Euler(0f, appliedTurnRate * Time.fixedDeltaTime, 0f);
        rb.MoveRotation(rb.rotation * turnRotation);

        // 3. HIZ: oyuncunun hızı + mesafe hatası.
        float distanceError = actualDistanceToPlayer - followBufferDistance;
        float targetSpeed = playerSpeed + distanceError * catchUpGain;
        targetSpeed = Mathf.Clamp(targetSpeed, 0f, maxSpeed);

        // 4. ÇARPIŞMAYI ÖNLEME
        if (actualDistanceToPlayer < minSafeDistance)
        {
            float safeFactor = Mathf.InverseLerp(emergencyStopDistance, minSafeDistance, actualDistanceToPlayer);
            float cappedSpeed = Mathf.Lerp(playerSpeed * 0.9f, targetSpeed, safeFactor);
            targetSpeed = Mathf.Min(targetSpeed, cappedSpeed);
        }

        // 5. DRIFT HIZ TABANI: drift sırasında (özellikle çarpışma güvenliği hızı kıstığında)
        //    polisin oyuncunun çok altına düşüp "durmasını" engeller -> seninle birlikte kayar.
        //    Acil durma mesafesindeyken bu taban devre dışı, çarpışmayı hâlâ önler.
        if (isDrifting && actualDistanceToPlayer > emergencyStopDistance)
        {
            targetSpeed = Mathf.Max(targetSpeed, playerSpeed * driftSpeedFloorFactor);
        }

        // Drift sırasında hız değişimi (hızlanma da yavaşlama da) daha çabuk tepki versin,
        // böylece polis oyuncunun hız/yön değişimlerine "gecikmeli" değil AYNI ANDA tepki verir.
        float effectiveAcceleration = isDrifting ? acceleration * driftAccelerationMultiplier : acceleration;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, effectiveAcceleration * Time.fixedDeltaTime);

        MoveForward();
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

    private float ObstacleAvoidanceSteer()
    {
        RaycastHit hit;
        Vector3 origin = transform.position + Vector3.up * 0.5f;

        if (Physics.Raycast(origin, transform.forward, out hit, obstacleCheckDistance, obstacleLayerMask))
        {
            Vector3 localHit = transform.InverseTransformPoint(hit.point);
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