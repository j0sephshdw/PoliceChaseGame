using UnityEngine;
using Unity.Cinemachine;

// ============================================================
// CAMERA SHAKE — Oyun Döngüsü ve UI (Bedirhan) sorumluluğunda.
// Kameranın konumunu Cinemachine her karede kendisi yazdığı için kamerayı elle
// sarsmak işe yaramıyor; sarsıntı, Cinemachine'in Impulse sistemiyle üretiliyor.
// Bu script oyuncu aracının üzerinde durur, böylece sarsıntı hep kameraların
// takip ettiği noktadan yayılır.
// Projede herhangi bir yerden CameraShake.Shake(0.5f) diyerek çağrılabilir;
// referans sürüklemeye gerek yok.
// ============================================================
[RequireComponent(typeof(CinemachineImpulseSource))]
public class CameraShake : MonoBehaviour
{
    // Sahnede tek örnek bulunur; diğer scriptlerin referanssız erişebilmesi için static tutuluyor.
    private static CameraShake instance;

    private CinemachineImpulseSource impulseSource;

    [Tooltip("Bütün sarsıntıların şiddetini topluca ayarlamak için genel çarpan")]
    [SerializeField] private float shakeMultiplier = 1f;

    private void Awake()
    {
        instance = this;
        impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    private void OnDestroy()
    {
        // static alanlar sahne değişince silinmediği için, yok olan örneğin
        // referansının geride kalmaması adına temizliyoruz.
        if (instance == this) instance = null;
    }

    // force: sarsıntı şiddeti. 0.3 hafif temas, 1.0 sert çarpma, 1.5 patlama gibi düşünülebilir.
    public static void Shake(float force)
    {
        if (instance == null || instance.impulseSource == null) return;

        instance.impulseSource.GenerateImpulseWithForce(force * instance.shakeMultiplier);
    }
}