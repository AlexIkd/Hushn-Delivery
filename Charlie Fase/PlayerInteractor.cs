using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float interactionRange = 2f;
    [SerializeField] private float interactionHeightOffset = 1.0f;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("Remote Interaction")]
    [Tooltip("Referência ao sistema do relógio. Pode ser preenchida manualmente ou encontrada automaticamente.")]
    [SerializeField] private RemoteLeverInteraction remoteInteraction;

    private IInteractable currentInteractable;

    private void Awake()
    {
        if (remoteInteraction == null)
            remoteInteraction = GetComponent<RemoteLeverInteraction>();

        if (remoteInteraction == null)
            remoteInteraction = FindFirstObjectByType<RemoteLeverInteraction>();
    }

    private void Update()
    {
        // O modo remoto usa a mesma tecla E para acionar alavancas à distância.
        // Bloqueia completamente o interactor comum para evitar conflitos.
        if (IsRemoteModeActive())
        {
            currentInteractable = null;
            return;
        }

        CheckForInteractable();

        // Se o diálogo já está aberto, NÃO deixa o E registrar uma nova interação.
        if (NPCDialogueManager.Instance != null && NPCDialogueManager.Instance.IsDialogueActive)
            return;

        if (currentInteractable != null && Input.GetKeyDown(interactKey))
            currentInteractable.Interact(gameObject);
    }

    private bool IsRemoteModeActive()
    {
        return remoteInteraction != null && remoteInteraction.IsRemoteModeActive;
    }

    private void CheckForInteractable()
    {
        Vector3 detectionCenter = transform.position + Vector3.up * interactionHeightOffset;

        Collider[] colliders = Physics.OverlapSphere(
            detectionCenter,
            interactionRange,
            interactableLayer,
            QueryTriggerInteraction.Collide
        );

        IInteractable closestInteractable = null;
        float closestDistance = float.MaxValue;

        foreach (Collider col in colliders)
        {
            if (col.gameObject == gameObject)
                continue;

            IInteractable interactable = col.GetComponentInParent<IInteractable>();
            if (interactable == null)
                continue;

            float dist = Vector3.Distance(detectionCenter, col.transform.position);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closestInteractable = interactable;
            }
        }

        if (closestInteractable != null)
        {
            if (currentInteractable != closestInteractable)
                Debug.Log("<color=cyan>Interação:</color> Objeto detectado na área!");

            currentInteractable = closestInteractable;
        }
        else
        {
            currentInteractable = null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 detectionCenter = transform.position + Vector3.up * interactionHeightOffset;
        Gizmos.DrawWireSphere(detectionCenter, interactionRange);
    }

    public string GetCurrentInteractText()
    {
        return currentInteractable?.GetInteractText() ?? string.Empty;
    }
}
