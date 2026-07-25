using UnityEngine;

[CreateAssetMenu(fileName = "NewTestAbility", menuName = "Oyun Verileri/Yetenekler/Test Ability")]
public class TestAbilityData : ScriptableObject, IAbility
{
    [SerializeField] private string abilityName = "Test Yetenek";
    [SerializeField] private string description = "Bu bir test yeteneğidir";
    [SerializeField] private Sprite icon;

    public string AbilityName => abilityName;
    public string Description => description;
    public Sprite Icon => icon;

    public void Activate(GameObject target)
    {
        Debug.Log(abilityName + " test edildi!");
    }
}