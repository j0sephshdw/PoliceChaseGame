using UnityEngine;
using System; // Event kütüphanesi

public class PlayerHealth : MonoBehaviour
{
    [Header("Can Ayarları")]
    public int maxHealth = 100;
    public int currentHealth;

    // (3. Kişi)Bedirhan'ın (UI ve Oyun Döngüsü sorumlusu) kullanacağı tetikleyiciler
    public event Action<int> OnHealthChanged;
    public event Action OnPlayerDeath;

    private PlayerCarController carController;

    private void Awake()
    {
        carController = GetComponent<PlayerCarController>();
    }

    private void Start()
    {
        // Oyun başladığında canı fulle
        currentHealth = maxHealth;
    }

    // Dışarıdan (veya engellerden) hasar almak için kullanılacak fonksiyon
    public void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;

        // Can sıfırın altına düşmesinn
        currentHealth = Mathf.Max(currentHealth, 0);

        // (3. kişi)Bedirhan'ın yazacağı UI sistemine haber ver: Can değişti, can barını vb guncelle
        OnHealthChanged?.Invoke(currentHealth);

        Debug.Log("Araç Hasar Aldı! Kalan Can: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Araç Parçalandı! GAME OVER.");//şimdilik bu şekilde game over yazıyor

        // Aracı durdur
        if (carController != null)
        {
            carController.enabled = false;
        }

        // (3. kişi)Bedirha'nın Game Over ekranını tetiklemesi için haber ver
        OnPlayerDeath?.Invoke();
    }

    // (2. Kişi)Berat (Çevre ve AI sorumlusu) yapacağı engellere çarpma testi
    private void OnCollisionEnter(Collision collision)
    {
        // Eğer çarptığımız obje bir Engel veya Polis ise hasar al
        if (collision.gameObject.CompareTag("Obstacle") || collision.gameObject.CompareTag("Police"))
        {
            TakeDamage(25); // Çarpınca şimdilik 25 hasar alsın (4 vuruşta ölür) sonradan pako foreverdeki gibi tekte ölecek şekilde de değiştirebiliriz şimdilik boyle
        }
    }
}