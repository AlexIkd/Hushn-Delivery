using UnityEngine;
using TMPro; // Necessário para usar TextMeshPro

/// <summary>
/// Script de HUD básico para exibir informações do jogador.
/// Requer um componente TextMeshProUGUI para exibir o texto.
/// </summary>
public class PlayerHUD : MonoBehaviour
{
    [Header("Componentes de UI")]
    [Tooltip("Referência ao componente TextMeshProUGUI para exibir a velocidade.")]
    public TextMeshProUGUI speedText;

    [Header("Referência do Jogador")]
    [Tooltip("Referência ao script de movimento do jogador.")]
    private PlayerMovement_FrontiersStyle playerMovement;

    void Start()
    {
        // 1. Tenta encontrar o componente TextMeshProUGUI
        if (speedText == null)
        {
            speedText = GetComponent<TextMeshProUGUI>();
        }

        if (speedText == null)
        {
            Debug.LogError("Componente TextMeshProUGUI não atribuído! Arraste um objeto de texto (TMP) para o campo 'Speed Text' no Inspector.");
            enabled = false;
            return;
        }

        // 2. Tenta encontrar o script de movimento do jogador na cena
        playerMovement = FindObjectOfType<PlayerMovement_FrontiersStyle>();

        if (playerMovement == null)
        {
            Debug.LogError("Script PlayerMovement_FrontiersStyle não encontrado na cena. Certifique-se de que o jogador está ativo.");
            enabled = false;
        }
    }

    void Update()
    {
        if (playerMovement != null && speedText != null)
        {
            // Obtém a velocidade atual do jogador
            float currentSpeed = playerMovement.CurrentSpeed;

            // Formata o texto para exibir a velocidade com duas casas decimais
            // Ex: Velocidade: 15.50 m/s
            speedText.text = $"Velocidade: {currentSpeed:F2} m/s";
        }
    }
}
