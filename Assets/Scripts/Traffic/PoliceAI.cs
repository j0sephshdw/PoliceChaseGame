using UnityEngine;

/// <summary>
/// PAKO Forever tarzı, tamamen transform tabanlı polis takip AI'ı.
/// Fizik/collider/Rigidbody kullanmaz. Polis, oyuncunun POZİSYONUNU değil,
/// oyuncunun ARKASINDAKİ hayali bir "takip noktasını" (chase point) hedefler.
/// Bu sayede oyuncu döndüğünde polis de yörüngesini yumuşakça ayarlayarak
/// sürekli arkada kalır, üstüne binmez ve takılıp kalmaz.
/// </summary>
public class PoliceAI : MonoBehaviour
{
    [Header("Referanslar")]
    [Tooltip("Takip edilecek oyuncu aracının Transform'u")]
    public Transform player;

    [Header("Takip Mesafeleri")]
    [Tooltip("Polisin oyuncunun arkasında durmaya çalışacağı ideal mesafe")]
    public float followDistance = 6f;

    [Tooltip("Polisin bu mesafenin altına asla düşmemeye çalışacağı güvenli alan")]
    public float minDistance = 4f;

    [Tooltip("Oyuncu bu mesafeden daha uzaktaysa polis maksimum hızla yetişmeye çalışır")]
    public float catchUpDistance = 12f;

    [Tooltip("Oyuncu bu mesafeden daha yakınsa polis sertçe frene basar")]
    public float brakeDistance = 3f;

    [Tooltip("Oyuncunun tam arkası yerine hafif yandan takip etmek isterseniz (+sağ / -sol)")]
    public float sideOffset = 0f;

    [Header("Hız Ayarları")]
    [Tooltip("Polisin normal seyir hızı (oyuncu sabit hızda giderken hedeflenen hız)")]
    public float baseSpeed = 8f;

    [Tooltip("Polisin ulaşabileceği maksimum hız (yetişme hızı)")]
    public float maxSpeed = 14f;

    [Tooltip("Mesafe hatasının hıza ne kadar etki edeceği (P kontrolcü kazancı)")]
    public float distanceGain = 1.5f;

    [Tooltip("Hız değişimlerinin ne kadar yumuşak olacağı (küçük = ani, büyük = yumuşak)")]
    public float speedSmoothTime = 0.4f;

    [Header("Dönüş Ayarları")]
    [Tooltip("Polisin saniyede kaç derece dönebileceği (küçük = yumuşak/ağır, büyük = keskin)")]
    public float rotationSpeed = 140f;

    [Header("Yaklaşma (Arrival) Ayarı - ÖNEMLİ")]
    [Tooltip("Polis, TAKİP NOKTASINA bu mesafeden daha yakınsa hızını orantılı azaltır. " +
             "Bu değer olmazsa polis noktayı sürekli geçip tekrar dönerek etrafında daire çizer (orbiting bug).")]
    public float arrivalRadius = 3f;

    [Header("Debug")]
    public bool drawGizmos = true;

    // --- İç değişkenler ---
    private float currentSpeed;
    private float speedVelocity; // SmoothDamp için referans
    private Vector3 lastPlayerPosition;
    private float estimatedPlayerSpeed;
    private Vector3 chasePointCached; // Gizmo çizimi için

    private void Start()
    {
        if (player != null)
            lastPlayerPosition = player.position;
    }

    private void Update()
    {
        if (player == null) return;

        EstimatePlayerSpeed();

        Vector3 chasePoint = CalculateChasePoint();
        chasePointCached = chasePoint;

        RotateTowardsChasePoint(chasePoint);

        float distanceToChasePoint = Vector3.Distance(transform.position, chasePoint);
        float targetSpeed = CalculateTargetSpeed(distanceToChasePoint);

        // Hızı yumuşakça hedefe taşı (ani ivmelenme/frenleme olmasın)
        currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedVelocity, speedSmoothTime);
        currentSpeed = Mathf.Clamp(currentSpeed, 0f, maxSpeed);

        // Transform tabanlı hareket: polis kendi burnunun gösterdiği yöne ilerler (araba mantığı)
        transform.position += transform.forward * currentSpeed * Time.deltaTime;
    }

    /// <summary>
    /// 2) TAKİP NOKTASI (CHASE POINT) MANTIĞI
    /// Oyuncunun pozisyonundan, oyuncunun BAKTIĞI yönün TERSİNE doğru
    /// followDistance kadar geriye gidilerek hayali bir nokta bulunur.
    /// Oyuncu dönünce bu nokta da oyuncuyla birlikte döner,
    /// böylece polis "oyuncunun arkası" kavramını her zaman takip eder.
    /// </summary>
    private Vector3 CalculateChasePoint()
    {
        Vector3 behindPlayer = player.position - player.forward * followDistance;
        Vector3 sideShift = player.right * sideOffset;
        return behindPlayer + sideShift;
    }

    private void RotateTowardsChasePoint(Vector3 chasePoint)
    {
        Vector3 direction = chasePoint - transform.position;
        direction.y = 0f; // Yerde kalan araçlar için yükseklik farkını yok say

        if (direction.sqrMagnitude < 0.0001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

        // RotateTowards ile MAKSİMUM açısal hız sınırlanır -> ani/keskin dönüş olmaz
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    /// <summary>
    /// Basit bir "P kontrolcü" (oransal kontrol) mantığıyla hız hesaplanır:
    /// - Oyuncuya olan gerçek mesafe, ideal mesafeden (followDistance) FAZLA ise
    ///   polis oyuncunun hızının ÜSTÜNE çıkar (yetişir).
    /// - Mesafe ideal mesafeden AZ ise polis oyuncunun hızının ALTINA iner (uzaklaşır, üstüne binmez).
    /// - Çok yakınsa (brakeDistance) sert fren yapar.
    /// - Çok uzaksa (catchUpDistance) maksimum hıza geçer.
    /// </summary>
    private float CalculateTargetSpeed(float distanceToChasePoint)
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        float target;

        // Çok yakın -> sert fren, neredeyse dur
        if (distanceToPlayer < brakeDistance)
        {
            target = Mathf.Max(0f, estimatedPlayerSpeed * 0.3f);
        }
        // Çok uzak -> tam gaz yetişme modu
        else if (distanceToPlayer > catchUpDistance)
        {
            target = maxSpeed;
        }
        else
        {
            // Normal durum: hata payına göre oransal ayar
            float distanceError = distanceToPlayer - followDistance;
            target = estimatedPlayerSpeed + distanceError * distanceGain;

            // minDistance altına düşmemeye ekstra özen göster
            if (distanceToPlayer < minDistance)
            {
                target = Mathf.Min(target, estimatedPlayerSpeed * 0.5f);
            }
        }

        target = Mathf.Clamp(target, 0f, maxSpeed);

        // --- ARRIVAL / YAVAŞLAMA (orbiting hatasının çözümü) ---
        // Polis, hedef olan TAKİP NOKTASINA çok yaklaştıysa, o noktayı "geçip"
        // sürekli dönmemesi için hızını noktaya olan mesafeyle orantılı düşür.
        // Oyuncu duruyorsa bu, polisin noktanın hemen gerisinde sakince durmasını sağlar.
        if (distanceToChasePoint < arrivalRadius)
        {
            float arrivalFactor = Mathf.Clamp01(distanceToChasePoint / arrivalRadius);
            target *= arrivalFactor;
        }

        return target;
    }

    private void EstimatePlayerSpeed()
    {
        if (Time.deltaTime <= 0f) return;

        float displacement = Vector3.Distance(player.position, lastPlayerPosition);
        float instantSpeed = displacement / Time.deltaTime;

        // Ani titremeleri azaltmak için hafif yumuşatma
        estimatedPlayerSpeed = Mathf.Lerp(estimatedPlayerSpeed, instantSpeed, 0.3f);

        lastPlayerPosition = player.position;
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos || player == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(chasePointCached, 0.4f);
        Gizmos.DrawLine(transform.position, chasePointCached);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(player.position, minDistance);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(player.position, followDistance);
    }
}
