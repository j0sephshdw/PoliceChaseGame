using UnityEngine;

[CreateAssetMenu(fileName = "NewDamageReductionAbility", menuName = "Oyun Verileri/Yetenekler/Hasar Azaltma")]
public class DamageReductionAbilityData : ScriptableObject, IAbility
{
    public string AbilityName => "Hasar Azaltma";
    public string Description => "Aldığın hasarı kalıcı olarak azaltır.";
    public Sprite Icon => icon;
    public int MaxLevel => maxLevel;

    [SerializeField] private Sprite icon;
    [SerializeField] [Range(0f, 1f)] private float percentageIncrease = 0.08f;
    [SerializeField] [Range(0f, 1f)] private float diminishingFactor = 0.7f;
    [SerializeField] private int maxLevel = 5;

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
        float delta = GetTotalIncreaseAtLevel(currentLevel + 1) - GetTotalIncreaseAtLevel(currentLevel);

        PlayerHealth health = target.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.IncreaseDamageReduction(delta);
        }
    }
}