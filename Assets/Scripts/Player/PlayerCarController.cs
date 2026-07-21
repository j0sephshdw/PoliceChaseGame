using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(Rigidbody))]
public class PlayerCarController : MonoBehaviour
{
    // Kapsülleme (Encapsulation) kurallarına uygun olarak private değişkenleri sınıfın en başında tanımladım
    private float originalMaxSpeed; // Hızlanma bitince eski hıza dönebilmek için orijinal hızı burada tuttum
    private float currentSpeed = 0f; // Aracın o anki hızını ivmelenme için takip ettim
    private float turnInput;
    private Rigidbody rb;
    private Vector3 currentMoveDirection; // Aracın virajlarda kayma (drift) yönünü hesaplamak için ekledim
    private BoxCollider boxCollider; // Box Collider'ı kontrol etmek için ekledim
    private List<Transform> wheels = new List<Transform>(); // Arabanın tekerleklerini tutacağımız liste
    [Header("Araç Veri Paketi")]
    public CarData currentCarData; // ScriptableObject veri modelini araca bağladım

    [Header("Görsel Model Kapsayıcısı (CarMesh)")]
    public Transform carMesh; // Farklı 3D modellerin dinamik olarak ekleneceği ana objeyi tanımladım

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        boxCollider = GetComponent<BoxCollider>(); // Collider'ı hafızaya aldım
        // Aracın fiziksel çarpışmalarda takla atmasını engellemek için X ve Z eksenindeki dönüşleri kilitledim
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    private void Start()
    {
        // Eğer araca bir veri dosyası bağlandıysa değerleri oradan çektim
        if (currentCarData != null)
        {
            originalMaxSpeed = currentCarData.maxSpeed;
            LoadCarModel(); // Oyun başladığında ScriptableObject'teki 3D modeli sahneye yüklettim
        }
        else
        {
            originalMaxSpeed = 15f; // Veri yoksa hata vermemesi için varsayılan bir hız atadım
        }

        // Oyun başladığında hareket vektörünü arabanın burnunun baktığı yöne eşitledim
        currentMoveDirection = transform.forward;
    }

    // Seçilen CarData paketindeki 3D prefab modelini dinamik olarak CarMesh altına ekleyen fonksiyonu yazdım
    private void LoadCarModel()
    {
        if (carMesh == null || currentCarData == null || currentCarData.carPrefab == null) return;

        // CarMesh'in altında daha önceden kalan geçici (placeholder) modeller varsa onları sildim
        foreach (Transform child in carMesh)
        {
            Destroy(child.gameObject);
        }

        // Yeni araba modelini Instantiate ile oluşturup CarMesh objesinin içine (child olarak) yerleştirdim
        GameObject newModel = Instantiate(currentCarData.carPrefab, carMesh);
        newModel.transform.localPosition = Vector3.zero; // Modeli tam merkeze oturttum
        newModel.transform.localRotation = Quaternion.identity; // Rotasyon sapmalarını sıfırladım
        // SEÇİLEN ARACIN ÇARPIŞMA KUTUSUNU (COLLIDER) OTOMATİK AYARLAMA MANTIĞI EKLENDİ
        if (boxCollider != null)
        {
            boxCollider.center = currentCarData.colliderCenter;
            boxCollider.size = currentCarData.colliderSize;
        }
        wheels.Clear(); // Eski arabanın tekerleklerini unut

        // Yeni doğan arabanın içindeki BÜTÜN parçaları tarıyoruz
        Transform[] allChildren = newModel.GetComponentsInChildren<Transform>();
        foreach (Transform child in allChildren)
        {
            // Eğer parçanın isminde (büyük/küçük harf fark etmeksizin) "wheel" geçiyorsa listeye al
            if (child.name.ToLower().Contains("wheel"))
            {
                wheels.Add(child);
            }
        }
    }

    private void Update()
    {
        turnInput = 0f;

        if (Input.GetAxisRaw("Horizontal") != 0)
        {
            turnInput = Input.GetAxisRaw("Horizontal");
        }

        // Mobil cihazlarda ekranın sağına ve soluna dokunmayı algılayacak Touch Input sistemini entegre ettim
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.position.x < Screen.width / 2f) turnInput = -1f;
            else if (touch.position.x > Screen.width / 2f) turnInput = 1f;
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ActivateSpeedBoost(2f, 1.5f);
        }

        // Tekerlek animasyonunu her karede çalıştır
        SpinWheels();

        // UI sistemini beklerken hızlanma (Dash) yeteneğini kendi ortamımda test etmek için Space tuşunu atadım
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ActivateSpeedBoost(2f, 1.5f);
        }
    }

    // 3. Kişinin arayüzden çağıracağı hızlanma fonksiyonunu public olarak ayarladım
    public void ActivateSpeedBoost(float multiplier, float duration)
    {
        StartCoroutine(SpeedBoostRoutine(multiplier, duration));
    }

    // Hızlanma sürecini arka planda asenkron yürütmek için Coroutine (Zamanlayıcı) mantığı kurdum
    private IEnumerator SpeedBoostRoutine(float multiplier, float duration)
    {
        originalMaxSpeed *= multiplier;
        yield return new WaitForSeconds(duration);
        originalMaxSpeed /= multiplier;
    }

    private void FixedUpdate()
    {
        // Fiziksel işlemlerin kare atlamadan pürüzsüz çalışması için FixedUpdate içerisinde çağırdım
        MoveCar();
        SteerCar();
        ApplyBodyLean(); // Ağırlık transferi fiziği
    }

    private void MoveCar()
    {
        float accel = currentCarData != null ? currentCarData.acceleration : 5f;
        float grip = currentCarData != null ? currentCarData.driftGrip : 3f;

        // Aracın anında max hıza çıkmasını engelleyerek yavaş yavaş ivmelenmesi için MoveTowards kullandım
        currentSpeed = Mathf.MoveTowards(currentSpeed, originalMaxSpeed, accel * Time.fixedDeltaTime);

        // OTO-DRİFT MANTIĞI: Aracın burnu dönse de, hareket vektörünü Lerp fonksiyonuyla gecikmeli getirerek yanal savrulma (drift) sağladım
        currentMoveDirection = Vector3.Lerp(currentMoveDirection, transform.forward, grip * Time.fixedDeltaTime);

        Vector3 movement = currentMoveDirection * currentSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movement);
    }

    private void SteerCar()
    {
        float tSpeed = currentCarData != null ? currentCarData.turnSpeed : 100f;

        // Kullanıcının sağ/sol girdisini alıp Y ekseninde dönüş (Quaternion) uyguladım
        float turn = turnInput * tSpeed * Time.fixedDeltaTime;
        Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
        rb.MoveRotation(rb.rotation * turnRotation);
    }

    private void ApplyBodyLean()
    {
        // GÖVDE EĞİLMESİ (AĞIRLIK TRANSFERİ): Merkezkaç kuvveti hissiyatı için dönüşlerde süspansiyon esneme mekaniği ekledim
        if (carMesh != null && carMesh.childCount > 0)
        {
            float maxLean = currentCarData != null ? currentCarData.maxLeanAngle : 15f;

            // Sağa dönerken (turnInput = 1) aracı sola yatırdım, tersi durumda sağa yatırdım (-Z ekseni)
            float targetLean = turnInput * maxLean;

            Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetLean);

            // Kasa eğilmesinin pürüzsüz (smooth) gerçekleşmesi için rotasyon geçişine Lerp uyguladım
            carMesh.localRotation = Quaternion.Lerp(carMesh.localRotation, targetRotation, 10f * Time.fixedDeltaTime);
        }
    }
    private void SpinWheels()
    {
        // Tekerleklerin dönüş hızını arabanın hızıyla çarpıyoruz (20f değerini göz zevkine göre artırıp azaltabilirsin)
        float spinAmount = currentSpeed * 20f * Time.deltaTime;

        foreach (Transform wheel in wheels)
        {
            // Tekerlekleri X ekseninde (ileri doğru) döndür
            wheel.Rotate(Vector3.right, spinAmount, Space.Self);
        }
    }
}