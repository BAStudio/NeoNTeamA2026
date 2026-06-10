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

    private bool inDanger;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        PlayRandomNormal();
        PlayRandomDanger();

        normalSource.volume = maxVolume;
        dangerSource.volume = 0f;
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

        if (!normalSource.isPlaying)
            PlayRandomNormal();

        if (!dangerSource.isPlaying)
            PlayRandomDanger();
    }

    public void SetDangerState(bool state)
    {
        inDanger = state;
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