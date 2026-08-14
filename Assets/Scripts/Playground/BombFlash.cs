using UnityEngine;

public class BombFlash : MonoBehaviour
{
    [Header("Parlama Ayarları")]
    public Color normalRenk = new Color(0.5f, 0f, 0f); // Koyu kırmızı
    public Color parlamaRengi = Color.red;             // Parlak kırmızı
    public float parlamaHizi = 8f;                     // Yanıp sönme hızı

    private Renderer meshRenderer;
    private MaterialPropertyBlock propBlock;

    void Awake()
    {
        // Modelin ana objesinde veya alt çocuklarında olan Renderer'ı otomatik bulur
        meshRenderer = GetComponentInChildren<Renderer>();
        propBlock = new MaterialPropertyBlock();
    }

    void Update()
    {
        if (meshRenderer == null) return;

        // Yumuşak geçiş sinüs dalgası
        float gecis = (Mathf.Sin(Time.time * parlamaHizi) + 1f) / 2f;
        Color guncelRenk = Color.Lerp(normalRenk, parlamaRengi, gecis);

        // Performans dostu MaterialPropertyBlock
        meshRenderer.GetPropertyBlock(propBlock);
        propBlock.SetColor("_Color", guncelRenk);
        propBlock.SetColor("_BaseColor", guncelRenk); // URP / HDRP desteği için
        meshRenderer.SetPropertyBlock(propBlock);
    }
}
