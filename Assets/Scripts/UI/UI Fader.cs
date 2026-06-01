using UnityEngine;
using TMPro;

public class UIFader : MonoBehaviour
{
    [SerializeField] private float _fadeSpeed = 2f;

    private TextMeshPro _textMeshPro;

    private float _targetAlpha = 0f;

    private void Start()
    {
        _textMeshPro = GetComponentInChildren<TextMeshPro>();

        if (_textMeshPro == null)
        {
            Debug.LogError("No TextMeshPro found!");
            return;
        }

        _textMeshPro.alpha = 0f;
    }

    private void Update()
    {
        // Smoothly move alpha toward target
        float currentAlpha = _textMeshPro.alpha;

        float newAlpha = Mathf.MoveTowards(currentAlpha, _targetAlpha, _fadeSpeed * Time.deltaTime);

        _textMeshPro.alpha = newAlpha;

        if (Input.GetKey(KeyCode.F))
        {
            GetFadeIn();
        }

        if (Input.GetKey(KeyCode.G))
        { 
            GetFadeOut();
        }
    }

    public void GetFadeIn()
    {
        _targetAlpha = 1f;
        Debug.Log("Fading In");
    }

    public void GetFadeOut()
    {
        _targetAlpha = 0f;
        Debug.Log("Fading Out");
    }
}

