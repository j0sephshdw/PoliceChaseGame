using UnityEngine;
using TMPro;

public class PolicePursuitUI : MonoBehaviour
{
    [Header("UI Referansları")]
    public TextMeshProUGUI copCountText;

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
            copCountText.text = currentCount.ToString();
        }
        else
        {
            copCountText.text = string.Empty;
        }

        lastCopCount = currentCount;
    }
}