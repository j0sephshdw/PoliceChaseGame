using Unity.VisualScripting;
using UnityEngine;

public class TrafficCarController : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    private float speed = 3f;
    public float destroyDistance = 300f;

    private Vector3 startPosition;

    private void Start()
    {
        // Sadece Traffic tag'lı araçlar hareket etsin
        if (!CompareTag("Traffic"))
        {
            enabled = false;
            return;
        }

        // Spawn noktasının rotasyonunu koruyarak modele 270° ekle
        transform.Rotate(0f, 270f, 0f, Space.Self);

        // Ölçeği ayarla
        transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

        Vector3 pos = transform.position;
        pos.y = 0.01f;
        transform.position = pos;

        startPosition = transform.position;
    }

    private void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime, Space.Self);

        if (Vector3.Distance(startPosition, transform.position) >= destroyDistance)
        {
            Destroy(gameObject);
        }
    }
}