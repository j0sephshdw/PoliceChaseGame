using System;
using UnityEngine;

// ============================================================
// GAME MANAGER — Oyun Döngüsü ve UI (Bedirhan) sorumluluğunda.
// Oyunun o anki durumunu (Playing/Paused/GameOver) TEK bir yerden tutar.
// Diğer scriptler kendi "oyun durdu mu" değişkenini TUTMAMALI,
// bunun yerine GameManager.Instance.CurrentState'i okumalı veya
// OnGameStateChanged event'ine abone olmalı.
// ============================================================

// Oyunun olabileceği durumlar. int/string yerine enum kullanmak,
// kodun her yerinde "GameState.Paused" gibi okunaklı isimlerle
// karşılaştırma yapmamızı sağlıyor.
public enum GameState
{
    Playing,
    Paused,
    GameOver
}

public class GameManager : MonoBehaviour
{
    // Singleton: sahnede tek bir GameManager olur, her script
    // "GameManager.Instance" diyerek erişebilir (referans sürüklemeye gerek yok).
    public static GameManager Instance { get; private set; }

    // Dışarıya sadece okuma izni: durumu değiştirmenin tek yolu
    // SetState()/PauseGame()/ResumeGame()/TriggerGameOver() metotları.
    public GameState CurrentState { get; private set; }

    // Durum her değiştiğinde tetiklenir. HUD, Pause ekranı, Game Over ekranı
    // gibi UI sistemleri bunu dinleyip kendini açıp kapatacak.
    public event Action<GameState> OnGameStateChanged;

    private void Awake()
    {
        // Sahnede yanlışlıkla birden fazla GameManager oluşursa fazlasını yok edip
        // tek bir Instance kalmasını garanti ediyorum.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Sahne yüklendiğinde oyun direkt oynanabilir durumda başlasın.
        SetState(GameState.Playing);
    }

    public void SetState(GameState newState)
    {
        CurrentState = newState;

        switch (newState)
        {
            case GameState.Playing:
                Time.timeScale = 1f; // Zaman normal akışına döner
                break;
            case GameState.Paused:
                // Zaman durur. NOT: PlayerCarController'a hiç dokunmadık —
                // Time.timeScale = 0 olunca FixedUpdate içindeki Time.fixedDeltaTime
                // otomatik olarak 0 olur, araç kendiliğinden durur.
                Time.timeScale = 0f;
                break;
            case GameState.GameOver:
                Time.timeScale = 0f; // Game Over'da da her şey donsun, oyuncu araba sürmeye devam edemesin
                break;
        }

        OnGameStateChanged?.Invoke(newState);
    }

    // --- BEDİRHAN (Pause Butonu / HUD) İÇİN NOT ---
    // HUD'daki Pause butonuna tıklanınca bu fonksiyon çağrılacak.
    public void PauseGame()
    {
        // Sadece oyun oynanırken duraklatmaya izin ver (örn. zaten Game Over'dayken
        // yanlışlıkla Paused durumuna geçilmesin diye).
        if (CurrentState == GameState.Playing)
            SetState(GameState.Paused);
    }

    public void ResumeGame()
    {
        if (CurrentState == GameState.Paused)
            SetState(GameState.Playing);
    }

    // --- KİŞİ 1 (Yusuf — Araç/Can sorumlusu) İÇİN NOT ---
    // PlayerHealth.cs'deki OnPlayerDeath event'i tetiklendiğinde (can sıfırlanınca)
    // bu fonksiyonun çağrılması gerekiyor: GameManager.Instance.TriggerGameOver();
    // Bu bağlantıyı GameEvents.cs'i yazınca ben kuracağım, sizin tarafınızda
    // ekstra bir şey yapmanıza gerek yok — sadece OnPlayerDeath'in doğru anda
    // (can <= 0) tetiklendiğinden emin olun (ki zaten öyle yazılmış).
    //
    // --- KİŞİ 2 (Berat — AI/Engel sorumlusu) İÇİN NOT ---
    // Polis AI'ınızın hareketi Update/FixedUpdate + Time.deltaTime kullanıyorsa,
    // Time.timeScale = 0 olduğunda o da otomatik duracaktır, ekstra kod gerekmez.
    // Ama Coroutine'lerde WaitForSecondsRealtime veya gerçek zamanlı bir Invoke
    // kullanırsanız, bu Paused durumunda da çalışmaya devam eder — dikkat edin.
    public void TriggerGameOver()
    {
        SetState(GameState.GameOver);
    }
}