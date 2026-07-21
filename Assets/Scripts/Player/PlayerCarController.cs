using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class PlayerCarController : MonoBehaviour
{
    [Header("Araç Veri Paketi")]
    public CarData currentCarData; // Farklı araçların özelliklerini çekeceğim veri dosyası

    private float originalMaxSpeed; // Hızlanma yeteneği için orijinal hızı hafızada tutacağım
    private float currentSpeed = 0f; // Aracın anlık hızı (İvmelenme için)
    private float turnInput;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    private void Start()
    {
        // Eğer araca bir veri dosyası (CarData) bağlandıysa özelliklerini oradan çek, yoksa varsayılan değerler ata
        if (currentCarData != null)
        {
            originalMaxSpeed = currentCarData.maxSpeed;
        }
        else
        {
            Debug.LogWarning("DİKKAT: Araca bir CarData bağlanmamış! Varsayılan hızlar kullanılıyor.");
            originalMaxSpeed = 15f;
        }
    }

    private void Update()
    {
        turnInput = 0f;

        if (Input.GetAxisRaw("Horizontal") != 0)
        {
            turnInput = Input.GetAxisRaw("Horizontal");
        }

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.position.x < Screen.width / 2f) turnInput = -1f;
            else if (touch.position.x > Screen.width / 2f) turnInput = 1f;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            ActivateSpeedBoost(2f, 1.5f);
        }
    }

    public void ActivateSpeedBoost(float multiplier, float duration)
    {
        StartCoroutine(SpeedBoostRoutine(multiplier, duration));
    }

    private IEnumerator SpeedBoostRoutine(float multiplier, float duration)
    {
        Debug.Log("🚀 HIZLANMA AKTİF!");
        originalMaxSpeed *= multiplier; // Hedef hızı geçici olarak katladım

        yield return new WaitForSeconds(duration);

        originalMaxSpeed /= multiplier; // Süre bitince hedef hızı eski haline getirdim
        Debug.Log("🚀 Hızlanma BİTTİ!");
    }

    private void FixedUpdate()
    {
        MoveCar();
        SteerCar();
    }

    private void MoveCar()
    {
        // İVMELENME MANTIĞI: Araç anında max hıza çıkmaz. Mevcut hızını, max hıza doğru ivme değeri kadar yavaş yavaş artırır.
        float accel = currentCarData != null ? currentCarData.acceleration : 5f;

        currentSpeed = Mathf.MoveTowards(currentSpeed, originalMaxSpeed, accel * Time.fixedDeltaTime);

        Vector3 movement = transform.forward * currentSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movement);
    }

    private void SteerCar()
    {
        float tSpeed = currentCarData != null ? currentCarData.turnSpeed : 100f;

        float turn = turnInput * tSpeed * Time.fixedDeltaTime;
        Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
        rb.MoveRotation(rb.rotation * turnRotation);
    }
}