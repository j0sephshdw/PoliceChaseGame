using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class BombTimer : MonoBehaviour
{
    [Header("Bomba Ayarları")]
    public float geriSayimSuresi = 45f;
    public TextMeshProUGUI sureText;

    [Header("Görsel Ayarlar")]
    public Color normalRenk = new Color(1f, 0.3f, 0f); // Dijital turuncu/kırmızı tonu
    public Color panikRenk = Color.red;

    [Header("Ses Ayarları")]
    public AudioClip beepSesi;
    [Tooltip("Seste baştan boşluk varsa, kaçıncı saniyeden başlayacağını buraya yaz (Örn: 0.75)")]
    public float sesBaslamaNoktasi = 0.75f;

    // --- Çift Hoparlör (Ping-Pong) Sistemi ---
    private AudioSource[] hoparlorler = new AudioSource[2];
    private int aktifHoparlor = 0;

    private float sonrakiBeepZamani = 0f;
    private bool oyunBasladi = false;
    private bool patladiMi = false;

    void Start()
    {
        // 🚨 Sadece VehicleTestScene'de çalışsın
        if (SceneManager.GetActiveScene().name != "VehicleTestScene")
        {
            if (sureText != null) sureText.gameObject.SetActive(false);
            this.enabled = false;
            return;
        }

        // Koda arka planda otomatik 2 adet AudioSource ekletiyoruz ki sesler birbirini kesmesin
        hoparlorler[0] = gameObject.AddComponent<AudioSource>();
        hoparlorler[1] = gameObject.AddComponent<AudioSource>();

        // Araç seçimi ekranındayken sayacı gizle
        if (sureText != null)
        {
            sureText.color = normalRenk;
            sureText.gameObject.SetActive(false);
        }

        // GameManager durumunu dinle
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += DurumKontrol;
        }
        else
        {
            OyunuBaslat();
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= DurumKontrol;
        }
    }

    private void DurumKontrol(GameState yeniDurum)
    {
        if (yeniDurum == GameState.Playing)
        {
            OyunuBaslat();
        }
    }

    private void OyunuBaslat()
    {
        oyunBasladi = true;
        sonrakiBeepZamani = Time.time;

        if (sureText != null)
        {
            sureText.gameObject.SetActive(true);
        }

        // Sesleri önceden iki hoparlöre de yükle (Gecikmeyi önler)
        if (beepSesi != null)
        {
            hoparlorler[0].clip = beepSesi;
            hoparlorler[1].clip = beepSesi;
        }
    }

    void Update()
    {
        if (!oyunBasladi || patladiMi) return;

        geriSayimSuresi -= Time.deltaTime;

        if (sureText != null)
        {
            float kalan = Mathf.Max(0, geriSayimSuresi);
            int dakika = Mathf.FloorToInt(kalan / 60f);
            int saniye = Mathf.FloorToInt(kalan % 60f);

            // 💣 DİJİTAL BOMBA FORMATI: 00:45
            sureText.text = string.Format("{0:00}:{1:00}", dakika, saniye);

            if (geriSayimSuresi <= 10f && geriSayimSuresi > 0f)
            {
                sureText.color = panikRenk;
                float nabiz = Mathf.PingPong(Time.time * 6f, 0.25f) + 1f;
                sureText.transform.localScale = new Vector3(nabiz, nabiz, 1f);
            }
        }

        // --- 🔊 BİP SESİ KONTROLÜ (PING-PONG SİSTEMİ) ---
        if (beepSesi != null && geriSayimSuresi > 0f)
        {
            if (Time.time >= sonrakiBeepZamani)
            {
                AudioSource calinacakHoparlor = hoparlorler[aktifHoparlor];

                calinacakHoparlor.time = sesBaslamaNoktasi;
                calinacakHoparlor.pitch = 1f; // Ses incelmesini iptal ettik, her zaman orijinal tonda kalacak
                calinacakHoparlor.Play();

                // Bir sonraki atışta diğer hoparlörü kullanmak için sırayı değiştir
                aktifHoparlor = (aktifHoparlor + 1) % 2;

                // Son 10 saniyede bekleme aralığı yarıya düşer
                float beklemeAraligi = (geriSayimSuresi <= 10f) ? 0.5f : 1f;
                sonrakiBeepZamani = Time.time + beklemeAraligi;
            }
        }

        if (geriSayimSuresi <= 0f)
        {
            Patlat();
        }
    }

    void Patlat()
    {
        patladiMi = true;

        if (sureText != null)
        {
            sureText.text = "00:00";
            sureText.color = Color.red;
            sureText.transform.localScale = Vector3.one * 1.3f;
        }

        PlayerHealth oyuncuSaglik = Object.FindFirstObjectByType<PlayerHealth>();
        if (oyuncuSaglik != null)
        {
            oyuncuSaglik.InstantKill();
        }
    }

    public void SureEkle(float kazanilanSaniye)
    {
        if (!oyunBasladi || patladiMi) return;

        geriSayimSuresi += kazanilanSaniye;

        if (sureText != null)
        {
            sureText.color = Color.green;
            CancelInvoke(nameof(RengiNormaleDondur));
            Invoke(nameof(RengiNormaleDondur), 0.5f);
        }
    }

    private void RengiNormaleDondur()
    {
        if (sureText != null)
        {
            sureText.color = (geriSayimSuresi <= 10f) ? panikRenk : normalRenk;
        }
    }
}