using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

// Bir araç seçeneğinin kart üzerinde nasıl görüneceğini ve hangi skorda
// açılacağını tutan basit, serileştirilebilir veri sınıfı. CarData.cs'e
// (Kişi 1'in dosyası) dokunmadan burada kendi UI verimizi tutuyoruz.
[System.Serializable]
public class CarOption
{
    public CarData carData;
    public Sprite previewImage;
    public int requiredScore; // 0 = başlangıç aracı, hep açık
}

public class CarSelectionUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject carSelectionPanel;

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

    // Artık Inspector'dan elle sürüklenmiyor — hangi haritaya kopyalanırsak
    // kopyalanalım, o sahnenin kendi PlayerCar'ını otomatik bulsun diye.
    private PlayerCarController playerCarController;

    private int currentIndex = 0;

    private void Start()
    {
        playerCarController = FindAnyObjectByType<PlayerCarController>();

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

    // Ekrandaki araç görselini/ismini günceller, en yüksek skora göre
    // kilitli/açık durumunu (renk + tıklanabilirlik) belirler.
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
            carPreviewButton.interactable = false; // kilitliyken tıklanamaz, oyun yanlışlıkla başlamaz
        }
    }

    // Araç görseline tıklanınca çağrılır: seçilen aracı uygula ve oyunu başlat.
    private void SelectCurrentCar()
    {
        CarOption car = availableCars[currentIndex];
        playerCarController.currentCarData = car.carData;
        playerCarController.LoadCarModel(); // Kişi 1'in public yaptığı fonksiyon

        carSelectionPanel.SetActive(false);
        GameManager.Instance.SetState(GameState.Playing);
    }
}