using UnityEngine;

public class KutuYikim : MonoBehaviour
{
    [Header("Puan Ayarları")]
    public int verilecekPuan = 5;

    private bool puanVerildiMi = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (puanVerildiMi) return;

        if (collision.gameObject.CompareTag("Player") || collision.relativeVelocity.magnitude > 3f)
        {
            puanVerildiMi = true;

            ScoreManager scoreManager = Object.FindFirstObjectByType<ScoreManager>();
            if (scoreManager != null)
            {
                // scoreManager.AddScore(verilecekPuan);
                Debug.Log("Kutu yıkıldı! +" + verilecekPuan + " Puan");
            }

            BombTimer timer = Object.FindFirstObjectByType<BombTimer>();
            if (timer != null)
            {
                // 🚨 Dengelemek için süreyi 1 saniyeye düşürdük
                timer.SureEkle(1f);
            }

            Renderer kutuRengi = GetComponent<Renderer>();
            if (kutuRengi != null)
            {
                kutuRengi.material.color = Color.gray;
            }

            // 🚨 KESİN KİLİT: Kutu scriptini tamamen devre dışı bırakıyoruz.
            // Bu sayede aynı salisede birden fazla çarpışma algılansa bile 2. kez çalışama
            this.enabled = false;
        }
    }
}