using UnityEngine;

public class PickupItem : MonoBehaviour
{
    public enum PickupType { Shield, SpeedBoost, Heal, Shockwave}

    [SerializeField] private PickupType type;
    [SerializeField] private float effectDuration = 3f; //efekt süresi
    [SerializeField] private float speedMultiplier = 1.5f; //hızlanma yüzdesi
    [SerializeField] private int healAmount = 10; //can miktarı
    [HideInInspector] public GameObject SourcePrefab; // PickupSpawner tarafından spawn edilirken atanır
    [SerializeField] private GameObject shockwavePrefab; // ShockwaveProjectile bileşenli prefab buraya sürüklenecek
    [SerializeField] private float rotationSpeed = 90f;

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
            case PickupType.Shockwave:
            {
                if (shockwavePrefab != null)
                {
                    Vector3 spawnPos = other.transform.position;
                    Quaternion spawnRot = Quaternion.LookRotation(-other.transform.forward);
                    Instantiate(shockwavePrefab, spawnPos, spawnRot);
                }
                break;
            }
        }
        UISoundPlayer.PlayCardSelect();
        PickupSpawner.ReturnToPool(SourcePrefab, gameObject); 
    }

    void Update()
    {
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);
    }
}

