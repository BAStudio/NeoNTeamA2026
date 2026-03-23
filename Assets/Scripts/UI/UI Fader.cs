using NUnit;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using TMPro;

public class UIFader : MonoBehaviour
{
    [SerializeField] private float _fadeDuration = 1f;

    /*[SerializeField] private CanvasGroup _canvasGroup;*/
 
    [SerializeField] private float _fadeTimer = 0f;

    private TMPro.TextMeshPro _textMeshPro;

    private float _fadeTime = 0f;

    private bool _fadeIn;

    private float _fadeEndValue;

    private Color32 startColor;

    private void Start()
    {
        if (_textMeshPro == null)
        {
            _textMeshPro = GetComponentInChildren<TextMeshPro>();

            Debug.Log("TextMeshPro component found: " + _textMeshPro.name);
        }

        _textMeshPro.alpha = 0f;
    }

    private void Update()
    { 
        if(Input.GetKey(KeyCode.F))
        {
            GetFadeIn();
        }

        if (Input.GetKey(KeyCode.G))
        {
            GetFadeOut();
        }

        if(_fadeIn && _textMeshPro.alpha >= 0f)
        {
            _textMeshPro.alpha = 1f;
            //_canvasGroup.alpha = Mathf.Lerp(_canvasGroup.alpha, _fadeEndValue, _fadeDuration / _fadeTime);

            _fadeTime += Time.deltaTime;
        }

        if(!_fadeIn && _textMeshPro.alpha <= 1f)
        {
            _textMeshPro.alpha = 0f;
            //_textMeshPro.alpha *= _fadeDuration * Time.deltaTime;
            //_canvasGroup.alpha = Mathf.Lerp(_canvasGroup.alpha, _fadeEndValue, _fadeDuration / _fadeTime);

            _fadeTime += Time.deltaTime;
        }

        //UITrigger.PlayerUITriggerEnter += GetFadeIn;
        //UITrigger.PlayerUITriggerExit += GetFadeOut;

        UIInteraction.InteractionUIFadeOut += GetFadeOut;
    }

    public void GetFadeIn()
    {
        //Fade(_canvasGroup.alpha, 1f, _fadeDuration);

        _fadeIn = true;

        //_fadeEndValue = 1f;

        Debug.Log("Fading In");
    }

    public void GetFadeOut()
    {
        //Fade(_canvasGroup.alpha, 0f, _fadeDuration);

        _fadeIn = false;

        //_fadeEndValue = 0f;

        Debug.Log("Fading Out");
    }
}

