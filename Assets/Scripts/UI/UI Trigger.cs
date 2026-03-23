using UnityEngine;
using UnityEngine.Events;

public class UITrigger : MonoBehaviour
{
    //[SerializeField] private UIFader _UIFaderScript;

    [SerializeField] private bool _colliderIsTrigger;

    [SerializeField] private string _colliderTagName;

    [SerializeField] private float _raycastDistance;

    public static event System.Action PlayerUITriggerEnter;
    public static event System.Action PlayerUITriggerExit;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        RaycastHit hit;

        if (Physics.Raycast(transform.position, Camera.main.transform.forward, out hit, _raycastDistance) && !_colliderIsTrigger)
        {
            if (hit.collider.CompareTag(_colliderTagName))
            {                
                PlayerUITriggerEnter?.Invoke();

                hit.collider.gameObject.GetComponentInChildren<UIFader>()?.GetFadeIn();

                Debug.Log("Raycast hit: " + hit.collider.name + ", which has the tag: " + _colliderTagName);
            }
            else 
            {
                PlayerUITriggerExit?.Invoke();

                Debug.Log("Raycast hit: " + hit.collider.name + ", which does not have the tag: " + _colliderTagName);
            }
        }
        else
        {
            PlayerUITriggerExit?.Invoke();

            hit.collider.gameObject.GetComponentInChildren<UIFader>()?.GetFadeOut();

            Debug.Log("Raycast did not hit anything within the distance of: " + _raycastDistance);
        }

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(_colliderTagName) && _colliderIsTrigger)
        {
            Debug.Log("The player has entered the trigger area of: " + _colliderTagName);
            
            PlayerUITriggerEnter?.Invoke();

            other.gameObject.GetComponentInChildren<UIFader>()?.GetFadeIn();

            //_UIFaderScript.GetFadeIn();

            //other.gameObject.transform.GetComponentInChildren<UIFader>().GetFadeIn();

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

            other.gameObject.GetComponentInChildren<UIFader>()?.GetFadeOut();

            //_UIFaderScript.GetFadeOut();

            //other.gameObject.transform.GetComponentInChildren<UIFader>().GetFadeOut();
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
