using UnityEngine;
using System.Collections; // Zamanlayıcı kullanabilmek için ekledim

[RequireComponent(typeof(Rigidbody))]
public class PlayerCarController : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    [SerializeField] private float moveSpeed = 15f;
    [SerializeField] private float turnSpeed = 100f;

    private float originalMoveSpeed; // Yetenek bitince arabanın eski hızına dönmesi için başlangıç hızını burada tuttum
    private float turnInput;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // Aracın fiziksel olarak takla atmasını engellemek için dönüşleri kilitledim
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        originalMoveSpeed = moveSpeed; // Başlangıç hızını kaydettim
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

        // Bedirhan UI kısmını halledene kadar kendi bilgisayarımda Space tuşu ile test edebilmek için ekledim
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ActivateSpeedBoost(2f, 1.5f); // Space'e basınca 1.5 saniye boyunca hızı 2 katına çıkardım
        }
    }

    // Bedirhan'ın UI üzerinden çağıracağı Hızlanma fonksiyonunu hazırladım
    public void ActivateSpeedBoost(float multiplier, float duration)
    {
        StartCoroutine(SpeedBoostRoutine(multiplier, duration));
    }

    // Hızlanma süresini hesaplaması için arka plan işlemi (Coroutine) yazdım
    private IEnumerator SpeedBoostRoutine(float multiplier, float duration)
    {
        Debug.Log("🚀 HIZLANMA AKTİF!");
        moveSpeed = originalMoveSpeed * multiplier; // Hızı geçici olarak çarptım

        yield return new WaitForSeconds(duration); // Belirttiğim süre dolana kadar beklettim

        moveSpeed = originalMoveSpeed; // Süre bitince aracı orijinal hızına döndürdüm
        Debug.Log("🚀 Hızlanma BİTTİ!");
    }

    private void FixedUpdate()
    {
        MoveCar();
        SteerCar();
    }

    private void MoveCar()
    {
        Vector3 movement = transform.forward * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movement);
    }

    private void SteerCar()
    {
        float turn = turnInput * turnSpeed * Time.fixedDeltaTime;
        Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
        rb.MoveRotation(rb.rotation * turnRotation);
    }
}