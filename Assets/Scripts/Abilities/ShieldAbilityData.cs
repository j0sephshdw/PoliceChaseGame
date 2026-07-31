using UnityEngine;

[CreateAssetMenu(fileName = "NewShieldAbility", menuName = "Oyun Verileri/Yetenekler/Kalkan (Shield)")]
public class ShieldAbilityData : ScriptableObject, IAbility
{
    [Header("UI Görsel Ayarları")]
    [SerializeField] private string abilityName = "Enerji Kalkanı";
    [SerializeField] private string description = "Aracı kısa süreliğine hasar almaz yapar.";
    [SerializeField] private Sprite icon;

    [Header("Mekanik Ayarları")]
    public float duration = 3f;

    public string AbilityName => abilityName;
    public string Description => description;
    public Sprite Icon => icon;

    public int MaxLevel => throw new System.NotImplementedException();

    public void Activate(GameObject target)
    {
        // Burada aracın sağlık/hasar scriptine ulaşatım
        
        PlayerHealth healthController = target.GetComponent<PlayerHealth>();

        if (healthController != null)
        {
            // PlayerHealth içinde yazacağın fonksiyonu tetikliyoruz.
            healthController.ActivateShield(duration);
            Debug.Log($"🛡️ {abilityName} aktif edildi! Süre: {duration}s");
        }
    }

    public string GetValueAtLevel(int level)
    {
        throw new System.NotImplementedException();
    }

    public void Activate(GameObject target, int currentLevel)
    {
        throw new System.NotImplementedException();
    }
}