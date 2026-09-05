using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float interactionRange = 2f;
    [SerializeField] private float interactionHeightOffset = 1.0f; // Altura da linha de interação
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    private IInteractable currentInteractable;

    void Update()
    {
        CheckForInteractable();

        // Se o diálogo já está aberto, NÃO deixa o E registrar uma nova interação
        // (isso evita reiniciar o diálogo — durante a conversa, o E só avança a fala no NPCDialogueManager)
        if (NPCDialogueManager.Instance != null && NPCDialogueManager.Instance.IsDialogueActive)
            return;

        if (currentInteractable != null && Input.GetKeyDown(interactKey))
        {
            currentInteractable.Interact(gameObject);
        }
    }

    private void CheckForInteractable()
    {
        // Posição central da área de detecção
        Vector3 detectionCenter = transform.position + Vector3.up * interactionHeightOffset;
        
        // Detecta todos os colisores dentro da esfera (Área)
        Collider[] colliders = Physics.OverlapSphere(detectionCenter, interactionRange, interactableLayer, QueryTriggerInteraction.Collide);

        IInteractable closestInteractable = null;
        float closestDistance = float.MaxValue;

        foreach (var col in colliders)
        {
            // Ignora o próprio jogador
            if (col.gameObject == gameObject) continue;

            IInteractable interactable = col.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                // Encontra o objeto mais próximo na área
                float dist = Vector3.Distance(detectionCenter, col.transform.position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closestInteractable = interactable;
                }
            }
        }

        if (closestInteractable != null)
        {
            if (currentInteractable != closestInteractable)
            {
                Debug.Log("<color=cyan>Interação:</color> Objeto detectado na área!");
            }
            currentInteractable = closestInteractable;
        }
        else
        {
            currentInteractable = null;
        }
    }

    // Desenha a área de detecção no Editor para facilitar o ajuste
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 detectionCenter = transform.position + Vector3.up * interactionHeightOffset;
        Gizmos.DrawWireSphere(detectionCenter, interactionRange);
    }

    // Para uso na UI
    public string GetCurrentInteractText()
    {
        return currentInteractable?.GetInteractText() ?? "";
    }
}
