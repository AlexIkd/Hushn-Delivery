using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Integra o pulo do sistema de água com o sistema de movimentação normal
/// Detecta quando o jogador pula enquanto afundado e retorna ao sistema normal
/// </summary>
public class WaterJumpIntegration : MonoBehaviour
{
    [Header("Configurações")]
    [SerializeField] private bool showDebugInfo = false;

    private PlayerMovement_FrontiersStyle normalMovement;
    private WaterMovement_System waterMovement;
    private Keyboard keyboard;

    void Start()
    {
        normalMovement = GetComponent<PlayerMovement_FrontiersStyle>();
        waterMovement = GetComponent<WaterMovement_System>();
        keyboard = Keyboard.current;

        if (normalMovement == null)
            Debug.LogError("PlayerMovement_FrontiersStyle não encontrado!");

        if (waterMovement == null)
            Debug.LogError("WaterMovement_System não encontrado!");
    }

    void Update()
    {
        // Verificar se está na água e no estado de afundamento
        if (waterMovement == null || !waterMovement.IsInWater)
            return;

        if (waterMovement.CurrentState != WaterMovement_System.WaterState.Sinking)
            return;

        // Detectar entrada de pulo
        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
        {
            HandleWaterJump();
        }
    }

    /// <summary>
    /// Gerencia o pulo enquanto afundado na água
    /// </summary>
    private void HandleWaterJump()
    {
        if (showDebugInfo)
            Debug.Log("⬆️ Pulo detectado enquanto afundado na água");

        // Chamar o método de pulo do sistema de água
        waterMovement.Jump();

        if (showDebugInfo)
            Debug.Log("✅ Retornando ao sistema de movimentação normal");
    }

    /// <summary>
    /// Método público para forçar saída da água (pode ser chamado por eventos)
    /// </summary>
    public void ForceExitWater()
    {
        if (waterMovement != null && waterMovement.IsInWater)
        {
            waterMovement.Jump();
        }
    }
}
