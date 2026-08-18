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

    [Header("Çevre Algılama (Raycast)")]
    public float obstacleCheckDistance = 5f;
    public LayerMask obstacleLayerMask;

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

    [Header("Ödül Ayarları")]
    public int xpReward = 25; // Bu araç patlayınca oyuncuya verilecek XP miktarı
    public float xpZoneRadius = 20f; // Oyuncuya bu mesafeden yakın patlarsa XP verilir, uzaktaki patlamalar sayılmaz

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
    public int hitPoints = 2;

    private float stuckTimer;
    private float lastDistanceToPlayer = 999f;
    private float difficultyMultiplier = 1f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        rb.centerOfMass = new Vector3(0f, -0.6f, 0f);
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.mass = collisionMass;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;

        currentMoveDir = transform.forward;
    }

    private void OnEnable()
    {
        isDead = false;
        spawnTime = Time.time;
        hitPoints = 2;
        isStunned = false;
        isRecovering = false;
        isDrifting = false;
        stuckTimer = 0f;
        lastDistanceToPlayer = 999f;

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

        UpdateStuckDetection(distance);
        ChaseTarget(distance);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isDead) return;

        if (collision.gameObject.CompareTag("Obstacle") || collision.gameObject.CompareTag("Traffic") || collision.gameObject.CompareTag("Police"))
        {
            currentSpeed *= 0.4f;
            TakeHit();
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
        TakeHit();
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

    private void TakeHit()
    {
        if (Time.time < spawnTime + spawnInvulnerabilityDuration) return; // spawn koruması sırasında hak harcanmasın
        hitPoints--;
        if (hitPoints <= 0)
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

        // Patlama oyuncuya yakın bir alanda mı oldu diye bakıyoruz;
        // sadece bu alanın (xpZoneRadius) içinde patlarsa oyuncuyu ödüllendiriyoruz
        bool isInsideXPZone = target != null && FlatDistance(transform.position, target.position) <= xpZoneRadius;

        if (isInsideXPZone && ScoreManager.Instance != null)
        {
            int starMultiplier = WantedManager.Instance != null ? WantedManager.Instance.CurrentStars : 1;
            ScoreManager.Instance.AddXP(xpReward * starMultiplier);
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

    private void UpdateStuckDetection(float distance)
    {
        if (distance > lastDistanceToPlayer + 0.05f)
            stuckTimer += Time.fixedDeltaTime;
        else
            stuckTimer = Mathf.Max(0f, stuckTimer - Time.fixedDeltaTime * 2f);

        lastDistanceToPlayer = distance;
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

        bool isAheadOfPlayer = Vector3.Dot(target.forward, toPoliceFromPlayer) > 0.45f;

        Vector3 aimPoint = ComputeAimPoint(isAheadOfPlayer, flatTargetVel, actualDistanceToPlayer);

        ApplySteering(aimPoint, playerSpeed, flatTargetVel, actualDistanceToPlayer);

        float targetSpeed = ComputeTargetSpeed(isAheadOfPlayer, actualDistanceToPlayer, playerSpeed);
        ApplySpeed(targetSpeed, playerSpeed, actualDistanceToPlayer);

        MoveForward();
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

        desiredTurnRate += ObstacleAvoidanceSteer() * 0.15f;

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
        Vector3 movement = currentMoveDir * currentSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movement);
    }

    private float ObstacleAvoidanceSteer()
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        float steer = 0f;
        int hits = 0;

        TryObstacleRay(origin, transform.forward, 1.2f, ref steer, ref hits);
        TryObstacleRay(origin, (transform.forward + transform.right * 0.45f).normalized, 0.9f, ref steer, ref hits);
        TryObstacleRay(origin, (transform.forward - transform.right * 0.45f).normalized, 0.9f, ref steer, ref hits);

        return hits > 0 ? steer / hits : 0f;
    }

    private void TryObstacleRay(Vector3 origin, Vector3 direction, float radius, ref float steer, ref int hits)
    {
        if (!Physics.SphereCast(origin, radius, direction, out RaycastHit hit, obstacleCheckDistance, obstacleLayerMask))
            return;

        if (hit.collider.CompareTag("Player") || hit.collider.CompareTag("Road") || hit.collider.CompareTag("Ground")) return;

        Vector3 localHit = transform.InverseTransformPoint(hit.point);
        steer += localHit.x < 0f ? 10f : -10f;
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

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}