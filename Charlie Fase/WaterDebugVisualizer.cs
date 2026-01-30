using UnityEngine;

/// <summary>
/// Visualiza informações de debug do sistema de água no editor
/// </summary>
public class WaterDebugVisualizer : MonoBehaviour
{
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color slideColor = Color.cyan;
    [SerializeField] private Color sinkColor = Color.blue;
    [SerializeField] private float gizmoSize = 0.5f;

    private WaterMovement_System waterMovement;
    private WaterZone waterZone;

    void OnDrawGizmos()
    {
        if (!showGizmos)
            return;

        // Desenhar zona de água
        waterZone = GetComponent<WaterZone>();
        if (waterZone != null)
        {
            Collider collider = GetComponent<Collider>();
            if (collider != null)
            {
                Gizmos.color = new Color(0, 1, 1, 0.3f);
                Gizmos.DrawCube(collider.bounds.center, collider.bounds.size);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmos)
            return;

        // Desenhar informações do personagem quando selecionado
        waterMovement = GetComponent<WaterMovement_System>();
        if (waterMovement != null && waterMovement.IsInWater)
        {
            // Desenhar esfera de detecção de água
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.5f);

            // Desenhar direção de movimento
            Gizmos.color = waterMovement.CurrentState == WaterMovement_System.WaterState.Sliding ? slideColor : sinkColor;
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * waterMovement.CurrentSpeed);
        }
    }
}
