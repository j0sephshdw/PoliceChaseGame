using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// ============================================================
// GAME UI MANAGER — Oyun Döngüsü ve UI (Bedirhan) sorumluluğunda.
// Oyun sahnesinde ("CityScene") çalışır. HUD/Pause/GameOver
// panellerini GameManager, ScoreManager ve GameEvents'e bağlar.
// ============================================================
public class GameUIManager : MonoBehaviour
{
    [Header("Paneller")]
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject gameOverPanel;

    [Header("HUD Elemanları")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private Slider healthBar;
    [SerializeField] private Slider xpBar;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Image hudSoundIcon;
    [SerializeField] private Sprite audioOnSprite;
    [SerializeField] private Sprite audioOffSprite;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private GameObject wantedStarsText; // Sadece Playing durumunda gösterilecek

    [Header("Game Over Elemanları")]
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private TMP_Text highScoreText;

    [Header("Game Over İstatistikleri")]
    [SerializeField] private TMP_Text survivalTimeText;
    [SerializeField] private TMP_Text finalLevelText;
    [SerializeField] private TMP_Text policeDestroyedText;
    [SerializeField] private GameObject newRecordObject;
    [SerializeField] private int timeLabelIndex = 23;
    [SerializeField] private int neutralizedLabelIndex = 24;

    [Header("Sahne Ayarları")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Pause Menü Elemanları")]
    [SerializeField] private Slider gameVolumeSlider;

    [Header("Yerelleştirme")]
    [SerializeField] private LocalizationData sceneData;
    [SerializeField] private int scoreLabelIndex = 12;
    [SerializeField] private int levelLabelIndex = 21;
    [SerializeField] private int highScoreLabelIndex = 11;

    private const string GameVolumeKey = "GameVolume";

    // PlayerPrefs okuması her karede 12'den fazla yerden çağrıldığı için değeri önbelleğe
    // alıyoruz; yalnızca slider değiştiğinde güncelleniyor. Negatif değer "henüz okunmadı" demek
    // (ses seviyesi 0 olabileceği için sıfırı işaret olarak kullanamıyoruz).
    private static float cachedGameVolume = -1f;

    private void Start()
    {
        GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        ScoreManager.Instance.OnScoreChanged += HandleScoreChanged;
        ScoreManager.Instance.OnXPChanged += HandleXPChanged;
        GameEvents.Instance.OnPlayerHealthChanged += HandleHealthChanged;

        hudPanel.SetActive(true);
        hudSoundIcon.sprite = UIManager.IsMuted() ? audioOffSprite : audioOnSprite;
        pausePanel.SetActive(false);
        gameVolumeSlider.value = PlayerPrefs.GetFloat(GameVolumeKey, 1f);
        gameVolumeSlider.onValueChanged.AddListener(OnGameVolumeChanged);
        gameOverPanel.SetActive(false);
        wantedStarsText.SetActive(GameManager.Instance.CurrentState == GameState.Playing);
    }

    private void OnDestroy()
    {
        // Sahne kapanırken diğer Manager'lar bizden önce yok edilmiş olabilir,
        // bu yüzden aboneliği iptal etmeden önce null kontrolü yapıyoruz.
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged -= HandleScoreChanged;
            ScoreManager.Instance.OnXPChanged -= HandleXPChanged;
        }

        if (GameEvents.Instance != null)
            GameEvents.Instance.OnPlayerHealthChanged -= HandleHealthChanged;
    }
    
    private string GetLabel(int index)
    {
        if (sceneData == null) return "";
        LocalizationData.LocalizedEntry entry = sceneData.entries[index];
        return Localization.CurrentLanguage == Localization.Language.Turkish ? entry.turkish : entry.english;
    }

    // GameManager durumu her değiştiğinde (Playing/Paused/GameOver) çalışır.
    private void HandleGameStateChanged(GameState newState)
    {
        pausePanel.SetActive(newState == GameState.Paused);
        gameOverPanel.SetActive(newState == GameState.GameOver);
        wantedStarsText.SetActive(newState == GameState.Playing);

        if (newState == GameState.GameOver)
        {
            UpdateGameOverScreen();
        }
    }

    private void HandleScoreChanged(int newScore)
    {
        scoreText.text = GetLabel(scoreLabelIndex) + " " + newScore;
    }

    private void HandleXPChanged(int currentXP, int xpToNextLevel)
    {
        xpBar.value = (float)currentXP / xpToNextLevel; // 0-1 arası oran, Slider Max Value 1 olduğu için
        levelText.text = GetLabel(levelLabelIndex) + " " + ScoreManager.Instance.Level;
    }

    private void HandleHealthChanged(int currentHealth, int maxHealth)
    {
        // maxHealth araca göre değişebildiği için (CarData), Slider'ın üst sınırını da her seferinde güncelliyoruz.
        healthBar.maxValue = maxHealth;
        healthBar.value = currentHealth;
        healthText.text = currentHealth + " / " + maxHealth;
    }

    private void OnGameVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat(GameVolumeKey, value);
        cachedGameVolume = value; // Önbelleği güncel tut
    }

    public static float GetGameVolume()
    {
        if (cachedGameVolume < 0f)
            cachedGameVolume = PlayerPrefs.GetFloat(GameVolumeKey, 1f);

        return cachedGameVolume;
    }

    private void UpdateGameOverScreen()
    {
        finalScoreText.text = GetLabel(scoreLabelIndex) + " " + ScoreManager.Instance.Score;
        highScoreText.text = GetLabel(highScoreLabelIndex) + " " + ScoreManager.GetHighScore();

        // Tur istatistikleri
        if (survivalTimeText != null)
            survivalTimeText.text = GetLabel(timeLabelIndex) + " " + ScoreManager.Instance.GetSurvivalTimeText();

        if (finalLevelText != null)
            finalLevelText.text = GetLabel(levelLabelIndex) + " " + ScoreManager.Instance.Level;

        if (policeDestroyedText != null)
            policeDestroyedText.text = GetLabel(neutralizedLabelIndex) + " " + ScoreManager.Instance.PoliceDestroyed;

        // Rekor bildirimi yalnızca bu turda rekor kırıldıysa görünsün
        if (newRecordObject != null)
            newRecordObject.SetActive(ScoreManager.Instance.IsNewHighScore);
    }

    // --- Buton fonksiyonları (OnClick() ile bağlanacak) ---

    public void OnPauseButtonClicked()
    {
        GameManager.Instance.PauseGame();
    }

    public void OnResumeButtonClicked()
    {
        GameManager.Instance.ResumeGame();
    }

    public void OnRestartButtonClicked()
    {
        // GameOver'da Time.timeScale'i 0 yapmıştık; sıfırlamadan sahneyi yeniden yüklersek
        // yeni sahne de donuk (donmuş) başlar, bu yüzden önce 1'e döndürüyoruz.
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnMainMenuButtonClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void OnSoundButtonClicked()
    {
        UIManager.ToggleMute();
        hudSoundIcon.sprite = UIManager.IsMuted() ? audioOffSprite : audioOnSprite;
    }

    public void OnQuitButtonClicked()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
