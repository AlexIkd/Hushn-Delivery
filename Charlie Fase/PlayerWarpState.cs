using UnityEngine;

/// <summary>
/// Gerencia o estado do jogador durante o Warp.
/// Corrigido para ser compatível com as propriedades do novo WarpSystem.
/// </summary>
public class PlayerWarpState : MonoBehaviour
{
    private WarpSystem warpSystem;
    private PlayerMovement_FrontiersStyle movementScript;
    private Animator animator;
    private bool isWarpingState = false;

    // Cache do Hash da animação para performance
    private static readonly int HashIsHanging = Animator.StringToHash("isHanging");

    private void Awake()
    {
        // Usar Awake para garantir que as referências sejam pegas antes do Start
        warpSystem = GetComponent<WarpSystem>();
        movementScript = GetComponent<PlayerMovement_FrontiersStyle>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (warpSystem == null) return;

        // CORREÇÃO: IsPreparing e IsHanging agora são propriedades, não métodos.
        // Removidos os parênteses ()
        bool shouldBeDisabled = warpSystem.IsPreparing || warpSystem.IsHanging;

        if (shouldBeDisabled && !isWarpingState)
        {
            EnterWarpState();
        }
        else if (!shouldBeDisabled && isWarpingState)
        {
            ExitWarpState();
        }

        // Otimização: Só desativa o script se ele já não estiver desativado
        if (isWarpingState && movementScript != null && movementScript.enabled)
        {
            movementScript.enabled = false;
        }
    }

    private void EnterWarpState()
    {
        isWarpingState = true;
        if (movementScript != null) movementScript.enabled = false;
    }

    private void ExitWarpState()
    {
        isWarpingState = false;
        if (movementScript != null) movementScript.enabled = true;
        
        // Uso do Hash para performance
        if (animator != null) animator.SetBool(HashIsHanging, false);
    }
}
