using UnityEngine;
using System.Collections;

// ============================================================
// TOUCH HINT — Oyun Döngüsü ve UI (Bedirhan) sorumluluğunda.
// Tur başladığında ekranın sol ve sağ yarısında birer ok gösterip
// kısa süre sonra soldurur. Dokunmatik kontrolün nasıl çalıştığı
// ilk bakışta belli olmadığı için eklendi.
// ============================================================
public class TouchHint : MonoBehaviour
{
    [Header("Referans")]
    [SerializeField] private CanvasGroup hintGroup;

    [Header("Süreler")]
    [Tooltip("Göstergenin tam görünür kalacağı süre")]
    [SerializeField] private float holdDuration = 2f;
    [Tooltip("Solma süresi")]
    [SerializeField] private float fadeDuration = 0.8f;
    [Tooltip("Belirme süresi")]
    [SerializeField] private float fadeInDuration = 0.4f;

    private bool alreadyShown;

    private void Start()
    {
        if (hintGroup != null) hintGroup.alpha = 0f;

        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDestroy()
    {
        // Sahne kapanırken GameManager bizden önce yok edilmiş olabilir
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void HandleGameStateChanged(GameState newState)
    {
        // Yalnızca turun ilk kez başladığı anda gösteriyoruz.
        // Duraklatıp devam edildiğinde durum tekrar Playing olduğu için bu kontrol gerekli.
        if (newState != GameState.Playing || alreadyShown) return;

        alreadyShown = true;
        StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        if (hintGroup == null) yield break;

        hintGroup.alpha = 0f;

        // Ölçeklenmemiş zaman kullanıyoruz; oyun duraklatılsa bile gösterge ekranda takılı kalmasın

        // Belirme: saydamlık 0'dan 1'e
        float timer = 0f;
        while (timer < fadeInDuration)
        {
            timer += Time.unscaledDeltaTime;
            hintGroup.alpha = timer / fadeInDuration;
            yield return null;
        }
        hintGroup.alpha = 0.7f;

        // Tam görünür kalma
        timer = 0f;
        while (timer < holdDuration)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        // Solma: saydamlık 1'den 0'a
        timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            hintGroup.alpha = 1f - (timer / fadeDuration);
            yield return null;
        }

        hintGroup.alpha = 0f;
        hintGroup.gameObject.SetActive(false); // İşi bitti, tamamen devre dışı bırak
    }
}