using UnityEngine;
using UnityEngine.UI;

public class SoundSystem : MonoBehaviour
{
    [SerializeField] private AudioSource HorrorAudioSource;
    [SerializeField] private AudioSource NormalAudioSource;
    [SerializeField] private AudioClip horrorBackgroundMusic;
    [SerializeField] private AudioClip normalBackgroundMusic;
    [SerializeField] private bool playerIsInDanger;
    [SerializeField] private float maxMusicVolume = 1f;
    [SerializeField] private float musicTransitionSpeed = 1f;


    private void Start()
    {
        NormalAudioSource.clip = normalBackgroundMusic;
        NormalAudioSource.volume = maxMusicVolume;
        NormalAudioSource.Play();

        HorrorAudioSource.clip = horrorBackgroundMusic;
        HorrorAudioSource.volume = 0f;
        playerIsInDanger = false;
    }

    private void Update()
    {
        if (playerIsInDanger)
        {
            HorrorAudioSource.volume = Mathf.Lerp(HorrorAudioSource.volume, maxMusicVolume, musicTransitionSpeed * Time.deltaTime);
            NormalAudioSource.volume = Mathf.Lerp(NormalAudioSource.volume, 0f, musicTransitionSpeed * Time.deltaTime);
            if (HorrorAudioSource.clip != horrorBackgroundMusic)
            {
                HorrorAudioSource.clip = horrorBackgroundMusic;
                HorrorAudioSource.Play();
            }
        }
        else
        {
            NormalAudioSource.volume = Mathf.Lerp(NormalAudioSource.volume, maxMusicVolume, musicTransitionSpeed * Time.deltaTime);
            HorrorAudioSource.volume = Mathf.Lerp(HorrorAudioSource.volume, 0f, musicTransitionSpeed * Time.deltaTime);
            if (NormalAudioSource.clip != normalBackgroundMusic)
            {
                NormalAudioSource.clip = normalBackgroundMusic;
                NormalAudioSource.Play();
            }
        }
    }

}
