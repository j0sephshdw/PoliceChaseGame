using UnityEngine;
using TMPro; // TextMeshPro UI sistemini kullanabilmek için ekledik

public class StuntOdul : MonoBehaviour
{
    [Header("Uçuş Puanı")]
    public int ucusPuani = 500;

    [Header("Arayüz")]
    public TextMeshProUGUI stuntText; // Ekrana yazdıracağımız metin referansı

    // Oyuncu aynı rampadan defalarca uçarsa tekrar puan alabilsin diye minik bir bekleme süresi
    private bool puanAlabilirMi = true;

    private void Start()
    {
        // Oyun başladığında ekranda yazı kalmasın diye gizliyoruz
        if (stuntText != null)
            stuntText.gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Havada içinden geçen obje oyuncunun arabasıysa
        if (other.CompareTag("Player") && puanAlabilirMi)
        {
            puanAlabilirMi = false;

            // 1. Ana skora puan ekle
            ScoreManager scoreManager = Object.FindFirstObjectByType<ScoreManager>();
            if (scoreManager != null)
            {
                // scoreManager.AddScore(ucusPuani);
                Debug.Log("Havada Süzülme! +" + ucusPuani + " Puan");
            }

            // 2. BOMBA SÜRESİ EKLE (+10 Saniye)
            BombTimer timer = Object.FindFirstObjectByType<BombTimer>();
            if (timer != null)
            {
                timer.SureEkle(10f);
            }

            // 3. EKRANA FİYAKALI YAZI YAZDIR
            if (stuntText != null)
            {
                stuntText.gameObject.SetActive(true);
                stuntText.text = "MÜKEMMEL UÇUŞ!\n<size=50>+10 SANİYE</size>";
                stuntText.color = new Color(0f, 1f, 1f); // Neon Mavi (Cyan) renk harika durur
            }

            // 2 Saniye sonra sistemi sıfırla ki tekrar uçtuğunda çalışsın
            Invoke(nameof(SistemiSifirla), 2f);
        }
    }

    private void SistemiSifirla()
    {
        puanAlabilirMi = true;

        // Süre bittiğinde metni ekrandan kaldır
        if (stuntText != null)
            stuntText.gameObject.SetActive(false);
    }
}