using UnityEngine;

public class SoundSystem : MonoBehaviour
{
    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    [Header("Sound Arrays")]
    [SerializeField] private AudioClip[] ambientSounds;
    [SerializeField] private AudioClip[] scareSounds;
    [SerializeField] private AudioClip[] randomCreepySounds;

    [Header("Settings")]
    [SerializeField] private float volume = 1f;

    public void PlayAmbient()
    {
        PlayRandom(ambientSounds);
    }

    public void PlayScare()
    {
        PlayRandom(scareSounds);
    }

    public void PlayCreepy()
    {
        PlayRandom(randomCreepySounds);
    }

    private void PlayRandom(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0)
            return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];

        audioSource.PlayOneShot(clip, volume);
    }
}