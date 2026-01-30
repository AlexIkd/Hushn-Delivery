using UnityEngine;

/// <summary>
/// Script de Ponto de Warp atualizado.
/// Inclui suporte ao sistema de cache estático do WarpSystem, mantendo offset e Gizmos.
/// </summary>
public class WarpPoint : MonoBehaviour
{
    [Header("Configurações do Ponto")]
    [Tooltip("Offset para onde o jogador deve ficar posicionado em relação ao ponto.")]
    public Vector3 offset = Vector3.zero; 
    public bool isAvailable = true;

    // --- SISTEMA DE CACHE ESTÁTICO ---
    // Registra automaticamente o ponto no WarpSystem para evitar buscas lentas no Update
    private void OnEnable()
    {
        WarpSystem.RegisterPoint(this);
    }

    private void OnDisable()
    {
        WarpSystem.UnregisterPoint(this);
    }

    /// <summary>
    /// Retorna a posição exata do teletransporte (ponto + offset rotacionado).
    /// </summary>
    public Vector3 GetWarpPosition()
    {
        // Transformamos o offset local para o espaço global baseado na rotação do ponto
        return transform.position + transform.TransformDirection(offset);
    }

    // --- VISUALIZAÇÃO NO EDITOR ---
    private void OnDrawGizmos()
    {
        Vector3 warpPos = GetWarpPosition();

        // Desenha a esfera no ponto de origem
        Gizmos.color = isAvailable ? Color.cyan : Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        // Desenha a linha e a esfera no ponto de destino (com offset)
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, warpPos);
        Gizmos.DrawSphere(warpPos, 0.2f);
    }
}
