using UnityEngine;

public class RampBoost : MonoBehaviour
{
    [Header("Rampa Hızlandırma Verisi")]
    // Senin o kusursuz ScriptableObject verin!
    public SpeedBoostAbilityData boostData;

    private void OnTriggerEnter(Collider other)
    {
        // Çarpan şey oyuncu ise
        if (other.CompareTag("Player"))
        {
            // ScriptableObject içindeki Activate metodunu çalıştır
            if (boostData != null)
            {
                boostData.Activate(other.gameObject);
            }
        }
    }
}