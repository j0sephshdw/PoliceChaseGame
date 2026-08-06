using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [Header("Kamera Referansları")]
    public GameObject topDownCamera;
    public GameObject backCamera;

    private bool isTopDown = true;

    void Start()
    {
        // 1. Oyun ilk açıldığında (Araç Seçim Ekranında) ZORUNLU olarak TopDown açık kalsın.
        // Böylece menü açısı bozulmasın
        topDownCamera.SetActive(true);
        backCamera.SetActive(false);
        isTopDown = true;
    }

    // 2. BU METODU OYUNU BAŞLATAN "ARAÇ SEÇME" BUTONUNA BAĞLADIM
    public void ApplySavedCamera()
    {
        int savedCamera = PlayerPrefs.GetInt("IsTopDown", 1);
        isTopDown = (savedCamera == 1);

        topDownCamera.SetActive(isTopDown);
        backCamera.SetActive(!isTopDown);
    }

    public void SwitchCamera()
    {
        isTopDown = !isTopDown;

        topDownCamera.SetActive(isTopDown);
        backCamera.SetActive(!isTopDown);

        PlayerPrefs.SetInt("IsTopDown", isTopDown ? 1 : 0);
        PlayerPrefs.Save();
    }
}