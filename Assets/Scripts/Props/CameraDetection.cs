using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraDetection : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            string currentSceneMane = SceneManager.GetActiveScene().name;
            SceneManager.LoadScene("YouLose");
        }
    }
}
