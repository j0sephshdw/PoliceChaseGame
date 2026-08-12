using System.Collections;
using UnityEngine;

public class SpikeStrip : MonoBehaviour
{
    [Header("Efektler")]
    public AudioClip punctureSound;

    [Header("Ceza Ayarları")]
    public float playerSpeedPenalty = 0.35f;
    public float penaltyDuration = 2.5f;
    [Tooltip("Kapanın oyuncudan eksilteceği can miktarı (Örn: 20)")]
    public int damageAmount = 20;

    private Coroutine disableCoroutine;

    private void OnEnable()
    {
        disableCoroutine = StartCoroutine(DisableAfterTime(15f));
    }

    private void OnDisable()
    {
        if (disableCoroutine != null)
        {
            StopCoroutine(disableCoroutine);
            disableCoroutine = null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Sadece arabayı değil, arabanın üstündeki PlayerHealth scriptini de bul!
            PlayerCarController playerCar = other.GetComponentInParent<PlayerCarController>();
            PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();

            if (playerCar != null)
            {
                // Canı senin asıl sisteminden düşürecek
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damageAmount);
                }

                playerCar.ActivateSpeedBoost(playerSpeedPenalty, penaltyDuration);

                PlayEffects();
                PoliceHelicopter.Instance.ReturnSpike(gameObject);
            }
        }
        else if (other.CompareTag("Police"))
        {
            PoliceCarAI policeCar = other.GetComponentInParent<PoliceCarAI>();
            if (policeCar != null && policeCar.isActiveAndEnabled)
            {
                policeCar.Explode();
                PlayEffects();
                PoliceHelicopter.Instance.ReturnSpike(gameObject);
            }
        }
    }

    private void PlayEffects()
    {
        if (punctureSound != null)
        {
            AudioSource.PlayClipAtPoint(punctureSound, transform.position, GameUIManager.GetGameVolume());
        }

        if (PoliceHelicopter.Instance != null)
        {
            GameObject vfx = PoliceHelicopter.Instance.GetVFX();
            if (vfx != null)
            {
                vfx.transform.position = transform.position;
                vfx.transform.rotation = Quaternion.identity;

                PoliceHelicopter.Instance.ReturnVFX(vfx, 2f);
            }
        }
    }

    private IEnumerator DisableAfterTime(float time)
    {
        yield return new WaitForSeconds(time);
        if (PoliceHelicopter.Instance != null)
        {
            PoliceHelicopter.Instance.ReturnSpike(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}