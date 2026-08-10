using UnityEngine;
using TMPro; // TextMeshPro kütüphanesi

public class PolicePursuitUI : MonoBehaviour
{
    [Header("UI Referansı")]
    public TextMeshProUGUI copCountText;

    [Header("NFS Most Wanted Ayarları")]
    public string prefixText = "COPS";
    

    private void Update()
    {
        if (PoliceSpawner.Instance != null)
        {
            int count = PoliceSpawner.Instance.GetActivePoliceCount();

            // Eğer peşimizde polis varsa sayıyı göster
            if (count > 0)
            {
               
                copCountText.text = $"{prefixText}  <color=#FF6600>{count}</color>";
            }
            else
            {
                // Polis yoksa ekranda gereksiz kalabalık yapmasın
                copCountText.text = "";
            }
        }
    }
}