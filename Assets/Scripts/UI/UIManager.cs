using UnityEngine;
using TMPro;

// ============================================================
// UI MANAGER (Ana Menü) — Oyun Döngüsü ve UI (Bedirhan) sorumluluğunda.
// Sadece "MainMenu" sahnesinde çalışır; Ana Menü, Ayarlar, Nasıl Oynanır
// ve Harita Seçim panelleri arasındaki geçişleri, ayrıca ses aç/kapa ve
// en yüksek skor gösterimini yönetir.
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
    [SerializeField] private GameObject mapSelectionPanel;

    [Header("Diğer UI Elemanları")]
    [SerializeField] private TMP_Text highScoreText;   // MainMenuPanel'deki "En Yüksek Skor: 0" yazısı
    [SerializeField] private UnityEngine.UI.Image soundIcon;
    [SerializeField] private Sprite audioOnSprite;
    [SerializeField] private Sprite audioOffSprite; 
    [SerializeField] private UnityEngine.UI.Slider musicVolumeSlider;
    [SerializeField] private UnityEngine.UI.Slider sfxVolumeSlider;
    [SerializeField] private UnityEngine.UI.Toggle vibrationToggle;
    [SerializeField] private TMP_Text languageButtonText;
    [SerializeField] private LocalizationData sceneData;
    [SerializeField] private int highScoreLabelIndex = 11;

    // Ses açık/kapalı durumunu PlayerPrefs'te saklamak için kullandığımız anahtar.
    private const string MuteKey = "IsMuted";
    private const string MusicVolumeKey = "MusicVolume";
    private const string SFXVolumeKey = "SFXVolume";
    private const string VibrationKey = "VibrationEnabled";

    private void Start()
    {
        // Sahne ilk açıldığında sadece Ana Menü görünsün, diğer paneller kapalı kalsın.
        ShowMainMenu();

        UpdateHighScoreText();

        // Daha önce kaydedilmiş bir ses tercihi varsa onu uygula (varsayılan: sessiz değil).
        ApplySavedMuteState();
        soundIcon.sprite = IsMuted() ? audioOffSprite : audioOnSprite;

        musicVolumeSlider.value = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
        sfxVolumeSlider.value = PlayerPrefs.GetFloat(SFXVolumeKey, 1f);
        vibrationToggle.isOn = PlayerPrefs.GetInt(VibrationKey, 1) == 1;

        musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        vibrationToggle.onValueChanged.AddListener(OnVibrationChanged);
        UpdateLanguageButtonText();
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

    // "SES" butonuna bağlı. Basıldıkça açık/kapalı durumu tersine çevirir,
    // tercihi PlayerPrefs'e kalıcı olarak kaydeder (oyunu kapatıp açsan bile hatırlanır).
    public void ToggleSound()
    {
        ToggleMute();
        soundIcon.sprite = IsMuted() ? audioOffSprite : audioOnSprite;
    }

    // Ses durumunu gerçekten uygular: AudioListener.volume, sahnedeki TÜM
    // seslerin (müzik, efekt) ana ses seviyesini kontrol eder — 0 tamamen
    // sessiz, 1 normal ses demek. Buton yazısını da duruma göre günceller.
    public static bool IsMuted()
    {
        return PlayerPrefs.GetInt(MuteKey, 0) == 1;
    }

    public static void ToggleMute()
    {
        bool newState = !IsMuted();
        PlayerPrefs.SetInt(MuteKey, newState ? 1 : 0);
        PlayerPrefs.Save();
        AudioListener.volume = newState ? 0f : 1f;
    }

    // "DİL" butonuna bağlanacak. Basıldıkça Türkçe/İngilizce arasında geçiş yapar.
    public void ToggleLanguage()
    {
        Localization.CurrentLanguage = Localization.CurrentLanguage == Localization.Language.Turkish
            ? Localization.Language.English
            : Localization.Language.Turkish;

        UpdateLanguageButtonText();
        UpdateHighScoreText();
    }

    private void UpdateLanguageButtonText()
    {
        if (languageButtonText != null)
        {
            languageButtonText.text = Localization.CurrentLanguage == Localization.Language.Turkish ? "TR" : "EN";
        }
    }

    private void UpdateHighScoreText()
    {
        if (highScoreText == null || sceneData == null) return;

        LocalizationData.LocalizedEntry entry = sceneData.entries[highScoreLabelIndex];
        string label = Localization.CurrentLanguage == Localization.Language.Turkish ? entry.turkish : entry.english;

        highScoreText.text = label + " " + ScoreManager.GetHighScore();
    }
    public static void ApplySavedMuteState()
    {
        AudioListener.volume = IsMuted() ? 0f : 1f;
    }

    private void OnMusicVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat(MusicVolumeKey, value);
    }

    private void OnSFXVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat(SFXVolumeKey, value);
    }

    private void OnVibrationChanged(bool isOn)
    {
        PlayerPrefs.SetInt(VibrationKey, isOn ? 1 : 0);
    }

    public static float GetMusicVolume()
    {
        return PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
    }

    public static float GetSFXVolume()
    {
        return PlayerPrefs.GetFloat(SFXVolumeKey, 1f);
    }

    public static bool IsVibrationEnabled()
    {
        return PlayerPrefs.GetInt(VibrationKey, 1) == 1;
    }

    // Titreşim tercihi açıksa cihazı titretir. Handheld.Vibrate() yalnızca Android ve iOS'ta
    // çalıştığı için platform kontrolü koyuyoruz; PC ve Editor'de hiçbir şey olmuyor.
    public static void Vibrate()
    {
        Debug.Log("Titreşim tetiklendi");
        if (!IsVibrationEnabled()) return;

#if UNITY_ANDROID || UNITY_IOS
        if (!Application.isEditor)
        {
            Handheld.Vibrate();
        }
#endif
    }
}