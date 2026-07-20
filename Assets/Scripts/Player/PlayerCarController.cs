using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerCarController : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    [SerializeField] private float moveSpeed = 15f; // Otomatik ileri gitme hızı
    [SerializeField] private float turnSpeed = 100f; // Sağa/sola dönme hızı

    private float turnInput;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        //  aracın takla atmasını engellemek için dönüşleri kilitledim
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    private void Update()
    {
        turnInput = 0f; // Her karede (frame) dönüşü sıfırla elimi çekince araba düzelsin diye

        // 1. BİLGİSAYAR İÇİN TEST KONTROLLERİ (A/D veya Sol/Sağ Yön Tuşları)
        // GetAxisRaw kullandm çünkü araba anında tepki versin istiyoruz
        if (Input.GetAxisRaw("Horizontal") != 0)
        {
            turnInput = Input.GetAxisRaw("Horizontal");
        }

        // 2. MOBİL TELEFON İÇİN DOKUNMATİK KONTROLLERİ
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0); // Ekrana yapılan ilk dokunuşu al

            // Eğer dokunulan yer ekranın genişliğinin yarısından küçükse (Sol taraf)
            if (touch.position.x < Screen.width / 2f)
            {
                turnInput = -1f; // Sola dön
            }
            // Eğer dokunulan yer ekranın genişliğinin yarısından büyükse (Sağ taraf)
            else if (touch.position.x > Screen.width / 2f)
            {
                turnInput = 1f; // Sağa dön
            }
        }
    }

    private void FixedUpdate()
    {
        MoveCar();
        SteerCar();
    }

    private void MoveCar()
    {
        // Araç oyuncu girdisi beklemeden OTOMATİK olarak hep ileri gitsin
        Vector3 movement = transform.forward * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movement);
    }

    private void SteerCar()
    {
        // Araç zaten otomatik gittiği için sadece dönüş uygula
        float turn = turnInput * turnSpeed * Time.fixedDeltaTime;
        Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
        rb.MoveRotation(rb.rotation * turnRotation);
    }
}