using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class PullUpMinigame : MonoBehaviour
{
    [SerializeField] private PlayerCamera _playerCamera;

    //[SerializeField] private float _mouseY;

    //[SerializeField] private Image _gameCursor;

    [SerializeField] private Image _fadeImage;

    [SerializeField] private float _downwardForce = 15f;

    [SerializeField] private float _dangerZone = 60f;

    [SerializeField] private float _failTime = 3f;

    [SerializeField] private float _fadeSpeed = 2f;

    public static event System.Action DropDown;

    private bool _minigameActive;

    private Vector3 _mousePosition;

    private float _failTimer;

    private bool _failed;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _minigameActive = false;

        //Debug.Log("Mouse Position at Start: " + _gameCursor.transform.position.y);

        if (_fadeImage != null)
        {
            Color c = _fadeImage.color;

            c.a = 0f;

            _fadeImage.color = c;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Y))
        {
            HoldUpMinigame();

            Debug.Log("Minigame Active");
        }

        if (_minigameActive)
        {    
            _playerCamera.AddXRotation(_downwardForce * Time.deltaTime);

            float currentRotation = _playerCamera.GetXRotation();

            if (currentRotation >= _dangerZone)
            {
                _failTimer += Time.deltaTime;

                if (_fadeImage != null)
                {
                    Color c = _fadeImage.color;

                    c.a = Mathf.Lerp(c.a, 0.7f, _fadeSpeed * Time.deltaTime);

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
        }
    }

    private void HoldUpMinigame()
    {
        _minigameActive = true;

        _failed = false;

        _failTimer = 0f;
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