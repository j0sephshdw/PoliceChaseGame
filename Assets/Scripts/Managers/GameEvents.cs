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
    // Singleton: sahnede tek bir GameEvents olur, her script
    // "GameEvents.Instance" diyerek erişebilir.
    public static GameEvents Instance { get; private set; }

    // Artık Inspector'dan elle sürüklenmiyor — her haritaya (sahneye) kopyalandığında
    // o sahnenin kendi PlayerHealth'ini otomatik bulsun diye Awake()'te aranıyor.
    private PlayerHealth playerHealth;

    // HUD ve diğer UI scriptleri PlayerHealth'i değil, bu event'leri dinleyecek.
    public event Action<int, int> OnPlayerHealthChanged;
    public event Action OnPlayerDied;

    private void Awake()
    {
        // Sahnede yanlışlıkla birden fazla GameEvents oluşursa fazlasını yok edip
        // tek bir Instance kalmasını garanti ediyorum.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Bu sahnedeki PlayerHealth'i otomatik bul (hangi haritaya kopyalanırsak kopyalanalım çalışsın diye).
        playerHealth = FindAnyObjectByType<PlayerHealth>();
    }

    private void OnEnable()
    {
        // PlayerHealth'in event'lerine abone oluyorum. "if" kontrolü, playerHealth
        // bulunamazsa (null kalırsa) çökme yaşanmasını engelliyor.
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += HandleHealthChanged;
            playerHealth.OnPlayerDeath += HandlePlayerDeath;
        }
    }

    private void OnDisable()
    {
        // Her += için bir -= şart: aksi halde obje tekrar aktif olduğunda
        // aynı fonksiyon listeye ikinci kez eklenir ve birden fazla kez tetiklenir.
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= HandleHealthChanged;
            playerHealth.OnPlayerDeath -= HandlePlayerDeath;
        }
    }

    // PlayerHealth.OnHealthChanged tetiklenince çalışır, gelen değeri kendi
    // event'imize aktarıp (forward) HUD'a haber veriyorum.
    private void HandleHealthChanged(int currentHealth, int maxHealth)
    {
        OnPlayerHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // PlayerHealth.OnPlayerDeath tetiklenince (can sıfırlanınca) çalışır.
    private void HandlePlayerDeath()
    {
        ScoreManager.Instance.SaveHighScoreIfNeeded();
        GameManager.Instance.TriggerGameOver();
        OnPlayerDied?.Invoke();
    }

    // --- KİŞİ 1 (Yusuf) İÇİN NOT ---
    // PlayerHealth.cs'deki OnHealthChanged/OnPlayerDeath event imzalarını
    // değiştirirseniz bana haber verin, sadece bu dosyayı güncellemem yeterli olur.
}