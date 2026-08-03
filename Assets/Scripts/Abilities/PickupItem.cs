using UnityEngine;

public class PickupItem : MonoBehaviour
{
    public enum PickupType { Shield, SpeedBoost, Heal }

    [SerializeField] private PickupType type;
    [SerializeField] private float effectDuration = 3f; //efekt süresi
    [SerializeField] private float speedMultiplier = 1.5f; //hızlanma yüzdesi
    [SerializeField] private int healAmount = 10; //can miktarı

    private void OnTriggerEnter(Collider other)
    {
        if(!other.CompareTag("Player")) return;

        switch (type)
        {
            case PickupType.Shield:
            {
                PlayerHealth health = other.GetComponent<PlayerHealth>();
                if (health != null) health.ActivateShield(effectDuration);
                break;
            }
            case PickupType.SpeedBoost:
            {
                PlayerCarController car = other.GetComponent<PlayerCarController>();
                if (car != null) car.ActivateSpeedBoost(speedMultiplier, effectDuration);
                break;
            }
            case PickupType.Heal:
            {
                PlayerHealth health = other.GetComponent<PlayerHealth>();
                if (health != null) health.Heal(healAmount);
                break;
            }
        }
        UISoundPlayer.PlayCardSelect();
        Destroy(gameObject);
            
        
    }
}

