using UnityEngine;

[CreateAssetMenu(fileName = "NewArmorBoostAbility", menuName = "Oyun Verileri/Yetenekler/Zırh Takviyesi")]
public class ArmorBoostAbilityData : ScriptableObject, IAbility
{
    public string AbilityName => "Zırh Takviyesi";
    public string Description => "Maksimum canını kalıcı olarak artırır.";
    public Sprite Icon => icon;
    public int MaxLevel => maxLevel;

    [SerializeField] private Sprite icon;
    [SerializeField] [Range(0f, 1f)] private float percentageIncrease = 0.05f;
    [SerializeField] [Range(0f, 1f)] private float diminishingFactor = 0.7f;
    [SerializeField] private int maxLevel = 5;

    // Belirtilen seviyeye kadar birikmiş TOPLAM artış yüzdesini hesaplar.
    // Her seviyede bir öncekinin sadece %70'i kadar (diminishingFactor) artış eklenir.
    private float GetTotalIncreaseAtLevel(int level)
    {
        float total = 0f;
        float currentIncrement = percentageIncrease;
        for (int i = 0; i < level; i++)
        {
            total += currentIncrement;
            currentIncrement *= diminishingFactor;
        }
        return total;
    }

    public string GetValueAtLevel(int level)
    {
        float totalPercent = GetTotalIncreaseAtLevel(level) * 100f;
        return Mathf.RoundToInt(totalPercent) + "%";
    }

    public void Activate(GameObject target, int currentLevel)
    {
        // Bu seçime özel, küçülen artış miktarı: bir sonraki seviyenin toplamı - şu anki seviyenin toplamı
        float delta = GetTotalIncreaseAtLevel(currentLevel + 1) - GetTotalIncreaseAtLevel(currentLevel);

        PlayerHealth health = target.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.IncreaseMaxHealth(delta);
        }
    }
}