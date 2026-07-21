using UnityEngine;
using System; // Event'leri kullanabilmek için ekledim
using System.Collections; // Zamanlayıcı (Coroutine) kullanabilmek için ekledim

public class PlayerHealth : MonoBehaviour
{
    [Header("Can Ayarları")]
    public int maxHealth = 100;
    public int currentHealth;
    private bool isShieldActive = false; // Kalkanın o an açık olup olmadığını takip etmesi için değişken tanımladım

    // Bedirhan'ın (UI ve Oyun Döngüsü sorumlusu) kendi sisteminde kullanacağı event'leri (tetikleyicileri) oluşturdum
    public event Action<int> OnHealthChanged;
    public event Action OnPlayerDeath;

    private PlayerCarController carController;

    private void Awake()
    {
        carController = GetComponent<PlayerCarController>();
    }

    private void Start()
    {
        // Oyun başladığında canı fulledim
        currentHealth = maxHealth;
    }

    private void Update()
    {
        // Bedirhan UI kısmını yapana kadar E tuşu ile kalkanı test edebilmek için geçici olarak yazdım
        if (Input.GetKeyDown(KeyCode.E))
        {
            ActivateShield(3f); // E'ye basınca 3 saniyelik kalkan açılmasını sağladım
        }
    }

    // Bedirhan'ın UI'dan çağıracağı Kalkan Fonksiyonunu hazırladım
    public void ActivateShield(float duration)
    {
        if (!isShieldActive) // Eğer kalkan zaten açık değilse açılmasını sağladım
        {
            StartCoroutine(ShieldRoutine(duration));
        }
    }

    // Kalkanın ne kadar süre açık kalacağını hesaplaması için arka plan işlemi yazdım
    private IEnumerator ShieldRoutine(float duration)
    {
        isShieldActive = true;
        Debug.Log("🛡️ Kalkan AKTİF! Hasar alınmayacak.");

        yield return new WaitForSeconds(duration); // Belirttiğim süre kadar beklettim

        isShieldActive = false;
        Debug.Log("🛡️ Kalkan KAPANDI!");
    }

    // Dışarıdan veya engellerden hasar alınca çalışacak olan fonksiyonu yazdım
    public void TakeDamage(int damageAmount)
    {
        // KALKAN FIX: Kalkanın hasarı engellemesi için bu kontrolü ekledim. Kalkan açıksa hasar almadan fonksiyondan çıkıyor.
        if (isShieldActive)
        {
            Debug.Log("Bloklandı! Kalkan hasarı emdi.");
            return;
        }

        currentHealth -= damageAmount;

        // Can sıfırın altına düşmesin diye sınırlandırdım
        currentHealth = Mathf.Max(currentHealth, 0);

        // Bedirhan'ın UI sistemine haber verdim: "Can değişti, can barını vs. güncelle"
        OnHealthChanged?.Invoke(currentHealth);

        Debug.Log("Araç Hasar Aldı! Kalan Can: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Araç Parçalandı! GAME OVER."); // Şimdilik konsola yazdırdım

        // Aracı durdurdum
        if (carController != null)
        {
            carController.enabled = false;
        }

        // Bedirhan'ın Game Over ekranını tetiklemesi için haber verdim (Event'i çağırdım)
        OnPlayerDeath?.Invoke();
    }

    // Berat'ın (Çevre ve AI sorumlusu) yapacağı engellere veya polislere çarpma durumunu test ettim
    private void OnCollisionEnter(Collision collision)
    {
        // Eğer çarptığımız obje bir Engel veya Polis ise hasar almasını sağladım
        if (collision.gameObject.CompareTag("Obstacle") || collision.gameObject.CompareTag("Police"))
        {
            TakeDamage(25); // Çarpınca şimdilik 25 hasar almasını ayarladım (4 vuruşta ölür). Sonradan Pako Forever'daki gibi tekte ölecek şekilde de değiştirebiliriz.
        }
    }
}