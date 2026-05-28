
using UnityEngine;

public class SlopeRamp : MonoBehaviour
{
    [Header("Configurações da Rampa")]
    [Tooltip("Largura máxima permitida para o movimento lateral do jogador nesta rampa.")]
    [SerializeField] public float rampWidth = 5.0f;

    [Tooltip("Ponto de início da rampa (para cálculo de posição lateral).")]
    [SerializeField] public Transform startPoint;

    [Tooltip("Ponto final da rampa (para cálculo de posição lateral).")]
    [SerializeField] public Transform endPoint;

    void OnDrawGizmos()
    {
        if (startPoint != null && endPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(startPoint.position, endPoint.position);

            // Desenha a largura da rampa
            Vector3 direction = (endPoint.position - startPoint.position).normalized;
            Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;

            Vector3 p1 = startPoint.position + perpendicular * (rampWidth / 2);
            Vector3 p2 = startPoint.position - perpendicular * (rampWidth / 2);
            Vector3 p3 = endPoint.position + perpendicular * (rampWidth / 2);
            Vector3 p4 = endPoint.position - perpendicular * (rampWidth / 2);

            Gizmos.DrawLine(p1, p2);
            Gizmos.DrawLine(p3, p4);
            Gizmos.DrawLine(p1, p3);
            Gizmos.DrawLine(p2, p4);
        }
    }

    // Método para obter a posição lateral normalizada do jogador na rampa
    // Retorna um valor entre -0.5 (esquerda) e 0.5 (direita)
    public float GetLateralPositionNormalized(Vector3 playerPosition)
    {
        if (startPoint == null || endPoint == null) return 0;

        Vector3 rampDirection = (endPoint.position - startPoint.position).normalized;
        Vector3 playerToStart = playerPosition - startPoint.position;

        // Projeta o vetor playerToStart na direção da rampa para encontrar a posição ao longo da rampa
        Vector3 projectedOnRamp = Vector3.Project(playerToStart, rampDirection);

        // Calcula o vetor perpendicular à rampa no plano horizontal
        Vector3 perpendicular = Vector3.Cross(rampDirection, Vector3.up).normalized;

        // Calcula a distância lateral do jogador em relação ao centro da rampa
        float lateralDistance = Vector3.Dot(playerToStart - projectedOnRamp, perpendicular);

        // Normaliza a distância lateral para um valor entre -0.5 e 0.5
        return lateralDistance / rampWidth;
    }
}