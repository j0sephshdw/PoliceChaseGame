using UnityEngine;

public class StuntOdul : MonoBehaviour
{
    [Header("Uçuş Puanı")]
    public int ucusPuani = 500;

    // Oyuncu aynı rampadan defalarca uçarsa tekrar puan alabilsin diye minik bir bekleme süresi
    private bool puanAlabilirMi = true;

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

            // --- YENİ: BOMBA SÜRESİ EKLE (+10 Saniye) ---
            BombTimer timer = Object.FindFirstObjectByType<BombTimer>();
            if (timer != null)
            {
                timer.SureEkle(10f);
            }

            // 2 Saniye sonra oyuncu tekrar uçarsa puan alabilsin diye sistemi sıfırla
            Invoke(nameof(SistemiSifirla), 2f);
        }
    }

    private void SistemiSifirla()
    {
        puanAlabilirMi = true;
    }
}