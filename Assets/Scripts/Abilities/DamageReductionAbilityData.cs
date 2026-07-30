using UnityEngine;

[CreateAssetMenu(fileName = "NewDamageReductionAbility", menuName = "Oyun Verileri/Yetenekler/Hasar Azaltma")]
public class DamageReductionAbilityData : ScriptableObject, IAbility
{
    public string AbilityName => "Hasar Azaltma";
    public string Description => "Aldığın hasarı kalıcı olarak azaltır.";
    public Sprite Icon => icon;

    [SerializeField] private Sprite icon;
    [SerializeField] [Range(0f, 1f)] private float percentageIncrease = 0.05f;

    public void Activate(GameObject target)
    {
        PlayerHealth health = target.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.IncreaseDamageReduction(percentageIncrease);
        }
    }
}