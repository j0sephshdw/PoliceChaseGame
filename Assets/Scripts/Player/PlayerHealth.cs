using UnityEngine;
using System;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    // Kapsülleme (Encapsulation): Dışarıdan müdahaleye kapalı özel değişkenler
    private int maxHealth;
    private int currentHealth;
    private bool isShieldActive = false; // Kalkanın o an açık olup olmadığını takip eden değişken

    
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
        // Bedirhan UI kısmını yapana kadar E tuşu ile kalkanı test edebilmek için geçici olarak yazdım
        if (Input.GetKeyDown(KeyCode.E))
        {
            ActivateShield(3f); // E'ye basınca 3 saniyelik kalkan açılmasını sağladım
        }
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
        // KALKAN FIX: Kalkan açıksa hasar almadan fonksiyondan çıkılıyor
        if (isShieldActive)
        {
            Debug.Log("Bloklandı! Kalkan hasarı emdi."); 
            return;
        }

        currentHealth -= damageAmount;

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

    private void Die()
    {
        Debug.Log("Araç Parçalandı! GAME OVER.");

        // Aracı ve motor sesini durdurduk
        if (carController != null)
        {
            carController.StopEngineSound(); // Sesi kesen metodu çağırdık
            carController.enabled = false;
        }

        // Bedirhan'ın Game Over ekranını tetiklemesi için Event'i fırlattım
        OnPlayerDeath?.Invoke();
    }

    // Berat'ın (Çevre ve AI sorumlusu) yapacağı engellere veya polislere çarpma durumunu test ettim
    private void OnCollisionEnter(Collision collision)
    {
        // Eğer çarptığımız obje bir Engel veya Polis ise hasar almasını sağladım
        if (collision.gameObject.CompareTag("Obstacle") || collision.gameObject.CompareTag("Police"))
        {
            TakeDamage(25); // Çarpınca şimdilik 25 hasar almasını ayarladım (4 vuruşta ölür)
        }
    }
}