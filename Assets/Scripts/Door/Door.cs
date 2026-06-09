using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(AudioSource))]
public class Door : MonoBehaviour
{
    public static event Action<Door> DoorStateChanged;

    [Header("Door")]
    [SerializeField] private bool startsOpen;
    [SerializeField] private bool isLocked;

    [Header("Animation")]
    [SerializeField] private string animatorBool = "Door Open";

    [Header("Collider")]
    [SerializeField] private Collider blockingCollider;

    [Header("Audio")]
    [SerializeField] private AudioClip openClip;
    [SerializeField] private AudioClip closeClip;

    [SerializeField] private float volume = 1f;
    [SerializeField] private float openDelay;
    [SerializeField] private float closeDelay;

    private Animator anim;
    private AudioSource source;

    private bool isOpen;

    public bool IsLocked => isLocked;
    public bool IsOpen => isOpen;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        source = GetComponent<AudioSource>();

        if (blockingCollider == null)
            blockingCollider = GetComponentInChildren<Collider>();
    }

    private void Start()
    {
        isOpen = startsOpen && !isLocked;
        ApplyInstantState();

        DoorStateChanged?.Invoke(this);
    }

    public void SetLocked(bool locked)
    {
        isLocked = locked;

        if (isLocked && isOpen)
            CloseDoor();

        DoorStateChanged?.Invoke(this);
    }

    public void OpenDoor()
    {
        if (isLocked || isOpen)
            return;

        isOpen = true;

        anim.SetBool(animatorBool, true);

        if (blockingCollider != null)
            blockingCollider.enabled = false;

        if (openClip != null)
            StartCoroutine(PlayAudio(openClip, openDelay));

        DoorStateChanged?.Invoke(this);
    }

    public void CloseDoor()
    {
        if (!isOpen)
            return;

        isOpen = false;

        anim.SetBool(animatorBool, false);

        if (blockingCollider != null)
            blockingCollider.enabled = true;

        if (closeClip != null)
            StartCoroutine(PlayAudio(closeClip, closeDelay));

        DoorStateChanged?.Invoke(this);
    }

    public void ToggleDoor()
    {
        if (isLocked)
            return;

        if (isOpen)
            CloseDoor();
        else
            OpenDoor();
    }

    private void ApplyInstantState()
    {
        anim.SetBool(animatorBool, isOpen);

        if (blockingCollider != null)
            blockingCollider.enabled = !isOpen;
    }

    private IEnumerator PlayAudio(AudioClip clip, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        source.PlayOneShot(clip, volume);
    }
}