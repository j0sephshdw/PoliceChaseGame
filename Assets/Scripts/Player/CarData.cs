using UnityEngine;

// Bu satır sayesinde Unity'de sağ tık menüsüne "Yeni Araç Verisi" oluşturma butonu ekledim
[CreateAssetMenu(fileName = "NewCarData", menuName = "Oyun Verileri/Yeni Araç", order = 1)]
public class CarData : ScriptableObject
{
    [Header("Araç Kimliği")]
    public string carName; // Aracın menüde görünecek ismi

    [Header("Sürüş Hissiyatı ve Fizikler")]
    public float maxSpeed = 15f; // Ulaşabileceği maksimum hız
    public float acceleration = 5f; // İvmelenme hızı 
    public float turnSpeed = 100f; // Dönüş kıvraklığı/hassasiyeti

    [Header("Dayanıklılık (Sonra Bağlanacak)")]
    public int maxHealth = 100;
    public int baseArmor = 0;
}