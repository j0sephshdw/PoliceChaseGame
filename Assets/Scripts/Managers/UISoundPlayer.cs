using UnityEngine;

// ============================================================
// UI SOUND PLAYER — Oyun Döngüsü ve UI (Bedirhan) sorumluluğunda.
// Projedeki her yerden (herhangi bir sahneden, herhangi bir script'ten)
// UISoundPlayer.PlayClick() gibi çağrılarla ses çalabilmemizi sağlayan
// static (tekil örneğe ihtiyaç duymayan) yardımcı sınıf.
// ============================================================
public static class UISoundPlayer
{
    // Bu üç alanı UISoundInitializer dolduracak (Inspector'dan sürüklenen ses dosyalarıyla).
    public static AudioClip clickClip;
    public static AudioClip cardSelectClip;
    public static AudioClip errorClip;

    // Sesleri gerçekten çalacak AudioSource — ilk ses çalınmaya çalışıldığında
    // kendiliğinden oluşturulur, sahne değişse bile silinmemesi için
    // DontDestroyOnLoad ile kalıcı yapılır.
    private static AudioSource audioSource;

    private static void EnsureAudioSource()
    {
        if (audioSource == null)
        {
            GameObject obj = new GameObject("UISoundPlayer_AudioSource");
            Object.DontDestroyOnLoad(obj);
            audioSource = obj.AddComponent<AudioSource>();
        }
    }

    public static void PlayClick()
    {
        EnsureAudioSource();
        if (clickClip != null)
            audioSource.PlayOneShot(clickClip, UIManager.GetSFXVolume());
    }

    public static void PlayCardSelect()
    {
        EnsureAudioSource();
        if (cardSelectClip != null)
            audioSource.PlayOneShot(cardSelectClip, UIManager.GetSFXVolume());
    }

    public static void PlayError()
    {
        EnsureAudioSource();
        if (errorClip != null)
            audioSource.PlayOneShot(errorClip, UIManager.GetSFXVolume());
    }
}