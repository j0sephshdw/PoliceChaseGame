using UnityEngine;
using TMPro;

// ============================================================
// FPS COUNTER — Test amaçlı basit kare hızı göstergesi.
// Telefonda Profiler'a bağlanmadan gerçek performansı görebilmek için eklendi.
// Yayına hazırlanırken objesi kapatılabilir veya silinebilir.
// ============================================================
public class FPSCounter : MonoBehaviour
{
    [SerializeField] private TMP_Text fpsText;
    [Tooltip("Kaç saniyede bir güncellensin; her karede güncellemek okunamayacak kadar oynak olur")]
    [SerializeField] private float updateInterval = 0.5f;

    private float timer;
    private int frameCount;

    private void Update()
    {
        timer += Time.unscaledDeltaTime;
        frameCount++;

        if (timer >= updateInterval)
        {
            int fps = Mathf.RoundToInt(frameCount / timer);
            if (fpsText != null) fpsText.text = fps + " FPS";

            timer = 0f;
            frameCount = 0;
        }
    }
}