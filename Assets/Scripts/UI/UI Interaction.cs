using UnityEngine;
using UnityEngine.Events;

public class UIInteraction : MonoBehaviour
{
    [SerializeField] private UnityEvent InteractionFunction;

    [SerializeField] private bool _holdingKey;

    [SerializeField] private float _holdingTime;

    [SerializeField] private float _interactionCooldown = 1f;

    [SerializeField] private KeyCode _key;

    public static event System.Action InteractionUIFadeOut;

    private bool _playerUITriggerEnter;

    private float _holdingTimer = 0f;

    private float _interactionCooldownTimer = 0f;

    private bool _keyHeld;

    private bool _keyHasBeenPressed;

    private bool _keyHasBeenHeld;

    private bool _allowedToInteract;

    void Start()
    {
        _playerUITriggerEnter = false;

        _keyHeld = false;

        _allowedToInteract = true;

        _keyHasBeenPressed = false;

        _keyHasBeenHeld = false;
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

            if (_holdingTimer >= _holdingTime && !_keyHeld)
            {
                HoldingKey();

                _keyHeld = true;

                Debug.Log("Player is holding: " + _key);
            }
        }
        else
        {
            _holdingTimer = 0f;

            _keyHeld = false;
        }

        // ✅ FIXED cooldown logic
        if (_interactionCooldownTimer > 0f)
        {
            _interactionCooldownTimer -= Time.deltaTime;

            if (_interactionCooldownTimer <= 0f)
            {
                _allowedToInteract = true;

                Debug.Log("The interaction cooldown has ended. The player can interact again.");
            }
        }
    }

    private void PressingKey()
    {
        InteractionFunction?.Invoke();

        _keyHasBeenPressed = true;

        // ✅ start cooldown ONCE
        _allowedToInteract = false;

        _interactionCooldownTimer = _interactionCooldown;

        Debug.Log("Successfully executed pressing action with input: " + _key);
    }

    private void HoldingKey()
    {
        InteractionFunction?.Invoke();

        _keyHasBeenHeld = true;

        // ✅ start cooldown ONCE
        _allowedToInteract = false;

        _interactionCooldownTimer = _interactionCooldown;

        Debug.Log("Successfully executed holding action with input: " + _key);
    }
}
