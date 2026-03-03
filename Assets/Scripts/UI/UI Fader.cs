using UnityEngine;
using System.Collections;

public class UIFader : MonoBehaviour
{
    [SerializeField] private float _fadeDuration = 1f;

    private bool _shouldFadeIn = false;

    private bool _shouldFadeOut = false;

    float time = 0;

    private CanvasGroup canvasGroup;
    
    private void Start()
    {
        canvasGroup.alpha = 0f;
    }

    private void Update()
    {
        if(_shouldFadeIn)
        {
            FadeIn();
        }
        else if (_shouldFadeOut)
        {
            FadeOut();
        }
    }

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void FadeIn()
    {
        Fade(1f, 0f, _fadeDuration);
    }

    private void FadeOut()
    {
        Fade(0f, 1f, _fadeDuration);
    }
    public void GetFadeIn()
    {
        _shouldFadeIn = true;
    }

    public void GetFadeOut()
    {
        _shouldFadeOut = true;
    }

    private void Fade(float start, float end, float duration)
    {
        if(time < duration)
        {
            canvasGroup.alpha = Mathf.Lerp(start, end, time / duration);
            time += Time.deltaTime;
        }
        if (time >= duration)
        {
            time = 0;
            _shouldFadeIn = false;
            _shouldFadeOut = false;
            canvasGroup.alpha = end;
        }
    }
}

