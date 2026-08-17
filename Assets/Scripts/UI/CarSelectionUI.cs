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
    [SerializeField] private Vector3 modelOffset = new Vector3(0, 0, 0); // Merkez kaydırma ayarı

    //  Havuz  Sistemi - Araçları sadece 1 kere üreteceğiz
    private Dictionary<CarData, GameObject> carModelCache = new Dictionary<CarData, GameObject>();

    [Header("UI Elemanları")]
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button carPreviewButton;
    [SerializeField] private TMP_Text carNameText;
    [SerializeField] private TMP_Text lockInfoText;
    [SerializeField] private GameObject lockIcon;

    [SerializeField] private LocalizationData sceneData;
    [SerializeField] private int scoreLabelIndex = 12;
    [SerializeField] private int requiredLabelIndex = 13;

    private PlayerCarController playerCarController;
    private int currentIndex = 0;

    private void Start()
    {
        carSelectionPanel.SetActive(true);
        playerCarController = FindAnyObjectByType<PlayerCarController>();

        // Garaj menüsü açıldığında yoldaki asıl arabayı tamamen görünmez yap
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

        carNameText.text = Localization.CurrentLanguage == Localization.Language.Turkish
            ? car.carData.carNameTurkish
            : car.carData.carNameEnglish;

        // 1. Havuzdaki tüm araç modellerini gizle
        foreach (var kvp in carModelCache)
        {
            if (kvp.Value != null) kvp.Value.SetActive(false);
        }

        // 2. Eğer bu araç daha önce üretildiyse, havuzdan çağır ve aktif et (Performans için)
        if (carModelCache.ContainsKey(car.carData) && carModelCache[car.carData] != null)
        {
            GameObject cachedModel = carModelCache[car.carData];
            cachedModel.SetActive(true);
            cachedModel.transform.localPosition = modelOffset;
            cachedModel.transform.localScale = Vector3.one;
        }
        else
        {
            // 3. Daha önce hiç üretilmediyse sıfırdan üret ve havuza kaydet
            GameObject newModel = Instantiate(car.carPrefab, carSpawnPoint.position, carSpawnPoint.rotation);
            newModel.transform.SetParent(carSpawnPoint);
            newModel.transform.localPosition = modelOffset;
            newModel.transform.localScale = Vector3.one;

            carModelCache[car.carData] = newModel;
        }

        if (isUnlocked)
        {
            lockInfoText.text = "";
            lockIcon.SetActive(false);
        }
        else
        {
            lockInfoText.text = GetLockText(car.requiredScore);
            lockIcon.SetActive(true);
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

    private void SelectCurrentCar()
    {
        CarOption car = availableCars[currentIndex];
        bool isUnlocked = ScoreManager.GetHighScore() >= car.requiredScore;

        if (!isUnlocked)
        {
            UISoundPlayer.PlayError();
            return;
        }

        // Oyna tuşuna basıldığında asıl arabayı tekrar yolda görünür hale getir
        if (playerCarController != null)
        {
            playerCarController.gameObject.SetActive(true);
        }

        playerCarController.currentCarData = car.carData;
        playerCarController.LoadCarModel();

                PlayerHealth playerHealth = playerCarController.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.RefreshHealthFromCarData();
        }

        // Oyuna girerken bellek temizliği Garajdaki modelleri yok et
        foreach (var kvp in carModelCache)
        {
            if (kvp.Value != null) Destroy(kvp.Value);
        }
        carModelCache.Clear();

        carSelectionPanel.SetActive(false);
        GameManager.Instance.SetState(GameState.Playing);
    }
}