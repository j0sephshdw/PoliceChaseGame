using UnityEngine;
using TMPro;

public class PolicePursuitUI : MonoBehaviour
{
    [Header("UI Referansları")]
    public TextMeshProUGUI copCountText;

    [Header("Görsel Ayarlar")]
    // \U0001F693 = 🚔 (Önden Polis Arabası Emojisi)
    public string prefixText = "COPS \U0001F693";

    [Tooltip("Sayıların rengi (Hex kodu).")]
    public string numberColorHex = "#FF6600";

    // --- OPTİMİZASYON DEĞİŞKENİ ---
    private int lastCopCount = -1;

    private void Start()
    {
        if (copCountText != null)
        {
            copCountText.text = string.Empty;
        }
    }

    private void Update()
    {
        if (PoliceSpawner.Instance == null || copCountText == null) return;

        int currentCount = PoliceSpawner.Instance.GetActivePoliceCount();

        // KRİTİK OPTİMİZASYON: Sadece sayı değiştiğinde string oluşturur.
        // Bu sayede saniyede 60 kere gereksiz RAM tüketimi (GC Alloc) yapılmaz.
        if (currentCount == lastCopCount) return;

        if (currentCount > 0)
        {
            // SetText'in string kısıtlamasından dolayı standart interpolation kullanıyoruz
            copCountText.text = $"{prefixText} <color={numberColorHex}>{currentCount}</color>";
        }
        else
        {
            copCountText.text = string.Empty;
        }

        lastCopCount = currentCount;
    }
}