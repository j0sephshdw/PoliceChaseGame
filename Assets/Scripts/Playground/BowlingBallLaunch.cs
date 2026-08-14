using UnityEngine;

public class BowlingBallLaunch : MonoBehaviour
{
    [Header("Fırlama Ayarları")]
    public float firlatmaGucu = 600f; 
    public float yukariGuc = 20f;     

    private Rigidbody rb;
    private BowlingManager manager; 

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        manager = Object.FindFirstObjectByType<BowlingManager>(); 
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Vector3 vurusYonu = (transform.position - collision.transform.position).normalized;
            vurusYonu.y = 0f;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            Vector3 kuvvet = (vurusYonu * firlatmaGucu) + (Vector3.up * yukariGuc);
            rb.AddForce(kuvvet, ForceMode.Impulse);

            
            // Eğer yönetici varsa VE bu top "SoccerBall" tag'ine sahip DEĞİLSE hakeme haber ver
            if (manager != null && !gameObject.CompareTag("SoccerBall"))
            {
                manager.ArabaTopaVurdu();
            }
        }
    }
}