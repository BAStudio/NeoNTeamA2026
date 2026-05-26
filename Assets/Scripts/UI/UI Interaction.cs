using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UIInteraction : MonoBehaviour
{
    [SerializeField] private UnityEvent InteractionFunction;

    [SerializeField] private KeyCode _key;
    
    [SerializeField] private Slider _interactionSlider;

    [SerializeField] private Image _sliderFill;

    [SerializeField] private bool _holdingKey;

    [SerializeField] private float _holdingTime;

    [SerializeField] private float _interactionCooldown = 1f;

    [SerializeField] private float _fadeSpeed = 2f;

    [SerializeField] private bool _callCooldownFromOtherFunction;

    public static event System.Action InteractionUIFadeOut;

    private bool _playerUITriggerEnter;

    private float _holdingTimer = 0f;

    private float _interactionCooldownTimer = 0f;

    private bool _keyHeld;

    private bool _keyHasBeenPressed;

    private bool _keyHasBeenHeld;

    private bool _allowedToInteract;

    private float _sliderTarget;

    private float _targetAlpha;

    private bool _isFadingOut;

    void Start()
    {
        _playerUITriggerEnter = false;

        _keyHeld = false;

        _allowedToInteract = true;

        _keyHasBeenPressed = false;

        _keyHasBeenHeld = false;

        _interactionSlider.value = 0f;

        _sliderTarget = 0f;

        if (_sliderFill != null)
        {
            Color c = _sliderFill.color;
            c.a = 0f;
            _sliderFill.color = c;
        }

        _targetAlpha = 0f;
        _isFadingOut = false;
    }

    private void OnEnable()
    {
        UITrigger.PlayerUITriggerEnter += OnEnter;
        UITrigger.PlayerUITriggerExit += OnExit;
    }

    private void OnDisable()
    {
        UITrigger.PlayerUITriggerEnter -= OnEnter;
        UITrigger.PlayerUITriggerExit -= OnExit;
    }

    private void OnEnter(GameObject obj)
    {
        _playerUITriggerEnter = true;
    }

    private void OnExit(GameObject obj)
    {
        _playerUITriggerEnter = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(_key) && _playerUITriggerEnter && !_holdingKey && _allowedToInteract)
        {
            PressingKey();

            Debug.Log("Player is pressing: " + _key);
        }

        if (Input.GetKey(_key) && _playerUITriggerEnter && _holdingKey && _allowedToInteract)
        {
            _holdingTimer += Time.deltaTime;

            _sliderTarget = _holdingTimer / _holdingTime;

            _targetAlpha = 1f;

            if (_holdingTimer >= _holdingTime && !_keyHeld)
            {
                HoldingKey();

                _keyHeld = true;

                _isFadingOut = true;

                Debug.Log("Player is holding: " + _key);
            }
        }
        else
        {
            if (!_isFadingOut)
            {
                _holdingTimer = 0f;
                _sliderTarget = 0f;
                _targetAlpha = 0f;
            }

            _keyHeld = false;
        }

        if (_interactionCooldownTimer > 0f)
        {
            _interactionCooldownTimer -= Time.deltaTime;

            if (_interactionCooldownTimer <= 0f)
            {
                _allowedToInteract = true;

                Debug.Log("The interaction cooldown has ended. The player can interact again.");
            }
        }

        _interactionSlider.value = Mathf.Lerp(_interactionSlider.value, _sliderTarget, Time.deltaTime * 10f);

        if (_sliderFill != null)
        {
            float currentAlpha = _sliderFill.color.a;
            float newAlpha = Mathf.MoveTowards(currentAlpha, _targetAlpha, _fadeSpeed * Time.deltaTime);

            Color c = _sliderFill.color;
            c.a = newAlpha;
            _sliderFill.color = c;

            if (_isFadingOut && newAlpha <= 0f)
            {
                _isFadingOut = false;

                _sliderTarget = 0f;
                _interactionSlider.value = 0f;
                _holdingTimer = 0f;
            }
        }
    }

    private void PressingKey()
    {
        InteractionFunction?.Invoke();

        _keyHasBeenPressed = true;

        _allowedToInteract = false;

        if (!_callCooldownFromOtherFunction)
        {
            _interactionCooldownTimer = _interactionCooldown;
        }

        Debug.Log("Successfully executed pressing action with input: " + _key);
    }

    private void HoldingKey()
    {
        InteractionFunction?.Invoke();

        _keyHasBeenHeld = true;

        _allowedToInteract = false;

        if (!_callCooldownFromOtherFunction)
        {
            _interactionCooldownTimer = _interactionCooldown;
        }

        _targetAlpha = 0f;

        Debug.Log("Successfully executed holding action with input: " + _key);
    }
}