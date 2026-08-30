using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class WantedManager : MonoBehaviour
{
    [Header("UI Referansı")]
    public TextMeshProUGUI wantedText;

    [Header("Skor Barajları (Spawner ile Uyumlu)")]
    public int suvSkor = 100;
    public int muscleSkor = 300;
    public int sportsSkor = 600;
    public int maxSkor = 1000;

    public static WantedManager Instance;
    private int currentStars = 0;
    public int CurrentStars => currentStars;
    private Color defaultColor; // Yıldızın Inspector'daki orijinal rengini (Sarı) hafızada tutar

    private void Awake()
    {
        Instance = this;
    }
    
    void Start()
    {
        if (wantedText != null)
        {
            defaultColor = wantedText.color;
        }
        UpdateStarsUI(1);
    }

    void Update()
    {
        if (ScoreManager.Instance == null || wantedText == null) return;

        int score = ScoreManager.Instance.Score;
        int newStars = CalculateStars(score);

        // Eğer yıldız seviyesinde bir değişiklik varsa
        if (newStars != currentStars)
        {
            // Eğer yıldız ARTTIYSA yanıp sönme efektini tetikle
            if (newStars > currentStars && currentStars != 0)
            {
                StartCoroutine(FlashStarsEffect());
            }

            currentStars = newStars;

            // Tur sonunda gösterilmek üzere ulaşılan en yüksek seviyeyi kaydediyoruz
            if (ScoreManager.Instance != null)
                ScoreManager.Instance.RegisterWantedLevel(currentStars);

            UpdateStarsUI(currentStars);
        }
    }

    int CalculateStars(int score)
    {
        if (score >= maxSkor) return 5;
        if (score >= sportsSkor) return 4;
        if (score >= muscleSkor) return 3;
        if (score >= suvSkor) return 2;
        return 1;
    }

    void UpdateStarsUI(int stars)
    {
        if (wantedText == null) return;

        string starsGraphic = "";

        for (int i = 1; i <= 5; i++)
        {
            if (i <= stars)
            {
                starsGraphic += "★ "; // Dolu Yıldız
            }
            else
            {
                starsGraphic += "☆ "; // Boş Yıldız
            }
        }

        wantedText.text = starsGraphic.TrimEnd();
    }

    // Kırmızı-Mavi Çakar Efekti
    IEnumerator FlashStarsEffect()
    {
        // 3 kere hızlıca Kırmızı ve Mavi arasında gidip gelir
        for (int i = 0; i < 3; i++)
        {
            wantedText.color = Color.red;
            yield return new WaitForSeconds(0.15f);

            wantedText.color = Color.blue;
            yield return new WaitForSeconds(0.15f);
        }

        // Efekt bitince yıldızları tekrar orijinal Sarı rengine döndür
        wantedText.color = defaultColor;
    }
}