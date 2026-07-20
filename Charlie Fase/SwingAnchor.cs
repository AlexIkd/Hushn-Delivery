using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Script de Ponto de Ancoragem para o Swing.
/// Segue o mesmo padrão do WarpPoint para garantir compatibilidade com o sistema de detecção.
/// </summary>
public class SwingAnchor : MonoBehaviour
{
    [Header("Configurações do Ponto")]
    [Tooltip("Offset para o ponto exato de ancoragem da corda.")]
    public Vector3 anchorOffset = Vector3.zero;
    public bool isAvailable = true;

    // --- SISTEMA DE CACHE ESTÁTICO ---
    // Lista global para que o PlayerSwingSystem possa encontrar os pontos rapidamente
    public static List<SwingAnchor> allAnchors = new List<SwingAnchor>();

    private void OnEnable()
    {
        if (!allAnchors.Contains(this))
        {
            allAnchors.Add(this);
        }
    }

    private void OnDisable()
    {
        if (allAnchors.Contains(this))
        {
            allAnchors.Remove(this);
        }
    }

    /// <summary>
    /// Retorna a posição exata de ancoragem (ponto + offset rotacionado).
    /// </summary>
    public Vector3 GetAnchorPosition()
    {
        // Transformamos o offset local para o espaço global baseado na rotação do ponto
        return transform.position + transform.TransformDirection(anchorOffset);
    }

    // --- VISUALIZAÇÃO NO EDITOR ---
    private void OnDrawGizmos()
    {
        Vector3 anchorPos = GetAnchorPosition();

        // Desenha a esfera no ponto de origem
        Gizmos.color = isAvailable ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        // Desenha a linha e a esfera no ponto de destino (com offset)
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, anchorPos);
        Gizmos.DrawSphere(anchorPos, 0.2f);
    }
}
