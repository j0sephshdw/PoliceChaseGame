using UnityEngine;

public class TrafficCarController : MonoBehaviour
{
    public float speed = 10f;
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

        startPosition = transform.position;
    }

    private void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        if (Vector3.Distance(startPosition, transform.position) >= destroyDistance)
        {
            Destroy(gameObject);
        }
    }
}