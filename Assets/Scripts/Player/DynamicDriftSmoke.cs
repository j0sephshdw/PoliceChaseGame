using UnityEngine;

public class DynamicDriftSmoke : MonoBehaviour
{
    [Header("Bileşenler")]
    public ParticleSystem leftSmoke;
    public ParticleSystem rightSmoke;
    public BoxCollider carCollider;

    [Header("Ayarlar")]
    [Tooltip("Dumanın çıkması için arabanın ne kadar hızlı hareket etmesi gerektiği (Duran arabada direksiyonu çevirince duman çıkmasın diye).")]
    public float minimumSpeedForSmoke = 1.0f; // Sadece hız kontrolü için bir eşik

    // Play/Stop yerine sadece vanayı açıp kapatmak için Emission modüllerini alıyoruz
    private ParticleSystem.EmissionModule leftEmission;
    private ParticleSystem.EmissionModule rightEmission;

    private PlayerCarController carController;
    private Vector3 lastPosition;
    private float currentSpeed;

    void Start()
    {
        lastPosition = transform.position;

        // Modülleri koda bağlıyoruz
        leftEmission = leftSmoke.emission;
        rightEmission = rightSmoke.emission;

        // Duman sistemini baştan başlat ve hiç durdurma
        if (!leftSmoke.isPlaying) leftSmoke.Play();
        if (!rightSmoke.isPlaying) rightSmoke.Play();

        // Sadece vanaları (emission) kapalı tut
        leftEmission.enabled = false;
        rightEmission.enabled = false;

        // Dönüş girdisini okuyabilmek için PlayerCarController'ı alıyoruz
        carController = GetComponentInParent<PlayerCarController>();
    }

    void FixedUpdate()
    {
        if (carCollider == null || Time.fixedDeltaTime == 0) return;

        // 1. Dumanların Yerini Otomatik Ayarla (Jant hizası)
        Vector3 center = carCollider.center;
        Vector3 size = carCollider.size;
        float smokeHeight = center.y - (size.y / 2) + 0.3f;

        leftSmoke.transform.localPosition = new Vector3(center.x - (size.x / 2), smokeHeight, center.z - (size.z / 2));
        rightSmoke.transform.localPosition = new Vector3(center.x + (size.x / 2), smokeHeight, center.z - (size.z / 2));

        // 2. Sadece arabanın ileri doğru hareket edip etmediğini kontrol et (Duran arabada duman çıkmasın)
        currentSpeed = Vector3.Distance(transform.position, lastPosition) / Time.fixedDeltaTime;
        lastPosition = transform.position;

        // 3. Duman Eşiği Kontrolü: 
        // Eğer araba hareket ediyorsa VE oyuncu sağa/sola dönüyorsa (Input sıfır değilse) duman çıkar.
        bool isDrifting = false;

        if (carController != null && currentSpeed > minimumSpeedForSmoke)
        {
            // Input.GetAxisRaw("Horizontal") veya senin koddaki turnInput değerini kontrol ediyoruz.
            // Eğer oyuncu dönüyorsa (turnInput sıfır değilse) duman çıkar
            float horizontalInput = Input.GetAxisRaw("Horizontal");
            if (Mathf.Abs(horizontalInput) > 0.1f)
            {
                isDrifting = true;
            }
        }

        leftEmission.enabled = isDrifting;
        rightEmission.enabled = isDrifting;
    }
}