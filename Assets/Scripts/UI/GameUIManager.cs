using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// ============================================================
// GAME UI MANAGER — Oyun Döngüsü ve UI (Bedirhan) sorumluluğunda.
// Sadece "SampleScene" (oyun sahnesi) içinde çalışır. HUD/Pause/GameOver
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

    [Header("Game Over Elemanları")]
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private TMP_Text highScoreText;

    [Header("Sahne Ayarları")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private void Start()
    {
        GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        ScoreManager.Instance.OnScoreChanged += HandleScoreChanged;
        ScoreManager.Instance.OnXPChanged += HandleXPChanged;
        GameEvents.Instance.OnPlayerHealthChanged += HandleHealthChanged;

        hudPanel.SetActive(true);
        pausePanel.SetActive(false);
        gameOverPanel.SetActive(false);
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

    // GameManager durumu her değiştiğinde (Playing/Paused/GameOver) çalışır.
    private void HandleGameStateChanged(GameState newState)
    {
        pausePanel.SetActive(newState == GameState.Paused);
        gameOverPanel.SetActive(newState == GameState.GameOver);

        if (newState == GameState.GameOver)
        {
            UpdateGameOverScreen();
        }
    }

    private void HandleScoreChanged(int newScore)
    {
        scoreText.text = "Skor: " + newScore;
    }

    private void HandleXPChanged(int currentXP, int xpToNextLevel)
    {
        xpBar.value = (float)currentXP / xpToNextLevel; // 0-1 arası oran, Slider Max Value 1 olduğu için
        levelText.text = "Seviye: " + ScoreManager.Instance.Level;
    }

    private void HandleHealthChanged(int currentHealth, int maxHealth)
    {
        // maxHealth araca göre değişebildiği için (CarData), Slider'ın üst sınırını da her seferinde güncelliyoruz.
        healthBar.maxValue = maxHealth;
        healthBar.value = currentHealth;
    }

    private void UpdateGameOverScreen()
    {
        finalScoreText.text = "Skor: " + ScoreManager.Instance.Score;
        highScoreText.text = "En Yüksek Skor: " + ScoreManager.GetHighScore();
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

    public void OnQuitButtonClicked()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
