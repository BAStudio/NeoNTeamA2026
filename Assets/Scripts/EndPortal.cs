using UnityEngine;
using UnityEngine.SceneManagement;

public class EndPortal : MonoBehaviour
{
        private void OnTriggerEnter(Collider other)
        {
            if (other.tag == "Player")
            {
                Debug.Log("Player reached the end portal! You win!");
            SceneManager.LoadScene("YouWIN");
        }
    }
}
