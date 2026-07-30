using UnityEngine;

[CreateAssetMenu(fileName = "NewScoreMultiplierAbility", menuName = "Oyun Verileri/Yetenekler/Skor Çarpanı")]
public class ScoreMultiplierAbilityData : ScriptableObject, IAbility
{
    public string AbilityName => "Skor Çarpanı";
    public string Description => "Kazandığın puanı kalıcı olarak artırır.";
    public Sprite Icon => icon;

    [SerializeField] private Sprite icon;
    [SerializeField] [Range(0f, 1f)] private float percentageIncrease = 0.10f;

    public void Activate(GameObject target)
    {
        ScoreManager.Instance.IncreaseScoreMultiplier(percentageIncrease);
    }
}