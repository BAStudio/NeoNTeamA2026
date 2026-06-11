using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource normalSource;
    [SerializeField] private AudioSource dangerSource;

    [Header("Music")]
    [SerializeField] private AudioClip[] normalTracks;
    [SerializeField] private AudioClip[] dangerTracks;

    [Header("Settings")]
    [SerializeField] private float transitionSpeed = 2f;
    [SerializeField] private float maxVolume = 1f;

    [SerializeField] private bool inDanger;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        normalSource.volume = maxVolume;
        dangerSource.volume = 0f;

        PlayRandomNormal();
    }

    private void Update()
    {
        float targetNormal = inDanger ? 0f : maxVolume;
        float targetDanger = inDanger ? maxVolume : 0f;

        normalSource.volume = Mathf.Lerp(
            normalSource.volume,
            targetNormal,
            transitionSpeed * Time.deltaTime);

        dangerSource.volume = Mathf.Lerp(
            dangerSource.volume,
            targetDanger,
            transitionSpeed * Time.deltaTime);

        // Keep music looping with random tracks
        if (!inDanger && !normalSource.isPlaying)
        {
            PlayRandomNormal();
        }

        if (inDanger && !dangerSource.isPlaying)
        {
            PlayRandomDanger();
        }
    }

    public void SetDangerState(bool state)
    {
        if (inDanger == state)
            return;

        inDanger = state;

        if (inDanger)
        {
            normalSource.Stop();
            PlayRandomDanger();
        }
        else
        {
            dangerSource.Stop();
            PlayRandomNormal();
        }
    }

    private void PlayRandomNormal()
    {
        if (normalTracks.Length == 0)
            return;

        normalSource.clip =
            normalTracks[Random.Range(0, normalTracks.Length)];

        normalSource.Play();
    }

    private void PlayRandomDanger()
    {
        if (dangerTracks.Length == 0)
            return;

        dangerSource.clip =
            dangerTracks[Random.Range(0, dangerTracks.Length)];

        dangerSource.Play();
    }
}