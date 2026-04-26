using UnityEngine;
using UnityEngine.UI;

public class Clock : MonoBehaviour
{

    public Text timerText; // drag your legacy text here in inspector

    private float elapsedTime = 0f;
    private bool isRunning = false;

    public float baseTimeScale = 2f;

    void Update()
    {
        if (isRunning)
        {
            elapsedTime += Time.deltaTime / baseTimeScale;

            int minutes = Mathf.FloorToInt(elapsedTime / 60);
            int seconds = Mathf.FloorToInt(elapsedTime % 60);
            int milliseconds = Mathf.FloorToInt((elapsedTime * 1000) % 1000);

            timerText.text = string.Format("{0:00}:{1:00}.{2:000}",
                minutes, seconds, milliseconds);
        }
    }

    public void StartTimer()
    {
        elapsedTime = 0f;
        isRunning = true;
    }

    public void StopTimer()
    {
        isRunning = false;
    }
    
}
