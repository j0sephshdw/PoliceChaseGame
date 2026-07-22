using System;
using UnityEngine;

// ============================================================
// SCORE MANAGER — Oyun Döngüsü ve UI (Bedirhan) sorumluluğunda.
// Skor, XP ve seviye sistemini TEK bir yerden yönetir.
// Diğer scriptler kendi skor/XP değişkenlerini TUTMAMALI,
// sadece ScoreManager.Instance üzerinden bu sisteme erişmeli.
// ============================================================
public class ScoreManager : MonoBehaviour
{
    // Singleton: sahnede tek bir ScoreManager olur, her script
    // "ScoreManager.Instance" diyerek erişebilir (referans sürüklemeye gerek yok).
    public static ScoreManager Instance { get; private set; }

    // PlayerPrefs'te en yüksek skoru okurken/yazarken kullanılan anahtar ismi.
    // İki yerde ayrı ayrı yazıp yanlışlıkla farklı isim yazma riskini önlemek için sabit tuttum.
    private const string HighScoreKey = "HighScore";

    [Header("Skor Ayarları")]
    [SerializeField] private int score = 0; // Anlık skor (Inspector'dan test amaçlı izlenebilir)

    [Header("XP / Seviye Ayarları")]
    [SerializeField] private int level = 1;              // Oyuncunun mevcut seviyesi, 1'den başlar
    [SerializeField] private int currentXP = 0;           // Mevcut seviyede biriken XP
    [SerializeField] private int baseXPToLevelUp = 100;   // Seviye başı gereken XP çarpanı (dengelemek istersen buradan oyna)

    // Dışarıya SADECE okuma izni veren property'ler.
    // Skoru/XP'yi değiştirmenin tek yolu AddScore()/AddXP() — böylece başka bir
    // script yanlışlıkla "score = 999999" gibi bir satır yazamaz.
    public int Score => score;
    public int Level => level;
    public int CurrentXP => currentXP;
    public int XPToNextLevel { get; private set; }

    // --- EVENT'LER ---
    // HUD (can/skor barı) ve Kart Seçim Ekranı gibi sistemler bunlara abone olacak.
    public event Action<int> OnScoreChanged;   // Skor her değiştiğinde tetiklenir
    public event Action<int, int> OnXPChanged; // (mevcut XP, sonraki seviyeye gereken XP)
    public event Action<int> OnLevelUp;        // Seviye atlanınca tetiklenir -> Kart Seçim Ekranı bunu dinleyecek

    private void Awake()
    {
        // Sahnede yanlışlıkla birden fazla ScoreManager oluşursa fazlasını yok edip
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
        // Oyun başlar başlamaz ilk seviyenin XP eşiğini hesaplıyorum ki
        // HUD, doğru bir başlangıç değeriyle (örn. 0/100) açılsın.
        XPToNextLevel = CalculateXPToNextLevel(level);
        OnXPChanged?.Invoke(currentXP, XPToNextLevel);
    }

    // --- KİŞİ 2 (Berat — Çevre/Engel/AI sorumlusu) İÇİN NOT ---
    // Bir polis arabası etkisiz hale getirildiğinde, bir engel başarıyla aşıldığında
    // veya benzeri "puan kazandıran" bir olay olduğunda kendi scriptinden şunu çağır:
    //     ScoreManager.Instance.AddScore(10);
    // (Miktarı olayın önemine göre sen belirleyebilirsin, dengeleme konusunu birlikte konuşuruz.)
    public void AddScore(int amount)
    {
        score += amount;
        OnScoreChanged?.Invoke(score);
    }

    // --- KİŞİ 2 (Berat) İÇİN NOT ---
    // XP kazandıran bir olay olduğunda: ScoreManager.Instance.AddXP(25);
    // Skor ile XP'yi kasıtlı olarak ayrı tutuyorum: XP seviye atlamayı tetikler,
    // skor ise sadece Game Over ekranı ve Ana Menü'de gösterilen "başarı" sayısıdır.
    public void AddXP(int amount)
    {
        currentXP += amount;

        // if değil while kullanıyorum çünkü tek seferde büyük XP kazanılırsa
        // (örn. 250 XP) oyuncu birden fazla seviye birden atlayabilmeli.
        while (currentXP >= XPToNextLevel)
        {
            currentXP -= XPToNextLevel;
            LevelUp();
        }

        OnXPChanged?.Invoke(currentXP, XPToNextLevel);
    }

    private void LevelUp()
    {
        level++;
        XPToNextLevel = CalculateXPToNextLevel(level);

        // Bu event tetiklenince Kart Seçim Ekranı (CardSelectionUI) açılacak;
        // seçilen kart da Kişi 1'in (Yusuf) yazacağı IAbility.Activate() fonksiyonunu tetikleyecek.
        OnLevelUp?.Invoke(level);
    }

    // Seviye arttıkça bir sonraki seviyeye ulaşmanın daha fazla XP istemesini sağlayan
    // basit bir formül (100, 200, 300... şeklinde doğrusal artıyor).
    private int CalculateXPToNextLevel(int currentLevel)
    {
        return baseXPToLevelUp * currentLevel;
    }

    // --- OYUN DÖNGÜSÜ NOTU (Bedirhan/GameManager tarafı) ---
    // Game Over anında (PlayerHealth.OnPlayerDeath tetiklenince) bu fonksiyon çağrılacak;
    // mevcut skor rekoru geçtiyse PlayerPrefs'e kaydedilecek.
    public void SaveHighScoreIfNeeded()
    {
        int savedHighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        if (score > savedHighScore)
        {
            PlayerPrefs.SetInt(HighScoreKey, score);
            PlayerPrefs.Save();
        }
    }

    // --- KİŞİ 1 (Yusuf) ve KİŞİ 2 (Berat) İÇİN NOT ---
    // Bu metot static olduğu için ScoreManager.Instance'a ihtiyaç duymadan,
    // hangi sahnede olursanız olun ScoreManager.GetHighScore() diye direkt çağırabilirsiniz.
    // (Ben bunu Ana Menü sahnesinde en yüksek skoru göstermek için kullanıyorum.)
    public static int GetHighScore()
    {
        return PlayerPrefs.GetInt(HighScoreKey, 0);
    }
}