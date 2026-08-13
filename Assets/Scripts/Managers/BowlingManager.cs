using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BowlingManager : MonoBehaviour
{
    public enum OyunDurumu { Hazir, AtisYapildi, DusmeBekleniyor, SkorGosteriliyor }

    [Header("Referanslar")]
    public Transform bowlingTopu;
    public Transform lobutKlasoru;
    public TextMeshProUGUI skorText;

    [Header("Sıfırlama Ayarları")]
    public float beklemeSuresi = 4f;
    public float topMaksimumUzaklik = 60f;

    private Vector3 topBaslangicPos;
    private Quaternion topBaslangicRot;
    private Rigidbody topRb;

    private OyunDurumu durum = OyunDurumu.Hazir;
    private Coroutine aktifSifirlama;

    private class LobutVerisi
    {
        public Transform transform;
        public Vector3 ilkPozisyon;
        public Quaternion ilkRotasyon;
        public Rigidbody rb;
    }

    private List<LobutVerisi> lobutlar = new List<LobutVerisi>();

    void Start()
    {
        if (skorText != null) skorText.gameObject.SetActive(false);

        if (bowlingTopu != null)
        {
            topBaslangicPos = bowlingTopu.position;
            topBaslangicRot = bowlingTopu.rotation;
            topRb = bowlingTopu.GetComponent<Rigidbody>();
        }

        if (lobutKlasoru != null)
        {
            foreach (Transform cocuk in lobutKlasoru)
            {
                LobutVerisi veri = new LobutVerisi();
                veri.transform = cocuk;
                veri.ilkPozisyon = cocuk.position;
                veri.ilkRotasyon = cocuk.rotation;
                veri.rb = cocuk.GetComponent<Rigidbody>();
                lobutlar.Add(veri);
            }
        }
    }

    public void ArabaTopaVurdu()
    {
        if (durum == OyunDurumu.SkorGosteriliyor) return;

        if (durum == OyunDurumu.Hazir)
        {
            durum = OyunDurumu.AtisYapildi;
        }
        else if (durum == OyunDurumu.AtisYapildi || durum == OyunDurumu.DusmeBekleniyor)
        {
            if (aktifSifirlama != null) StopCoroutine(aktifSifirlama);
            StartCoroutine(SkorGosterVeResetle());
        }
    }

    void Update()
    {
        if (durum != OyunDurumu.AtisYapildi) return;

        bool kuralTetiklendi = false;

        foreach (var lobut in lobutlar)
        {
            if (Vector3.Dot(lobut.transform.up, Vector3.up) < 0.6f)
            {
                kuralTetiklendi = true;
                break;
            }
        }

        if (bowlingTopu != null && topRb != null && !kuralTetiklendi)
        {
            float uzaklik = Vector3.Distance(bowlingTopu.position, topBaslangicPos);

            if (uzaklik > topMaksimumUzaklik || bowlingTopu.position.y < -5f)
            {
                kuralTetiklendi = true;
            }
            else if (uzaklik > 2f && topRb.linearVelocity.magnitude < 0.5f)
            {
                kuralTetiklendi = true;
            }
        }

        if (kuralTetiklendi)
        {
            durum = OyunDurumu.DusmeBekleniyor;
            aktifSifirlama = StartCoroutine(NormalSifirlamaIslemi());
        }
    }

    IEnumerator NormalSifirlamaIslemi()
    {
        yield return new WaitForSeconds(beklemeSuresi);
        yield return StartCoroutine(SkorGosterVeResetle());
    }

    IEnumerator SkorGosterVeResetle()
    {
        durum = OyunDurumu.SkorGosteriliyor;

        int devrilenSayisi = 0;
        foreach (var lobut in lobutlar)
        {
            if (Vector3.Dot(lobut.transform.up, Vector3.up) < 0.6f)
            {
                devrilenSayisi++;
            }
        }

        if (skorText != null)
        {
            skorText.gameObject.SetActive(true);

            if (devrilenSayisi == lobutlar.Count)
            {
                skorText.text = "STRIKE!!!";
                skorText.color = Color.yellow;
            }
            else if (devrilenSayisi == 0)
            {
                skorText.text = "MISS!";
                skorText.color = Color.red;
            }
            else
            {
                skorText.text = devrilenSayisi + " PINS!";
                skorText.color = Color.white;
            }
        }

        yield return new WaitForSeconds(2f);

        if (skorText != null) skorText.gameObject.SetActive(false);

        if (bowlingTopu != null && topRb != null)
        {
            topRb.linearVelocity = Vector3.zero;
            topRb.angularVelocity = Vector3.zero;
            bowlingTopu.position = topBaslangicPos;
            bowlingTopu.rotation = topBaslangicRot;
        }

        foreach (var lobut in lobutlar)
        {
            if (lobut.rb != null)
            {
                lobut.rb.linearVelocity = Vector3.zero;
                lobut.rb.angularVelocity = Vector3.zero;
            }
            lobut.transform.position = lobut.ilkPozisyon;
            lobut.transform.rotation = lobut.ilkRotasyon;
        }

        durum = OyunDurumu.Hazir;
    }
}