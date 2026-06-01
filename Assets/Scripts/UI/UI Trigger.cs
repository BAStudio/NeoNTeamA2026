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

    private GameObject _currentLookObject;

    public static event System.Action<GameObject> PlayerUITriggerEnter;
    public static event System.Action<GameObject> PlayerUITriggerExit;

    void Start()
    {

    }

    void FixedUpdate()
    {
        UIFader newFader = null;

        RaycastHit hit;

        GameObject hitObject = null;

        if (Physics.Raycast(transform.position, Camera.main.transform.forward, out hit, _raycastDistance) && !_colliderIsTrigger)
        {
            if (hit.collider.CompareTag(_colliderTagName))
            {
                hitObject = hit.collider.gameObject;

                newFader = hitObject.GetComponentInChildren<UIFader>();

                _lookAwayTimer = 0f;

                Debug.Log("Raycast hit: " + hit.collider.name + ", which has the tag: " + _colliderTagName);
            }
            else
            {
                if (_currentLookObject != null)
                {
                    PlayerUITriggerExit?.Invoke(_currentLookObject);

                    _currentLookObject = null;
                }

                _lookAwayTimer += Time.deltaTime;

                Debug.Log("Raycast hit: " + hit.collider.name + ", which does not have the tag: " + _colliderTagName);
            }
        }
        else
        {
            if (_currentLookObject != null)
            {
                PlayerUITriggerExit?.Invoke(_currentLookObject);

                _currentLookObject = null;
            }

            _lookAwayTimer += Time.deltaTime;
        }

        if (hitObject != _currentLookObject)
        {
            if (_currentLookObject != null)
            {
                PlayerUITriggerExit?.Invoke(_currentLookObject);
            }

            if (hitObject != null)
            {
                PlayerUITriggerEnter?.Invoke(hitObject);
            }

            _currentLookObject = hitObject;
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

            PlayerUITriggerEnter?.Invoke(other.gameObject);

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

            PlayerUITriggerExit?.Invoke(other.gameObject);

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