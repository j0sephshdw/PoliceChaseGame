using UnityEngine;

[CreateAssetMenu(fileName = "NewSelfRepairAbility", menuName = "Oyun Verileri/Yetenekler/Kendi Kendini Onarma")]
public class SelfRepairAbilityData : ScriptableObject, IAbility
{
    public string AbilityName => "Kendi Kendini Onarma";
    public string Description => "Zamanla yavaşça can yeniler.";
    public Sprite Icon => icon;

    [SerializeField] private Sprite icon;
    [SerializeField] private float healPerSecond = 2f;

    public void Activate(GameObject target)
    {
        PlayerHealth health = target.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.IncreaseRegen(healPerSecond);
        }
    }
}