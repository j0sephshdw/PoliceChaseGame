using UnityEngine;

// ============================================================
// ARMOR BOOST ABILITY — Oyun Döngüsü ve UI (Bedirhan) sorumluluğunda.
// Seçildiğinde aracın maksimum canını kalıcı olarak artıran pasif yetenek.
// IAbility sözleşmesine uyduğu için CardSelectionUI bunu diğer
// yeteneklerden (Kalkan, Şok Dalgası vb.) ayırt etmeden kullanabiliyor.
// ============================================================
[CreateAssetMenu(fileName = "NewArmorBoostAbility", menuName = "Oyun Verileri/Yetenekler/Zırh Takviyesi")]
public class ArmorBoostAbilityData : ScriptableObject, IAbility
{
    public string AbilityName => "Zırh Takviyesi";
    public string Description => "Maksimum canını kalıcı olarak artırır.";
    public Sprite Icon => icon;

    [SerializeField] private Sprite icon;
    [SerializeField] [Range(0f, 1f)] private float percentageIncrease = 0.10f; // %10 varsayılan

    public void Activate(GameObject target)
    {
        PlayerHealth health = target.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.IncreaseMaxHealth(percentageIncrease);
        }
    }
}