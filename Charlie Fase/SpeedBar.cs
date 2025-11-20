// SpeedBar.cs
using UnityEngine;
using UnityEngine.UI; // Importante para usar o componente Image

public class SpeedBar : MonoBehaviour
{
    // Arraste o objeto do seu jogador (que tem o script PlayerMovement_FrontiersStyle) para este campo no Inspector
    public PlayerMovement_FrontiersStyle playerController; 
    
    private Image speedBarImage;    

    void Start()
    {
        speedBarImage = GetComponent<Image>();

        if (playerController == null)
        {
            Debug.LogError("O PlayerController não foi atribuído no Inspector da SpeedBar!");
        }
    }

    void Update()
    {
        if (playerController != null)
        {
            // Calcula a proporção da velocidade atual em relação à velocidade máxima.
            // O Fill Amount da Image vai de 0 a 1.
            // Usamos Mathf.Abs para garantir que a velocidade seja sempre positiva (se o jogo tiver movimento para trás).
            float currentSpeed = Mathf.Abs(playerController.currentSpeed);
            float maxSpeed = playerController.maxSpeed;
            
            // Garante que não haja divisão por zero e que o valor não passe de 1.
            float fillValue = Mathf.Clamp01(currentSpeed / maxSpeed);

            // Define o preenchimento visual da barra
            speedBarImage.fillAmount = fillValue;
        }
    }
}
