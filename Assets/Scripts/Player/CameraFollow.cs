using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Takip Edilecek Obje")]
    [SerializeField] private Transform target;

    private Vector3 offset;

    private void Start()
    {
        // Oyun başladığında kamera ile araba arasındaki mesafeyı kaydet
        if (target != null)
        {
            offset = transform.position - target.position;
        }
    }

    
    private void LateUpdate()
    {
        if (target != null)
        {
            // Kameranın pozisyonunu güncelle
            transform.position = target.position + offset;
        }
    }
}