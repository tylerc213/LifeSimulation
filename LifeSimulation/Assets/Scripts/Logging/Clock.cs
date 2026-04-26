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
            int ticks = LogManager.Instance.currentTick;
            timerText.text = $"{ticks}";
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
