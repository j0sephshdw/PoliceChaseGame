using System.Collections;
using UnityEngine;
using TMPro;

public class SoccerGoal : MonoBehaviour
{
    [Header("Referanslar")]
    public Transform futbolTopu;
    public TextMeshProUGUI golText;

    [Header("Puan Ayarı")]
    public int golPuani = 200;

    [Header("Iskalama Ayarı")]
    public float durmaHizi = 0.5f;
    public float hareketsizKalmaSuresi = 1.0f; // Top kaç saniye hareketsiz kalırsa Miss saysın?

    private Vector3 topBaslangicPos;
    private Quaternion topBaslangicRot;
    private Rigidbody topRb;

    private bool islemDevamEdiyor = false;
    private bool topVurulduMu = false;
    private float durmaSayaci = 0f; //Topun durduğu süreyi sayacak

    void Start()
    {
        if (golText != null)
            golText.gameObject.SetActive(false);

        if (futbolTopu != null)
        {
            topBaslangicPos = futbolTopu.position;
            topBaslangicRot = futbolTopu.rotation;
            topRb = futbolTopu.GetComponent<Rigidbody>();
        }
    }

    void Update()
    {
        if (islemDevamEdiyor || topRb == null) return;

        float uzaklik = Vector3.Distance(futbolTopu.position, topBaslangicPos);
        if (uzaklik > 2f)
        {
            topVurulduMu = true;
        }

        if (topVurulduMu)
        {
            // Eğer top yavaşladıysa sayacı artır
            if (topRb.linearVelocity.magnitude < durmaHizi)
            {
                durmaSayaci += Time.deltaTime; // Saniyeleri saymaya başla

                // Top belirlediğimiz süre (örn: 1 saniye) boyunca durduysa
                if (durmaSayaci >= hareketsizKalmaSuresi)
                {
                    StartCoroutine(MissSureci());
                }
            }
            else // Eğer top tekrar hızlanırsa 
            {
                durmaSayaci = 0f; // Sayacı sıfırla ve tehlikeyi atlat
            }

            // Haritadan aşağı düşme kontrolü
            if (futbolTopu.position.y < -5f)
            {
                StartCoroutine(MissSureci());
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (islemDevamEdiyor) return;

        if (other.CompareTag("SoccerBall") || other.transform == futbolTopu)
        {
            Rigidbody ballRb = other.GetComponent<Rigidbody>();

            if (ballRb != null && Vector3.Dot(ballRb.linearVelocity.normalized, transform.forward) > 0.1f)
            {
                StartCoroutine(GolSureci());
            }
        }
    }

    IEnumerator GolSureci()
    {
        islemDevamEdiyor = true;

        // ---  BOMBA SÜRESİ EKLE (+15 Saniye) ---
        BombTimer timer = Object.FindFirstObjectByType<BombTimer>();
        if (timer != null)
        {
            timer.SureEkle(15f);
        }

        if (golText != null)
        {
            golText.gameObject.SetActive(true);
            golText.text = "GOAL!!!";
            golText.color = Color.green;
        }

        // SİLİNEN KISIM BURASIYDI: Bekleme ve sıfırlama kodları
        yield return new WaitForSeconds(2.5f);
        SistemiSifirla();
    }

    IEnumerator MissSureci()
    {
        islemDevamEdiyor = true;

        if (golText != null)
        {
            golText.gameObject.SetActive(true);
            golText.text = "MISS!";
            golText.color = Color.red;
        }

        yield return new WaitForSeconds(2.0f);

        SistemiSifirla();
    }

    private void SistemiSifirla()
    {
        if (golText != null)
            golText.gameObject.SetActive(false);

        if (futbolTopu != null && topRb != null)
        {
            topRb.linearVelocity = Vector3.zero;
            topRb.angularVelocity = Vector3.zero;
            futbolTopu.position = topBaslangicPos;
            futbolTopu.rotation = topBaslangicRot;
        }

        topVurulduMu = false;
        islemDevamEdiyor = false;
        durmaSayaci = 0f; // Tur bitince sayacı sıfırla
    }
}