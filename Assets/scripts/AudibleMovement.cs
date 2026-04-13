using UnityEngine;

public class AudibleMovement : MonoBehaviour
{
    [SerializeField] AudioSource _audioSource;

    [SerializeField] AudioClip walkClip;
    [SerializeField] AudioClip runClip;

    [SerializeField] private float fadeSpeed = 6f;

    private Coroutine _fadeRoutine;

    void Start()
    {

    }

    void Update()
    {

    }

    public void PlayWalk()
    {
        Debug.Log("Playing walk sound");

        if (walkClip == null || _audioSource == null)
        {
            Debug.LogWarning("Walk clip or AudioSource is missing!");
            return;
        }

        SwitchClip(walkClip);
    }

    public void PlayRun()
    {
        Debug.Log("Playing run sound");

        if (runClip == null || _audioSource == null)
        {
            Debug.LogWarning("Run clip or AudioSource is missing!");
            return;
        }

        SwitchClip(runClip);
    }

    public void StopAudio()
    {
        Debug.Log("Stopping movement audio");

        if (_fadeRoutine != null)
            StopCoroutine(_fadeRoutine);

        _audioSource.Stop();
        _audioSource.volume = 0f;
    }

    private void SwitchClip(AudioClip newClip)
    {
        if (_fadeRoutine != null)
            StopCoroutine(_fadeRoutine);

        _fadeRoutine = StartCoroutine(FadeToClip(newClip));
    }

    private System.Collections.IEnumerator FadeToClip(AudioClip newClip)
    {
        while (_audioSource.volume > 0.05f)
        {
            _audioSource.volume -= Time.deltaTime * fadeSpeed;
            yield return null;
        }

        _audioSource.volume = 0f;

        _audioSource.clip = newClip;

        if (!_audioSource.isPlaying)
            _audioSource.Play();

        while (_audioSource.volume < 1f)
        {
            _audioSource.volume += Time.deltaTime * fadeSpeed;
            yield return null;
        }

        _audioSource.volume = 1f;
    }
}
