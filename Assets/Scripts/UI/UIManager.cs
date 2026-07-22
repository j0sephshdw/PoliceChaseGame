using UnityEngine;
using UnityEngine.SceneManagement;

// ============================================================
// UI MANAGER (Ana Menü) — Oyun Döngüsü ve UI (Bedirhan) sorumluluğunda.
// Sadece "MainMenu" sahnesinde çalışır; Ana Menü, Ayarlar ve Nasıl Oynanır
// panelleri arasındaki geçişleri ve oyun sahnesine geçişi yönetir.
// Kişi 1/Kişi 2'nin sistemleriyle doğrudan bir bağlantısı yok — bu tamamen
// menü sahnesine özel, bağımsız bir script.
// ============================================================
public class UIManager : MonoBehaviour
{
    [Header("Paneller")]
    // Inspector'dan Hierarchy'deki ilgili panel objeleri buraya sürüklenip bağlandı.
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject howToPlayPanel;

    [Header("Sahne Ayarları")]
    // "OYNA" butonuna basılınca yüklenecek sahnenin adı. Sahne ileride
    // "Game" gibi bir isme değişirse burayı Inspector'dan güncellemek yeterli,
    // kodda arama yapmaya gerek kalmaz.
    [SerializeField] private string gameSceneName = "SampleScene";

    private void Start()
    {
        // Sahne ilk açıldığında sadece Ana Menü görünsün, diğer iki panel kapalı kalsın.
        ShowMainMenu();
    }

    // Üç ShowX() fonksiyonu da aynı mantıkta çalışıyor: istenen paneli açıp
    // diğer ikisini kapatıyor. Aynı anda birden fazla panelin açık kalmasını
    // (örn. Ayarlar ile Ana Menü'nün üst üste binmesini) bu şekilde engelliyoruz.
    public void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        settingsPanel.SetActive(false);
        howToPlayPanel.SetActive(false);
    }

    public void ShowSettings()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
        howToPlayPanel.SetActive(false);
    }

    public void ShowHowToPlay()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        howToPlayPanel.SetActive(true);
    }

    // "OYNA" butonuna bağlı. SceneManager.LoadScene, mevcut sahneyi (MainMenu)
    // kapatıp belirtilen sahneyi (gameSceneName) yükler.
    public void PlayGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    // "ÇIKIŞ" butonuna bağlı. Application.Quit() gerçek (build alınmış) oyunda çalışır;
    // Unity Editor içinde Play modunda hiçbir şey yapmaz, bu yüzden altındaki
    // #if UNITY_EDITOR bloğu sadece Editor'de test ederken Play modundan çıkmamızı sağlıyor.
    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // --- TODO (Bedirhan) ---
    // Ses aç/kapa butonu ve "En Yüksek Skor" yazısı şu an sahnede görsel olarak
    // duruyor ama bu script'e henüz bağlı değil. Sonraki bir adımda:
    //   - Ses butonu: AudioListener.volume ile mute/unmute + PlayerPrefs'e kaydetme
    //   - En Yüksek Skor yazısı: Start()'ta ScoreManager.GetHighScore() okunup metne yazılacak
    // eklenecek.
}