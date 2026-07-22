using UnityEngine;
using System;

// ============================================================
// GAME EVENTS — Oyun Döngüsü ve UI (Bedirhan) sorumluluğunda.
// Kişi 1'in (Yusuf) yazdığı PlayerHealth.cs'i dinleyip, can/ölüm durumunu
// GameManager ve ScoreManager'a bağlayan "köprü" script'i.
// HUD gibi UI scriptleri PlayerHealth'i DOĞRUDAN tanımamalı, bunun yerine
// sadece bu script'in event'lerine (OnPlayerHealthChanged/OnPlayerDied) abone olmalı.
// ============================================================
public class GameEvents : MonoBehaviour
{
    public static GameEvents Instance { get; private set; }

    [SerializeField] private PlayerHealth playerHealth;

    // (mevcut can, maksimum can) — max artık sabit değil, seçili araca (CarData) göre
    // değişebiliyor, bu yüzden HUD can barını oranlarken (current / max) ikisini de almalı.
    public event Action<int, int> OnPlayerHealthChanged;
    public event Action OnPlayerDied;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += HandleHealthChanged;
            playerHealth.OnPlayerDeath += HandlePlayerDeath;
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= HandleHealthChanged;
            playerHealth.OnPlayerDeath -= HandlePlayerDeath;
        }
    }

    // PlayerHealth.OnHealthChanged artık Action<int, int> olduğu için imza burada da
    // iki int parametre almak zorunda, aksi halde derleyici eşleştiremiyor (aldığımız hata buydu).
    private void HandleHealthChanged(int currentHealth, int maxHealth)
    {
        OnPlayerHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void HandlePlayerDeath()
    {
        ScoreManager.Instance.SaveHighScoreIfNeeded();
        GameManager.Instance.TriggerGameOver();
        OnPlayerDied?.Invoke();
    }

    // --- KİŞİ 1 (Yusuf) İÇİN NOT ---
    // PlayerHealth.cs'deki OnHealthChanged/OnPlayerDeath event imzalarını
    // (parametre tipi/sayısı) değiştirirseniz bana haber verin, sadece bu dosyayı
    // güncellemem yeterli olur — HUD/GameOver tarafında hiçbir şey bozulmaz.
    //
    // --- KİŞİ 2 (Berat) İÇİN NOT ---
    // Bu script sadece oyuncunun canını dinliyor; polis/engel AI'ınızla ilgili
    // doğrudan bir bağlantısı yok. İleride "oyuncu bir polis arabasını etkisiz
    // hale getirdi" gibi bir olay eklerseniz, onu ScoreManager.Instance.AddScore()
    // ile doğrudan kendi scriptinizden çağırmanız yeterli (GameEvents'e gerek yok).
}