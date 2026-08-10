using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PoliceCarAI : MonoBehaviour
{
    // --- TEMEL BİLGİLER VE KORUMA ---
    [Header("Araç Verisi (Car Data)")]
    public CarData carData;

    [Header("Spawn Koruma")]
    [Tooltip("Polis doğduğu gibi bir yere çarpıp patlamasın diye verilen süre")]
    public float spawnInvulnerabilityDuration = 1.5f;
    private float spawnTime; // Polisin sahneye çıktığı anı tutarız

    // --- HEDEF VE TAKİP AYARLARI ---
    [Header("Hedef (Oyuncu)")]
    public Transform target;

    [Header("Yakın Takip")]
    public float followBufferDistance = 1.2f;
    public float catchUpGain = 2.8f;
    public float ramSpeedBonus = 7f;
    public float ramCloseDistance = 12f;
    public float ramPushForce = 18f; // Çarptığımızda oyuncuyu ittirme gücümüz

    // --- FİZİK VE HAREKET ---
    [Header("Tahmin (Prediction)")]
    public float predictionTime = 0.65f; // Oyuncunun gideceği yeri tahmin etme süresi
    public float predictionFadeStart = 4f;
    public float predictionFadeEnd = 22f;

    [Header("Hız")]
    public float maxSpeed = 20f;
    public float acceleration = 9f;

    [Header("Çarpışma")]
    public float collisionMass = 400f; // Aracın ağırlığı
    public float emergencyStopDistance = 1.4f;
    public float recoverDuration = 0.35f; // Çarpıştıktan sonra toparlanma süresi

    [Header("Dönüş")]
    public float turnSpeed = 55f;
    public float turnResponsiveness = 3.2f;
    public float turnSmoothing = 10f;
    public float driftEntrySmoothing = 30f;
    public float fullTurnSpeedThreshold = 3f;

    // --- DRIFT AYARLARI ---
    [Header("Drift")]
    public float driftAngleThreshold = 18f;
    public float driftMinSpeed = 5f;
    public float normalGrip = 7f; // Normal yol tutuşu
    public float driftGrip = 1.4f; // Kayarkenki yol tutuşu (düşük olmalı)
    public float driftTurnSpeedMultiplier = 1.8f;
    public float driftTurnResponsivenessMultiplier = 1.5f;
    [Range(0f, 1f)] public float driftSpeedFloorFactor = 0.8f;
    public float driftAccelerationMultiplier = 2.4f;

    [Header("Oyuncu Drift Senkronu")]
    public bool usePlayerDriftInputOverride = true;
    public float playerDriftDetectAngle = 14f;
    public float playerDriftMinSpeed = 3f;

    // --- YAPAY ZEKA ÇEVRE ALGILAMASI ---
    [Header("Engel Algılama")]
    public float obstacleCheckDistance = 5f; // Çarpışmayı önlemek için ışın (ray) atılacak mesafe
    public LayerMask obstacleLayerMask;

    [Header("Zorluk Ölçekleme")]
    public bool scaleWithScore = true;
    public float scoreSpeedScale = 0.004f;
    public float scoreAccelScale = 0.006f;

    // --- GÖRSEL VE İŞİTSEL EFEKTLER ---
    [Header("Patlama ve Görsel Efektler")]
    public Transform carMesh;             // Polis aracının asıl gövdesi (mesh)
    public GameObject explosionVFX;       // Patlayınca çıkacak partikül
    public AudioClip explosionSound;
    public float patlamaGucu = 10f;       // Parçaların ne kadar şiddetli fırlayacağı
    public float patlamaYaricapi = 8f;
    public float havayaFirlatmaGucu = 0.5f;

    [Header("Sesler")]
    public AudioClip sirenSound;

    // --- GİZLİ (PRIVATE) DEĞİŞKENLER ---
    private AudioSource audioSource;
    private Rigidbody rb;
    private Rigidbody targetRb;

    private float currentSpeed;
    private float currentTurnRate;
    private Vector3 currentMoveDir;
    private float randomLateralOffset; // Polislerin ip gibi dizilmemesi için rastgele sapma

    // Durum kontrol bayrakları (Flags)
    private bool playerDriftInputActive;
    private bool isDrifting;
    private bool isStunned;
    private bool isRecovering;
    private bool isDead = false;

    private float stuckTimer; // Aracın bir yere takılıp takılmadığını anladığımız sayaç
    private float lastDistanceToPlayer = 999f;
    private float difficultyMultiplier = 1f;

    private void Awake()
    {
        // Oyun başlarken gerekli komponentleri çekiyoruz
        rb = GetComponent<Rigidbody>();

        // Fizik motorunun kafası karışmasın ve araba takla atmasın diye ayarlar
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // Hızlı çarpışmaları kaçırmamak için
        rb.interpolation = RigidbodyInterpolation.Interpolate; // Hareketin pürüzsüz akması için
        rb.centerOfMass = new Vector3(0f, -0.4f, 0f); // Ağırlık merkezini aşağı çektik ki araç devrilmesin
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ; // Şaha kalkmayı önler
        rb.mass = collisionMass;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;

        currentMoveDir = transform.forward;
    }

    /// <summary>
    /// Obje havuzundan (Object Pool) araç her çağrıldığında burası çalışır.
    /// Eski verileri temizleyip aracı sıfırlarız ki "ölü" doğmasın.
    /// </summary>
    private void OnEnable()
    {
        isDead = false;
        spawnTime = Time.time; // Polisin sahneye çıktığı anı kaydettik (Spawn koruması için)

        Collider mainCollider = GetComponent<Collider>();
        if (mainCollider != null)
        {
            mainCollider.enabled = true;
        }
        this.enabled = true;
    }

    // Unity Inspector'da scripti resetleyince Car Mesh'i otomatik bulur (Elle uğraşmamak için)
    private void Reset()
    {
        if (carMesh == null && transform.childCount > 0)
        {
            carMesh = transform.GetChild(0);
        }
    }

    private void Start()
    {
        ResolveTarget(); // Kovalayacağımız oyuncuyu buluyoruz

        if (sirenSound != null)
        {
            audioSource.clip = sirenSound;
            audioSource.volume = GameUIManager.GetGameVolume();
            audioSource.Play();
        }

        ApplyCarDataBuff(); // Araç verilerini koda aktar
        randomLateralOffset = Random.Range(-2.5f, 2.5f); // Tüm polisler aynı çizgiden gitmesin diye ufak bir sapma
        RefreshDifficulty();
    }

    private void Update()
    {
        if (isDead) return; // Araç öldüyse Update işlemlerini durdur (Performans tasarrufu)

        if (audioSource != null)
            audioSource.volume = GameUIManager.GetGameVolume();

        // Her frame yerine 30 frame'de bir zorluk kontrolü yapıyoruz (Optimizasyon)
        if (Time.frameCount % 30 == 0)
            RefreshDifficulty();
    }

    private void FixedUpdate()
    {
        // Fizik hesaplamaları her zaman FixedUpdate içinde yapılmalıdır!
        if (target == null || isDead) return;

        ZeroVerticalVelocity(); // Araba zıplamasın diye Y eksenindeki hızı sıfırlıyoruz

        if (isStunned) return;

        float distance = FlatDistance(transform.position, target.position);

        // Eğer oyuncudan çok uzaklaştıysa polisi havuzdan kaldır (Ekranda kalabalık yapmasın)
        if (distance > 120f)
        {
            gameObject.SetActive(false);
            return;
        }

        UpdateStuckDetection(distance);
        ChaseTarget(distance);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isDead) return;

        // 1. ENGEL VEYA DİĞER POLİSE ÇARPINCA DİREKT PATLA
        if (collision.gameObject.CompareTag("Obstacle") || collision.gameObject.CompareTag("Police"))
        {
            Explode();
            return;
        }

        // Çarptığımız şey oyuncu değilse metottan çık
        if (!collision.gameObject.CompareTag("Player")) return;

        // --- KAFA KAFAYA ÇARPIŞMA KONTROLÜ (Vektörel Matematik) ---
        // Dot product (İç Çarpım) kullanarak iki aracın baktığı yönü kıyaslıyoruz.
        // Eğer sonuç -1'e yakınsa, iki araç tam zıt yönden birbirine bakıyor demektir.
        float headOnDot = Vector3.Dot(transform.forward, target.forward);

        // İki aracın birbirine göre hızı (Çarpışmanın şiddeti)
        float impactSpeed = collision.relativeVelocity.magnitude;

        // Şart: Zıt yönden geliyorsak (-0.4'ten küçükse) VE hızlı çarptıysak polisi uçur
        if (headOnDot < -0.4f && impactSpeed > 15f)
        {
            Explode();
            return;
        }

        // --- NORMAL ÇARPIŞMA MANTIĞI ---
        float playerSpeed = targetRb != null ? targetRb.linearVelocity.magnitude : 0f;
        // Çarpışınca hızımızı düşürüyoruz (Gerçekçilik için)
        currentSpeed = Mathf.Max(currentSpeed * 0.94f, playerSpeed * 0.88f);

        if (collision.contactCount == 0) return;

        // Temas noktasının normalini alıp Y eksenini sıfırlıyoruz ki araba havaya uçmasın
        Vector3 contactNormal = collision.contacts[0].normal;
        contactNormal.y = 0f;
        if (contactNormal.sqrMagnitude < 0.01f) return;

        Vector3 pushDir = -contactNormal.normalized;
        float approach = Vector3.Dot(transform.forward, pushDir);

        // Eğer oyuncuya arkadan veya yandan yeterli açıyla vurduysak onu ittir
        if (approach > 0.25f && targetRb != null)
        {
            targetRb.AddForce(pushDir * ramPushForce * difficultyMultiplier, ForceMode.Impulse);
        }

        if (!isRecovering)
            StartCoroutine(RecoverRoutine());
    }

    /// <summary>
    /// Polisin patlayıp parçalara ayrıldığı metod.
    /// Optimizasyon için asıl objeyi silmiyoruz (SetActive=false yapıyoruz).
    /// Görsel olarak parçalanma hissini meshleri kopyalayarak veriyoruz.
    /// </summary>
    public void Explode()
    {
        if (isDead) return;

        // Spawn koruması: Doğar doğmaz patlamasın diye zaman kontrolü yapıyoruz
        if (Time.time < spawnTime + spawnInvulnerabilityDuration)
            return;

        isDead = true;

        // Sesleri sustur
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        // Patlama ses efekti
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, transform.position, GameUIManager.GetGameVolume());
        }

        // Görsel efekt
        if (explosionVFX != null)
        {
            GameObject vfx = Instantiate(explosionVFX, transform.position, Quaternion.identity);
            Destroy(vfx, 3f);
        }

        // Gövdeyi oluşturan alt parçaları (meshleri) buluyoruz
        MeshRenderer[] allParts = GetComponentsInChildren<MeshRenderer>();

        foreach (MeshRenderer meshPart in allParts)
        {
            // Her parça için geçici bir kopyasını oluşturuyoruz (Havada uçacak olanlar)
            GameObject flyingPart = new GameObject(meshPart.name + "_FlyingPiece");
            flyingPart.transform.position = meshPart.transform.position;
            flyingPart.transform.rotation = meshPart.transform.rotation;
            flyingPart.transform.localScale = meshPart.transform.lossyScale;

            // Görsel veriyi (Mesh ve Material) aktar
            MeshFilter originalFilter = meshPart.GetComponent<MeshFilter>();
            if (originalFilter != null)
            {
                flyingPart.AddComponent<MeshFilter>().sharedMesh = originalFilter.sharedMesh;
                flyingPart.AddComponent<MeshRenderer>().sharedMaterials = meshPart.sharedMaterials;
            }

            // Fizik ekle (Uçması için)
            flyingPart.AddComponent<BoxCollider>();
            Rigidbody partRb = flyingPart.AddComponent<Rigidbody>();
            partRb.mass = 1.5f;
            partRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // Patlama kuvvetini kopyalanan parçaya uygula
            partRb.AddExplosionForce(patlamaGucu, transform.position, patlamaYaricapi, havayaFirlatmaGucu, ForceMode.Impulse);
            partRb.AddTorque(Random.insideUnitSphere * 15f, ForceMode.Impulse); // Havada döne döne gitsin

            // Çöp olmasın diye uçan parçayı 5 saniye sonra sahneden siliyoruz
            Destroy(flyingPart, 5f);
        }

        // Asıl polis objesini havuza geri dönebilmesi için sadece gizliyoruz (Destroy yapmıyoruz!)
        gameObject.SetActive(false);
    }

    public void SetPlayerDriftInput(bool active)
    {
        playerDriftInputActive = active;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        targetRb = newTarget != null ? newTarget.GetComponent<Rigidbody>() : null;

        if (targetRb != null)
        {
            currentSpeed = targetRb.linearVelocity.magnitude;
            currentMoveDir = transform.forward;
        }
    }

    private void ResolveTarget()
    {
        if (target != null) return;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            SetTarget(playerObj.transform);
    }

    // Araç modeline göre hızı ve dönüşü ayarlıyoruz (Bypass/Buff)
    private void ApplyCarDataBuff()
    {
        if (carData == null) return;

        maxSpeed = carData.maxSpeed * 1.55f;
        acceleration = carData.acceleration * 4.2f;
        turnSpeed = carData.turnSpeed * 1.15f;

        turnResponsiveness = 1.8f;
        turnSmoothing = 12f;
        normalGrip = 18f;
        driftAngleThreshold = 40f;
        predictionTime = 0.2f;
    }

    private void RefreshDifficulty()
    {
        difficultyMultiplier = 1f;
        if (!scaleWithScore || ScoreManager.Instance == null) return;

        int score = ScoreManager.Instance.Score;
        difficultyMultiplier = 1f + score * 0.002f; // Oyuncunun skoru arttıkça AI daha da acımasızlaşır
    }

    private void ZeroVerticalVelocity()
    {
        // Aracın rampalarda veya çarpmalarda saçma sapan havalanmasını engellemek için Y hızını sıfırlarız
        Vector3 vel = rb.linearVelocity;
        vel.y = 0f;
        rb.linearVelocity = vel;
    }

    private void UpdateStuckDetection(float distance)
    {
        // Polisin bir yere takıldığını anlamak için oyuncuya olan mesafesini kontrol ediyoruz
        if (distance > lastDistanceToPlayer + 0.05f)
            stuckTimer += Time.fixedDeltaTime; // Mesafe açılıyorsa demek ki takıldık
        else
            stuckTimer = Mathf.Max(0f, stuckTimer - Time.fixedDeltaTime * 2f);

        lastDistanceToPlayer = distance;
    }

    // --- YAPAY ZEKANIN TEMEL SÜRÜŞ MANTIĞI ---
    private void ChaseTarget(float actualDistanceToPlayer)
    {
        float playerSpeed = 0f;
        Vector3 flatTargetVel = Vector3.zero;
        if (targetRb != null)
        {
            flatTargetVel = targetRb.linearVelocity;
            flatTargetVel.y = 0f; // Sadece X ve Z eksenindeki hızı önemsiyoruz
            playerSpeed = flatTargetVel.magnitude;
        }

        Vector3 toPoliceFromPlayer = transform.position - target.position;
        toPoliceFromPlayer.y = 0f;

        // Dot product ile polisin, oyuncunun önünde mi yoksa arkasında mı olduğunu buluyoruz
        bool isAheadOfPlayer = Vector3.Dot(target.forward, toPoliceFromPlayer) > 0.45f;

        // Hedeflenecek noktayı (Aim Point) hesapla
        Vector3 aimPoint = ComputeAimPoint(isAheadOfPlayer, flatTargetVel, actualDistanceToPlayer);

        ApplySteering(aimPoint, playerSpeed, flatTargetVel, actualDistanceToPlayer);

        float targetSpeed = ComputeTargetSpeed(isAheadOfPlayer, actualDistanceToPlayer, playerSpeed);
        ApplySpeed(targetSpeed, playerSpeed, actualDistanceToPlayer);

        MoveForward();
    }

    private Vector3 ComputeAimPoint(bool isAheadOfPlayer, Vector3 flatTargetVel, float distance)
    {
        // Oyuncunun bulunduğu noktaya gitmek yerine, gideceği yeri tahmin edip önünü kesiyoruz
        float predictionBlend = Mathf.InverseLerp(predictionFadeStart, predictionFadeEnd, distance);
        Vector3 predictedPos = target.position + flatTargetVel * (predictionTime * predictionBlend * difficultyMultiplier);

        if (isAheadOfPlayer)
        {
            float facingDot = Vector3.Dot(transform.forward, target.forward);

            if (facingDot > 0f)
            {
                return transform.position + transform.forward * 15f;
            }
            else
            {
                float playerSpeed = targetRb != null ? targetRb.linearVelocity.magnitude : 0f;
                return target.position + target.forward * (playerSpeed * 0.2f);
            }
        }

        // Polisler oyuncuyu takip ederken dümdüz bir çizgi yerine hafif sağa veya sola kaysınlar
        float lateralFactor = Mathf.Clamp01((distance - followBufferDistance) / ramCloseDistance);
        float effectiveOffset = randomLateralOffset * lateralFactor;

        return predictedPos - target.forward * 1.2f + target.right * effectiveOffset;
    }

    private float ComputeTargetSpeed(bool isAheadOfPlayer, float distance, float playerSpeed)
    {
        // Mesafe açıldıkça polis hızını artırıp yetişmeye çalışır (Rubberbanding mantığı)
        float targetSpeed = playerSpeed + (distance - followBufferDistance) * catchUpGain * difficultyMultiplier;

        if (isAheadOfPlayer)
        {
            float facingDot = Vector3.Dot(transform.forward, target.forward);

            if (facingDot > 0f)
                targetSpeed = playerSpeed * 0.9f; // Oyuncunun önündeysek hızı düşür ki geçsin
            else
                targetSpeed = maxSpeed * difficultyMultiplier; // Kafa kafaya geliyorsak tam gaz
        }
        else
        {
            // Yaklaşınca hız bonusu alıp oyuncuya sert çarpmasını sağlıyoruz
            if (distance < ramCloseDistance)
            {
                float closeBonus = Mathf.Lerp(1.5f, ramSpeedBonus * difficultyMultiplier, distance / ramCloseDistance);
                targetSpeed = Mathf.Max(targetSpeed, playerSpeed + closeBonus);
            }
        }

        float pursuitLimit = Mathf.Max(maxSpeed * difficultyMultiplier, playerSpeed + ramSpeedBonus + 2f);
        targetSpeed = Mathf.Clamp(targetSpeed, 0f, pursuitLimit);

        if (isDrifting && distance > emergencyStopDistance)
            targetSpeed = Mathf.Max(targetSpeed, playerSpeed * driftSpeedFloorFactor);

        return targetSpeed;
    }

    private void ApplySteering(Vector3 aimPoint, float playerSpeed, Vector3 flatTargetVel, float distance)
    {
        Vector3 toAimPoint = aimPoint - transform.position;
        toAimPoint.y = 0f;
        Vector3 dirToAimPoint = toAimPoint.sqrMagnitude > 0.0025f ? toAimPoint.normalized : transform.forward;

        float angleToAimPoint = Vector3.SignedAngle(transform.forward, dirToAimPoint, Vector3.up);

        bool playerDrift = DetectPlayerDrift(playerSpeed, flatTargetVel);
        bool selfDrift = Mathf.Abs(angleToAimPoint) > driftAngleThreshold && currentSpeed > driftMinSpeed;

        // Kendisi dönmek zorundaysa, oyuncu drift yapıyorsa veya takılı kaldıysa drift durumuna geç
        isDrifting = selfDrift || playerDrift || (stuckTimer > 1.2f && distance > ramCloseDistance);

        float effectiveTurnSpeed = turnSpeed;
        float effectiveResponsiveness = turnResponsiveness;
        if (isDrifting)
        {
            effectiveTurnSpeed *= 1.2f;
            effectiveResponsiveness *= 1.1f;
        }

        float angleMagnitude = Mathf.Abs(angleToAimPoint);
        float smoothFactor = Mathf.Clamp01(angleMagnitude / 12f); // Dönüşü yumuşatıyoruz ki araç robot gibi dönmesin
        float desiredTurnRate = Mathf.Clamp(angleToAimPoint * effectiveResponsiveness * smoothFactor, -effectiveTurnSpeed, effectiveTurnSpeed);

        // Eğer önümüzde bir engel (bina vs.) varsa dönüş yönüne ekstra müdahale et (Obstacle Avoidance)
        desiredTurnRate += ObstacleAvoidanceSteer() * 0.2f;

        float appliedSmoothing = isDrifting ? driftEntrySmoothing : turnSmoothing;
        currentTurnRate = Mathf.Lerp(currentTurnRate, desiredTurnRate, appliedSmoothing * Time.fixedDeltaTime);

        float speedFactor = Mathf.Clamp01(Mathf.Abs(currentSpeed) / fullTurnSpeedThreshold);
        float appliedTurnRate = currentTurnRate * Mathf.Lerp(0.4f, 1f, speedFactor);

        Quaternion turnRotation = Quaternion.Euler(0f, appliedTurnRate * Time.fixedDeltaTime, 0f);
        rb.MoveRotation(rb.rotation * turnRotation);
    }

    private bool DetectPlayerDrift(float playerSpeed, Vector3 flatTargetVel)
    {
        if (usePlayerDriftInputOverride && playerDriftInputActive) return true;
        if (playerSpeed <= playerDriftMinSpeed) return false;

        Vector3 playerForwardFlat = target.forward;
        playerForwardFlat.y = 0f;
        if (playerForwardFlat.sqrMagnitude < 0.0001f) return false;

        // Oyuncunun baktığı yön ile gittiği yön (Velocity) arasındaki açıya bakarak drift yapıp yapmadığını anlıyoruz
        float slipAngle = Vector3.Angle(playerForwardFlat.normalized, flatTargetVel.normalized);
        return slipAngle > playerDriftDetectAngle;
    }

    private void ApplySpeed(float targetSpeed, float playerSpeed, float distance)
    {
        float effectiveAcceleration = acceleration * difficultyMultiplier;

        if (scaleWithScore && ScoreManager.Instance != null)
            effectiveAcceleration *= 1f + ScoreManager.Instance.Score * scoreAccelScale;

        if (isDrifting || isRecovering)
            effectiveAcceleration *= driftAccelerationMultiplier;

        // Hedef hıza yumuşak bir şekilde ulaşmak için MoveTowards kullanıyoruz
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, effectiveAcceleration * Time.fixedDeltaTime);
    }

    private void MoveForward()
    {
        float grip = isDrifting ? driftGrip : normalGrip;

        // Slerp ile aracın burnunun gösterdiği yöne doğru hareketini pürüzsüzleştiriyoruz
        currentMoveDir = Vector3.Slerp(currentMoveDir, transform.forward, grip * Time.fixedDeltaTime).normalized;

        Vector3 movement = currentMoveDir * currentSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movement);
    }

    // Polisin binalara veya engellere bodoslama girmemesi için etrafa ışın (Raycast) atarak yön saptırır
    private float ObstacleAvoidanceSteer()
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        float steer = 0f;
        int hits = 0;

        // İleri, sağ çapraz ve sol çapraz olmak üzere 3 SphereCast yolluyoruz
        TryObstacleRay(origin, transform.forward, 1.2f, ref steer, ref hits);
        TryObstacleRay(origin, (transform.forward + transform.right * 0.45f).normalized, 0.9f, ref steer, ref hits);
        TryObstacleRay(origin, (transform.forward - transform.right * 0.45f).normalized, 0.9f, ref steer, ref hits);

        return hits > 0 ? steer / hits : 0f;
    }

    private void TryObstacleRay(Vector3 origin, Vector3 direction, float radius, ref float steer, ref int hits)
    {
        if (!Physics.SphereCast(origin, radius, direction, out RaycastHit hit, obstacleCheckDistance, obstacleLayerMask))
            return;

        // Oyuncuya, yola veya yere çarpıyorsa tepki verme
        if (hit.collider.CompareTag("Player") || hit.collider.CompareTag("Road") || hit.collider.CompareTag("Ground")) return;

        // Engel sağdaysa sola kır, soldaysa sağa kır
        Vector3 localHit = transform.InverseTransformPoint(hit.point);
        steer += localHit.x < 0f ? 20f : -20f;
        hits++;
    }

    private IEnumerator RecoverRoutine()
    {
        isRecovering = true;
        yield return new WaitForSeconds(recoverDuration);
        isRecovering = false;
    }

    public void Stun(float duration)
    {
        if (isStunned) return;
        StartCoroutine(StunRoutine(duration));
    }

    private IEnumerator StunRoutine(float duration)
    {
        isStunned = true;
        currentSpeed = 0f;
        yield return new WaitForSeconds(duration);
        isStunned = false;
    }

    // İki obje arasındaki uzaklığı ölçerken Y eksenini(yüksekliği) umursamayarak daha tutarlı bir değer döndürür
    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}