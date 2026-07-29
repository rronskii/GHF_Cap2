using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SceneMusicPlayer : MonoBehaviour
{
    [Header("Track Selection")]
    [Tooltip("Drag your MP3 or WAV file here")]
    public AudioClip backgroundMusic;

    [Header("Playback Settings")]
    [Range(0f, 1f)]
    public float volume = 0.5f;
    public bool loopMusic = true;

    private AudioSource audioSource;

    private void Awake()
    {
        // Automatically grab the AudioSource this script creates
        audioSource = GetComponent<AudioSource>();

        // Apply your settings
        audioSource.clip = backgroundMusic;
        audioSource.volume = volume;
        audioSource.loop = loopMusic;

        // We turn off playOnAwake so we can control exactly when it starts
        audioSource.playOnAwake = false;
    }

    private void Start()
    {
        if (backgroundMusic != null)
        {
            audioSource.Play();
        }
        else
        {
            Debug.LogWarning($"[SceneMusicPlayer] No audio clip assigned to the music player in {gameObject.scene.name}!");
        }
    }
}