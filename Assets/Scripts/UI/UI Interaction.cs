using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Events;

public class UIInteraction : MonoBehaviour
{
    [SerializeField] private UnityEvent InteractionFunction;
    
    //[SerializeField] private UIFader _UIFaderScript;

    [SerializeField] private bool _holdingKey;

    [SerializeField] private float _holdingTime;

    [SerializeField] private KeyCode _key;

    [SerializeField] private UIFader _uiFaderScript;

    public static event System.Action InteractionUIFadeOut;

    private bool _playerUITriggerEnter;

    private float _holdingTimer = 0f;
    
    //private float _fadeOutTimer = 0f;

    private bool _keyHeld;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //_UIFaderScript = GetComponent<UIFader>();

        _playerUITriggerEnter = false;

        _keyHeld = false;
    }

    // Update is called once per frame
    void Update()
    {
        UITrigger.PlayerUITriggerEnter += () => _playerUITriggerEnter = true;
        UITrigger.PlayerUITriggerExit += () => _playerUITriggerEnter = false;

        if (Input.GetKeyDown(_key) && _playerUITriggerEnter && !_holdingKey)
        {
            PressingKey();

            Debug.Log("Player is pressing: " + _key);
        }

        if (Input.GetKey(_key) && _playerUITriggerEnter && _holdingKey)
        {
            _holdingTimer += Time.deltaTime;

            //Debug.Log(_holdingTimer);

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
    }

    private void PressingKey()
    {
        InteractionFunction?.Invoke();

        _uiFaderScript.GetComponent<UIFader>()?.GetFadeOut();

        Debug.Log("Successfully executed pressing action with input: " + _key);
    }

    private void HoldingKey()
    {
        InteractionFunction?.Invoke();

        _uiFaderScript.GetComponent<UIFader>()?.GetFadeOut();

        Debug.Log("Successfully executed holding action with input: " + _key);
    }
}
