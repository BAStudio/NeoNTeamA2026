using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SendToScene : MonoBehaviour
{
    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    public void LoadSceneName(string SceneName)
    {
        SceneManager.LoadScene(SceneName);
    }
}
