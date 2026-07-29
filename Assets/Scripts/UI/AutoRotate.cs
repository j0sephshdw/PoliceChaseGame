using UnityEngine;

public class AutoRotate : MonoBehaviour
{
    public float donusHizi = 40f;

    void Update()
    {
        // unscaledDeltaTime sayesinde oyun (zaman) dursa bile araba menüde dönmeye devam et
        transform.Rotate(0, donusHizi * Time.unscaledDeltaTime, 0);
    }
}