using UnityEngine;
using TMPro;

// ============================================================
// XP POPUP — Oyun Döngüsü ve UI (Bedirhan) sorumluluğunda.
// XP kazanıldığında ekranda "+50 XP" yazısını gösterip yukarı kaydırarak söndürür.
// Kısa aralıkla gelen kazanımlar üst üste binmesin diye tek yazıda toplanır.
// ============================================================
public class XPPopup : MonoBehaviour
{
    [Header("Referans")]
    [SerializeField] private TMP_Text popupText;

    [Header("Animasyon")]
    [Tooltip("Yazının kaç piksel yukarı kayacağı")]
    [SerializeField] private float riseDistance = 40f;
    [Tooltip("Yazının ekranda kalma süresi")]
    [SerializeField] private float duration = 1f;
    [Tooltip("Bu süre içinde gelen yeni kazanımlar mevcut yazıya eklenir")]
    [SerializeField] private float accumulateWindow = 0.5f;

    private RectTransform rect;
    private Vector2 startPosition;
    private float timer = -1f; // Negatifse animasyon çalışmıyor demektir
    private int accumulated;

    private void Awake()
    {
        rect = popupText.rectTransform;
        startPosition = rect.anchoredPosition; // Yazının Inspector'daki konumu başlangıç noktası
        SetAlpha(0f);
    }

    private void Start()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnXPGained += HandleXPGained;
    }

    private void OnDestroy()
    {
        // Sahne kapanırken ScoreManager bizden önce yok edilmiş olabilir
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnXPGained -= HandleXPGained;
    }

    private void HandleXPGained(int amount)
    {
        // Animasyon henüz yeni başladıysa miktarı mevcut yazıya ekliyoruz,
        // aksi halde yeni bir yazı başlatıyoruz.
        if (timer >= 0f && timer < accumulateWindow)
            accumulated += amount;
        else
            accumulated = amount;

        timer = 0f;
        popupText.text = "+" + accumulated + " XP";
    }

    private void Update()
    {
        if (timer < 0f) return;

        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / duration);

        // Yazı yukarı doğru kayıyor
        rect.anchoredPosition = startPosition + Vector2.up * (riseDistance * t);

        // İlk yarıda tam görünür, ikinci yarıda sönüyor
        SetAlpha(1f - Mathf.Clamp01((t - 0.5f) / 0.5f));

        if (t >= 1f)
        {
            timer = -1f;
            rect.anchoredPosition = startPosition; // Bir sonraki kazanım için başa alıyoruz
            SetAlpha(0f);
        }
    }

    private void SetAlpha(float alpha)
    {
        Color c = popupText.color;
        c.a = alpha;
        popupText.color = c;
    }
}