using UnityEngine;

public class DynamicDriftSmoke : MonoBehaviour
{
    [Header("Bileşenler")]
    public ParticleSystem leftSmoke;
    public ParticleSystem rightSmoke;

    [Header("Lastik İzleri (Trail)")]
    public TrailRenderer leftTrail;
    public TrailRenderer rightTrail;

    public BoxCollider carCollider;

    [Header("Ayarlar")]
    public float minimumSpeedForSmoke = 1.0f;

    private ParticleSystem.EmissionModule leftEmission;
    private ParticleSystem.EmissionModule rightEmission;

    private PlayerCarController carController;
    private Vector3 lastPosition;
    private float currentSpeed;

    void Start()
    {
        lastPosition = transform.position;

        if (leftSmoke != null) leftEmission = leftSmoke.emission;
        if (rightSmoke != null) rightEmission = rightSmoke.emission;

        leftEmission.enabled = false;
        rightEmission.enabled = false;

        if (leftTrail != null) leftTrail.emitting = false;
        if (rightTrail != null) rightTrail.emitting = false;
    }

    void OnEnable()
    {
        if (leftSmoke != null && !leftSmoke.isPlaying) leftSmoke.Play();
        if (rightSmoke != null && !rightSmoke.isPlaying) rightSmoke.Play();
    }

    void FixedUpdate()
    {
        if (carCollider == null) return;

        if (leftSmoke != null && !leftSmoke.isPlaying) leftSmoke.Play();
        if (rightSmoke != null && !rightSmoke.isPlaying) rightSmoke.Play();

        if (carController == null)
        {
            carController = GetComponent<PlayerCarController>();
            if (carController == null) carController = GetComponentInParent<PlayerCarController>();
        }

        // 1. Dumanların ve İzlerin Yerini Otomatik Ayarla (Tekerlek hizası)
        Vector3 center = carCollider.center;
        Vector3 size = carCollider.size;
        float groundHeight = center.y - (size.y / 2) + 0.1f; // Yere çok yakın olsun ki havada çizilmesin

        if (leftSmoke != null) leftSmoke.transform.localPosition = new Vector3(center.x - (size.x / 2), groundHeight + 0.2f, center.z - (size.z / 2));
        if (rightSmoke != null) rightSmoke.transform.localPosition = new Vector3(center.x + (size.x / 2), groundHeight + 0.2f, center.z - (size.z / 2));

        if (leftTrail != null) leftTrail.transform.localPosition = new Vector3(center.x - (size.x / 2), groundHeight, center.z - (size.z / 2));
        if (rightTrail != null) rightTrail.transform.localPosition = new Vector3(center.x + (size.x / 2), groundHeight, center.z - (size.z / 2));

        // 2. Hız Kontrolü
        currentSpeed = Vector3.Distance(transform.position, lastPosition) / Time.fixedDeltaTime;
        lastPosition = transform.position;

        // 3. Eşik Kontrolü
        bool isDrifting = false;

        if (carController != null && currentSpeed > minimumSpeedForSmoke)
        {
            float horizontalInput = carController.GetTurnInput();

            if (Mathf.Abs(horizontalInput) > 0.1f || carController.GetHandbrakeStatus())
            {
                isDrifting = true;
            }
        }

        // Vanaları ve Çizim modunu aç/kapat
        leftEmission.enabled = isDrifting;
        rightEmission.enabled = isDrifting;

        if (leftTrail != null) leftTrail.emitting = isDrifting;
        if (rightTrail != null) rightTrail.emitting = isDrifting;
    }
}