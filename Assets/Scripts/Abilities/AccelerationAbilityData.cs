using UnityEngine;

[CreateAssetMenu(fileName = "NewAccelerationAbility", menuName = "Oyun Verileri/Yetenekler/İvme Artışı")]
public class AccelerationAbilityData : ScriptableObject, IAbility
{
    public string AbilityName => "İvme Artışı";
    public string Description => "Aracın ivmelenmesini (hıza ulaşma süresini) kalıcı olarak artırır.";
    public Sprite Icon => icon;

    [SerializeField] private Sprite icon;
    [SerializeField] [Range(0f, 1f)] private float percentageIncrease = 0.15f;

    public void Activate(GameObject target)
    {
        PlayerCarController car = target.GetComponent<PlayerCarController>();
        if (car != null)
        {
            car.IncreaseAcceleration(percentageIncrease);
        }
    }
}