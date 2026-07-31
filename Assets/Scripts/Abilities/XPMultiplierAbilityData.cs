using UnityEngine;

[CreateAssetMenu(fileName = "NewXPMultiplierAbility", menuName = "Oyun Verileri/Yetenekler/XP Çarpanı")]
public class XPMultiplierAbilityData : ScriptableObject, IAbility
{
    public string AbilityName => "XP Çarpanı";
    public string Description => "Kazandığın deneyim puanını (XP) kalıcı olarak artırır.";
    public Sprite Icon => icon;
    public int MaxLevel => maxLevel;

    [SerializeField] private Sprite icon;
    [SerializeField] [Range(0f, 1f)] private float percentageIncrease = 0.05f;
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
        ScoreManager.Instance.IncreaseXPMultiplier(delta);
    }
}