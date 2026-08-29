using UnityEngine;
using Unity.Cinemachine;

// ============================================================
// SPEED FOV — Oyun Döngüsü ve UI (Bedirhan) sorumluluğunda.
// Araç hızlandıkça kameranın görüş açısını hafifçe açarak hız hissini artırır.
// Her sanal kameranın üzerine ayrı ayrı eklenir; taban açı olarak kameranın
// kendi ayarı okunduğu için üstten ve arkadan kameralar farklı açılarda olsa da
// ikisi de doğru çalışır.
// ============================================================
[RequireComponent(typeof(CinemachineCamera))]
public class SpeedFOV : MonoBehaviour
{
    [Tooltip("Tam hızda görüş açısına eklenecek derece miktarı")]
    [SerializeField] private float fovIncrease = 12f;
    [Tooltip("Görüş açısının değişim yumuşaklığı; küçük değer daha yumuşak")]
    [SerializeField] private float fovLerpSpeed = 3f;

    private CinemachineCamera vcam;
    private PlayerCarController player;
    private float baseFOV;

    private void Awake()
    {
        vcam = GetComponent<CinemachineCamera>();
        baseFOV = vcam.Lens.FieldOfView; // Kameranın Inspector'daki kendi açısını taban alıyoruz
    }

    private void Update()
    {
        // Oyuncu aracı araç seçimi sırasında kapalı olduğu için tek seferde bulunamıyor;
        // bulunana kadar her karede tekrar deniyoruz.
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.GetComponent<PlayerCarController>();
            if (player == null) return;
        }

        float maxSpeed = Mathf.Max(0.1f, player.MaxSpeed); // Sıfıra bölmeyi engelliyoruz
        float speedRatio = Mathf.Clamp01(Mathf.Abs(player.CurrentSpeed) / maxSpeed);

        float targetFOV = baseFOV + (fovIncrease * speedRatio);

        // LensSettings bir struct olduğu için önce kopyasını alıp değiştirip geri yazıyoruz
        LensSettings lens = vcam.Lens;
        lens.FieldOfView = Mathf.Lerp(lens.FieldOfView, targetFOV, fovLerpSpeed * Time.deltaTime);
        vcam.Lens = lens;
    }
}