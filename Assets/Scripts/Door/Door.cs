using System.Collections;
using UnityEngine;

public class Door : MonoBehaviour
{
    private Animator _doorAnimator;

    private AudioSource _doorAudioSource;

    [SerializeField] private AudioClip _doorOpenAudioClip;

    [SerializeField] private AudioClip _doorCloseAudioClip;

    [SerializeField] private float _doorVolume = 1f;

    [SerializeField] private float _doorOpenAudioDelay = 0f;

    [SerializeField] private float _doorCloseAudioDelay = 0f;

    [SerializeField] private bool _doorOpen;

    private enum State
    {
        Open,
        Closed,
    }

    private State _currentState;

    void Start()
    {
        _doorAnimator = GetComponent<Animator>();

        _doorAudioSource = GetComponent<AudioSource>();

        _currentState = State.Closed;
    }

    void Update()
    {
        if (_doorOpen && _currentState != State.Open)
        {
            OpenDoor();
        }
        else if (!_doorOpen && _currentState != State.Closed)
        {
            CloseDoor();
        }
    }

    private void OpenDoor()
    {
        _doorAnimator.SetBool("Door Open", true);

        StartCoroutine(PlayAudioAfterDelay(_doorOpenAudioClip, _doorOpenAudioDelay));

        _currentState = State.Open;
    }

    private void CloseDoor()
    {
        _doorAnimator.SetBool("Door Open", false);

        StartCoroutine(PlayAudioAfterDelay(_doorCloseAudioClip, _doorCloseAudioDelay));

        _currentState = State.Closed;
    }

    private IEnumerator PlayAudioAfterDelay(AudioClip clip, float delay)
    {
        yield return new WaitForSeconds(delay);

        _doorAudioSource.PlayOneShot(clip, _doorVolume);
    }
}