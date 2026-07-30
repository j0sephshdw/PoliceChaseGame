using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerCarController : MonoBehaviour
{
    // Temel fizik ve hareket değişkenleri
    [SerializeField]private float originalMaxSpeed;
    [SerializeField]private float currentSpeed = 0f;
    private float turnInput;
    private float turnSpeedMultiplier = 1f;
    private float gripMultiplier = 1f;
    private Rigidbody rb;
    private Vector3 currentMoveDirection;
    private BoxCollider boxCollider;
    private List<Transform> wheels = new List<Transform>();
    private float accelerationMultiplier = 1f;

    // Yeni Input Sistemi
    private PlayerInputActions inputActions;

    // El Freni (Sert Manevra) Kontrolleri
    private bool isHandbrakeActive = false;
    private float handbrakeDirection = 0f;

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
        rb.centerOfMass = new Vector3(0f, -1.5f, 0f);

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
            if (globalCrashSound != null) effectsAudioSource.PlayOneShot(globalCrashSound);

            // 1. Anında hızı ve fiziksel ivmeyi SIFIRLA
            currentSpeed = 0f;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero; // Arabanın burnunun havaya kalkmasını (torku) iptal et

            // 2. Çarpma yönünü al ama Y (Yukarı) eksenini KESİNLİKLE sıfırla!
            Vector3 wallNormal = collision.contacts[0].normal;
            Vector3 flatPushBack = new Vector3(wallNormal.x, 0f, wallNormal.z).normalized;

            // 3. Arabayı YUKARI değil, SADECE yatayda (X ve Z ekseninde) geriye it
            rb.AddForce(flatPushBack * 10f, ForceMode.Impulse);

            // 4. Aracın yönünü duvardan uzağa kır (Yine Y eksenini sıfırlayarak)
            Vector3 flatForward = new Vector3(currentMoveDirection.x, 0f, currentMoveDirection.z);
            currentMoveDirection = Vector3.Reflect(flatForward, flatPushBack).normalized;
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Obstacle") || collision.gameObject.CompareTag("Police") || collision.gameObject.CompareTag("Traffic"))
        {
            if (collision.contacts.Length > 0)
            {
                Vector3 wallNormal = collision.contacts[0].normal;

                // Araba duvara ne kadar dik (kafa kafaya) çarpıyor? (1 = tam dik, 0 = paralel sürtünme)
                float hitAngle = Vector3.Dot(transform.forward, -wallNormal);

                // Eğer kafa kafaya giriyorsak hızı sıfırda tut ki ileri doğru (duvara) tırmanma gücü üretmesin
                if (hitAngle > 0.8f)
                {
                    currentSpeed = 0f;
                }
                else
                {
                    // Yandan sürtüyorsa duvar boyunca kaydır ama yukarı çıkmasını YASAKLA
                    Vector3 slideDirection = Vector3.ProjectOnPlane(transform.forward, wallNormal);
                    slideDirection.y = 0f; // Tırmanmayı kesin olarak engelle

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
        if (globalExplosionSound != null) AudioSource.PlayClipAtPoint(globalExplosionSound, transform.position);

        // 3. Cinemachine kameranın bizi takip etmeyi bırakmasını sağla
        var vcam = FindObjectOfType<Unity.Cinemachine.CinemachineCamera>();
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

    // Yetenek sistemi (Kartlar vb.) için geçici hız artışı
    public void ActivateSpeedBoost(float multiplier, float duration)
    {
        StartCoroutine(SpeedBoostRoutine(multiplier, duration));
    }

    private IEnumerator SpeedBoostRoutine(float multiplier, float duration)
    {
        originalMaxSpeed *= multiplier;
        yield return new WaitForSeconds(duration);
        originalMaxSpeed /= multiplier;
    }

    private void FixedUpdate()
    {
        MoveCar();
        SteerCar();
        ApplyBodyLean();
    }

    private void MoveCar()
    {
        float accel = (currentCarData != null ? currentCarData.acceleration : 5f) * accelerationMultiplier;
        float baseGrip = (currentCarData != null ? currentCarData.driftGrip : 3f) * gripMultiplier;

        // El freni çekiliyse yol tutuşunu düşürerek drift başlat
        float finalGrip = isHandbrakeActive ? (baseGrip * 0.2f) : baseGrip;

        currentSpeed = Mathf.MoveTowards(currentSpeed, originalMaxSpeed, accel * Time.fixedDeltaTime);
        currentMoveDirection = Vector3.Lerp(currentMoveDirection, transform.forward, finalGrip * Time.fixedDeltaTime);

        Vector3 movement = currentMoveDirection * currentSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movement);
    }
    
    public void IncreaseAcceleration(float percentage)
    {
        accelerationMultiplier *= (1f + percentage);
    }

    private void SteerCar()
    {
        float baseTurnSpeed = (currentCarData != null ? currentCarData.turnSpeed : 100f) * turnSpeedMultiplier;

        // El freni devredeyse dönüş keskinliğini artır
        float finalTurnSpeed = isHandbrakeActive ? (baseTurnSpeed * 1.8f) : baseTurnSpeed;

        float turn = turnInput * finalTurnSpeed * Time.fixedDeltaTime;
        Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
        rb.MoveRotation(rb.rotation * turnRotation);
    }

    // Virajlarda merkezkaç kuvvetiyle kasanın yana yatması
    private void ApplyBodyLean()
    {
        if (carMesh != null && carMesh.childCount > 0)
        {
            float maxLean = currentCarData != null ? currentCarData.maxLeanAngle : 15f;
            float targetLean = turnInput * maxLean;
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
}