using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class MapOption
{
    public string mapNameTurkish;
    public string mapNameEnglish;
    public string sceneName; // SceneManager.LoadScene için gerçek sahne adı
    public Sprite previewImage;
    public int requiredScore;
    public int groundTileIndex; // WorldGenerator'daki Ground Tile Prefabs dizisindeki sırayla eşleşmeli
}

public class MapSelectionUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject mapSelectionPanel;

    [Header("Haritalar")]
    [SerializeField] private List<MapOption> availableMaps;

    [Header("UI Elemanları")]
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Image mapPreviewImage;
    [SerializeField] private Button mapPreviewButton;
    [SerializeField] private TMP_Text mapNameText;
    [SerializeField] private TMP_Text lockInfoText;
    [SerializeField] private Image lockIcon;

    [SerializeField] private Color lockedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    [SerializeField] private Color unlockedColor = Color.white;

    [SerializeField] private LocalizationData sceneData;
    [SerializeField] private int scoreLabelIndex = 12;
    [SerializeField] private int requiredLabelIndex = 13;

    private int currentIndex = 0;

    private void Start()
    {
        previousButton.onClick.AddListener(ShowPreviousMap);
        nextButton.onClick.AddListener(ShowNextMap);
        mapPreviewButton.onClick.AddListener(SelectCurrentMap);

        UpdateDisplay();
    }

    private void ShowPreviousMap()
    {
        currentIndex--;
        if (currentIndex < 0) currentIndex = availableMaps.Count - 1;
        UpdateDisplay();
    }

    private void ShowNextMap()
    {
        currentIndex++;
        if (currentIndex >= availableMaps.Count) currentIndex = 0;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        MapOption map = availableMaps[currentIndex];
        bool isUnlocked = ScoreManager.GetHighScore() >= map.requiredScore;

        mapPreviewImage.sprite = map.previewImage;
        mapNameText.text = Localization.CurrentLanguage == Localization.Language.Turkish
            ? map.mapNameTurkish
            : map.mapNameEnglish;

        if (isUnlocked)
        {
            mapPreviewImage.color = unlockedColor;
            lockInfoText.text = "";
            lockIcon.gameObject.SetActive(false);
        }
        else
        {
            mapPreviewImage.color = lockedColor;
            lockInfoText.text = GetLockText(map.requiredScore);
            lockIcon.gameObject.SetActive(true);
        }
    }

    private string GetLockText(int requiredScore)
    {
        if (sceneData == null) return "";

        string scoreLabel = Localization.CurrentLanguage == Localization.Language.Turkish
            ? sceneData.entries[scoreLabelIndex].turkish
            : sceneData.entries[scoreLabelIndex].english;

        string requiredLabel = Localization.CurrentLanguage == Localization.Language.Turkish
            ? sceneData.entries[requiredLabelIndex].turkish
            : sceneData.entries[requiredLabelIndex].english;

        return scoreLabel + " " + requiredScore + " " + requiredLabel;
    }

    private void SelectCurrentMap()
    {
        MapOption map = availableMaps[currentIndex];
        bool isUnlocked = ScoreManager.GetHighScore() >= map.requiredScore;

        if (!isUnlocked)
        {
            UISoundPlayer.PlayError();
            return;
        }

        PlayerPrefs.SetInt("SelectedMapIndex", map.groundTileIndex);
        PlayerPrefs.Save();

        SceneManager.LoadScene(map.sceneName);
    }
}