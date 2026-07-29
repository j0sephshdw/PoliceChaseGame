using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class CarOption
{
    public CarData carData;
    public GameObject carPrefab;
    public int requiredScore;
}

public class CarSelectionUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject carSelectionPanel;

    [Header("Araçlar")]
    [SerializeField] private List<CarOption> availableCars;

    [Header("3D Garaj Ayarları")]
    [SerializeField] private Transform carSpawnPoint;

    [Tooltip("Araba yalpalanarak dönüyorsa Z eksenini buradan kaydırarak tam merkeze alabilirsiniz (Örn: 0.5 veya -0.5)")]
    [SerializeField] private Vector3 modelOffset = new Vector3(0, 0, 0); //  Merkez kaydırma ayarı

    private GameObject currentCarModel;

    [Header("UI Elemanları")]
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button carPreviewButton;
    [SerializeField] private TMP_Text carNameText;
    [SerializeField] private TMP_Text lockInfoText;
    [SerializeField] private GameObject lockIcon;

    private PlayerCarController playerCarController;
    private int currentIndex = 0;

    private void Start()
    {
        carSelectionPanel.SetActive(true);
        playerCarController = FindAnyObjectByType<PlayerCarController>();

        // 1. ÇÖZÜM: Garaj menüsü açıldığında yoldaki asıl arabayı tamamen görünmez yap!
        if (playerCarController != null)
        {
            playerCarController.gameObject.SetActive(false);
        }

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

        carNameText.text = car.carData.carName;

        if (currentCarModel != null) Destroy(currentCarModel);

        currentCarModel = Instantiate(car.carPrefab, carSpawnPoint.position, carSpawnPoint.rotation);
        currentCarModel.transform.SetParent(carSpawnPoint);

        //  Arabayı, Inspector'dan verdiğimiz kaydırma (Offset) değeri kadar ileri/geri al
        currentCarModel.transform.localPosition = modelOffset;
        currentCarModel.transform.localScale = Vector3.one;

        if (isUnlocked)
        {
            lockInfoText.text = "";
            lockIcon.SetActive(false);
        }
        else
        {
            lockInfoText.text = "Skor: " + car.requiredScore + " gerekli";
            lockIcon.SetActive(true);
        }
    }

    private void SelectCurrentCar()
    {
        CarOption car = availableCars[currentIndex];
        bool isUnlocked = ScoreManager.GetHighScore() >= car.requiredScore;

        if (!isUnlocked)
        {
            UISoundPlayer.PlayError();
            return;
        }

        //  Oyna tuşuna basıldığında asıl arabayı tekrar yolda görünür hale getir!
        if (playerCarController != null)
        {
            playerCarController.gameObject.SetActive(true);
        }

        playerCarController.currentCarData = car.carData;
        playerCarController.LoadCarModel();

        if (currentCarModel != null) Destroy(currentCarModel);

        carSelectionPanel.SetActive(false);
        GameManager.Instance.SetState(GameState.Playing);
    }
}