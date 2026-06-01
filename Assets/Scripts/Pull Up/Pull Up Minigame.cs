using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class PullUpMinigame : MonoBehaviour
{
    [SerializeField] private UnityEngine.Rendering.Volume _postProcessVolume;

    [SerializeField] private Player_Camera _playerCamera;

    [SerializeField] private Image _fadeImage;

    [SerializeField] private TMP_Text _fadeText;

    [SerializeField] private Transform _textTransform;

    [SerializeField] private AudioSource _audioSource;

    [SerializeField] private AudioClip _startClip;

    [SerializeField] private float _downwardForce = 15f;

    [SerializeField] private float _dangerZone = 60f;

    [SerializeField] private float _failTime = 3f;

    [SerializeField] private float _fadeSpeed = 2f;

    [SerializeField] private float _forceIncreaseRate = 0.5f;

    [SerializeField] private float _startTextFadeSpeed = 8f;

    [SerializeField] private float _shakeStartPoint = 65f;

    [SerializeField] private float _maxShakeAmount = 15f;

    [SerializeField] private float _audioFadeSpeed = 1f;

    [Header("DOF SETTINGS")] // NEW
    [SerializeField] private float _maxBlurFocusDistance = 10f;

    [SerializeField] private float _minBlurFocusDistance = 0.5f;

    [SerializeField] private float _postFailLingerTime = 2f;

    public static event System.Action DropDown;

    private bool _minigameActive;

    private float _failTimer;

    private bool _failed;

    private float _currentForceMultiplier = 1f;

    private bool _showStartText;

    private float _postFailTimer;

    private UnityEngine.Rendering.Universal.DepthOfField _dof;

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

        if (_postProcessVolume != null && _postProcessVolume.profile.TryGet(out _dof))
        {
        }

        if (_dof != null)
        {
            _dof.focusDistance.value = _maxBlurFocusDistance;
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
            _currentForceMultiplier += _forceIncreaseRate * Time.deltaTime;

            _playerCamera.AddXRotation(_downwardForce * _currentForceMultiplier * Time.deltaTime);

            float currentRotation = _playerCamera.GetXRotation();

            // ---------------- TEXT ----------------
            if (_fadeText != null)
            {
                if (currentRotation > _shakeStartPoint)
                {
                    Color c = _fadeText.color;
                    c.a = Mathf.Lerp(c.a, 1f, _startTextFadeSpeed * Time.deltaTime);
                    _fadeText.color = c;
                }
                else
                {
                    Color c = _fadeText.color;
                    c.a = Mathf.Lerp(c.a, 0f, _fadeSpeed * Time.deltaTime);
                    _fadeText.color = c;
                }
            }

            // ---------------- SHAKE ----------------
            if (currentRotation > _shakeStartPoint)
            {
                float t = Mathf.InverseLerp(_shakeStartPoint, _dangerZone, currentRotation);

                if (_textTransform != null)
                {
                    Vector3 basePos = _textTransform.localPosition;

                    float shakeAmount = Mathf.Lerp(0f, _maxShakeAmount, t);

                    Vector3 shakeOffset = new Vector3(
                        Random.Range(-shakeAmount, shakeAmount),
                        Random.Range(-shakeAmount, shakeAmount),
                        0f
                    );

                    _textTransform.localPosition = basePos + shakeOffset;
                }
            }
            else
            {
                if (_textTransform != null)
                {
                    _textTransform.localPosition =
                        Vector3.Lerp(_textTransform.localPosition, Vector3.zero, Time.deltaTime * 10f);
                }
            }

            // ---------------- DOF (FOCUS DISTANCE BLUR) ----------------
            if (_dof != null)
            {
                float t = Mathf.InverseLerp(_shakeStartPoint, _dangerZone, currentRotation);

                float targetFocus = Mathf.Lerp(
                    _maxBlurFocusDistance,
                    _minBlurFocusDistance,
                    t
                );

                // linger after fail
                if (_postFailTimer > 0f)
                {
                    float linger = _postFailTimer / _postFailLingerTime;
                    targetFocus = Mathf.Min(targetFocus, Mathf.Lerp(_maxBlurFocusDistance, _minBlurFocusDistance, linger));
                }

                _dof.focusDistance.value =
                    Mathf.Lerp(_dof.focusDistance.value, targetFocus, Time.deltaTime * 3f);

                _dof.aperture.value =
                    Mathf.Lerp(_dof.aperture.value, 16f, Time.deltaTime * 3f);
            }

            // ---------------- DANGER ----------------
            if (currentRotation >= _dangerZone)
            {
                _failTimer += Time.deltaTime;

                if (_fadeImage != null)
                {
                    Color c = _fadeImage.color;
                    c.a = Mathf.Lerp(c.a, 1f, _fadeSpeed * Time.deltaTime);
                    _fadeImage.color = c;
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
            }

            // ---------------- AUDIO ----------------
            if (_audioSource != null)
            {
                _audioSource.volume =
                    Mathf.Lerp(_audioSource.volume, 0f, _audioFadeSpeed * Time.deltaTime);
            }

            // ---------------- POST FAIL TIMER ----------------
            if (_postFailTimer > 0f)
            {
                _postFailTimer -= Time.deltaTime;
            }
        }
        else
        {
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

            if (_dof != null)
            {
                _dof.focusDistance.value =
                    Mathf.Lerp(_dof.focusDistance.value, _maxBlurFocusDistance, Time.deltaTime * 2f);
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

        _postFailTimer = 0f;

        if (_fadeText != null)
        {
            Color c = _fadeText.color;
            c.a = 0f;
            _fadeText.color = c;
        }

        if (_audioSource != null && _startClip != null)
        {
            _audioSource.clip = _startClip;
            _audioSource.volume = 1f;
            _audioSource.Play();
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

        _postFailTimer = _postFailLingerTime;

        Debug.Log("FAILED MINIGAME");

        DropDown?.Invoke();
    }
}