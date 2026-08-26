using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

// Yükleme ekranında gösterilecek ipuçları. Her ipucu iki dilde tanımlanıyor.
[System.Serializable]
public class LoadingTip
{
    [TextArea] public string turkish;
    [TextArea] public string english;
}

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

    [Header("Sahne Ayarları")]
    [SerializeField] private string gameSceneName = "CityScene";

    [Header("Yükleme Ekranı")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private UnityEngine.UI.Slider loadingBar;
    [SerializeField] private TMP_Text loadingPercentText;
    [SerializeField] private TMP_Text loadingTipText;
    [SerializeField] private UnityEngine.UI.Image loadingBackground;
    [Tooltip("Yükleme çok hızlı bitse bile ekranın en az bu kadar saniye görünmesini sağlar")]
    [SerializeField] private float minimumLoadingTime = 2f;
    [Tooltip("Bar boşken arka plan rengi (beyaz = görsel olduğu gibi)")]
    [SerializeField] private Color backgroundStartColor = Color.white;
    [Tooltip("Bar doluyken arka plan rengi (koyu = kararmış görsel)")]
    [SerializeField] private Color backgroundEndColor = new Color(0.15f, 0.15f, 0.15f, 1f);
    [SerializeField] private List<LoadingTip> loadingTips = new List<LoadingTip>();
    // Son gösterilen ipucunun sırası. static olduğu için sahne yeniden yüklendiğinde de
    // hatırlanıyor; böylece ana menüye dönüp tekrar oynadığında aynı ipucu çıkmıyor.
    private static int lastTipIndex = -1;

    // Ses açık/kapalı durumunu PlayerPrefs'te saklamak için kullandığımız anahtar.
    private const string MuteKey = "IsMuted";
    private const string MusicVolumeKey = "MusicVolume";
    private const string SFXVolumeKey = "SFXVolume";
    private const string VibrationKey = "VibrationEnabled";

    private void Start()
    {
        // Sahne ilk açıldığında sadece Ana Menü görünsün, diğer paneller kapalı kalsın.
        ShowMainMenu();

        // Yükleme ekranı sahne açılışında kapalı olsun
        if (loadingPanel != null) loadingPanel.SetActive(false);

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

    // "OYNA" butonuna bağlı. Harita seçimi şimdilik devre dışı olduğu için panel açmak yerine
    // yükleme ekranını gösterip oyun sahnesini arka planda yüklüyoruz.
    // Yeni harita eklendiğinde: aşağıdaki yükleme çağrılarını silip
    // mapSelectionPanel.SetActive(true); yazmak yeterli — panel ve MapSelectionUI hazır duruyor.
    public void PlayGame()
    {
        // Daha önce başka bir harita seçilmiş olabilir; kayıtlı seçimi ilk haritaya sıfırlıyoruz
        // ki kaldırılan haritanın kaydı yüzünden yanlış karo yüklenmesin.
        PlayerPrefs.SetInt("SelectedMapIndex", 0);
        PlayerPrefs.Save();

        // Menü panellerini kapatıp yükleme ekranını açıyoruz
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        howToPlayPanel.SetActive(false);
        mapSelectionPanel.SetActive(false);

        ShowLoadingScreen();
        StartCoroutine(LoadGameRoutine());
    }

    // Yükleme ekranını açar, barı sıfırlar ve rastgele bir ipucu seçip aktif dilde yazar
    private void ShowLoadingScreen()
    {
        if (loadingPanel == null) return;

        loadingPanel.SetActive(true);

        if (loadingBar != null) loadingBar.value = 0f;
        if (loadingPercentText != null) loadingPercentText.text = "%0";
        if (loadingBackground != null) loadingBackground.color = backgroundStartColor;

        if (loadingTipText != null && loadingTips.Count > 0)
        {
            int index = Random.Range(0, loadingTips.Count);

            // Bir öncekiyle aynı ipucu geldiyse bir sonrakine kaydırıyoruz.
            // (Listede tek ipucu varsa kaydıracak yer yok, olduğu gibi kalıyor.)
            if (loadingTips.Count > 1 && index == lastTipIndex)
                index = (index + 1) % loadingTips.Count;

            lastTipIndex = index;

            LoadingTip tip = loadingTips[index];
            loadingTipText.text = Localization.CurrentLanguage == Localization.Language.Turkish
                ? tip.turkish
                : tip.english;
        }
    }

    private IEnumerator LoadGameRoutine()
    {
        float startTime = Time.time;

        AsyncOperation op = SceneManager.LoadSceneAsync(gameSceneName);
        // Sahneyi biz "hazır" diyene kadar açmasın; yoksa yükleme biter bitmez ekran anında kaybolur
        op.allowSceneActivation = false;

        while (!op.isDone)
        {
            // op.progress en fazla 0.9'a çıkar ve sahne aktive edilene kadar orada bekler,
            // bu yüzden 0.9'a bölerek gerçek yüzdeye çeviriyoruz.
            float loadProgress = Mathf.Clamp01(op.progress / 0.9f);

            // Yükleme çok hızlı biterse ekran anlık görünüp kaybolmasın diye geçen süreyi de ilerleme sayıyoruz
            float timeProgress = minimumLoadingTime > 0f
                ? Mathf.Clamp01((Time.time - startTime) / minimumLoadingTime)
                : 1f;

            // İkisinden küçük olanı gösteriyoruz: bar hem yüklemeyi hem minimum süreyi beklemiş oluyor
            float shown = Mathf.Min(loadProgress, timeProgress);

            if (loadingBar != null) loadingBar.value = shown;
            if (loadingPercentText != null) loadingPercentText.text = "%" + Mathf.RoundToInt(shown * 100f);

            // Bar doldukça arka plan görselini kararttıyoruz
            if (loadingBackground != null)
                loadingBackground.color = Color.Lerp(backgroundStartColor, backgroundEndColor, shown);

            // Hem yükleme bitti hem minimum süre doldu: artık sahneyi açabiliriz
            if (loadProgress >= 1f && timeProgress >= 1f)
            {
                op.allowSceneActivation = true;
            }

            yield return null;
        }
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