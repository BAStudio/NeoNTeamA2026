using UnityEngine;
using System.Collections;

public class UIFader : MonoBehaviour
{
    [SerializeField] private float _fadeDuration = 1f;

    private CanvasGroup canvasGroup;
    
    private void Start()
    {
        canvasGroup.alpha = 0f;
    }

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void FadeIn()
    {
        StartCoroutine(Fade(0f, 1f, _fadeDuration));
    }

    private void FadeOut()
    {
        StartCoroutine(Fade(1f, 0f, _fadeDuration));
    }
    public void GetFadeIn()
    {
        FadeIn();
    }

    public void GetFadeOut()
    {
        FadeOut();
    }

    IEnumerator Fade(float start, float end, float duration)
    {
        float time = 0;

        while (time < duration)
        {
            canvasGroup.alpha = Mathf.Lerp(start, end, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        canvasGroup.alpha = end;
    }
}

