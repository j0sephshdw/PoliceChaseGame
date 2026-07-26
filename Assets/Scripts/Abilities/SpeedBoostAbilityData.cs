using UnityEngine;

[CreateAssetMenu(fileName = "NewSpeedBoostAbility", menuName = "Oyun Verileri/Yetenekler/Hızlanma (Speed Boost)")]
public class SpeedBoostAbilityData : ScriptableObject, IAbility
{
    [Header("UI Görsel Ayarları (Bedirhan İçin)")]
    [SerializeField] private string abilityName = "Nitro Hızlanma";
    [SerializeField] private string description = "Aracın maksimum hızını kısa süreliğine 2 katına çıkarır.";
    [SerializeField] private Sprite icon;

    [Header("Mekanik Ayarları (Senin İçin)")]
    public float speedMultiplier = 2f;
    public float duration = 1.5f;

    // IAbility arayüzünün zorunlu kıldığı özellikler
    public string AbilityName => abilityName;
    public string Description => description;
    public Sprite Icon => icon;

    // Bedirhan'ın sistemi kart seçildiğinde burayı tetikleyecek
    public void Activate(GameObject target)
    {
        // Gelen 'target' objesinden PlayerCarController bileşenini yakalıyoruz
        PlayerCarController carController = target.GetComponent<PlayerCarController>();

        if (carController != null)
        {
            //  hızlandırma fonksiyonunu çalıştır
            carController.ActivateSpeedBoost(speedMultiplier, duration);
            Debug.Log($"🚀 {abilityName} aktif edildi! Çarpan: {speedMultiplier}, Süre: {duration}s");
        }
        else
        {
            Debug.LogWarning("Hedef objede PlayerCarController bulunamadı!");
        }
    }
}