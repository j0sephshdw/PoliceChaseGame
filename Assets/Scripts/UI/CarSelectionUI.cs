using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class CarOption
{
    public CarData carData;
    public Sprite previewImage;
    public int requiredScore;
}

public class CarSelectionUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject carSelectionPanel;

    [Header("Oyuncu")]
    [SerializeField] private PlayerCarController playerCarController;

    [Header("Araçlar")]
    [SerializeField] private List<CarOption> availableCars;

    [Header("UI Elemanları")]
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Image carPreviewImage;
    [SerializeField] private Button carPreviewButton;
    [SerializeField] private TMP_Text carNameText;
    [SerializeField] private TMP_Text lockInfoText;

    [SerializeField] private Color lockedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    [SerializeField] private Color unlockedColor = Color.white;

    private int currentIndex = 0;

    private void Start()
    {
        previousButton.onClick.AddListener(ShowPreviousCar);
        nextButton.onClick.AddListener(ShowNextCar);
        carPreviewButton.onClick.AddListener(SelectCurrentCar);

        UpdateDisplay();
    }

    private void ShowPreviousCar()
    {
        currentIndex--;
        if (currentIndex < 0) currentIndex = availableCars.Count - 1;
        UpdateDisplay();
    }

    private void ShowNextCar()
    {
        currentIndex++;
        if (currentIndex >= availableCars.Count) currentIndex = 0;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        CarOption car = availableCars[currentIndex];
        bool isUnlocked = ScoreManager.GetHighScore() >= car.requiredScore;

        carPreviewImage.sprite = car.previewImage;
        carNameText.text = car.carData.carName;

        if (isUnlocked)
        {
            carPreviewImage.color = unlockedColor;
            lockInfoText.text = "";
            carPreviewButton.interactable = true;
        }
        else
        {
            carPreviewImage.color = lockedColor;
            lockInfoText.text = "Skor: " + car.requiredScore + " gerekli";
            carPreviewButton.interactable = false;
        }
    }

    private void SelectCurrentCar()
    {
        CarOption car = availableCars[currentIndex];
        playerCarController.currentCarData = car.carData;
        playerCarController.LoadCarModel();

        carSelectionPanel.SetActive(false);
        GameManager.Instance.SetState(GameState.Playing);
    }
}