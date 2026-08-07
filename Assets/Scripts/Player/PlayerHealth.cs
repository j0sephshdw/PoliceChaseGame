using UnityEngine;
using System;
using System.Collections;
using UnityEngine.InputSystem;
public class PlayerHealth : MonoBehaviour
{
    // Kapsülleme (Encapsulation): Dışarıdan müdahaleye kapalı özel değişkenler
    private int maxHealth;
    private int currentHealth;
    private bool isShieldActive = false; // Kalkanın o an açık olup olmadığını takip eden değişken
    private float regenPerSecond = 0f;
    private float regenAccumulator = 0f;
    private float damageReduction = 0f;
    // --- HASAR COOLDOWN (DOKUNULMAZLIK) SİSTEMİ ---
    public float damageCooldown = 1.5f; // Hasar aldıktan sonra 1.5 saniye dokunulmaz olur
    private float lastDamageTime = -9999f;

    // Bedirhan'ın (UI ve Oyun Döngüsü sorumlusu) kendi sisteminde kullanacağı tetikleyiciler
    public event Action<int, int> OnHealthChanged; // UI barının doğru oranlanması için hem current hem max canı gönderiyoruz
    public event Action OnPlayerDeath; // Ölüm anında fırlatılacak olay

    private PlayerCarController carController;

    private void Awake()
    {
        carController = GetComponent<PlayerCarController>(); // Aynı objedeki araç kontrolcüsünü hafızaya aldım
    }

    private void Start()
    {
        // Can değerini Inspector'dan manuel girmek yerine, otomatik olarak seçili CarData'dan çekiyoruz
        if (carController != null && carController.currentCarData != null)
        {
            maxHealth = carController.currentCarData.maxHealth;
        }
        else
        {
            maxHealth = 100; // Veri yoksa hata vermemesi için varsayılan güvenli değer
        }

        currentHealth = maxHealth; // Oyun başladığında canı fulledim

        // Oyun başlar başlamaz Bedirhan'ın UI sistemini güncellemesi için ilk tetiklemeyi yapıyoruz
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Update()
    {
        if(regenPerSecond > 0f && currentHealth < maxHealth)
        {
            regenAccumulator += regenPerSecond * Time.deltaTime;
            if(regenAccumulator >= 1f)
            {
                int healAmount = Mathf.FloorToInt(regenAccumulator);
                regenAccumulator -= healAmount;
                currentHealth = Mathf.Min(currentHealth + healAmount, maxHealth);
                OnHealthChanged?.Invoke(currentHealth,maxHealth);
            }
        }
    }
    public void IncreaseRegen(float amountPerSecond)
    {
        regenPerSecond += amountPerSecond;
    }

    // Bedirhan'ın UI'dan (veya yetenek sisteminden) çağıracağı Kalkan Fonksiyonu
    public void ActivateShield(float duration)
    {
        if (!isShieldActive) // Eğer kalkan zaten açık değilse açılmasını sağladım
        {
            StartCoroutine(ShieldRoutine(duration));
        }
    }

    // Kalkanın ne kadar süre açık kalacağını hesaplayan arka plan Coroutine işlemi
    private IEnumerator ShieldRoutine(float duration)
    {
        isShieldActive = true;
        Debug.Log("🛡️ Kalkan AKTİF! Hasar alınmayacak."); 

        yield return new WaitForSeconds(duration); // Belirtilen süre kadar beklettim

        isShieldActive = false;
        Debug.Log("🛡️ Kalkan KAPANDI!"); 
    }

    // Dışarıdan veya engellerden hasar alınca çalışacak olan fonksiyon
    public void TakeDamage(int damageAmount)
    {
        // 1. KALKAN KONTROLÜ
        if (isShieldActive)
        {
            Debug.Log("Bloklandı! Kalkan hasarı emdi.");
            return;
        }

        // 2. COOLDOWN KONTROLÜ (Peş peşe hasar almayı engeller)
        if (Time.time < lastDamageTime + damageCooldown)
        {
            // Cooldown dolmadıysa hasarı iptal et ve fonksiyondan çık
            return;
        }

        // Hasar almayı kabul ettik, sayacı şu anki zamana kuruyoruz
        lastDamageTime = Time.time;

        // Hasar hesaplaması
        int reducedDamage = Mathf.RoundToInt(damageAmount * (1f - damageReduction));
        currentHealth -= reducedDamage;

        // Can sıfırın altına düşmesin diye sınırlandırdım
        currentHealth = Mathf.Max(currentHealth, 0);

        // Bedirhan'ın UI sistemine haber verdim: "Can değişti, can barını güncelle"
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        Debug.Log("Araç Hasar Aldı! Kalan Can: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }
    public void IncreaseDamageReduction(float percentage)
    {
        // %90'ı geçmesin diye sınırladık — tamamen hasarsız (ölümsüz) hale gelmesin
        damageReduction = Mathf.Min(damageReduction + percentage, 0.5f);
    }

    private void Die()
    {
        Debug.Log("Araç Parçalandı! GAME OVER.");

        // Aracı ve motor sesini durdurduk
        if (carController != null)
        {
            carController.StopEngineSound(); // Sesi kesen metodu çağırdık
            carController.enabled = false;

            // parçalanma fiziğini tetikle (Parçalar havaya uçmaya başlar)
            carController.Explode();
        }

        // GAME OVER EKRANI GECİKMSİ: Parçalanma efektini 1.5 saniye izleyip sonra ekranı getiriyoruz
        Invoke(nameof(TriggerGameOverUI), 1.5f);
    }

    // Gecikmeli çalışacak UI tetikleme metodu
    private void TriggerGameOverUI()
    {
        // Bedirhan'ın Game Over ekranını tetiklemesi için Event'i fırlattım
        OnPlayerDeath?.Invoke();
    }

    // Berat'ın (Çevre ve AI sorumlusu) yapacağı engellere veya polislere çarpma durumunu test ettim
    private void OnCollisionEnter(Collision collision)
    {
        // Eğer çarptığımız obje bir Engel veya Polis ise hasar almasını sağladım
        if (collision.gameObject.CompareTag("Obstacle") || collision.gameObject.CompareTag("Police") || collision.gameObject.CompareTag("Traffic"))
        {
            TakeDamage(25); // Çarpınca şimdilik 25 hasar almasını ayarladım (4 vuruşta ölür)
        }
    } 

    public void IncreaseMaxHealth(float percentage)
    {
        int increase = Mathf.RoundToInt(maxHealth * percentage);
        maxHealth += increase;
        currentHealth += increase; // mevcut canı da artırmazsak, can barındaki oran aniden düşmüş gibi görünür
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // Araç Seçim ekranında farklı bir araç seçildiğinde, can değerlerini
    // o aracın CarData'sına göre yeniden hesaplamak için çağrılır.
    public void RefreshHealthFromCarData()
    {
        if (carController != null && carController.currentCarData != null)
        {
            maxHealth = carController.currentCarData.maxHealth;
        }
        else
        {
            maxHealth = 100;
        }

        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}