using UnityEngine;

[CreateAssetMenu(fileName = "NewSelfRepairAbility", menuName = "Oyun Verileri/Yetenekler/Kendi Kendini Onarma")]
public class SelfRepairAbilityData : ScriptableObject, IAbility
{
    public string AbilityName => "Kendi Kendini Onarma";
    public string Description => "Zamanla yavaşça can yeniler.";
    public Sprite Icon => icon;
    public int MaxLevel => maxLevel;

    [SerializeField] private Sprite icon;
    [SerializeField] private float healPerSecond = 1f;
    [SerializeField] [Range(0f, 1f)] private float diminishingFactor = 0.7f;
    [SerializeField] private int maxLevel = 5;

    private float GetTotalIncreaseAtLevel(int level)
    {
        float total = 0f;
        float currentIncrement = healPerSecond;
        for (int i = 0; i < level; i++)
        {
            total += currentIncrement;
            currentIncrement *= diminishingFactor;
        }
        return total;
    }

    public string GetValueAtLevel(int level)
    {
        float total = GetTotalIncreaseAtLevel(level);
        return total.ToString("0.#") + "/sn";
    }

    public void Activate(GameObject target, int currentLevel)
    {
        float delta = GetTotalIncreaseAtLevel(currentLevel + 1) - GetTotalIncreaseAtLevel(currentLevel);

        PlayerHealth health = target.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.IncreaseRegen(delta);
        }
    }
}