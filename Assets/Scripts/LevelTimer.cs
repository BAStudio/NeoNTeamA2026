using TMPro;
using TMPro.Examples;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelTimer : MonoBehaviour
{
    [SerializeField] private float timeLimit = 60f;
    [SerializeField] private TextMeshProUGUI timerText;

    private float timeRemaining;

    void Start()
    {
        timeRemaining = timeLimit;
    }

    // Update is called once per frame
    void Update()
    {
        timeRemaining -= Time.deltaTime;

        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            timerText.text = "00:00";
            string currentSceneMane = SceneManager.GetActiveScene().name;
            SceneManager.LoadScene(currentSceneMane);
        }
    }
}
