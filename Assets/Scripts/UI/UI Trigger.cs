using UnityEngine;
using UnityEngine.Events;

public class UITrigger : MonoBehaviour
{
    [SerializeField] private bool _colliderIsTrigger;

    [SerializeField] private string _colliderTagName;

    [SerializeField] private float _raycastDistance;

    [SerializeField] private float _fadeOutDelay = 2f;

    private UIFader _currentUIFader;

    private float _lookAwayTimer = 0f;

    public static event System.Action PlayerUITriggerEnter;
    public static event System.Action PlayerUITriggerExit;

    void Start()
    {

    }

    void FixedUpdate()
    {
        UIFader newFader = null;

        RaycastHit hit;

        if (Physics.Raycast(transform.position, Camera.main.transform.forward, out hit, _raycastDistance) && !_colliderIsTrigger)
        {
            if (hit.collider.CompareTag(_colliderTagName))
            {
                PlayerUITriggerEnter?.Invoke();

                newFader = hit.collider.GetComponentInChildren<UIFader>();

                _lookAwayTimer = 0f;

                Debug.Log("Raycast hit: " + hit.collider.name + ", which has the tag: " + _colliderTagName);
            }
            else
            {
                PlayerUITriggerExit?.Invoke();

                _lookAwayTimer += Time.deltaTime;

                Debug.Log("Raycast hit: " + hit.collider.name + ", which does not have the tag: " + _colliderTagName);
            }
        }
        else
        {
            _lookAwayTimer += Time.deltaTime; 
        }

        if (newFader != _currentUIFader)
        {
            if (_currentUIFader != null)
            {
                _currentUIFader.GetFadeOut();
            }

            if (newFader != null)
            {
                newFader.GetFadeIn();
            }

            _currentUIFader = newFader;
        }

        if (_currentUIFader != null && newFader == null && _lookAwayTimer >= _fadeOutDelay)
        {
            _currentUIFader.GetFadeOut();
            _currentUIFader = null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(_colliderTagName) && _colliderIsTrigger)
        {
            Debug.Log("The player has entered the trigger area of: " + _colliderTagName);

            PlayerUITriggerEnter?.Invoke();

            other.GetComponentInChildren<UIFader>()?.GetFadeIn();
        }
        else
        {
            if (other.CompareTag(_colliderTagName) && !_colliderIsTrigger)
            {
                Debug.Log("The player has entered the trigger area of: " + _colliderTagName + ", but since the 'Collider Is Trigger' box is unchecked, nothing happens");

                return;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(_colliderTagName) && _colliderIsTrigger)
        {
            Debug.Log("The player has exited the trigger area of: " + _colliderTagName);

            PlayerUITriggerExit?.Invoke();

            other.GetComponentInChildren<UIFader>()?.GetFadeOut();
        }
        else
        {
            if (other.CompareTag(_colliderTagName) && !_colliderIsTrigger)
            {
                Debug.Log("The player has exited the trigger area of: " + _colliderTagName + ", but since the 'Collider Is Trigger' box is unchecked, nothing happens");

                return;
            }
        }
    }
}
