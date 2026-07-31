using UnityEngine;

[CreateAssetMenu(fileName = "NewShockwaveAbility", menuName = "Oyun Verileri/Yetenekler/Şok Dalgası (Shockwave)")]
public class ShockwaveAbilityData : ScriptableObject, IAbility
{
    [Header("UI Görsel Ayarları")]
    [SerializeField] private string abilityName = "Şok Dalgası";
    [SerializeField] private string description = "Yakındaki engelleri ve polisleri uzağa fırlatır.";
    [SerializeField] private Sprite icon;

    [Header("Mekanik Ayarları")]
    public float radius = 10f; // Etki alanı
    public float explosionForce = 500f; // İtme gücü

    public string AbilityName => abilityName;
    public string Description => description;
    public Sprite Icon => icon;

    public int MaxLevel => throw new System.NotImplementedException();

    public void Activate(GameObject target)
    {
        PlayerCarController carController = target.GetComponent<PlayerCarController>();

        if (carController != null)
        {
            carController.ActivateShockwave(radius, explosionForce);
            Debug.Log($"💥 {abilityName} patlatıldı! Yarıçap: {radius}, Güç: {explosionForce}");
        }
    }

    public string GetValueAtLevel(int level)
    {
        throw new System.NotImplementedException();
    }

    public void Activate(GameObject target, int currentLevel)
    {
        throw new System.NotImplementedException();
    }
}