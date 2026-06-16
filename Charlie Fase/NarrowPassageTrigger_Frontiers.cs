using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class NarrowPassageTrigger_Frontiers : MonoBehaviour
{
    [Header("Configurações da Passagem")]
    [Tooltip("Velocidade do jogador dentro da passagem.")]
    public float movementSpeed = 2f;

    [Header("Configurações do Character Controller")]
    public float narrowRadius = 0.25f;
    public float narrowHeight = 1.8f;

    [Tooltip("Distância extra para fora do BoxCollider para os pontos de entrada/saída.")]
    public float passageOffset = 0.5f; // Valor padrão, pode ser ajustado no Inspector

    private BoxCollider boxCollider;

    void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
        boxCollider.isTrigger = true;
    }

    /// <summary>
    /// Calcula os pontos de entrada e saída. 
    /// O 'entryPoint' é onde o jogador começa a transição (fora do collider).
    /// O 'exitPoint' é o destino final após atravessar (também fora do collider).
    /// </summary>
    /// <summary>
    /// Retorna os dois pontos externos da passagem (um em cada extremidade, com offset).
    /// </summary>
    public void GetPassageEnds(out Vector3 point1, out Vector3 point2)
    {
        Vector3 center = transform.position + transform.TransformDirection(boxCollider.center);
        Vector3 direction = transform.forward;
        float halfLengthWithOffset = (boxCollider.size.z * transform.lossyScale.z * 0.5f) + passageOffset;
        float bottomY = center.y - (boxCollider.size.y * transform.lossyScale.y * 0.5f);

        point1 = center - direction * halfLengthWithOffset;
        point2 = center + direction * halfLengthWithOffset;
        point1.y = bottomY;
        point2.y = bottomY;
    }

    public void GetPassagePoints(Vector3 playerPos, out Vector3 entryPoint, out Vector3 exitPoint, out Vector3 direction)
    {
        GetPassageEnds(out Vector3 p1, out Vector3 p2);
        direction = transform.forward;

        if (Vector3.Distance(playerPos, p1) < Vector3.Distance(playerPos, p2))
        {
            entryPoint = p1;
            exitPoint = p2;
        }
        else
        {
            entryPoint = p2;
            exitPoint = p1;
            direction = -direction;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            NarrowPassageController npc = other.GetComponent<NarrowPassageController>();
            // Só entra se não estiver saindo ou já dentro
            if (npc != null && !npc.IsInNarrowPassageState && !npc.IsTransitioningState && npc.CurrentExitTimer <= 0)
            {
                Vector3 entry, exit, dir;
                GetPassagePoints(other.transform.position, out entry, out exit, out dir);
                npc.EnterNarrowPassage(this, entry, exit, dir);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            NarrowPassageController npc = other.GetComponent<NarrowPassageController>();
            
            if (npc != null && npc.IsInNarrowPassageState && !npc.IsTransitioningState)
            {
                // Quando sai do trigger, queremos levá-lo para o ponto externo mais próximo da posição atual dele
                GetPassageEnds(out Vector3 p1, out Vector3 p2);
                Vector3 closestExit = (Vector3.Distance(other.transform.position, p1) < Vector3.Distance(other.transform.position, p2)) ? p1 : p2;
                
                npc.ExitNarrowPassage(closestExit);
            }
        }
    }

    private void OnDrawGizmos()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (!box) return;

        // Desenha o volume do collider em espaço local
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0, 1, 1, 0.3f);
        Gizmos.DrawCube(box.center, box.size);
        
        // Resetamos a matriz para desenhar os pontos de entrada/saída em espaço de mundo
        // Isso garante que o passageOffset seja consistente independente da escala
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = Color.cyan;

        Vector3 center = transform.position + transform.TransformDirection(box.center);
        Vector3 size = box.size;
        Vector3 direction = transform.forward;
        float halfLength = (size.z * transform.lossyScale.z * 0.5f) + passageOffset;
        float bottomY = center.y - (size.y * transform.lossyScale.y * 0.5f);

        Vector3 gizmoP1 = center - direction * halfLength;
        Vector3 gizmoP2 = center + direction * halfLength;
        gizmoP1.y = bottomY;
        gizmoP2.y = bottomY;

        Gizmos.DrawLine(gizmoP1, gizmoP2);
        Gizmos.DrawWireSphere(gizmoP1, 0.2f);
        Gizmos.DrawWireSphere(gizmoP2, 0.2f);
    }
}
