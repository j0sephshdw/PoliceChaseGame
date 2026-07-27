using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class MapOption
{
    public string mapName;
    public string sceneName; // SceneManager.LoadScene için gerçek sahne adı
    public Sprite previewImage;
    public int requiredScore;
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

    [SerializeField] private Color lockedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    [SerializeField] private Color unlockedColor = Color.white;

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
        mapNameText.text = map.mapName;

        if (isUnlocked)
        {
            mapPreviewImage.color = unlockedColor;
            lockInfoText.text = "";
            mapPreviewButton.interactable = true;
        }
        else
        {
            mapPreviewImage.color = lockedColor;
            lockInfoText.text = "Skor: " + map.requiredScore + " gerekli";
            mapPreviewButton.interactable = false;
        }
    }

    private void SelectCurrentMap()
    {
        MapOption map = availableMaps[currentIndex];
        SceneManager.LoadScene(map.sceneName);
    }
}