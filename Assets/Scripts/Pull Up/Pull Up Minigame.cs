using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class PullUpMinigame : MonoBehaviour
{
    [SerializeField] private PlayerCamera _playerCamera;

    //[SerializeField] private float _mouseY;

    //[SerializeField] private Image _gameCursor;

    [SerializeField] private Image _fadeImage;

    [SerializeField] private TMP_Text _fadeText;

    [SerializeField] private float _downwardForce = 15f;

    [SerializeField] private float _dangerZone = 60f;

    [SerializeField] private float _failTime = 3f;

    [SerializeField] private float _fadeSpeed = 2f;

    [SerializeField] private float _forceIncreaseRate = 0.5f;

    [SerializeField] private float _startTextFadeSpeed = 8f;

    public static event System.Action DropDown;

    private bool _minigameActive;

    private Vector3 _mousePosition;

    private float _failTimer;

    private bool _failed;

    private float _currentForceMultiplier = 1f;

    private bool _showStartText;

    void Start()
    {
        _minigameActive = false;

        if (_fadeImage != null)
        {
            Color c = _fadeImage.color;

            c.a = 0f;

            _fadeImage.color = c;
        }

        if (_fadeText != null)
        {
            Color c = _fadeText.color;

            c.a = 0f;

            _fadeText.color = c;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Y))
        {
            HoldUpMinigame();

            Debug.Log("Minigame Active");
        }

        if (_minigameActive)
        {
            // gradually increase difficulty
            _currentForceMultiplier += _forceIncreaseRate * Time.deltaTime;

            _playerCamera.AddXRotation(_downwardForce * _currentForceMultiplier * Time.deltaTime);

            float currentRotation = _playerCamera.GetXRotation();

            // START TEXT FADE IN + OUT
            if (_showStartText && _fadeText != null)
            {
                Color c = _fadeText.color;

                // Fade in quickly
                if (c.a < 0.95f)
                {
                    c.a = Mathf.Lerp(c.a, 1f, _startTextFadeSpeed * Time.deltaTime);
                }
                // Then slowly fade out
                else
                {
                    c.a = Mathf.Lerp(c.a, 0f, (_fadeSpeed * 0.35f) * Time.deltaTime);

                    if (c.a <= 0.05f)
                    {
                        _showStartText = false;
                    }
                }

                _fadeText.color = c;
            }

            if (currentRotation >= _dangerZone)
            {
                _failTimer += Time.deltaTime;

                if (_fadeImage != null)
                {
                    Color c = _fadeImage.color;

                    c.a = Mathf.Lerp(c.a, 8f, _fadeSpeed * Time.deltaTime);

                    _fadeImage.color = c;
                }

                // Fade text in while in danger zone
                if (_fadeText != null)
                {
                    Color c = _fadeText.color;

                    c.a = Mathf.Lerp(c.a, 1f, _fadeSpeed * Time.deltaTime);

                    _fadeText.color = c;
                }

                if (_failTimer >= _failTime)
                {
                    FailMinigame();
                }
            }
            else
            {
                _failTimer = 0f;

                if (_fadeImage != null)
                {
                    Color c = _fadeImage.color;

                    c.a = Mathf.Lerp(c.a, 0f, _fadeSpeed * Time.deltaTime);

                    _fadeImage.color = c;
                }

                // Slowly fade out text normally
                if (_fadeText != null && !_showStartText && currentRotation < _dangerZone)
                {
                    Color c = _fadeText.color;

                    c.a = Mathf.Lerp(c.a, 0f, (_fadeSpeed * 0.35f) * Time.deltaTime);

                    _fadeText.color = c;
                }
            }
        }
        else
        {
            // fade back to normal when minigame is NOT active
            if (_fadeImage != null)
            {
                Color c = _fadeImage.color;

                c.a = Mathf.Lerp(c.a, 0f, _fadeSpeed * Time.deltaTime);

                _fadeImage.color = c;
            }

            if (_fadeText != null)
            {
                Color c = _fadeText.color;

                c.a = Mathf.Lerp(c.a, 0f, _fadeSpeed * Time.deltaTime);

                _fadeText.color = c;
            }
        }
    }

    public void HoldUpMinigame()
    {
        _minigameActive = true;

        _failed = false;

        _failTimer = 0f;

        _currentForceMultiplier = 1f;

        _showStartText = true;

        // Reset text alpha so it can fade in again
        if (_fadeText != null)
        {
            Color c = _fadeText.color;

            c.a = 0f;

            _fadeText.color = c;
        }
    }

    private void StopMinigame()
    {
        _minigameActive = false;

        _failTimer = 0f;
    }

    private void FailMinigame()
    {
        if (_failed)
            return;

        _failed = true;

        StopMinigame();

        Debug.Log("FAILED MINIGAME");

        DropDown?.Invoke();
    }
}