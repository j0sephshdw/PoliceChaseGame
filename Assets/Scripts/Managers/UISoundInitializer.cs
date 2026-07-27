using UnityEngine;

// ============================================================
// UI SOUND INITIALIZER — Oyun Döngüsü ve UI (Bedirhan) sorumluluğunda.
// Tek görevi: Inspector'dan atanan ses dosyalarını UISoundPlayer'ın
// static alanlarına aktarmak. Bu script'in SADECE "MainMenu" sahnesinde
// bir kere bulunması yeterli — çünkü UISoundPlayer'daki static alanlar,
// bir kere doldurulduktan sonra sahne değişse bile hafızada kalıyor
// (tıpkı ScoreManager.GetHighScore()'un PlayerPrefs'ten okuduğu gibi,
// ama bu sefer PlayerPrefs değil, doğrudan bellekte).
// ============================================================
public class UISoundInitializer : MonoBehaviour
{
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private AudioClip cardSelectClip;
    [SerializeField] private AudioClip errorClip;

    private void Awake()
    {
        UISoundPlayer.clickClip = clickClip;
        UISoundPlayer.cardSelectClip = cardSelectClip;
        UISoundPlayer.errorClip = errorClip;
    }
}