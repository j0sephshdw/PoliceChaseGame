using UnityEngine;

public class DynamicDriftSmoke : MonoBehaviour
{
    [Header("Bileşenler")]
    public ParticleSystem leftSmoke;
    public ParticleSystem rightSmoke;
    public BoxCollider carCollider;

    [Header("Ayarlar")]
    [Tooltip("Dumanın çıkması için gereken kayma şiddeti. (Çok düşürdük)")]
    public float driftThreshold = 0.2f;

    private Vector3 lastPosition;

    // Play/Stop yerine sadece vanayı açıp kapatmak için Emission modüllerini alıyoruz
    private ParticleSystem.EmissionModule leftEmission;
    private ParticleSystem.EmissionModule rightEmission;

    void Start()
    {
        lastPosition = transform.position;

        // Modülleri koda bağlıyoruz
        leftEmission = leftSmoke.emission;
        rightEmission = rightSmoke.emission;

        // Duman sistemini baştan başlat ve hiç durdurma
        if (!leftSmoke.isPlaying) leftSmoke.Play();
        if (!rightSmoke.isPlaying) rightSmoke.Play();

        // Sadece vanaları (emission) kapalı tut, araba kayınca açacağız
        leftEmission.enabled = false;
        rightEmission.enabled = false;
    }

    // Fiziğin hesaplandığı yerle senkronize çalışması için FixedUpdate kullanıyoruz!
    void FixedUpdate()
    {
        if (carCollider == null || Time.fixedDeltaTime == 0) return;

        // 1. Dumanların Yerini Otomatik Ayarla (Jant hizası)
        Vector3 center = carCollider.center;
        Vector3 size = carCollider.size;

        float smokeHeight = center.y - (size.y / 2) + 0.3f;

        leftSmoke.transform.localPosition = new Vector3(center.x - (size.x / 2), smokeHeight, center.z - (size.z / 2));
        rightSmoke.transform.localPosition = new Vector3(center.x + (size.x / 2), smokeHeight, center.z - (size.z / 2));

        // 2. Gerçek Hız Hesabı (Artık fizik karesinde hatasız ölçülecek)
        Vector3 realVelocity = (transform.position - lastPosition) / Time.fixedDeltaTime;
        lastPosition = transform.position;

        // 3. Yanal kayma (Drift) şiddetini vektörlerle bul
        float lateralVelocity = Vector3.Dot(realVelocity, transform.right);

        // 4. Eşik aşıldıysa duman vanasını aç, toparladıysa kapat!
        bool isDrifting = Mathf.Abs(lateralVelocity) > driftThreshold;

        leftEmission.enabled = isDrifting;
        rightEmission.enabled = isDrifting;
    }
}