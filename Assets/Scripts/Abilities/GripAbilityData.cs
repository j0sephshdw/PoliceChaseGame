using UnityEngine;

[CreateAssetMenu(fileName = "NewGripAbility", menuName = "Oyun Verileri/Yetenekler/Yol Tutuşu")]
public class GripAbilityData : ScriptableObject, IAbility
{
    public string AbilityName => "Yol Tutuşu";
    public string Description => "Viraj alma ve tutunma kabiliyetini kalıcı olarak artırır.";
    public Sprite Icon => icon;

    [SerializeField] private Sprite icon;
    [SerializeField] [Range(0f, 1f)] private float percentageIncrease = 0.15f;

    public void Activate(GameObject target)
    {
        PlayerCarController car = target.GetComponent<PlayerCarController>();
        if (car != null)
        {
            car.IncreaseGrip(percentageIncrease);
        }
    }
}