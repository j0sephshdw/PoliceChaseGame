using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerCarController : MonoBehaviour
{
    // Temel fizik ve hareket değişkenleri
    [SerializeField] private float originalMaxSpeed;

    [Range(0f, 1f)]
    [SerializeField] private float engineBaseVolume = 0.4f; // motor sesi diğer seslere göre çok baskın geldiği için düşürüldü
    
    [SerializeField] private float currentSpeed = 0f;
    private float turnInput;
    private float turnSpeedMultiplier = 1f;
    private float gripMultiplier = 1f;
    private Rigidbody rb;
    private Vector3 currentMoveDirection;
    private BoxCollider boxCollider;
    private List<Transform> wheels = new List<Transform>();
    private float accelerationMultiplier = 1f;

    private bool isSpeedBoostActive = false;
    private float activeSpeedBoostMultiplier = 1f;
    private Coroutine speedBoostCoroutine;

    // Yeni Input Sistemi
    private PlayerInputActions inputActions;

    // El Freni (Sert Manevra) Kontrolleri
    private bool isHandbrakeActive = false;
    private float handbrakeDirection = 0f;

    [Header("Ters Dönme / Respawn Ayarları")]
    public float flipThreshold = 0.2f; // Arabanın üst yönü bu değerin altına düşerse (yan yatar/ters dönerse) algılar
    public float respawnDelay = 2.5f; // Ters halde kaç saniye beklerse düzeltilecek
    private float flipTimer = 0f;
    private bool isFlipped = false;

    [Header("Patlama Fizik Ayarları")]
    public float patlamaGucu = 10f;      // Parçaların uzağa fırlama şiddeti
    public float patlamaYaricapi = 8f;     // Patlamanın etki alanı
    public float havayaFirlatmaGucu = 0.5f; // Parçaların yukarı kalkma oranı

    [Header("Görsel Efektler")]
    public GameObject explosionVFX; // Patlama efektini Inspector'dan buraya at

    [Header("Araç Özellikleri")]
    public CarData currentCarData;
    public Transform carMesh;

    [Header("Ses Efektleri")]
    public AudioClip globalCrashSound;
    public AudioClip globalExplosionSound;

    [Header("Şanzıman ve Motor Sesi")]
    public int numberOfGears = 5;
    private int currentGear = 0;
    private AudioSource engineAudioSource;
    private AudioSource effectsAudioSource;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
        rb = GetComponent<Rigidbody>();
        boxCollider = GetComponent<BoxCollider>();

        // Çarpışma hatalarını önlemek için fizik ayarları
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;


        rb.constraints = RigidbodyConstraints.None;

        // Ağırlık merkezini arabanın 1.5 metre altına çek
        rb.centerOfMass = new Vector3(0f, -0.4f, 0f);
        rb.angularDamping = 3.5f;

        // Efektler için AudioSource ayarları
        effectsAudioSource = gameObject.AddComponent<AudioSource>();
        effectsAudioSource.loop = false;
        effectsAudioSource.playOnAwake = false;

        // Motor sesi için AudioSource ayarları
        engineAudioSource = gameObject.AddComponent<AudioSource>();
        engineAudioSource.loop = true;
        engineAudioSource.playOnAwake = false;
        engineAudioSource.spatialBlend = 0f;
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    private void Start()
    {
        if (currentCarData != null)
        {
            originalMaxSpeed = currentCarData.maxSpeed;
            LoadCarModel();
        }
        else
        {
            originalMaxSpeed = 15f;
        }

        currentMoveDirection = transform.forward;
    }

    // ScriptableObject içindeki modele ve verilere göre aracı sahnede oluşturur
    public void LoadCarModel()
    {
        if (carMesh == null || currentCarData == null || currentCarData.carPrefab == null) return;

        // Eski placeholder modelleri temizle
        foreach (Transform child in carMesh) Destroy(child.gameObject);

        // Yeni aracı yükle ve pozisyonunu sıfırla
        GameObject newModel = Instantiate(currentCarData.carPrefab, carMesh);
        newModel.transform.localPosition = Vector3.zero;
        newModel.transform.localRotation = Quaternion.identity;

        // Aracın boyutlarına göre çarpışma kutusunu ayarla
        if (boxCollider != null)
        {
            boxCollider.center = currentCarData.colliderCenter;
            boxCollider.size = currentCarData.colliderSize;
        }

        // Tekerlekleri animasyon için listeye al
        wheels.Clear();
        Transform[] allChildren = newModel.GetComponentsInChildren<Transform>();
        foreach (Transform child in allChildren)
        {
            if (child.name.ToLower().Contains("wheel")) wheels.Add(child);
        }

        // Motor sesini başlat
        if (currentCarData.engineSound != null)
        {
            engineAudioSource.clip = currentCarData.engineSound;
            engineAudioSource.pitch = currentCarData.baseEnginePitch;
            engineAudioSource.Play();
        }
    }

    private void Update()
    {
        bool pressingLeft = false;
        bool pressingRight = false;

        // 1. KLAVYE GİRDİLERİ (İki tuşa aynı anda basıldığını algılamak için donanımı doğrudan okuyoruz)
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) pressingLeft = true;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) pressingRight = true;
        }

        // 2. MOBİL DOKUNMATİK GİRDİLERİ (Ekranın sağ ve sol kısımları)
        if (Touchscreen.current != null)
        {
            // Yeni sistemde ekrana dokunan tüm parmakları  tara
            foreach (var touch in Touchscreen.current.touches)
            {
                if (touch.press.isPressed)
                {
                    if (touch.position.ReadValue().x < Screen.width / 2f) pressingLeft = true;
                    else if (touch.position.ReadValue().x > Screen.width / 2f) pressingRight = true;
                }
            }
        }

        // 3. HOCANIN İSTEDİĞİ SAĞ + SOL İKİLİ BASMA (EL FRENİ) MANTIĞI
        if (pressingRight && pressingLeft)
        {
            if (!isHandbrakeActive)
            {
                isHandbrakeActive = true;
                handbrakeDirection = Mathf.Sign(turnInput);
                if (turnInput == 0) handbrakeDirection = 1f;
            }
            turnInput = handbrakeDirection;
        }
        else
        {
            isHandbrakeActive = false;

            if (pressingLeft) turnInput = -1f;
            else if (pressingRight) turnInput = 1f;
            else turnInput = 0f;
        }

        SpinWheels();
        UpdateEngineSound();
    }

    // Aracın hızına göre vites ve motor sesi hesaplamaları
    private void UpdateEngineSound()
    {
        if (engineAudioSource == null || currentCarData == null) return;

        engineAudioSource.volume = engineBaseVolume * GameUIManager.GetGameVolume();

        // Oyun duraklatıldığında motor sesini durdur
        if (Time.timeScale == 0f)
        {
            if (engineAudioSource.isPlaying) engineAudioSource.Pause();
            return;
        }
        else
        {
            if (!engineAudioSource.isPlaying) engineAudioSource.UnPause();
        }

        float speedRatio = currentSpeed / originalMaxSpeed;
        int newGear = Mathf.Clamp(Mathf.FloorToInt(speedRatio * numberOfGears), 0, numberOfGears - 1);

        // Vites değişimi anında motor sesini düşür (vites atma hissi)
        if (newGear != currentGear)
        {
            currentGear = newGear;
            engineAudioSource.pitch -= 0.25f;
        }

        // Mevcut vites içindeki devir oranını hesapla
        float gearMinRatio = (float)currentGear / numberOfGears;
        float gearMaxRatio = (float)(currentGear + 1) / numberOfGears;
        float currentGearRatio = (speedRatio - gearMinRatio) / (gearMaxRatio - gearMinRatio);

        float rpmCurve = Mathf.Pow(currentGearRatio, 1.5f);
        float gearBaseOffset = currentGear * 0.08f;
        float targetPitch = currentCarData.baseEnginePitch + gearBaseOffset + (rpmCurve * 0.50f);

        // Son viteste devir kesici (limiter) benzeri rahatlama
        if (currentGear == numberOfGears - 1 && currentGearRatio > 0.95f)
        {
            targetPitch -= 0.15f;
        }

        engineAudioSource.pitch = Mathf.Lerp(engineAudioSource.pitch, targetPitch, 6f * Time.deltaTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Obstacle") || collision.gameObject.CompareTag("Police") || collision.gameObject.CompareTag("Traffic"))
        {
            if (globalCrashSound != null) effectsAudioSource.PlayOneShot(globalCrashSound, GameUIManager.GetGameVolume());

            bool isPolice = collision.gameObject.CompareTag("Police");

            // Çarpılan şey polis ise (barikat vs.), kütlesini anında kağıda çevir ve fizik kilitlerini aç
            if (isPolice)
            {
                Rigidbody policeRb = collision.gameObject.GetComponent<Rigidbody>();
                if (policeRb != null)
                {
                    policeRb.mass = 200f;
                    policeRb.constraints = RigidbodyConstraints.None;

                    PoliceCarAI ai = collision.gameObject.GetComponent<PoliceCarAI>();
                    if (ai != null) ai.enabled = false;
                }
            }

            Vector3 wallNormal = collision.contacts[0].normal;
            float hitAngle = Vector3.Dot(transform.forward, -wallNormal);

            // 1. DURUM: Kafa kafaya veya sert bir açıyla çarpıldıysa
            if (hitAngle > 0.4f)
            {
                if (isPolice)
                {
                    // HOLLYWOOD EFEKTİ: Barikata girince durmak yok, delip geçiyoruz!
                    currentSpeed *= 0.65f; // Hızının %35'ini kaybeder (ceza) ama yoluna devam eder!
                    currentMoveDirection = transform.forward;

                    // Unity fiziğinin bizi geriye sektirmesini GÜÇ KULLANARAK engelliyoruz:
                    rb.linearVelocity = transform.forward * currentSpeed;
                    rb.angularVelocity = Vector3.zero;

                    // Polisi paramparça ederek havaya uçur
                    Rigidbody policeRb = collision.gameObject.GetComponent<Rigidbody>();
                    if (policeRb != null)
                    {
                        Vector3 firlatmaYonu = (transform.forward + (Vector3.up * 0.8f)).normalized;
                        policeRb.AddForce(firlatmaYonu * 55f, ForceMode.Impulse);
                        policeRb.AddTorque(UnityEngine.Random.insideUnitSphere * 50f, ForceMode.Impulse);
                    }
                }
                else
                {
                    // NORMAL DUVAR/BİNA ÇARPIŞMASI: (Eski sistem, taş gibi seker)
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    currentSpeed = -2f;
                    currentMoveDirection = transform.forward;
                }
            }
            // 2. DURUM: Yandan sıyırma / Sürtme durumu
            else
            {
                currentSpeed *= 0.5f;

                Vector3 flatPushBack = new Vector3(wallNormal.x, 0f, wallNormal.z).normalized;
                Vector3 flatForward = new Vector3(currentMoveDirection.x, 0f, currentMoveDirection.z);
                currentMoveDirection = Vector3.Reflect(flatForward, flatPushBack).normalized;

                if (isPolice)
                {
                    // Yandan çarpınca polisleri yana savur (Ama oyuncunun ivmesini sıfırlama)
                    rb.angularVelocity = Vector3.zero;

                    Rigidbody policeRb = collision.gameObject.GetComponent<Rigidbody>();
                    if (policeRb != null)
                    {
                        policeRb.AddForce(flatPushBack * -35f, ForceMode.Impulse);
                        policeRb.AddTorque(UnityEngine.Random.insideUnitSphere * 20f, ForceMode.Impulse);
                    }
                }
                else
                {
                    // Normal binalara sürtünce zıplamamak için ivmeyi sıfırla
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Obstacle") || collision.gameObject.CompareTag("Police") || collision.gameObject.CompareTag("Traffic"))
        {
            // KRİTİK DÜZELTME: Polis arabaları artık havaya uçtuğu için OnCollisionStay 
            // sürtünmesinden tamamen muaf tutuyoruz! (Bizi durdurmalarını engeller)
            if (collision.gameObject.CompareTag("Police")) return;

            if (collision.contacts.Length > 0)
            {
                Vector3 wallNormal = collision.contacts[0].normal;
                float hitAngle = Vector3.Dot(transform.forward, -wallNormal);

                if (hitAngle > 0.8f && currentSpeed > 0f)
                {
                    currentSpeed = 0f;
                }
                else
                {
                    Vector3 slideDirection = Vector3.ProjectOnPlane(transform.forward, wallNormal);
                    slideDirection.y = 0f;

                    if (slideDirection != Vector3.zero)
                    {
                        currentMoveDirection = Vector3.Lerp(currentMoveDirection, slideDirection.normalized, 10f * Time.fixedDeltaTime);
                    }
                }
            }
        }
    }


    // Araç canı sıfırlandığında çağrılan parçalanma fonksiyonu
    public void Explode()
    {
        // 1. Motor sesini hemen kes
        StopEngineSound();

        // 2. Patlama sesini çal
        if (globalExplosionSound != null) AudioSource.PlayClipAtPoint(globalExplosionSound, transform.position, GameUIManager.GetGameVolume());

        // 3. Cinemachine kameranın bizi takip etmeyi bırakmasını sağla
        var vcam = FindAnyObjectByType<Unity.Cinemachine.CinemachineCamera>();
        if (vcam != null)
        {
            vcam.Target.TrackingTarget = null;
        }

        // 4. Görsel parçalanma fiziği (Dışarı doğru patlama kuvveti)
        if (carMesh != null)
        {
            MeshRenderer[] allParts = carMesh.GetComponentsInChildren<MeshRenderer>();

            foreach (MeshRenderer meshPart in allParts)
            {
                Transform part = meshPart.transform;
                part.SetParent(null);

                part.gameObject.AddComponent<BoxCollider>();
                Rigidbody partRb = part.gameObject.AddComponent<Rigidbody>();

                // Kütleyi 1.5f yaptım
                partRb.mass = 1.5f;
                partRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

                // MERKEZDEN DIŞARIYA PATLAMA KUVVETİ
                partRb.AddExplosionForce(patlamaGucu, transform.position, patlamaYaricapi, havayaFirlatmaGucu, ForceMode.Impulse);

                // Parçaların havada kendi etrafında dönmesi için rastgele tork
                partRb.AddTorque(UnityEngine.Random.insideUnitSphere * 15f, ForceMode.Impulse);

                Destroy(part.gameObject, 5f);
            }
        }

        // 5. Patlama efekti (VFX)
        if (explosionVFX != null)
        {
            GameObject vfx = Instantiate(explosionVFX, transform.position, Quaternion.identity);
            Destroy(vfx, 3f);
        }

        // 6. Gövdeyi kapatıp arabayı 5 saniye sonra imha et
        if (carMesh != null)
        {
            carMesh.gameObject.SetActive(false);
        }

        Collider mainCollider = GetComponent<Collider>();
        if (mainCollider != null)
        {
            mainCollider.enabled = false;
        }

        Destroy(gameObject, 5f);
    }

    // Harita üzerindeki Hızlanma pickup'ı alındığında çağrılır (PickupItem.cs üzerinden)
    public void ActivateSpeedBoost(float multiplier, float duration)
    {
        if (isSpeedBoostActive)
        {
            // Zaten aktifken tekrar toplandıysa: çarpanı TEKRAR uygulamıyoruz (üst üste binmesin),
            // sadece eski sayacı iptal edip süresi sıfırlanmış yeni bir coroutine başlatıyoruz.
            StopCoroutine(speedBoostCoroutine);
            speedBoostCoroutine = StartCoroutine(SpeedBoostRoutine(duration));
            return;
        }

        isSpeedBoostActive = true;
        activeSpeedBoostMultiplier = multiplier; // süre bitince tam olarak bu çarpanı geri böleceğiz
        originalMaxSpeed *= multiplier;
        currentSpeed *= multiplier;
        speedBoostCoroutine = StartCoroutine(SpeedBoostRoutine(duration));
    }

    private IEnumerator SpeedBoostRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        originalMaxSpeed /= activeSpeedBoostMultiplier;
        isSpeedBoostActive = false;
    }

    private void FixedUpdate()
    {
        CheckFlipStatus();
        MoveCar();
        SteerCar();
        ApplyBodyLean();
    }

    private void MoveCar()
    {
        float accel = (currentCarData != null ? currentCarData.acceleration : 5f) * accelerationMultiplier;

        if (currentSpeed < 0)
        {
            accel *= 4f;
        }

        float baseGrip = (currentCarData != null ? currentCarData.driftGrip : 3f) * gripMultiplier;
        float finalGrip = isHandbrakeActive ? (baseGrip * 0.2f) : baseGrip;

        float targetSpeed = isHandbrakeActive ? (originalMaxSpeed * 0.4f) : originalMaxSpeed;
        float currentAccel = isHandbrakeActive ? (accel * 2f) : accel;

        // --- ŞAHA KALKMA VE TERS DÖNME DÜZELTMESİ ---
        if (isFlipped)
        {
            targetSpeed = 0f;
            currentAccel = accel * 10f; // Motoru ÇOK hızlı durdur ki zıplamayı anında kessin
        }

        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, currentAccel * Time.fixedDeltaTime);

        if (!isFlipped)
        {
            // Gökyüzüne sürmeyi engellemek için ileri yönü yere paralel hale getiriyoruz
            Vector3 flatForward = transform.forward;
            if (flatForward.y > 0.2f) flatForward.y = 0.2f; // Burnu %20'den fazla kalkarsa motor gücünü ufka daya

            currentMoveDirection = Vector3.Lerp(currentMoveDirection, flatForward.normalized, finalGrip * Time.fixedDeltaTime);
        }

        Vector3 movement = currentMoveDirection * currentSpeed * Time.fixedDeltaTime;

        // Eğer araba şaha kalkmış (dikilmiş) ise Y eksenindeki motor hareketini TAMAMEN iptal et (Gökyüzüne tırmanmasın)
        if (transform.forward.y > 0.5f)
        {
            movement.y = 0f;
        }

        rb.MovePosition(rb.position + movement);
    }

    public void IncreaseAcceleration(float percentage)
    {
        accelerationMultiplier *= (1f + percentage);
    }

    private void SteerCar()
    {
        // ---  Tersken direksiyonu kilitle ---
        if (isFlipped) return;

        float baseTurnSpeed = (currentCarData != null ? currentCarData.turnSpeed : 100f) * turnSpeedMultiplier;
        float finalTurnSpeed = isHandbrakeActive ? (baseTurnSpeed * 1.8f) : baseTurnSpeed;

        float speedFactor = Mathf.Clamp01(Mathf.Abs(currentSpeed) / originalMaxSpeed);
        float speedSensitiveTurn = Mathf.Lerp(finalTurnSpeed, finalTurnSpeed * 0.8f, speedFactor);

        float turn = turnInput * speedSensitiveTurn * Time.fixedDeltaTime;
        Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
        rb.MoveRotation(rb.rotation * turnRotation);
    }

    // Virajlarda merkezkaç kuvvetiyle kasanın yana yatması
    private void ApplyBodyLean()
    {
        if (carMesh != null && carMesh.childCount > 0)
        {
            float maxLean = currentCarData != null ? currentCarData.maxLeanAngle : 15f;

            // İYİLEŞTİRME: Kasa sadece araç hareket halindeyken ve hıza oranla yana yatsın
            float speedFactor = Mathf.Clamp01(Mathf.Abs(currentSpeed) / 15f) * 2.2f;
            float targetLean = turnInput * maxLean * speedFactor;

            Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetLean);
            carMesh.localRotation = Quaternion.Lerp(carMesh.localRotation, targetRotation, 10f * Time.fixedDeltaTime);
        }
    }

    private void SpinWheels()
    {
        float spinAmount = currentSpeed * 20f * Time.deltaTime;
        foreach (Transform wheel in wheels)
        {
            wheel.Rotate(Vector3.right, spinAmount, Space.Self);
        }
    }

    public void StopEngineSound()
    {
        if (engineAudioSource != null && engineAudioSource.isPlaying)
        {
            engineAudioSource.Stop();
        }
    }

    // Yetenek sistemi için etraftaki engelleri fırlatma
    public void ActivateShockwave(float radius, float force)
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, radius);
        foreach (Collider nearbyObject in colliders)
        {
            if (nearbyObject.CompareTag("Police") || nearbyObject.CompareTag("Obstacle"))
            {
                Rigidbody rb = nearbyObject.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddExplosionForce(force, transform.position, radius, 1f, ForceMode.Impulse);
                }
            }
        }
    }

    public float GetTurnInput()
    {
        return turnInput;
    }

    public bool GetHandbrakeStatus()
    {
        return isHandbrakeActive;
    }

    // Yetenek kartlarından "Motor Gücü" seçildiğinde çağrılır, hızı kalıcı olarak artırır.
    // currentCarData'ya değil, çalışma zamanı değerine dokunuyoruz ki paylaşılan
    // CarData asset dosyası kalıcı olarak değişmesin.
    public void IncreaseMaxSpeed(float percentage)
    {
        originalMaxSpeed *= (1f + percentage);
    }

    // Yetenek kartlarından "Yol Tutuşu" seçildiğinde çağrılır.
    public void IncreaseGrip(float percentage)
    {
        turnSpeedMultiplier *= (1f + percentage);
        gripMultiplier *= (1f + percentage);
    }
    private void CheckFlipStatus()
    {
        // 1 = tam düz, 0 = tam yan yatmış veya dikilmiş, -1 = tam ters
        if (Vector3.Dot(transform.up, Vector3.up) < 0.35f) // Daha erken algılaması için eşiği biraz artırdık
        {
            isFlipped = true;
            flipTimer += Time.fixedDeltaTime;

            if (flipTimer >= respawnDelay)
            {
                RespawnCar();
            }
        }
        else
        {
            isFlipped = false;
            // --- SAYAÇ SIFIRLAMA ---
            // Anında 0'a eşitlemek yerine yavaşça azaltıyoruz! 
            // Böylece araba tamponu üstünde zıplarken 1 saliseliğine düzelse bile 2.5 saniyelik sayaç bozulmuyor.
            flipTimer = Mathf.Max(0f, flipTimer - Time.fixedDeltaTime * 2f);
        }
    }

    private void RespawnCar()
    {
        flipTimer = 0f;
        isFlipped = false;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        currentSpeed = 0f;

        // Arabayı 1.5 metre havadan bırakıp beşik gibi sallandırmak yerine tam yola (Y = 0.5f) ok gibi oturtuyoruz.
        transform.position = new Vector3(transform.position.x, 0.5f, transform.position.z);
        transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        // Kasanın virajdaki yatma açısını sıfırla ki yamuk doğmasın
        if (carMesh != null)
        {
            carMesh.localRotation = Quaternion.identity;
        }

        currentMoveDirection = transform.forward;
    }
}