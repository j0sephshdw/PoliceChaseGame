using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerCarController : MonoBehaviour
{
    // Kapsülleme (Encapsulation) kurallarına uyarak değişkenlerimi private tanımladım
    private float originalMaxSpeed; // Dash (hızlanma) bitince eski hıza dönebilmek için orijinal hızı tutuyorum
    private float currentSpeed = 0f; // Aracın o anki hızını ivmelenme hesapları için takip ediyorum
    private float turnInput;
    private Rigidbody rb;
    private Vector3 currentMoveDirection; // Aracın virajlarda yanal kayma (drift) yönünü tutuyorum
    private BoxCollider boxCollider; // Her arabanın boyutuna göre dinamik değişecek çarpışma kutusu
    private List<Transform> wheels = new List<Transform>(); // Tekerlek dönme animasyonu için liste

    // --- EL FRENİ / SERT MANEVRA DEĞİŞKENLERİ ---
    private bool isHandbrakeActive = false; // El freni (iki tuşa basma) aktif mi?
    private float handbrakeDirection = 0f;  // El frenine girerken ilk basılan yön (1 sağ, -1 sol)

    [Header("Araç Veri Paketi (Scriptable Object)")]
    public CarData currentCarData; // Her arabanın kendine has verilerini (hız, ses) tutan dosyamız

    [Header("Görsel Model Kapsayıcısı (CarMesh)")]
    public Transform carMesh; // Farklı 3D araba modellerinin Instantiate edileceği ebeveyn obje

    [Header("Ortak Ses Efektleri (Tüm Araçlar İçin)")]
    public AudioClip globalCrashSound;     // Arabalar duvara çarpınca çalacak ortak kaza sesi
    public AudioClip globalExplosionSound; // Arabalar patlayıp yok olduğunda çalacak ses

    // --- YENİ: ŞANZIMAN (VİTES) SİSTEMİ DEĞİŞKENLERİ ---
    [Header("Şanzıman (Gearbox) Ayarları")]
    public int numberOfGears = 5; // Aracın toplam vites sayısı (Matematiksel dilimleme için)
    private int currentGear = 0; // Aracın o anki aktif vitesi (0'dan başlar)

    private AudioSource engineAudioSource;  // Gerçek motor sesini (.wav) sürekli çalacak oynatıcım
    private AudioSource effectsAudioSource; // Kaza ve patlama gibi anlık sesleri çalacak oynatıcım

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        boxCollider = GetComponent<BoxCollider>();

        // Aracın fiziksel çarpışmalarda takla atıp saçmalamasını engellemek için X ve Z dönüşlerini kilitledim
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // 1. Kaza ve anlık efektler için AudioSource kurulumu
        effectsAudioSource = gameObject.AddComponent<AudioSource>();
        effectsAudioSource.loop = false; // Kaza sesi bir kere çalar
        effectsAudioSource.playOnAwake = false;

        // 2. Gerçek motor sesi için AudioSource kurulumu
        engineAudioSource = gameObject.AddComponent<AudioSource>();
        engineAudioSource.loop = true; // Motor sesi hiç susmadan dönecek
        engineAudioSource.playOnAwake = false;
        engineAudioSource.spatialBlend = 0f; // Sesi 2D yaptım ki kamera uzaklaşınca duyulmamazlık yapmasın
    }

    private void Start()
    {
        // Veri dosyasından (Scriptable Object) aracın özelliklerini Null-Check yaparak çektim
        if (currentCarData != null)
        {
            originalMaxSpeed = currentCarData.maxSpeed;
            LoadCarModel(); // 3D modeli, tekerlekleri ve sesleri sahneye yüklettim
        }
        else
        {
            originalMaxSpeed = 15f; // Veri bağlanmamışsa oyun çökmesin diye varsayılan bir hız atadım
        }

        // Oyun başladığında hareket vektörünü arabanın baktığı yöne eşitledim
        currentMoveDirection = transform.forward;
    }

    // Seçilen CarData paketindeki 3D prefab modelini dinamik olarak yükleyen mimarim
    private void LoadCarModel()
    {
        if (carMesh == null || currentCarData == null || currentCarData.carPrefab == null) return;

        // CarMesh'in altındaki önceki geçici (placeholder) modelleri temizliyorum
        foreach (Transform child in carMesh) Destroy(child.gameObject);

        // Yeni araba modelini Instantiate edip tam merkeze sıfır rotasyonla oturttum
        GameObject newModel = Instantiate(currentCarData.carPrefab, carMesh);
        newModel.transform.localPosition = Vector3.zero;
        newModel.transform.localRotation = Quaternion.identity;

        // Çarpışma kutusunu (Collider) o araca özel (Tır ise büyük, Taksi ise küçük) otomatik boyutlandırdım
        if (boxCollider != null)
        {
            boxCollider.center = currentCarData.colliderCenter;
            boxCollider.size = currentCarData.colliderSize;
        }

        wheels.Clear(); // Arabanın tekerleklerini bulup animasyon listesine ekliyorum
        Transform[] allChildren = newModel.GetComponentsInChildren<Transform>();
        foreach (Transform child in allChildren)
        {
            if (child.name.ToLower().Contains("wheel")) wheels.Add(child);
        }

        // --- CAR DATA'DAKİ GERÇEK SESİ AL VE ÇALMAYA BAŞLA ---
        if (currentCarData.engineSound != null)
        {
            engineAudioSource.clip = currentCarData.engineSound; // Sesi teybe taktık

            // Araca özel temel ses kalınlığını atadım (Tır ise baştan kalın, spor ise ince başlayacak)
            engineAudioSource.pitch = currentCarData.baseEnginePitch;
            engineAudioSource.Play();
        }
    }

    private void Update()
    {
        // PC platformu için klavye yön girdileri
        float rawHorizontal = Input.GetAxisRaw("Horizontal");

        // --- YENİ: SAĞ-SOL AYNI ANDA BASILMA (EL FRENİ) KONTROLÜ ---
        bool pressingRight = Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D);
        bool pressingLeft = Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A);

        if (pressingRight && pressingLeft)
        {
            if (!isHandbrakeActive)
            {
                // İki tuşa ilk defa aynı anda basıldıysa:
                isHandbrakeActive = true;

                // Hangi tuşun önce basıldığını anlamak için önceki karenin (turnInput) yönünü kullan
                handbrakeDirection = Mathf.Sign(turnInput);

                // Eğer daha önce hiç dönmüyorsa ama ikisine aniden basıldıysa (nadir bir durum), varsayılan olarak sağa/sola bir karar ver
                if (turnInput == 0) handbrakeDirection = 1f;
            }
            // El freni modundayken dönüş yönünü kilitliyoruz (İlk basılan yöne doğru)
            turnInput = handbrakeDirection;
        }
        else
        {
            // İki tuşa aynı anda basılmıyorsa, normal dönüş sistemine geri dön
            isHandbrakeActive = false;
            turnInput = rawHorizontal;
        }

        // Mobil cihazlar için dokunmatik ekran kontrol altyapısı (Bunu el freni için sonra mobilde revize edebiliriz)
        if (Input.touchCount > 0 && !isHandbrakeActive)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.position.x < Screen.width / 2f) turnInput = -1f;
            else if (touch.position.x > Screen.width / 2f) turnInput = 1f;
        }

        if (Input.GetKeyDown(KeyCode.Space)) ActivateSpeedBoost(2f, 1.5f);

        SpinWheels();
        UpdateEngineSound();
    }

    // --- ŞANZIMAN VE MOTOR SESİ KONTROL SİSTEMİ ---
    private void UpdateEngineSound()
    {
        // Null-Check: Araç verisi veya ses dosyası atanmamışsa oyunun çökmesini (NullReferenceException) engelliyoruz.
        if (engineAudioSource == null || currentCarData == null) return;

        // BUG FİX: Oyun pause menüsünde durdurulduğunda (TimeScale = 0) motor sesinin arkada çalmaya devam etmesini engelliyoruz.
        if (Time.timeScale == 0f)
        {
            if (engineAudioSource.isPlaying) engineAudioSource.Pause();
            return; // Zaman akmadığı için gereksiz matematiksel hesaplamalara girmeden fonksiyondan çıkıyoruz.
        }
        else
        {
            // Oyun devam ettiğinde sesi sıfırdan başlatmak yerine kaldığı devirden devam etmesi için UnPause kullandık.
            if (!engineAudioSource.isPlaying) engineAudioSource.UnPause();
        }

        // Aracın o anki hızının maksimum hıza bölümüyle % kaçlık bir hıza ulaştığımızı hesaplıyoruz 0  1 arası
        float speedRatio = currentSpeed / originalMaxSpeed;

        // VİTES HESAPLAMA MANTIĞI: 
        // Toplam hız oranını vites sayısıyla çarpıp aşağı yuvarlıyoruz
        // Örn: 5 vitesli araçta %50 hızdaysak matematiksel olarak 2. vitesteyiz demektir. 
        // Dizi sınırlarını aşmamak için Clamp ile 0 ile (Max Vites - 1) arasında sınırlandırdık.
        int newGear = Mathf.Clamp(Mathf.FloorToInt(speedRatio * numberOfGears), 0, numberOfGears - 1);

        // VİTES ATMA ANINI YAKALAMA VE HİSSİYAT:
        if (newGear != currentGear)
        {
            currentGear = newGear;

            // Gerçek arabalarda vites büyüyünce motor devri aniden düşer ve ses kalınlaşır. 
            // Lerp'in yumuşak geçişini kırıp pitch değerini manuel olarak aşağı çekerek "vites atma" hissini verdik.
            engineAudioSource.pitch -= 0.25f;
        }

        // BULUNDUĞUMUZ VİTESİN İÇİNDE YÜZDE KAÇTAYIZ?
        // Sadece genel hıza bakarsak ses doğru çıkmıyor. Bu yüzden o anki vitesin alt ve üst hız sınırlarını bulup,
        // sadece o vitesin içindeki devir oranımızı hesaplıyoruz.
        float gearMinRatio = (float)currentGear / numberOfGears;
        float gearMaxRatio = (float)(currentGear + 1) / numberOfGears;
        float currentGearRatio = (speedRatio - gearMinRatio) / (gearMaxRatio - gearMinRatio);

        // MOTOR DEVİR SESİ (PITCH) MATEMATİĞİ:
        // Sesi doğrusal (lineer) arttırınca oyuncak araba gibi çıkıyordu. 
        // Mathf.Pow(..., 1.5f) kullanarak son devirlere doğru motorun daha agresif/yırtıcı bağırmasını sağladık.
        float rpmCurve = Mathf.Pow(currentGearRatio, 1.5f);

        // Üst viteslere çıktıkça motorun genel uğultusunu çok hafif kalınlaştırıyoruz (tok bir ses için).
        float gearBaseOffset = currentGear * 0.08f;

        // Temel sese , vites ağırlığını ve devir eğrisini ekleyerek hedef sesi buluyoruz.
        float targetPitch = currentCarData.baseEnginePitch + gearBaseOffset + (rpmCurve * 0.50f);

        // SON VİTES  RAHATLAMASI:
        // Son vitesin sonlarına gelindiğinde (maksimum hıza ulaşıldığında) motorun sürekli tiz bir şekilde 
        // bağırmasını engellemek için sesi biraz kısarak devri sabitliyoruz.
        if (currentGear == numberOfGears - 1 && currentGearRatio > 0.95f)
        {
            targetPitch -= 0.15f;
        }

        // Hedeflenen sese aniden geçmek yerine Lerp ile pürüzsüz bir geçiş sağlıyoruz. 
        // Geçiş hızını 6f tutarak motorun gaza ve yavaşlamaya anında tepki vermesini sağladık.
        engineAudioSource.pitch = Mathf.Lerp(engineAudioSource.pitch, targetPitch, 6f * Time.deltaTime);
    }

    // --- KAZA VE PATLAMA FİZİKLERİ ---
    private void OnCollisionEnter(Collision collision)
    {
        // Zeminle temas ettiğinde ses çıkmaması için sadece Engel veya Polis etiketli objeleri filtrele
        if (collision.gameObject.CompareTag("Obstacle") || collision.gameObject.CompareTag("Police"))
        {
            // Araç duvara veya polislere çarptığında ortak kaza sesini bir kere (PlayOneShot) çaldırdım
            if (globalCrashSound != null) effectsAudioSource.PlayOneShot(globalCrashSound);
        }
    }

    public void Explode()
    {
        // Araç silinse dahi patlama sesinin yarıda kesilmemesi için bağımsız 3D noktada ses ürettirdim
        if (globalExplosionSound != null) AudioSource.PlayClipAtPoint(globalExplosionSound, transform.position);

        Destroy(gameObject); // Aracı sahneden yok et
    }

    // Diğer scriptlerden (örneğin UI butonundan) çağrılacak hızlandırma (Dash) metodu
    public void ActivateSpeedBoost(float multiplier, float duration)
    {
        StartCoroutine(SpeedBoostRoutine(multiplier, duration));
    }

    // Hızlanma sürecini oyunu dondurmadan arka planda yürütmek için Coroutine (IEnumerator) kullandım
    private IEnumerator SpeedBoostRoutine(float multiplier, float duration)
    {
        originalMaxSpeed *= multiplier; // Maksimum hızı geçici olarak artır
        yield return new WaitForSeconds(duration); // Verilen süre kadar bekle
        originalMaxSpeed /= multiplier; // Süre dolunca eski hıza geri dön
    }

    // --- SÜRÜŞ FİZİKLERİ (Kare atlamaması için FixedUpdate içinde) ---
    private void FixedUpdate()
    {
        MoveCar();
        SteerCar();
        ApplyBodyLean(); // Merkezkaç ağırlık transferi fiziğini uygula
    }

    private void MoveCar()
    {
        float accel = currentCarData != null ? currentCarData.acceleration : 5f;

        // --- EL FRENİ AKTİFSE YOL TUTUŞUNU (GRIP) DÜŞÜR Kİ ARABA KAYMASIN (SAVRULSUN) ---
        float baseGrip = currentCarData != null ? currentCarData.driftGrip : 3f;
        float finalGrip = isHandbrakeActive ? (baseGrip * 0.2f) : baseGrip; // Tutunmayı %80 azalt

        currentSpeed = Mathf.MoveTowards(currentSpeed, originalMaxSpeed, accel * Time.fixedDeltaTime);

        // Oto-Drift mantığı (El freni varsa finalGrip düşük olacağı için araba savrulacak)
        currentMoveDirection = Vector3.Lerp(currentMoveDirection, transform.forward, finalGrip * Time.fixedDeltaTime);

        Vector3 movement = currentMoveDirection * currentSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movement);
    }

    private void SteerCar()
    {
        // --- EL FRENİ AKTİFSE DÖNÜŞ HIZINI (TURN SPEED) ARTIR Kİ SERT DÖNSÜN ---
        float baseTurnSpeed = currentCarData != null ? currentCarData.turnSpeed : 100f;
        float finalTurnSpeed = isHandbrakeActive ? (baseTurnSpeed * 1.8f) : baseTurnSpeed; // Dönüşü %80 hızlandır

        float turn = turnInput * finalTurnSpeed * Time.fixedDeltaTime;
        Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
        rb.MoveRotation(rb.rotation * turnRotation);
    }

    private void ApplyBodyLean()
    {
        // Virajlarda süspansiyon esnemesi (kasanın yana yatması) hissiyatı ekledim
        if (carMesh != null && carMesh.childCount > 0)
        {
            float maxLean = currentCarData != null ? currentCarData.maxLeanAngle : 15f;

            // Sağa dönerken aracı sola yatırdım (Z ekseninde)
            float targetLean = turnInput * maxLean;
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetLean);

            // Kasa eğilmesinin pürüzsüz gerçekleşmesi için rotasyon geçişine Lerp uyguladım
            carMesh.localRotation = Quaternion.Lerp(carMesh.localRotation, targetRotation, 10f * Time.fixedDeltaTime);
        }
    }

    private void SpinWheels()
    {
        // Tekerleklerin dönüş hızını arabanın mevcut hızıyla (currentSpeed) orantıladım
        float spinAmount = currentSpeed * 20f * Time.deltaTime;

        foreach (Transform wheel in wheels)
        {
            // Listeye aldığım tüm tekerlekleri X ekseninde ileri doğru döndürdüm
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
    public void ActivateShockwave(float radius, float force)
    {
        // Aracın etrafındaki belirtilen yarıçaptaki tüm colliderları bul
        Collider[] colliders = Physics.OverlapSphere(transform.position, radius);

        foreach (Collider nearbyObject in colliders)
        {
            // Sadece polisleri ve engelleri fırlat
            if (nearbyObject.CompareTag("Police") || nearbyObject.CompareTag("Obstacle"))
            {
                Rigidbody rb = nearbyObject.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    // Z eksenindeki freeze ayarlarını bozmamak kaydıyla nesneleri arabadan uzağa fırlat
                    rb.AddExplosionForce(force, transform.position, radius, 1f, ForceMode.Impulse);
                }
            }
        }
    }
    // Duman sisteminin kapsüllemeyi bozmadan dönüş değerlerini okusun diye Getterlar
    public float GetTurnInput()
    {
        return turnInput;
    }

    public bool GetHandbrakeStatus()
    {
        return isHandbrakeActive;
    }
}