using UnityEngine;
using UnityEngine.UI;

// ============================================================
// DAMAGE VIGNETTE — Oyun Döngüsü ve UI (Bedirhan) sorumluluğunda.
// Ekranın üzerine bindirilen kırmızı bir görselle hasar geri bildirimi verir.
// İki ayrı davranışı var:
//  1) Hasar alındığı anda kısa süreli bir parlama
//  2) Can kritik seviyenin altına indiğinde sürekli, hafifçe nabız atan kırmızılık
// Canı doğrudan PlayerHealth'ten değil GameEvents üzerinden dinliyor; böylece
// oyuncu aracı araç seçimi sırasında kapalı olsa bile referans sorunu çıkmıyor.
// ============================================================
public class DamageVignette : MonoBehaviour
{
    [Header("Referans")]
    [SerializeField] private Image vignetteImage;

    [Header("Hasar Parlaması")]
    [Tooltip("Hasar anında ulaşılacak en yüksek saydamsızlık")]
    [Range(0f, 1f)][SerializeField] private float flashAlpha = 0.4f;
    [Tooltip("Parlamanın sönme hızı")]
    [SerializeField] private float fadeSpeed = 2f;

    [Header("Kritik Can Uyarısı")]
    [Tooltip("Can bu oranın altına inince kritik sayılır (0.3 = %30)")]
    [Range(0f, 1f)][SerializeField] private float criticalHealthRatio = 0.3f;
    [Tooltip("Kritik durumdaki taban saydamsızlık")]
    [Range(0f, 1f)][SerializeField] private float criticalBaseAlpha = 0.2f;
    [Tooltip("Kritik durumda nabız atma hızı")]
    [SerializeField] private float pulseSpeed = 2.5f;

    private float flashAmount;      // Hasar parlamasının o anki miktarı (0-1)
    private float healthRatio = 1f; // Mevcut can oranı (0-1)
    private int lastHealth = -1;    // Bir önceki can; azaldıysa hasar almışız demektir

    private void Start()
    {
        if (GameEvents.Instance != null)
            GameEvents.Instance.OnPlayerHealthChanged += HandleHealthChanged;

        SetAlpha(0f);
    }

    private void OnDestroy()
    {
        // Sahne kapanırken GameEvents bizden önce yok edilmiş olabilir, önce null kontrolü yapıyoruz
        if (GameEvents.Instance != null)
            GameEvents.Instance.OnPlayerHealthChanged -= HandleHealthChanged;
    }

    private void HandleHealthChanged(int currentHealth, int maxHealth)
    {
        healthRatio = maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;

        // Sadece can AZALDIYSA parlama tetikleniyor; iyileşmede veya oyun başındaki
        // ilk bildirimde parlama olmaması için önceki değerle karşılaştırıyoruz.
        if (lastHealth >= 0 && currentHealth < lastHealth)
            flashAmount = 1f;

        lastHealth = currentHealth;
    }

    private void Update()
    {
        if (vignetteImage == null) return;

        // Hasar parlaması zamanla sönüyor. Duraklatmada da sönebilmesi için
        // ölçeklenmemiş zaman kullanıyoruz (duraklatmada Time.deltaTime sıfır olur).
        flashAmount = Mathf.MoveTowards(flashAmount, 0f, fadeSpeed * Time.unscaledDeltaTime);
        float flash = flashAmount * flashAlpha;

        // Can kritik seviyenin altındaysa sürekli bir kırmızılık ekleniyor.
        // Can azaldıkça şiddeti artıyor ve nabız daha belirgin hale geliyor.
        float critical = 0f;
        if (healthRatio > 0f && healthRatio < criticalHealthRatio)
        {
            float severity = 1f - (healthRatio / criticalHealthRatio);           // Cana göre 0-1 arası şiddet
            float pulse = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f; // 0-1 arası yumuşak nabız
            critical = criticalBaseAlpha * severity * Mathf.Lerp(0.5f, 1f, pulse);
        }

        // İkisinden büyük olanı gösteriyoruz; toplasaydık üst üste binip ekranı fazla kızartırdı
        SetAlpha(Mathf.Max(flash, critical));
    }

    private void SetAlpha(float alpha)
    {
        Color c = vignetteImage.color;
        c.a = alpha;
        vignetteImage.color = c;
    }
}