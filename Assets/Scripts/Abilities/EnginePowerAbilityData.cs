using UnityEngine;

[CreateAssetMenu(fileName = "NewEnginePowerAbility", menuName = "Oyun Verileri/Yetenekler/Motor Gücü")]
public class EnginePowerAbilityData : ScriptableObject, IAbility
{
    public string AbilityName => "Motor Gücü";
    public string Description => "Maksimum hızını kalıcı olarak artırır.";
    public Sprite Icon => icon;

    [SerializeField] private Sprite icon;
    [SerializeField] [Range(0f, 1f)] private float percentageIncrease = 0.10f;

    public void Activate(GameObject target)
    {
        PlayerCarController car = target.GetComponent<PlayerCarController>();
        if (car != null)
        {
            car.IncreaseMaxSpeed(percentageIncrease);
        }
    }
}