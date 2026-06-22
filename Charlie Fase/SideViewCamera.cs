using UnityEngine;

/// <summary>
/// Script de Câmera Lateral (Rail) com suporte a sincronização automática 
/// para encaixe perfeito com a DynamicFollowCamera.
/// </summary>
public class SideViewCamera : MonoBehaviour
{
    [Header("Alvo")]
    public Transform target;

    [Header("Configurações de Posição")]
    [Tooltip("Distância da câmera em relação ao jogador. (Será sincronizada automaticamente)")]
    public float distance = 10f;
    [Tooltip("Altura da câmera em relação ao jogador. (Será sincronizada automaticamente)")]
    public float heightOffset = 2f;
    [SerializeField] private float sideOffset = 0f; 
    
    [Header("Suavização")]
    [SerializeField] private float smoothSpeed = 0.125f;
    [SerializeField] private bool lockVertical = true; 
    [SerializeField] private float fixedHeight = 5f;

    [Header("Limites de Movimento")]
    [SerializeField] private bool preventBacktracking = false; 
    private float maxReachedX = -Mathf.Infinity;

    [Header("Configurações de Eixo")]
    [Tooltip("Define qual eixo é o movimento lateral (X ou Z)")]
    [SerializeField] private bool useZAsLateral = false;

    private Camera cam;

    private void Start()
    {
        cam = GetComponent<Camera>();
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }

        if (target != null)
        {
            maxReachedX = useZAsLateral ? target.position.z : target.position.x;
        }
    }

    /// <summary>
    /// MÉTODO DE SINCRONIZAÇÃO:
    /// Chama isso no Trigger antes de StartTransitionToRail para garantir o encaixe perfeito.
    /// </summary>
    public void SyncWithMainCamera(DynamicFollowCamera mainCamScript)
    {
        if (mainCamScript == null) return;

        // 1. Sincroniza o FOV
        if (cam == null) cam = GetComponent<Camera>();
        if (cam != null) cam.fieldOfView = mainCamScript.normalFOV;

        // 2. Sincroniza Distância e Altura com base nos parâmetros da sua câmera principal
        this.distance = mainCamScript.baseDistance;
        this.heightOffset = mainCamScript.height;

        // 3. Ajusta a altura fixa para bater com a visão atual do jogador
        this.fixedHeight = target.position.y + mainCamScript.height;
        
        Debug.Log($"[SideViewCamera] Sincronizado: Dist {distance}, Height {heightOffset}, FOV {cam.fieldOfView}");
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition;

        if (!useZAsLateral)
        {
            // Movimento no eixo X (Padrão)
            float currentX = target.position.x + sideOffset;

            if (preventBacktracking)
            {
                if (currentX > maxReachedX) maxReachedX = currentX;
                else currentX = maxReachedX;
            }

            float targetY = lockVertical ? fixedHeight : target.position.y + heightOffset;
            desiredPosition = new Vector3(currentX, targetY, target.position.z - distance);
        }
        else
        {
            // Movimento no eixo Z
            float currentZ = target.position.z + sideOffset;

            if (preventBacktracking)
            {
                if (currentZ > maxReachedX) maxReachedX = currentZ;
                else currentZ = maxReachedX;
            }

            float targetY = lockVertical ? fixedHeight : target.position.y + heightOffset;
            desiredPosition = new Vector3(target.position.x - distance, targetY, currentZ);
        }

        // Suavização do movimento
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        // Garante que a câmera esteja sempre olhando para o jogador
        Vector3 lookTarget = target.position + Vector3.up * heightOffset;
        transform.LookAt(lookTarget);
    }

    public void ResetCameraLimits()
    {
        if (target != null)
        {
            maxReachedX = useZAsLateral ? target.position.z : target.position.x;
        }
    }
}
