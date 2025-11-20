using UnityEngine;

/// <summary>
/// Componente responsável por receber o rank final e acionar a animação
/// correspondente no Animator do personagem.
/// Deve ser anexado ao objeto do personagem.
/// </summary>
public class PlayerRankAnimator : MonoBehaviour
{
    // Referência ao Animator do personagem
    [Tooltip("Arraste o componente Animator do seu personagem para este campo.")]
    [SerializeField] private Animator characterAnimator;

    // Instância estática para acesso fácil pelo RankingUIController
    public static PlayerRankAnimator Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Aciona o Trigger da animação de rank no Animator.
    /// </summary>
    /// <param name="rankName">O nome do rank (ex: "S", "A", "B").</param>
    public void SetRankTrigger(string rankName)
    {
        if (characterAnimator == null)
        {
            Debug.LogError("Animator do personagem não está configurado no PlayerRankAnimator.");
            return;
        }

        // Constrói o nome do Trigger da animação.
        // Ex: "S" -> "Victory_S"
        string animationTriggerName = "Victory_" + rankName;

        // Aciona o Trigger no Animator
        characterAnimator.SetTrigger(animationTriggerName);
        
        Debug.Log($"Animação de Rank acionada: {animationTriggerName}");
    }
}
