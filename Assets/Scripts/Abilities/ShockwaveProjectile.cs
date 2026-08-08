using UnityEngine;

public class ShockwaveProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 15f;
    [SerializeField] private float lifeTime = 7f;      // bu süre sonunda kendini yok eder
    [SerializeField] private float stunDuration = 3f;  // çarptığı polisi bu kadar süre durdurur

    private float elapsedTime = 0f;

    private void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        elapsedTime += Time.deltaTime;
        if (elapsedTime >= lifeTime)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Police")) return;

        PoliceCarAI policeAI = other.GetComponent<PoliceCarAI>();
        if (policeAI != null) policeAI.Stun(stunDuration);
    }
}