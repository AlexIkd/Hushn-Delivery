using UnityEngine;

/// <summary>
/// Sistema de Wall Dash Jump - Script otimizado para performance
/// Reduz chamadas de Reflection, Raycast e Debug em tempo real
/// ✅ ATUALIZADO: Dispara camera shake quando Wall Dash Jump é acionado
/// </summary>
public class WallDashJump : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private PlayerMovement_FrontiersStyle playerMovement;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Animator animator;
    
    // ✅ NOVO: Referência à câmera para camera shake
    [SerializeField] private DynamicFollowCamera dynamicCamera;

    [Header("Configurações")]
    [SerializeField] private float jumpForce = 20f;
    [SerializeField] private float wallDashDuration = 0.5f;
    [SerializeField] private bool enableWallDashJump = true;
    [SerializeField] private float wallDetectionDistance = 1.5f;
    [SerializeField] private LayerMask wallLayer;

    [Header("Partículas")]
    [SerializeField] private ParticleSystem jumpParticles;
    [SerializeField] private bool enableParticles = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false; // DESATIVADO por padrão

    // Estado interno
    private Vector3 jumpVelocity = Vector3.zero;
    private bool isWallDashing = false;
    private float wallDashCooldown = 0.15f;
    private float wallDashCooldownTimer = 0f;
    private float wallDashTimer = 0f;

    // CACHE - Evita Reflection a cada frame
    private System.Reflection.FieldInfo isDashingField;
    private System.Reflection.FieldInfo airDashTimerField;
    private System.Reflection.FieldInfo airDashParticlesField;
    private bool hasReflectionCached = false;

    // CACHE - Evita alocação de arrays a cada frame
    private Vector3[] rayDirections = new Vector3[5];
    private RaycastHit raycastHit;

    // CACHE - Evita GetComponent repetidos
    private Transform cachedTransform;
    private const float GRAVITY = 9.81f;
    private const float HORIZONTAL_MOMENTUM_RETAIN = 0.3f;

    private void Start()
    {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement_FrontiersStyle>();

        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        if (animator == null)
            animator = GetComponent<Animator>();

        cachedTransform = transform;

        // ✅ NOVO: Encontrar a câmera se não estiver atribuída
        if (dynamicCamera == null)
        {
            dynamicCamera = Camera.main?.GetComponent<DynamicFollowCamera>();
            if (dynamicCamera == null)
            {
                dynamicCamera = FindObjectOfType<DynamicFollowCamera>();
            }
        }

        // Cache de Reflection - feito UMA VEZ no Start
        CacheReflectionFields();

        if (showDebugInfo)
        {
            Debug.Log("=== WALL DASH JUMP INICIADO ===");
            Debug.Log($"PlayerMovement: {(playerMovement != null ? "✅" : "❌")}");
            Debug.Log($"CharacterController: {(characterController != null ? "✅" : "❌")}");
            Debug.Log($"Animator: {(animator != null ? "✅" : "❌")}");
            Debug.Log($"DynamicFollowCamera: {(dynamicCamera != null ? "✅" : "❌")}");
        }
    }

    /// <summary>
    /// Cache de campos de Reflection para evitar overhead a cada frame
    /// </summary>
    private void CacheReflectionFields()
    {
        if (playerMovement == null || hasReflectionCached)
            return;

        try
        {
            var bindingFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            isDashingField = playerMovement.GetType().GetField("isDashing", bindingFlags);
            airDashTimerField = playerMovement.GetType().GetField("airDashTimer", bindingFlags);
            airDashParticlesField = playerMovement.GetType().GetField("airDashParticles", bindingFlags);
            hasReflectionCached = true;
        }
        catch { }
    }

    private void Update()
    {
        if (!enableWallDashJump || playerMovement == null)
            return;

        // Atualiza cooldown
        if (wallDashCooldownTimer > 0)
            wallDashCooldownTimer -= Time.deltaTime;

        // Verifica se deve executar wall dash - OTIMIZADO: menos chamadas
        if (wallDashCooldownTimer <= 0 && !isWallDashing && IsPlayerDashing())
        {
            if (DetectWallDuringDash())
            {
                ExecuteWallDashJump();
                wallDashCooldownTimer = wallDashCooldown;
            }
        }

        // Gerencia duração do wall dash
        if (isWallDashing)
        {
            wallDashTimer -= Time.deltaTime;

            // Se o tempo acabou, desativa
            if (wallDashTimer <= 0f)
            {
                isWallDashing = false;
                if (showDebugInfo)
                    Debug.Log("⏱️ Duração do Wall Dash Jump terminou");
            }
            else
            {
                // Aplica impulso APENAS se ainda está ativo
                ApplyWallDashVelocity();
            }
        }
    }

    /// <summary>
    /// Verifica se o jogador está em air dash - OTIMIZADO: usa cache de Reflection
    /// </summary>
    private bool IsPlayerDashing()
    {
        if (playerMovement == null || isDashingField == null)
            return false;

        try
        {
            return (bool)isDashingField.GetValue(playerMovement);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Detecta parede próxima - OTIMIZADO: menos raycasts, sem alocação de arrays
    /// </summary>
    private bool DetectWallDuringDash()
    {
        Vector3 rayOrigin = cachedTransform.position;
        
        // Pré-calcula direções
        Vector3 forward = cachedTransform.forward;
        Vector3 right = cachedTransform.right;

        rayDirections[0] = forward;
        rayDirections[1] = right;
        rayDirections[2] = -right;
        rayDirections[3] = (forward + right).normalized;
        rayDirections[4] = (forward - right).normalized;

        // Raycast com early exit
        for (int i = 0; i < 5; i++)
        {
            if (Physics.Raycast(rayOrigin, rayDirections[i], out raycastHit, wallDetectionDistance, wallLayer))
            {
                if (showDebugInfo)
                    Debug.DrawRay(rayOrigin, rayDirections[i] * wallDetectionDistance, Color.red, 0.2f);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Executa o wall dash jump
    /// ✅ ATUALIZADO: Dispara camera shake
    /// </summary>
    private void ExecuteWallDashJump()
    {
        if (isWallDashing)
            return;

        isWallDashing = true;
        wallDashTimer = wallDashDuration;

        if (showDebugInfo)
            Debug.Log("🚀 WALL DASH JUMP ACIONADO");

        // ✅ NOVO: Dispara camera shake
        if (dynamicCamera != null)
        {
            dynamicCamera.TriggerWallDashShake();
            if (showDebugInfo)
                Debug.Log("📸 Camera Shake disparado!");
        }

        // Para o dash do jogador - OTIMIZADO: usa cache
        StopPlayerDash();

        // Desativa partículas de air dash - OTIMIZADO: usa cache
        DisableAirDashParticles();

        // Aplica impulso vertical
        jumpVelocity = Vector3.zero;
        jumpVelocity.y = jumpForce;

        // Mantém momentum horizontal
        if (characterController != null)
        {
            Vector3 currentVel = characterController.velocity;
            jumpVelocity.x = currentVel.x * HORIZONTAL_MOMENTUM_RETAIN;
            jumpVelocity.z = currentVel.z * HORIZONTAL_MOMENTUM_RETAIN;
        }

        // ACIONA TRIGGER
        if (animator != null)
        {
            animator.SetTrigger("WallDashJump");

            if (showDebugInfo)
                Debug.Log($"✅ Trigger 'WallDashJump' DISPARADO (duração: {wallDashDuration}s)");
        }

        // Partículas
        if (enableParticles && jumpParticles != null)
        {
            jumpParticles.Play();
        }

        if (showDebugInfo)
            Debug.Log($"Impulso: {jumpForce}, Duração: {wallDashDuration}s");
    }

    /// <summary>
    /// Aplica a velocidade do wall dash
    /// </summary>
    private void ApplyWallDashVelocity()
    {
        if (characterController == null || !characterController.enabled)
            return;

        // Move com a velocidade
        characterController.Move(jumpVelocity * Time.deltaTime);

        // Aplica gravidade
        jumpVelocity.y -= GRAVITY * Time.deltaTime;

        // Para quando chega ao chão
        if (characterController.isGrounded)
        {
            if (showDebugInfo)
                Debug.Log("🏁 Wall Dash Jump finalizado - Jogador no chão");

            jumpVelocity = Vector3.zero;
            isWallDashing = false;
            wallDashTimer = 0f;
        }
    }

    /// <summary>
    /// Para o air dash do jogador - OTIMIZADO: usa cache de Reflection
    /// </summary>
    private void StopPlayerDash()
    {
        if (playerMovement == null)
            return;

        try
        {
            if (isDashingField != null)
                isDashingField.SetValue(playerMovement, false);

            if (airDashTimerField != null)
                airDashTimerField.SetValue(playerMovement, 0f);

            if (showDebugInfo)
                Debug.Log("⏹️ Dash parado");
        }
        catch (System.Exception e)
        {
            if (showDebugInfo)
                Debug.LogWarning("Erro ao parar dash: " + e.Message);
        }
    }

    /// <summary>
    /// Desativa partículas de air dash - OTIMIZADO: usa cache de Reflection
    /// </summary>
    private void DisableAirDashParticles()
    {
        if (playerMovement == null || airDashParticlesField == null)
            return;

        try
        {
            var airDashParticles = airDashParticlesField.GetValue(playerMovement) as ParticleSystem;
            if (airDashParticles != null)
            {
                airDashParticles.Stop();
                if (showDebugInfo)
                    Debug.Log("⏹️ Partículas de Air Dash paradas");
            }
        }
        catch (System.Exception e)
        {
            if (showDebugInfo)
                Debug.LogWarning("Erro ao desativar partículas: " + e.Message);
        }
    }

    // Métodos públicos
    public float GetJumpForce() => jumpForce;
    public void SetJumpForce(float newForce) => jumpForce = newForce;
    public void SetEnabled(bool enabled) => enableWallDashJump = enabled;
    
    /// <summary>
    /// Retorna se Wall Dash Jump está ativo
    /// Usado para bloquear Wall Run enquanto Wall Dash Jump está em execução
    /// </summary>
    public bool IsWallDashing() => isWallDashing;
}
