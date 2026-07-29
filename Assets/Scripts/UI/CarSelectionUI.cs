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
    [SerializeField] private Image lockIcon;

    [SerializeField] private Color lockedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    [SerializeField] private Color unlockedColor = Color.white;

    // Artık Inspector'dan elle sürüklenmiyor — hangi haritaya kopyalanırsak
    // kopyalanalım, o sahnenin kendi PlayerCar'ını otomatik bulsun diye.
    private PlayerCarController playerCarController;

    private int currentIndex = 0;

    private void Start()
{
    // Bu paneli Editor'de yanlışlıkla kapalı bırakırsak oyun donuk kalır
    // (GameManager CarSelect durumunda bekler) — bu yüzden burada kod
    // panelin açık olmasını garantiliyor, Editor'deki checkbox'a güvenmiyoruz.
    carSelectionPanel.SetActive(true);

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
            lockIcon.gameObject.SetActive(false);
        }
        else
        {
            carPreviewImage.color = lockedColor;
            lockInfoText.text = "Skor: " + car.requiredScore + " gerekli";
            lockIcon.gameObject.SetActive(true);
        }
    }

    // Araç görseline tıklanınca çağrılır: seçilen aracı uygula ve oyunu başlat.
    private void SelectCurrentCar()
    {
        CarOption car = availableCars[currentIndex];
        bool isUnlocked = ScoreManager.GetHighScore() >= car.requiredScore;

        if (!isUnlocked)
        {
            UISoundPlayer.PlayError();
            return;
        }

        playerCarController.currentCarData = car.carData;
        playerCarController.LoadCarModel();

        carSelectionPanel.SetActive(false);
        GameManager.Instance.SetState(GameState.Playing);
    }
}