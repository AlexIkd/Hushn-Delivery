using UnityEngine;
using TMPro; // NOVO: Namespace para TextMeshPro

public class StopwatchTimer : MonoBehaviour
{
    [Header("Configurações do Timer")]
    // ALTERADO: Use TextMeshProUGUI para o componente de texto
    public TextMeshProUGUI timerText; 

    private float startTime;
    private bool isTimerRunning = false;

    void Start()
    {
        StartStopwatch();
    }

    void Update()
    {
        if (isTimerRunning)
        {
            float timeElapsed = Time.time - startTime;
            UpdateTimerDisplay(timeElapsed);
        }
    }

    void UpdateTimerDisplay(float timeToDisplay)
    {
        if (timerText != null)
        {
            // Formata o tempo para exibir Minutos, Segundos e Milissegundos
            int minutes = Mathf.FloorToInt(timeToDisplay / 60);
            int seconds = Mathf.FloorToInt(timeToDisplay % 60);
            int milliseconds = Mathf.FloorToInt((timeToDisplay * 100) % 100);

            timerText.text = string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, milliseconds);
        }
    }

    public void StartStopwatch()
    {
        startTime = Time.time; 
        isTimerRunning = true;
    }

    public float StopStopwatch()
    {
        isTimerRunning = false;
        return Time.time - startTime;
    }

    public void ResetStopwatch()
    {
        startTime = Time.time;
        isTimerRunning = true;
    }
}
