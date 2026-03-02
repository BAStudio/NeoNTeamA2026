using UnityEngine;

public class UITrigger : MonoBehaviour
{
    [SerializeField] private UIFader _UIFaderScript;

    [SerializeField] private bool _colliderIsTrigger;

    [SerializeField] private string _colliderTagName;

    [SerializeField] private float _rayCastDistance;

    [SerializeField] private float _fadeOutTime;

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

        if (Physics.Raycast(transform.position, Camera.main.transform.forward, out hit, _rayCastDistance) && !_colliderIsTrigger)
        {
            if(hit.collider.CompareTag(_colliderTagName))
            {
                PlayerUITriggerEnter?.Invoke();

                _UIFaderScript.GetFadeIn();

                hit.transform.gameObject.GetComponent<UIInteraction>().SetFadeOutTimer(_fadeOutTime);

                Debug.Log("Raycast hit: " + hit.collider.name + ", which has the tag: " + _colliderTagName);
            }
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(_colliderTagName) && _colliderIsTrigger)
        {
            Debug.Log("The player has entered the trigger area of: " + _colliderTagName);
            
            PlayerUITriggerEnter?.Invoke();

            _UIFaderScript.GetFadeIn();
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

            _UIFaderScript.GetFadeOut();
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
