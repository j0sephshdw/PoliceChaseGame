using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PoliceCarAI : MonoBehaviour
{
    [Header("Araç Verisi")]
    public CarData carData;

    [Header("Spawn Koruma")]
    [Tooltip("Araç aktifleştiğinde anında patlamasını önleyen koruma süresi")]
    public float spawnInvulnerabilityDuration = 3f;
    private float spawnTime;

    [Header("Hedef (Player)")]
    public Transform target;

    [Header("Takip Dinamikleri")]
    public float followBufferDistance = 1.2f;
    public float catchUpGain = 2.8f;
    public float ramSpeedBonus = 7f;
    public float ramCloseDistance = 12f;
    public float ramPushForce = 18f;

    [Header("Tahmin (Prediction)")]
    public float predictionTime = 0.65f;
    public float predictionFadeStart = 4f;
    public float predictionFadeEnd = 22f;

    [Header("Fizik & Hız")]
    public float maxSpeed = 20f;
    public float acceleration = 9f;
    public float collisionMass = 400f;
    public float emergencyStopDistance = 1.4f;
    public float recoverDuration = 0.35f;
    public float downforceMultiplier = 25f;

    [Header("Manevra Ayarları")]
    public float turnSpeed = 55f;
    public float turnResponsiveness = 3.2f;
    public float turnSmoothing = 10f;
    public float driftEntrySmoothing = 30f;
    public float fullTurnSpeedThreshold = 3f;

    [Header("Drift Ayarları")]
    public float driftAngleThreshold = 18f;
    public float driftMinSpeed = 5f;
    public float normalGrip = 7f;
    public float driftGrip = 1.4f;
    public float driftTurnSpeedMultiplier = 1.8f;
    public float driftTurnResponsivenessMultiplier = 1.5f;
    [Range(0f, 1f)] public float driftSpeedFloorFactor = 0.8f;
    public float driftAccelerationMultiplier = 2.4f;

    [Header("Player Drift Senkronizasyonu")]
    public bool usePlayerDriftInputOverride = true;
    public float playerDriftDetectAngle = 14f;
    public float playerDriftMinSpeed = 3f;

    [Header("Savrulma Sınırları")]
    [Tooltip("Aracın burnu ile gerçekte gittiği yön arasındaki maksimum açı; bunun üstünde yanlamasına savrulamaz")]
    public float maxSlideAngle = 45f;
    [Tooltip("Tam yanlamasına kayarken hızın kaça düşeceği (1 = hiç yavaşlamaz)")]
    [Range(0.2f, 1f)] public float slideSpeedLoss = 0.55f;

    [Header("Duvar Çarpışması")]
    [Tooltip("Kafa kafaya duvara çarpınca aracın geri sekme hızı")]
    public float wallBounceSpeed = 3f;
    [Tooltip("Duvara dayanıp kaldığında kaç saniye sonra patlayacağı")]
    public float wallStuckDuration = 1.5f;
    private float wallStuckTimer; // Duvara dayalı geçirdiği süre
    [Tooltip("Binayı gördükten sonra seçtiği kaçış yönüne kaç saniye kilitli kalsın")]
    public float wallFollowDuration = 1.2f;
    [Tooltip("Binaları kaç metre önceden görüp kaçış yönü seçsin")]
    public float wallDetectDistance = 6f;
    private Vector3 wallFollowDir;  // Binanın etrafından dolaşmak için seçilen kaçış yönü
    private float wallFollowTimer;  // Kaçış yönü kilidinin kalan süresi

    [Header("Çevre Algılama")]
    [Tooltip("Binaların bulunduğu katman")]
    public LayerMask obstacleLayerMask;

    [Header("Araç Kaçınma")]
    [Tooltip("Trafik ve polis araçlarının bulunduğu katman (genelde Default)")]
    public LayerMask vehicleLayerMask;
    [Tooltip("Öndeki araçları kaç metre önceden görüp kaçınmaya başlasın")]
    public float vehicleCheckDistance = 3f;
    [Tooltip("Araçlardan kaçınırken uygulanacak dönüş gücü (derece/saniye)")]
    public float vehicleAvoidStrength = 60f;

    private readonly RaycastHit[] vehicleHits = new RaycastHit[8]; // Her karede dizi oluşturmamak için önbelleğe alındı

    [Header("Zorluk Çarpanları")]
    public bool scaleWithScore = true;
    public float scoreSpeedScale = 0.004f;
    public float scoreAccelScale = 0.006f;

    [Header("VFX & SFX")]
    public Transform carMesh;
    public GameObject explosionVFX;
    public AudioClip explosionSound;
    public float patlamaGucu = 10f;
    public float patlamaYaricapi = 8f;
    public float havayaFirlatmaGucu = 0.5f;
    public AudioClip sirenSound;
    [Tooltip("Sirenin tam sesle duyulacağı mesafe")]
    public float sirenMinDistance = 8f;
    [Tooltip("Sirenin tamamen duyulmaz olacağı mesafe")]
    public float sirenMaxDistance = 60f;

    [Header("Ödül Ayarları")]
    public int xpReward = 25; // Bu araç patlayınca oyuncuya verilecek XP miktarı
    public float xpZoneRadius = 20f; // Oyuncuya bu mesafeden yakın patlarsa XP verilir, uzaktaki patlamalar sayılmaz

    [Header("Kamera Sarsıntısı")]
    [Tooltip("Patlamanın kamerayı sarsacağı en uzak mesafe")]
    public float shakeDistance = 12f;
    [Tooltip("Tam dibinde patladığında uygulanacak sarsıntı şiddeti")]
    public float explosionShakeForce = 1.2f;

    // --- Private Değişkenler ---
    private AudioSource audioSource;
    private Rigidbody rb;
    private Rigidbody targetRb;

    private float currentSpeed;
    private float currentTurnRate;
    private Vector3 currentMoveDir;
    private float randomLateralOffset;

    private bool playerDriftInputActive;
    private bool isDrifting;
    private bool isStunned;
    private bool isRecovering;
    private bool isDead = false;
    [HideInInspector] public int poolGeneration = 0;

    [Header("Çarpma Hakkı")]
    [Tooltip("Oyuncuyla kaç kez çarpıştıktan sonra patlar")]
    public int playerHitPoints = 1;
    [Tooltip("Çevreye (duvar, trafik, diğer polis) kaç kez hafifçe çarptıktan sonra patlar")]
    public int environmentHitPoints = 3;
    [Tooltip("Bu sertliğin üstündeki çarpmalar aracı tek seferde patlatır; altındakiler 1 hak eksiltir")]
    public float environmentDamageSpeed = 12f;

    private int currentPlayerHits;      // Oyuncudan kalan çarpma hakkı
    private int currentEnvironmentHits; // Çevreden kalan çarpma hakkı

    private float stuckTimer;
    private Vector3 lastPosition; // Gerçek sıkışma tespiti için bir önceki fizik karesindeki konum
    private float difficultyMultiplier = 1f;
    private BoxCollider bodyCollider; // Tarama (BoxCast) için aracın çarpışma kutusu

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        bodyCollider = GetComponent<BoxCollider>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        rb.centerOfMass = new Vector3(0f, -0.6f, 0f);
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.mass = collisionMass;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;

        // Siren sesini 3B yapıyoruz: prefablardaki AudioSource'lar varsayılan (2B) ayarda geldiği için
        // sahnedeki bütün polislerin sireni, ne kadar uzakta olurlarsa olsunlar tam sesle çalıyordu.
        audioSource.spatialBlend = 1f;                     // 1 = tamamen 3B, mesafeye göre kısılır
        audioSource.rolloffMode = AudioRolloffMode.Linear; // Uzaklaştıkça düzgün şekilde azalsın
        audioSource.minDistance = sirenMinDistance;        // Bu mesafeye kadar tam ses
        audioSource.maxDistance = sirenMaxDistance;        // Bu mesafeden sonra hiç duyulmaz
        audioSource.dopplerLevel = 0f;                     // Hızlı geçişlerde ses tizleşip bozulmasın

        currentMoveDir = transform.forward;
    }

    private void OnEnable()
    {
        isDead = false;
        spawnTime = Time.time;
        // Havuzdan tekrar kullanılan araçların haklarını Inspector'daki değerlerden sıfırlıyoruz
        currentPlayerHits = playerHitPoints;
        currentEnvironmentHits = environmentHitPoints;
        isStunned = false;
        isRecovering = false;
        isDrifting = false;
        stuckTimer = 0f;
        lastPosition = transform.position;
        wallStuckTimer = 0f;
        wallFollowTimer = 0f;

        Collider mainCollider = GetComponent<Collider>();
        if (mainCollider != null)
        {
            mainCollider.enabled = true;
        }
        this.enabled = true;
    }

    private void Reset()
    {
        if (carMesh == null && transform.childCount > 0)
        {
            carMesh = transform.GetChild(0);
        }
    }

    private void Start()
    {
        ResolveTarget();

        if (sirenSound != null)
        {
            audioSource.clip = sirenSound;
            audioSource.volume = GameUIManager.GetGameVolume();
            audioSource.Play();
        }

        ApplyCarDataBuff();
        randomLateralOffset = Random.Range(-2.5f, 2.5f);
        RefreshDifficulty();
    }

    private void Update()
    {
        if (isDead) return;

        if (audioSource != null)
            audioSource.volume = GameUIManager.GetGameVolume();

        if (Time.frameCount % 30 == 0)
            RefreshDifficulty();
    }

    private void FixedUpdate()
    {
        if (target == null || isDead) return;

        ApplyDownforce();

        if (isStunned) return;

        float distance = FlatDistance(transform.position, target.position);

        if (distance > 120f)
        {
            gameObject.SetActive(false);
            return;
        }

        UpdateStuckDetection();
        ChaseTarget(distance);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isDead) return;

        if (collision.gameObject.CompareTag("Obstacle") || collision.gameObject.CompareTag("Traffic") || collision.gameObject.CompareTag("Police"))
        {
            if (collision.contactCount == 0) return;

            Vector3 wallNormal = collision.contacts[0].normal;

            // Araç MovePosition ile hareket ettiği için Rigidbody'nin gerçek hızı (relativeVelocity)
            // neredeyse sıfır kalıyor; bu yüzden çarpmanın sertliğini AI'nın kendi hızından hesaplıyoruz.
            float impactAlignment = Mathf.Abs(Vector3.Dot(currentMoveDir, wallNormal)); // 1 = tam dik çarpma, 0 = teğet sıyırma
            float envImpactSpeed = Mathf.Abs(currentSpeed) * impactAlignment;           // Sertlik = hız × çarpmanın dikliği

            // Sert çarpma: hak beklemeden tek seferde patlasın
            if (envImpactSpeed >= environmentDamageSpeed)
            {
                Explode();
                return;
            }

            // Hafif temas: çevre haklarından biri gitsin, hakkı bitince patlasın
            TakeHit(false);
            if (isDead) return; // Hakkı bitip patladıysa aşağıdaki sekme kısmı boşuna çalışmasın

            // Sadece binalar/duvarlar için sekme tepkisi uyguluyoruz
            if (collision.gameObject.CompareTag("Obstacle"))
            {
                Vector3 flatNormal = new Vector3(wallNormal.x, 0f, wallNormal.z).normalized; // Duvarın yatay yüzey yönü
                if (flatNormal.sqrMagnitude < 0.01f) return; // Tamamen dikey temas (üstüne çıkma), sekme uygulamıyoruz

                float hitAngle = Vector3.Dot(transform.forward, -flatNormal); // 1'e yakınsa kafa kafaya, 0'a yakınsa sürtme

                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                if (hitAngle > 0.4f)
                {
                    // Kafa kafaya: dönebilecek yer açmak için biraz geri seksin
                    currentSpeed = -wallBounceSpeed;
                }
                else
                {
                    // Yandan sürtme: sadece yavaşlasın. Hareket yönünü burada DEĞİŞTİRMİYORUZ;
                    // yansıtma (Vector3.Reflect) aracı rastgele yönlere savurup sapıtmasına sebep oluyordu.
                    currentSpeed *= 0.6f;
                }
            }
            else
            {
                // Trafik veya diğer polis: fizik motoru itişmeyi kendi hallediyor, sadece hız kaybı uyguluyoruz
                currentSpeed *= 0.85f;
            }

            return;
        }

        if (!collision.gameObject.CompareTag("Player")) return;
        // TARGET NULL KONTROLÜ (Barikat polisleri için kaza koruması)
        if (target == null)
        {
            ResolveTarget();
            if (target == null) return;
        }

        float headOnDot = Vector3.Dot(transform.forward, target.forward);
        float impactSpeed = collision.relativeVelocity.magnitude;

        if (headOnDot < -0.4f && impactSpeed > 15f)
        {
            Explode();
            return;
        }

        float playerSpeed = targetRb != null ? targetRb.linearVelocity.magnitude : 0f;
        currentSpeed = Mathf.Max(currentSpeed * 0.94f, playerSpeed * 0.88f);
        TakeHit(true);
        if (isDead) return; // hak bitip patladıysa devamındaki itme kısmı çalışmasın

        if (collision.contactCount == 0) return;

        Vector3 contactNormal = collision.contacts[0].normal;
        contactNormal.y = 0f;
        if (contactNormal.sqrMagnitude < 0.01f) return;

        Vector3 pushDir = -contactNormal.normalized;
        float approach = Vector3.Dot(transform.forward, pushDir);

        if (approach > 0.25f && targetRb != null)
        {
            targetRb.AddForce(pushDir * ramPushForce * difficultyMultiplier, ForceMode.Impulse);
        }

        if (!isRecovering)
            StartCoroutine(RecoverRoutine());
    }

    // fromPlayer = true ise oyuncuyla çarpışma, false ise çevreye (duvar/trafik/polis) çarpma demek
    private void TakeHit(bool fromPlayer)
    {
        if (Time.time < spawnTime + spawnInvulnerabilityDuration) return; // spawn koruması sırasında hak harcanmasın

        if (fromPlayer)
            currentPlayerHits--;      // Oyuncu kaynaklı hasar kendi havuzundan düşer
        else
            currentEnvironmentHits--; // Çevre kaynaklı hasar ayrı havuzdan düşer

        // İki havuzdan herhangi biri tükendiğinde araç patlar
        if (currentPlayerHits <= 0 || currentEnvironmentHits <= 0)
        {
            Explode();
        }
    }

    public void Explode()
    {
        if (isDead) return;

        if (Time.time < spawnTime + spawnInvulnerabilityDuration)
            return;

        isDead = true;

        // Oyuncuya olan mesafeyi bir kez hesaplayıp hem ödül hem sarsıntı için kullanıyoruz
        float distanceToPlayer = target != null ? FlatDistance(transform.position, target.position) : float.MaxValue;

        // Patlama oyuncuya yakın bir alanda mı oldu diye bakıyoruz;
        // sadece bu alanın (xpZoneRadius) içinde patlarsa oyuncuyu ödüllendiriyoruz
        bool isInsideXPZone = distanceToPlayer <= xpZoneRadius;

        // Tek şart: patlama oyuncuya yakın alanda (xpZoneRadius) gerçekleşmiş olsun
        if (isInsideXPZone && ScoreManager.Instance != null)
        {
            int starMultiplier = WantedManager.Instance != null ? WantedManager.Instance.CurrentStars : 1;
            ScoreManager.Instance.AddXP(xpReward * starMultiplier);
        }

        // Yakında gerçekleşen patlamalar kamerayı sarsıyor; uzaklaştıkça sarsıntı zayıflıyor,
        // böylece haritanın öbür ucundaki patlamalar oyuncuyu rahatsız etmiyor.
        if (distanceToPlayer <= shakeDistance)
        {
            float closeness = 1f - Mathf.Clamp01(distanceToPlayer / shakeDistance);
            CameraShake.Shake(explosionShakeForce * closeness);
        }

        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();

        if (explosionSound != null)
            AudioSource.PlayClipAtPoint(explosionSound, transform.position, GameUIManager.GetGameVolume());

        if (explosionVFX != null)
        {
            GameObject vfx = Instantiate(explosionVFX, transform.position, Quaternion.identity);
            Destroy(vfx, 3f);
        }

        MeshRenderer[] allParts = GetComponentsInChildren<MeshRenderer>();

        foreach (MeshRenderer meshPart in allParts)
        {
            GameObject flyingPart = new GameObject(meshPart.name + "_FlyingPiece");
            flyingPart.transform.position = meshPart.transform.position;
            flyingPart.transform.rotation = meshPart.transform.rotation;
            flyingPart.transform.localScale = meshPart.transform.lossyScale;

            MeshFilter originalFilter = meshPart.GetComponent<MeshFilter>();
            if (originalFilter != null)
            {
                flyingPart.AddComponent<MeshFilter>().sharedMesh = originalFilter.sharedMesh;
                flyingPart.AddComponent<MeshRenderer>().sharedMaterials = meshPart.sharedMaterials;
            }

            flyingPart.AddComponent<BoxCollider>();
            Rigidbody partRb = flyingPart.AddComponent<Rigidbody>();
            partRb.mass = 1.5f;
            partRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            partRb.AddExplosionForce(patlamaGucu, transform.position, patlamaYaricapi, havayaFirlatmaGucu, ForceMode.Impulse);
            partRb.AddTorque(Random.insideUnitSphere * 15f, ForceMode.Impulse);

            Destroy(flyingPart, 5f);
        }

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
        difficultyMultiplier = 1f + score * 0.002f;
    }

    private void ApplyDownforce()
    {
        rb.AddForce(Vector3.down * downforceMultiplier * rb.mass, ForceMode.Force);

        Vector3 vel = rb.linearVelocity;
        if (vel.y > 1f)
        {
            vel.y = Mathf.Lerp(vel.y, 0f, Time.fixedDeltaTime * 10f);
            rb.linearVelocity = vel;
        }
    }

    private void UpdateStuckDetection()
    {
        // Gerçek sıkışma: araç hızlanmak istediği halde yerinden kımıldayamıyor demektir.
        // (Eskiden "oyuncudan uzaklaşıyor" sıkışma sayılıyordu; bu, geride kalan her aracı
        //  yanlışlıkla drift moduna sokup savrulmasına yol açıyordu.)
        float movedThisStep = Vector3.Distance(transform.position, lastPosition); // Bu karede gerçekten kat ettiği yol
        float expectedMove = currentSpeed * Time.fixedDeltaTime;                  // Kat etmesi gereken yol

        // Gitmek istiyor ama beklenenin %30'undan azını kat edebiliyorsa gerçekten sıkışmıştır
        if (currentSpeed > 2f && movedThisStep < expectedMove * 0.3f)
            stuckTimer += Time.fixedDeltaTime;
        else
            stuckTimer = Mathf.Max(0f, stuckTimer - Time.fixedDeltaTime * 2f);

        lastPosition = transform.position;
    }

    private void ChaseTarget(float actualDistanceToPlayer)
    {
        float playerSpeed = 0f;
        Vector3 flatTargetVel = Vector3.zero;
        if (targetRb != null)
        {
            flatTargetVel = targetRb.linearVelocity;
            flatTargetVel.y = 0f;
            playerSpeed = flatTargetVel.magnitude;
        }

        Vector3 toPoliceFromPlayer = transform.position - target.position;
        toPoliceFromPlayer.y = 0f;

        // Yönü normalize ediyoruz: normalize edilmezse çarpım mesafeyle birlikte büyüyor ve
        // oyuncunun yanındaki uzak bir araç bile "önde" sayılıyordu. Normalize edilince eşik,
        // gerçek bir açı ölçüsüne dönüşüyor (0.45 ≈ oyuncunun burnundan 63 derecelik koni).
        Vector3 dirToPolice = toPoliceFromPlayer.sqrMagnitude > 0.01f ? toPoliceFromPlayer.normalized : target.forward;
        bool isAheadOfPlayer = Vector3.Dot(target.forward, dirToPolice) > 0.45f;

        Vector3 aimPoint = ComputeAimPoint(isAheadOfPlayer, flatTargetVel, actualDistanceToPlayer);

        UpdateWallAvoidance(); // Önümüzdeki binayı çarpmadan önce görüp kaçış yönü seçiyoruz

        ApplySteering(aimPoint, playerSpeed, flatTargetVel, actualDistanceToPlayer);

        float targetSpeed = ComputeTargetSpeed(isAheadOfPlayer, actualDistanceToPlayer, playerSpeed);
        ApplySpeed(targetSpeed, playerSpeed, actualDistanceToPlayer);

        MoveForward();
    }

    // Önümüzde bina var mı diye bakar; varsa etrafından dolaşacak bir kaçış yönü seçip kilitler.
    // Kilit süresince yön YENİDEN HESAPLANMAZ — her karede yeniden hesaplamak, araç döndükçe
    // hesabın da değişmesine ve aracın iki yön arasında gidip gelip sapıtmasına yol açıyordu.
    private void UpdateWallAvoidance()
    {
        if (bodyCollider == null || target == null) return;

        Vector3 boxCenter = transform.TransformPoint(bodyCollider.center);
        Vector3 halfExtents = Vector3.Scale(bodyCollider.size, transform.lossyScale) * 0.5f;

        // Kilit aktifse yönü değiştirmiyoruz, sadece süreyi işletiyoruz
        if (wallFollowTimer > 0f)
        {
            wallFollowTimer -= Time.fixedDeltaTime;

            // Oyuncuya giden yol açıldıysa kilidi erken bırak, gereksiz yere duvar boyunca sürmesin
            Vector3 toPlayerDir = target.position - transform.position;
            toPlayerDir.y = 0f;
            if (toPlayerDir.sqrMagnitude > 1f)
            {
                toPlayerDir.Normalize();
                if (!Physics.BoxCast(boxCenter, halfExtents, toPlayerDir, out _,
                                     transform.rotation, wallDetectDistance, obstacleLayerMask, QueryTriggerInteraction.Ignore))
                {
                    wallFollowTimer = 0f;
                }
            }
            return;
        }

        // Önümüzde bina var mı?
        if (!Physics.BoxCast(boxCenter, halfExtents, transform.forward, out RaycastHit hit,
                             transform.rotation, wallDetectDistance, obstacleLayerMask, QueryTriggerInteraction.Ignore))
            return;

        Vector3 wallNormal = hit.normal;
        wallNormal.y = 0f;
        if (wallNormal.sqrMagnitude < 0.01f) return; // Tavan/zemin gibi yatay bir yüzey, kaçış yönü çıkmaz
        wallNormal.Normalize();

        Vector3 tangent = Vector3.Cross(Vector3.up, wallNormal); // Duvar yüzeyine paralel yön

        // Duvar boyunca iki yön var (sağ ve sol); oyuncuya doğru olanı seçiyoruz ki binayı
        // doğru taraftan dolaşsın, ters tarafa gidip kovalamacayı kaybetmesin
        Vector3 toPlayer = target.position - transform.position;
        toPlayer.y = 0f;

        wallFollowDir = Vector3.Dot(tangent, toPlayer) >= 0f ? tangent : -tangent;
        wallFollowTimer = wallFollowDuration;
    }

    private Vector3 ComputeAimPoint(bool isAheadOfPlayer, Vector3 flatTargetVel, float distance)
    {
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

        if (distance < ramCloseDistance)
        {
            return predictedPos; // yakınken tam üzerine gitsin, çarpmaya çalışsın
        }

        float lateralFactor = Mathf.InverseLerp(5f, 15f, distance);
        float effectiveOffset = randomLateralOffset * lateralFactor;

        return predictedPos - target.forward * 1.2f + target.right * effectiveOffset;
    }

    private float ComputeTargetSpeed(bool isAheadOfPlayer, float distance, float playerSpeed)
    {
        float targetSpeed = playerSpeed + (distance - followBufferDistance) * catchUpGain * difficultyMultiplier;

        if (isAheadOfPlayer)
        {
            float facingDot = Vector3.Dot(transform.forward, target.forward);

            if (facingDot > 0f)
                targetSpeed = playerSpeed * 0.9f;
            else
                targetSpeed = maxSpeed * difficultyMultiplier;
        }
        else
        {
            if (distance < ramCloseDistance)
            {
                float closeBonus = Mathf.Lerp(ramSpeedBonus * difficultyMultiplier, 1.5f, distance / ramCloseDistance);
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
        // Bina kaçınması aktifse oyuncuya değil, seçilmiş kaçış yönüne dönüyoruz.
        // Sadece direksiyona müdahale ediyoruz; hareket yönüne (currentMoveDir) dokunmuyoruz.
        if (wallFollowTimer > 0f)
        {
            aimPoint = transform.position + wallFollowDir * 10f;
        }

        Vector3 toAimPoint = aimPoint - transform.position;
        toAimPoint.y = 0f;
        Vector3 dirToAimPoint = toAimPoint.sqrMagnitude > 0.0025f ? toAimPoint.normalized : transform.forward;

        float angleToAimPoint = Vector3.SignedAngle(transform.forward, dirToAimPoint, Vector3.up);

        // Açı farkı ve hız yeterliyse normal viraj/drift durumu
        bool selfDrift = Mathf.Abs(angleToAimPoint) > driftAngleThreshold && currentSpeed > driftMinSpeed;
        // Artık mesafeye bakılmaksızın çalışıyor, yakın mesafede sıkışan araçlar da kurtulabiliyor
        isDrifting = selfDrift || stuckTimer > 1.2f;

        float effectiveTurnSpeed = turnSpeed;
        float effectiveResponsiveness = turnResponsiveness;
        if (isDrifting)
        {
            effectiveTurnSpeed *= driftTurnSpeedMultiplier;
            effectiveResponsiveness *= driftTurnResponsivenessMultiplier;
        }

        float angleMagnitude = Mathf.Abs(angleToAimPoint);
        float smoothFactor = Mathf.Clamp01(angleMagnitude / 12f);
        float desiredTurnRate = Mathf.Clamp(angleToAimPoint * effectiveResponsiveness * smoothFactor, -effectiveTurnSpeed, effectiveTurnSpeed);

        desiredTurnRate += VehicleAvoidanceSteer();

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

        float slipAngle = Vector3.Angle(playerForwardFlat.normalized, flatTargetVel.normalized);
        return slipAngle > playerDriftDetectAngle;
    }

    private void ApplySpeed(float targetSpeed, float playerSpeed, float distance)
    {
        float effectiveAcceleration = acceleration * difficultyMultiplier;

        if (scaleWithScore && ScoreManager.Instance != null)
            effectiveAcceleration *= 1f + ScoreManager.Instance.Score * scoreAccelScale;

        // OYUNCU DRİFT YAPIYORSA AGRESİFLEŞ (Ama füzeye dönüşme)
        if (DetectPlayerDrift(playerSpeed, targetRb != null ? targetRb.linearVelocity : Vector3.zero))
        {
            // Körü körüne maksimum hıza çıkmak yerine, oyuncudan kontrollü bir şekilde daha hızlı ol
            targetSpeed = Mathf.Max(targetSpeed, playerSpeed + 6f * difficultyMultiplier);
            effectiveAcceleration *= 1.25f;
        }

        if (isDrifting || isRecovering)
            effectiveAcceleration *= driftAccelerationMultiplier;

        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, effectiveAcceleration * Time.fixedDeltaTime);
    }

    private void MoveForward()
    {
        float grip = isDrifting ? driftGrip : normalGrip;

        // DİNAMİK YOL TUTUŞU: Yüksek hızlarda aracın buzda kayar gibi uçmasını önlemek için grip'i toparlıyoruz.
        if (isDrifting && currentSpeed > 10f)
        {
            float maxPossibleSpeed = maxSpeed * difficultyMultiplier;
            float speedFactor = Mathf.Clamp01((currentSpeed - 10f) / maxPossibleSpeed);
            // Araç hızlandıkça yol tutuşu (grip) yavaşça normalGrip seviyesinin %65'ine kadar geri toparlanır
            grip = Mathf.Lerp(driftGrip, normalGrip * 0.65f, speedFactor);
        }

        currentMoveDir = Vector3.Slerp(currentMoveDir, transform.forward, grip * Time.fixedDeltaTime).normalized;

        // SAVRULMA SINIRI: gerçek hareket yönü, aracın burnundan maxSlideAngle dereceden fazla
        // ayrılamaz. Bu sınır olmadan araç tamamen yanlamasına dönüp uzağa uçabiliyordu.
        float slideAngle = Vector3.Angle(currentMoveDir, transform.forward);
        if (slideAngle > maxSlideAngle)
        {
            float fazlaAci = (slideAngle - maxSlideAngle) * Mathf.Deg2Rad; // Sınırı aşan kısmı radyana çeviriyoruz
            currentMoveDir = Vector3.RotateTowards(currentMoveDir, transform.forward, fazlaAci, 0f).normalized;
        }

        // Yanlamasına kayarken gerçek araçlar gibi hız kaybetsin; burnu tam ileriyken tam hız
        float forwardAlignment = Mathf.Clamp01(Vector3.Dot(currentMoveDir, transform.forward));
        float effectiveSpeed = currentSpeed * Mathf.Lerp(slideSpeedLoss, 1f, forwardAlignment);

        Vector3 movement = currentMoveDir * effectiveSpeed * Time.fixedDeltaTime;

        float moveDistance = movement.magnitude;
        if (moveDistance > 0.01f && bodyCollider != null)
        {
            Vector3 moveDir = movement / moveDistance; // Hareketin yönü (geri sekerken geri yönü olur)
            Vector3 boxCenter = transform.TransformPoint(bodyCollider.center);
            Vector3 halfExtents = Vector3.Scale(bodyCollider.size, transform.lossyScale) * 0.5f;

            // Buranın tek işi tünellemeyi engellemek: binaya girecek kadar uzun bir adım atılırsa
            // hareketi binanın hemen önünde kesiyoruz. Yön değiştirme işi UpdateWallAvoidance'ın.
            // BoxCast kullanıyoruz çünkü layer maskesi alabiliyor; böylece sadece binalar engel sayılıyor.
            if (Physics.BoxCast(boxCenter, halfExtents, moveDir, out RaycastHit sweepHit,
                                transform.rotation, moveDistance, obstacleLayerMask, QueryTriggerInteraction.Ignore))
            {
                movement = moveDir * Mathf.Max(0f, sweepHit.distance - 0.05f); // Duvarın 5 cm önünde dur
                wallStuckTimer += Time.fixedDeltaTime;
            }
            else
            {
                wallStuckTimer = 0f; // Önü açık, sıkışma sayacını sıfırlıyoruz
            }
        }

        // Belirlenen süre boyunca duvardan kurtulamadıysa aracı kaza yapmış sayıp patlatıyoruz
        if (wallStuckTimer >= wallStuckDuration)
        {
            Explode();
            return;
        }

        // Hesaplanan hareketi araca uygula
        rb.MovePosition(rb.position + movement);
    }

    // Öndeki trafik ve polis araçlarından kaçınmak için bir dönüş miktarı üretir.
    // Binaları UpdateWallAvoidance hallettiği için burada sadece araçlara bakıyoruz.
    // Tarama, sabit bir küre yerine aracın kendi çarpışma kutusuyla yapılıyor; eski sistemde
    // kullanılan 1.2 yarıçaplı küre, aracın üç katı büyüklüğünde olduğu için hep yanlış sonuç veriyordu.
    private float VehicleAvoidanceSteer()
    {
        if (bodyCollider == null) return 0f;

        Vector3 boxCenter = transform.TransformPoint(bodyCollider.center);
        Vector3 halfExtents = Vector3.Scale(bodyCollider.size, transform.lossyScale) * 0.5f;

        int count = Physics.BoxCastNonAlloc(boxCenter, halfExtents, transform.forward, vehicleHits,
                                            transform.rotation, vehicleCheckDistance, vehicleLayerMask, QueryTriggerInteraction.Ignore);

        // Taramanın önüne zemin ve yol gibi başka nesneler de çıkabildiği için hepsini gezip
        // yalnızca araçlar arasından en yakınını seçiyoruz.
        float nearestDistance = float.MaxValue;
        int nearestIndex = -1;

        for (int i = 0; i < count; i++)
        {
            Collider col = vehicleHits[i].collider;
            if (col.attachedRigidbody == rb) continue; // Kendi gövdemizi engel saymıyoruz
            if (!col.CompareTag("Traffic") && !col.CompareTag("Police")) continue;

            if (vehicleHits[i].distance < nearestDistance)
            {
                nearestDistance = vehicleHits[i].distance;
                nearestIndex = i;
            }
        }

        if (nearestIndex < 0) return 0f;

        // Araç solumuzdaysa sağa, sağımızdaysa sola kırıyoruz
        Vector3 localHit = transform.InverseTransformPoint(vehicleHits[nearestIndex].collider.transform.position);
        float direction = localHit.x < 0f ? 1f : -1f;

        // Araca yaklaştıkça kaçınma sertleşsin
        float closeness = 1f - Mathf.Clamp01(nearestDistance / vehicleCheckDistance);

        return direction * vehicleAvoidStrength * closeness;
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

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}