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
    private List<Transform> wheels = new List<Transform>(); // Tekerlek dönme animasyonu için listem

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
        turnInput = 0f;

        // PC platformu için klavye yön girdileri (Input.GetAxisRaw ile keskin dönüş)
        if (Input.GetAxisRaw("Horizontal") != 0) turnInput = Input.GetAxisRaw("Horizontal");

        // Mobil cihazlar için dokunmatik ekran kontrol altyapısı entegrasyonu
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.position.x < Screen.width / 2f) turnInput = -1f;
            else if (touch.position.x > Screen.width / 2f) turnInput = 1f;
        }

        // Space tuşuna basınca Dash (Hızlanma) yeteneğini test etmek için tetikledim
        if (Input.GetKeyDown(KeyCode.Space)) ActivateSpeedBoost(2f, 1.5f);

        SpinWheels(); // Tekerlek animasyonunu her karede çağır
        UpdateEngineSound(); // Vites ve devir seslerini hesapla
    }

    // --- ŞANZIMAN SİMÜLASYONU VE VİTES GEÇİŞ MATEMATİĞİ ---
    private void UpdateEngineSound()
    {
        if (engineAudioSource == null || !engineAudioSource.isPlaying || currentCarData == null) return;

        // Aracın genel hız oranını bul (0.0 ile 1.0 arası)
        float speedRatio = currentSpeed / originalMaxSpeed;

        //  O ANKİ VİTESİ HESAPLAMA 
        int newGear = Mathf.Clamp(Mathf.FloorToInt(speedRatio * numberOfGears), 0, numberOfGears - 1);

        if (newGear != currentGear)
        {
            currentGear = newGear;
        }

        //  VİTES İÇİ DEVİR ORANINI BULMA
        float gearMinRatio = (float)currentGear / numberOfGears;
        float gearMaxRatio = (float)(currentGear + 1) / numberOfGears;
        float currentGearRatio = (speedRatio - gearMinRatio) / (gearMaxRatio - gearMinRatio);

        // PITCH (DEVİR SESİ) HESAPLAMASI (Sakinleştirildi)
        // Vites büyüdükçe temel ses çok ufak artsın (0.1 yerine 0.05)
        float gearBaseOffset = currentGear * 0.05f;

        // Vitesin içindeki bağırma payını  0.35te tuttum ki kesiciye girmiş gibi tizleşmesin
        float targetPitch = currentCarData.baseEnginePitch + gearBaseOffset + (currentGearRatio * 0.35f);

        // 4. KESİCİ ENGELLEYİCİ (Overdrive Rahatlaması)
        // Araba son vitesteyse ve maksimum hıza çok yaklaştıysa motor bağırmayı bırakıp sabit bir devirde rahatlar
        if (currentGear == numberOfGears - 1 && currentGearRatio > 0.9f)
        {
            targetPitch -= 0.15f;
        }

       
        // Lerp hızını 12'den 4'e çektim. Böylece devir aniden sekmez, gerçek araba gibi ağır ağır yükselip düşer.
        engineAudioSource.pitch = Mathf.Lerp(engineAudioSource.pitch, targetPitch, 4f * Time.deltaTime);
    }

    // --- KAZA VE PATLAMA FİZİKLERİ ---
    private void OnCollisionEnter(Collision collision)
    {
        // Araç duvara veya polislere çarptığında ortak kaza sesini bir kere (PlayOneShot) çaldırdım
        if (globalCrashSound != null) effectsAudioSource.PlayOneShot(globalCrashSound);
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
        float grip = currentCarData != null ? currentCarData.driftGrip : 3f;

        // Aniden hızlanmak yerine pürüzsüz ivmelenme (Acceleration) için MoveTowards kullandım
        currentSpeed = Mathf.MoveTowards(currentSpeed, originalMaxSpeed, accel * Time.fixedDeltaTime);

        // Oto-Drift mantığı: Yanal kayma sağlamak için hareket vektörünü Lerp ile yumuşatarak getirdim
        currentMoveDirection = Vector3.Lerp(currentMoveDirection, transform.forward, grip * Time.fixedDeltaTime);

        Vector3 movement = currentMoveDirection * currentSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movement); // Rigidbody üzerinden aracı fiziksel olarak hareket ettirdim
    }

    private void SteerCar()
    {
        float tSpeed = currentCarData != null ? currentCarData.turnSpeed : 100f;

        // Kullanıcının dönüş girdisini (turnInput) alıp Y ekseninde Quaternion rotasyonu uyguladım
        float turn = turnInput * tSpeed * Time.fixedDeltaTime;
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
}