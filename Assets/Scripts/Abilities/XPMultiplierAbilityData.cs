using UnityEngine;

[CreateAssetMenu(fileName = "NewXPMultiplierAbility", menuName = "Oyun Verileri/Yetenekler/XP Çarpanı")]
public class XPMultiplierAbilityData : ScriptableObject, IAbility
{
    public string AbilityName => "XP Çarpanı";
    public string Description => "Kazandığın deneyim puanını (XP) kalıcı olarak artırır.";
    public Sprite Icon => icon;

    [SerializeField] private Sprite icon;
    [SerializeField] [Range(0f, 1f)] private float percentageIncrease = 0.10f;

    public void Activate(GameObject target)
    {
        ScoreManager.Instance.IncreaseXPMultiplier(percentageIncrease);
    }
}