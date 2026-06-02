using UnityEngine;

public class HorizontalBar : MonoBehaviour
{
    [Header("Configurações da Barra")]
    [SerializeField] public float swingSpeed = 720f;
    [SerializeField] public float launchForce = 25f; // Aumentado para mais momentum
    [SerializeField] public float upwardBoost = 15f; // Aumentado para garantir o pulo alto
    [SerializeField] public float verticalLaunchMultiplier = 1.8f; // Aumentado para ser mais responsivo à tangente para cima
    
    [Header("Pontos de Referência")]
    [SerializeField] public Transform grabPoint;
    
    // Método para calcular a velocidade de lançamento baseada no grabPoint (Estilo Sonic Unleashed)
    public Vector3 CalculateLaunchVelocity(Vector3 launchDirection)
    {
        if (grabPoint == null) return launchDirection * launchForce;

        // No estilo Unleashed, a direção já vem pré-definida pelo quadrante (Frente, Cima, Trás ou Baixo)
        // Aplicamos a força base (launchForce) e multiplicamos se for um lançamento para cima
        Vector3 finalVelocity = launchDirection * launchForce;

        // Se o lançamento for para cima (ou tiver componente para cima), aplicamos o boost extra
        float upDot = Vector3.Dot(launchDirection, grabPoint.up);
        if (upDot > 0.1f)
        {
            // Adiciona o upwardBoost e aplica o multiplicador para garantir a altura
            finalVelocity += grabPoint.up * upwardBoost;
            finalVelocity *= verticalLaunchMultiplier;
        }

        return finalVelocity;
    }

    private void OnDrawGizmos()
    {
        if (grabPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(grabPoint.position, 0.5f);
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(grabPoint.position, grabPoint.forward * 2f);
            
            // Desenha o eixo Y (Up) do grabPoint para visualização do arremesso
            Gizmos.color = Color.green;
            Gizmos.DrawRay(grabPoint.position, grabPoint.up * 2f);
        }
    }
}
