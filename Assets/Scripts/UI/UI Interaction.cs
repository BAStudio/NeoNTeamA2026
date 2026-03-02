using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Events;

public class UIInteraction : MonoBehaviour
{
    [SerializeField] private UnityEvent _InteractionFunction;

    [SerializeField] private UIFader _UIFaderScript;

    [SerializeField] private bool _holdingKey;

    [SerializeField] private float _holdingTime;

    [SerializeField] private KeyCode _key;
    
    private bool _playerUITriggerEnter;

    private float _holdingTimer = 0f;
    
    private float _fadeOutTimer = 0f;

    private bool _keyHeld;

    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
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


        if(_fadeOutTimer > 0)
        {
            _fadeOutTimer -= Time.deltaTime;
        }
        else
        {
            _UIFaderScript.GetFadeOut();

            _fadeOutTimer = 0f;
        }


    }

    private void PressingKey()
    {
        _UIFaderScript.GetFadeOut();

        _InteractionFunction.Invoke();

        Debug.Log("Successfully excecuted pressing action with input:" + _key);
    }

    private void HoldingKey()
    {
        _UIFaderScript.GetFadeOut();

        _InteractionFunction.Invoke();

        Debug.Log("Successfully excecuted holding action with input:" + _key);
    }

    public void SetFadeOutTimer(float fadeOutTime)
    {
        _fadeOutTimer = fadeOutTime;
    }
}
