using UnityEngine;

public class MusicManager : MonoBehaviour
{
    [SerializeField] private AudioClip musicClip;

    [Range(0f, 1f)]
    [SerializeField] private float musicBaseVolume = 0.6f; // müzik diğer seslere göre baskın gelmesin diye taban seviye eklendi

    private AudioSource audioSource;

    private void Awake() 
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = musicClip;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
    }

    void Start()
    {
        audioSource.volume = musicBaseVolume * UIManager.GetMusicVolume();
        audioSource.Play();
    }

    void Update()
    {
        audioSource.volume = musicBaseVolume * UIManager.GetMusicVolume();
    }
}
