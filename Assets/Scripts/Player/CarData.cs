using UnityEngine;

[CreateAssetMenu(fileName = "NewCarData", menuName = "Oyun Verileri/Yeni Araç", order = 1)]
public class CarData : ScriptableObject
{
    [Header("Araç Kimliği")]
    public string carName;

    [Header("3D Görsel Model")]
    public GameObject carPrefab;

    [Header("Çarpışma Kutusu (Box Collider) Ölçüleri")]
    public Vector3 colliderCenter = new Vector3(0, 0.5f, 0); // Varsayılan merkez
    public Vector3 colliderSize = new Vector3(1, 1, 2);      // Varsayılan boyut

    [Header("Sürüş Hissiyatı ve Fizikler")]
    public float maxSpeed = 15f;
    public float acceleration = 5f;
    public float turnSpeed = 100f;

    [Header("Drift ve Ağırlık Transferi")]
    public float driftGrip = 3f;
    public float maxLeanAngle = 15f;

    [Header("Dayanıklılık")]
    public int maxHealth = 100;
    public int baseArmor = 0;
}