using UnityEngine;

// ============================================================
// UI MANAGER (Ana Menü) — Oyun Döngüsü ve UI (Bedirhan) sorumluluğunda.
// Sadece "MainMenu" sahnesinde çalışır; Ana Menü, Ayarlar, Nasıl Oynanır
// ve Harita Seçim panelleri arasındaki geçişleri yönetir.
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
    [SerializeField] private GameObject mapSelectionPanel; // Harita Seçim ekranı (yeni eklendi)

    private void Start()
    {
        // Sahne ilk açıldığında sadece Ana Menü görünsün, diğer paneller kapalı kalsın.
        ShowMainMenu();
    }

    // Dört ShowX() fonksiyonu da aynı mantıkta çalışıyor: istenen paneli açıp
    // diğerlerini kapatıyor. Aynı anda birden fazla panelin açık kalmasını
    // (örn. Ayarlar ile Harita Seçimi'nin üst üste binmesini) bu şekilde engelliyoruz.
    public void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        settingsPanel.SetActive(false);
        howToPlayPanel.SetActive(false);
        mapSelectionPanel.SetActive(false);
    }

    public void ShowSettings()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
        howToPlayPanel.SetActive(false);
        mapSelectionPanel.SetActive(false);
    }

    public void ShowHowToPlay()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        howToPlayPanel.SetActive(true);
        mapSelectionPanel.SetActive(false);
    }

    // "OYNA" butonuna bağlı. Artık direkt oyun sahnesine geçmiyoruz — önce
    // Harita Seçim ekranını açıyoruz; asıl sahne geçişini (SceneManager.LoadScene)
    // MapSelectionUI, oyuncunun seçtiği haritaya göre kendisi yapacak.
    public void PlayGame()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        howToPlayPanel.SetActive(false);
        mapSelectionPanel.SetActive(true);
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