using UnityEngine;

/// <summary>
/// Sistema de Wall Dash Jump - Mecânica estilo A Hat in Time
/// Quando o jogador faz dash contra uma parede, ele sobe correndo pela parede,
/// fica parado por um momento e depois desliza para baixo.
///
/// INTEGRAÇÃO COM PlayerMovement_FrontiersStyle:
/// Em vez de chamar CharacterController.Move() diretamente, este script
/// modifica o moveDirection do PlayerMovement, que é o script que realmente
/// controla o CharacterController.
/// </summary>
public class WallDashJump : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private PlayerMovement_FrontiersStyle playerMovement;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Animator animator;
    
    // Referência à câmera para camera shake
    [SerializeField] private DynamicFollowCamera dynamicCamera;

    [Header("Configurações")]
    [SerializeField] private float wallDashDuration = 0.5f;
    [SerializeField] private bool enableWallDashJump = true;
    [SerializeField] private float wallDetectionDistance = 1.5f;
    [SerializeField] private LayerMask wallLayer;

    [Header("Partículas")]
    [SerializeField] private ParticleSystem jumpParticles;
    [SerializeField] private bool enableParticles = true;

    [Header("Configurações de Rotação da Animação na Parede")]
    [Tooltip("Rotação no eixo X aplicada à animação durante stick e slide (graus). Ex: 90 para ficar perpendicular à parede.")]
    [SerializeField] private float wallAnimationRotationX = 90f;
    [Tooltip("Rotação no eixo Y aplicada à animação durante o climb (escalada). Graus. 0 = sem rotação adicional.")]
    [SerializeField] private float wallClimbRotationY = 0f;
    [Tooltip("Velocidade de interpolação da rotação. Valores maiores = mais rápido.")]
    [SerializeField] private float wallRotationSpeed = 10f;
    [Tooltip("Se true, a rotação é aplicada ao transform do personagem. Se false, é aplicada via Animator root rotation.")]
    [SerializeField] private bool useTransformRotation = true;

    [Header("Configurações de Bloqueio de Air Dash após Pulos de Parede")]
    [Tooltip("Tempo em que o Air Dash fica bloqueado após o pulo automático do topo da parede. 0 = sem bloqueio.")]
    [SerializeField] private float airDashLockAfterAutoJump = 0.5f;
    [Tooltip("Tempo em que o Air Dash fica bloqueado após o pulo de cancelamento (manual ou automático pelo timer do slide). 0 = sem bloqueio.")]
    [SerializeField] private float airDashLockAfterWallCancel = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    // Estado interno
    private bool isWallDashing = false;
    private float wallDashCooldown = 0.15f;
    private float wallDashCooldownTimer = 0f;

    // Variáveis para a mecânica de A Hat in Time
    [Header("Configurações de A Hat in Time")]
    [SerializeField] private float wallClimbSpeed = 8f;
    [SerializeField] private float wallClimbDuration = 0.6f;
    [SerializeField] private float wallStickDuration = 0.4f;
    [SerializeField] private float wallSlideSpeed = 3f;
    [Tooltip("Tempo máximo do deslizamento na parede. Quando acabar, executa o pulo de cancelamento automaticamente. 0 = sem limite (desliza até chegar no chão ou sair da parede).")]
    [SerializeField] private float wallSlideDuration = 0f;
    [SerializeField] private float wallJumpForce = 15f;
    [SerializeField] private float wallJumpUpForce = 10f;
    [SerializeField] private float wallRecheckDistance = 1.0f;
    [SerializeField] private float autoJumpForwardForce = 12f;
    [SerializeField] private float autoJumpUpForce = 8f;
    [SerializeField] private float backwardJumpForce = 12f;
    [SerializeField] private float backwardJumpUpForce = 8f;
    [SerializeField] private float minClimbTimeBeforeAutoJump = 0.2f;
    [SerializeField] private KeyCode cancelKey = KeyCode.Space;

    // Propriedades públicas para o PlayerMovement verificar
    [HideInInspector] public bool isWallClimbing = false;
    [HideInInspector] public bool isWallSticking = false;
    [HideInInspector] public bool isWallSliding = false;
    
    private float currentWallClimbTimer = 0f;
    private float currentWallStickTimer = 0f;
    private float currentWallSlideTimer = 0f;
    private Vector3 currentWallNormal = Vector3.zero;
    private Vector3 wallContactPoint = Vector3.zero;

    // Rotação desejada durante stick/slide
    private Quaternion targetWallRotation;
    private Quaternion originalRotation;
    private bool hasTargetWallRotation = false;
    private bool isWallRotated = false;

    // ================================================================
    // TIMER DE BLOQUEIO DE AIR DASH APÓS PULOS DE PAREDE
    // ================================================================
    private float airDashLockTimer = 0f;

    // CACHE
    private System.Reflection.FieldInfo isDashingField;
    private System.Reflection.FieldInfo airDashTimerField;
    private System.Reflection.FieldInfo airDashParticlesField;
    private System.Reflection.FieldInfo isRotationLockedField;
    private bool hasReflectionCached = false;

    // CACHE - Evita alocação de arrays a cada frame
    private Vector3[] rayDirections = new Vector3[5];
    private RaycastHit raycastHit;

    // CACHE
    private Transform cachedTransform;

    private void Start()
    {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement_FrontiersStyle>();

        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        if (animator == null)
            animator = GetComponent<Animator>();

        cachedTransform = transform;

        if (dynamicCamera == null)
        {
            dynamicCamera = Camera.main?.GetComponent<DynamicFollowCamera>();
            if (dynamicCamera == null)
            {
                dynamicCamera = FindObjectOfType<DynamicFollowCamera>();
            }
        }

        CacheReflectionFields();

        if (showDebugInfo)
        {
            Debug.Log("=== WALL DASH JUMP INICIADO ===");
        }
    }

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
            isRotationLockedField = playerMovement.GetType().GetField("isRotationLocked", bindingFlags);
            hasReflectionCached = true;
        }
        catch { }
    }

    private void Update()
    {
        if (!enableWallDashJump || playerMovement == null)
            return;

        // Atualiza o timer de bloqueio de Air Dash
        UpdateAirDashLockTimer();

        if (wallDashCooldownTimer > 0)
            wallDashCooldownTimer -= Time.deltaTime;

        if (IsWallInteracting() && Input.GetKeyDown(cancelKey))
        {
            if (showDebugInfo)
                Debug.Log("Jogador cancelou a interação com a parede");
            PerformWallJump();
        }

        if (wallDashCooldownTimer <= 0 && !IsWallInteracting() && IsPlayerDashing())
        {
            if (DetectWallDuringDash())
            {
                ExecuteWallDashJump();
                wallDashCooldownTimer = wallDashCooldown;
            }
        }

        if (isWallClimbing)
        {
            UpdateWallClimb();
        }
        else if (isWallSticking)
        {
            UpdateWallStick();
        }
        else if (isWallSliding)
        {
            UpdateWallSlide();
        }
    }

    // ================================================================
    // SISTEMA DE BLOQUEIO DE AIR DASH APÓS PULOS DE PAREDE
    // ================================================================

    /// <summary>
    /// Atualiza o timer de bloqueio de Air Dash. Enquanto o timer estiver ativo,
    /// o Air Dash não poderá ser usado. O PlayerMovement deve verificar
    /// IsAirDashLocked() ANTES de permitir a ativação do dash.
    /// </summary>
    private void UpdateAirDashLockTimer()
    {
        if (airDashLockTimer > 0f)
        {
            airDashLockTimer -= Time.deltaTime;

            if (airDashLockTimer <= 0f)
            {
                airDashLockTimer = 0f;
                if (showDebugInfo)
                    Debug.Log("Bloqueio de Air Dash removido.");
            }
        }
    }

    /// <summary>
    /// Aplica o bloqueio de Air Dash por um determinado tempo.
    /// Chamado internamente após o pulo automático e o pulo de cancelamento.
    /// </summary>
    private void ApplyAirDashLock(float duration)
    {
        if (duration <= 0f)
            return;

        airDashLockTimer = duration;

        if (showDebugInfo)
            Debug.Log($"Air Dash bloqueado por {duration}s.");
    }

    /// <summary>
    /// Verifica se o Air Dash está atualmente bloqueado pelo sistema de parede.
    /// Chamado pelo PlayerMovement.HandleAirDash() para impedir a ativação.
    /// </summary>
    public bool IsAirDashLocked()
    {
        return airDashLockTimer > 0f;
    }

    /// <summary>
    /// Retorna o tempo restante do bloqueio de Air Dash.
    /// </summary>
    public float GetAirDashLockRemainingTime()
    {
        return airDashLockTimer;
    }

    // ================================================================
    // MÉTODOS DE ESTADO
    // ================================================================

    private void UpdateWallClimb()
    {
        float elapsedTime = wallClimbDuration - currentWallClimbTimer;
        currentWallClimbTimer -= Time.deltaTime;

        if (characterController != null && characterController.isGrounded)
        {
            if (showDebugInfo) Debug.Log("Escalada encerrada - Jogador no chão");
            ExitAllWallStates();
            return;
        }

        if (elapsedTime > minClimbTimeBeforeAutoJump && IsStillNearWall())
        {
            if (!IsWallAtHeight(1.2f)) 
            {
                if (showDebugInfo) Debug.Log("Topo do objeto alcançado! Executando pulo automático.");
                PerformAutoForwardJump();
                return;
            }
        }

        if (!IsStillNearWall())
        {
            if (showDebugInfo) Debug.Log("Escalada encerrada - Parede não encontrada mais");
            ExitAllWallStates();
            return;
        }

        if (currentWallClimbTimer <= 0f)
        {
            isWallClimbing = false;
            isWallSticking = true;
            currentWallStickTimer = wallStickDuration;
            if (showDebugInfo) Debug.Log("Escalada terminou. Iniciando 'stick' na parede.");
            
            if (animator != null)
            {
                animator.CrossFade("WallStick", 0.15f); // Transição suave para a animação de WallStick
                animator.SetBool("IsWallClimbing", false);
                animator.SetBool("IsWallSticking", true);
                animator.SetBool("IsWallSliding", false);
            }
            return;
        }

        playerMovement.moveDirection = Vector3.up * wallClimbSpeed;
        playerMovement.currentSpeed = 0f;

        playerMovement.doubleJumpCharges = playerMovement.maxDoubleJumpCharges;
        playerMovement.airDashCharges = playerMovement.maxAirDashCharges;

        // ✅ NOVO: Aplica a rotação no eixo Y durante o climb
        ApplyClimbRotationY();

        // A animação de Climb já foi iniciada no ExecuteWallDashJump
        // Mantemos os Bools para compatibilidade com a máquina de estados, se houver
        if (animator != null)
        {
            // Se já estamos em WallClimb, não precisamos de CrossFade novamente aqui
            // Apenas garantimos que os Bools estão corretos
            animator.SetBool("IsWallClimbing", true);
            animator.SetBool("IsWallSticking", false);
            animator.SetBool("IsWallSliding", false);
        }
    }

    // ================================================================
    // SISTEMA DE ROTAÇÃO DA ANIMAÇÃO NO EIXO Y DURANTE O CLIMB
    // ================================================================

    /// <summary>
    /// Aplica a rotação no eixo Y à animação durante o climb (escalada na parede).
    /// Isso permite que a animação de escalada fique orientada corretamente
    /// em relação à parede, independentemente da direção de entrada do jogador.
    /// </summary>
    private void ApplyClimbRotationY()
    {
        if (currentWallNormal == Vector3.zero || Mathf.Abs(wallClimbRotationY) < 0.01f)
            return;

        // Bloqueia o sistema de rotação do PlayerMovement para não conflitar
        SetPlayerRotationLocked(true);



        // A rotação base já foi definida instantaneamente em ExecuteWallDashJump.
        // Aqui, apenas aplicamos a rotação Y adicional, se houver, interpolando a partir da rotação atual.
        Quaternion targetRotation = originalRotation * Quaternion.Euler(0f, wallClimbRotationY, 0f);

        cachedTransform.rotation = Quaternion.Slerp(
            cachedTransform.rotation,
            targetRotation,
            Time.deltaTime * wallRotationSpeed
        );
    }

    private void UpdateWallStick()
    {
        currentWallStickTimer -= Time.deltaTime;

        if (characterController != null && characterController.isGrounded)
        {
            if (showDebugInfo) Debug.Log("Stick encerrado - Jogador no chão");
            ExitAllWallStates();
            return;
        }

        if (!IsStillNearWall())
        {
            if (showDebugInfo) Debug.Log("Stick encerrado - Parede não encontrada mais");
            ExitAllWallStates();
            return;
        }

        if (currentWallStickTimer <= 0f)
        {
            isWallSticking = false;
            isWallSliding = true;
            // ✅ NOVO: Inicializa o timer do slide ao entrar no estado
            currentWallSlideTimer = wallSlideDuration;
            if (showDebugInfo) Debug.Log("'Stick' terminou. Iniciando deslizamento.");
            
            if (animator != null)
            {
                animator.CrossFade("WallSlide", 0.15f); // Transição suave para a animação de WallSlide
                animator.SetBool("IsWallClimbing", false);
                animator.SetBool("IsWallSticking", false);
                animator.SetBool("IsWallSliding", true);
            }
            return;
        }

        playerMovement.moveDirection = Vector3.zero;
        playerMovement.currentSpeed = 0f;

        // ✅ ROTAÇÃO X: Aplica a rotação no eixo X durante o stick
        ApplyWallAnimationRotation();

        if (animator != null)
        {
            animator.SetBool("IsWallClimbing", false);
            animator.SetBool("IsWallSticking", true);
            animator.SetBool("IsWallSliding", false);
        }
    }

    private void UpdateWallSlide()
    {
        if (!IsStillNearWall())
        {
            if (showDebugInfo) Debug.Log("Deslizamento encerrado - Parede não encontrada mais");
            ExitAllWallStates();
            return;
        }

        if (characterController != null && characterController.isGrounded)
        {
            if (showDebugInfo) Debug.Log("Deslizamento encerrado - Jogador no chão");
            ExitAllWallStates();
            return;
        }

        // ✅ NOVO: Conta o timer do slide
        if (wallSlideDuration > 0f)
        {
            currentWallSlideTimer -= Time.deltaTime;

            // ✅ Quando o timer do slide acabar, executa o pulo de cancelamento automaticamente
            if (currentWallSlideTimer <= 0f)
            {
                if (showDebugInfo)
                    Debug.Log("Tempo do slide esgotado! Executando pulo de cancelamento automático.");
                PerformWallJump();
                return;
            }
        }

        playerMovement.moveDirection = Vector3.down * wallSlideSpeed;
        playerMovement.currentSpeed = 0f;

        // ✅ ROTAÇÃO X: Aplica a rotação no eixo X durante o slide
        ApplyWallAnimationRotation();
    }

    // ================================================================
    // SISTEMA DE ROTAÇÃO DA ANIMAÇÃO NO EIXO X
    // ================================================================

    /// <summary>
    /// Aplica a rotação no eixo X à animação durante wall stick e wall slide.
    /// Isso faz a animação ficar perpendicular à parede (como se o personagem
    /// estivesse de lado grudado nela).
    /// </summary>
    private void ApplyWallAnimationRotation()
    {
        if (currentWallNormal == Vector3.zero)
            return;

        // ✅ Bloqueia o sistema de rotação do PlayerMovement para não conflitar
        SetPlayerRotationLocked(true);

            if (useTransformRotation)
            {
                if (!isWallRotated)
                {
                    originalRotation = cachedTransform.rotation;
                    isWallRotated = true;

                    // ✅ CORREÇÃO: Calcula a rotação para encarar a parede
                    Vector3 lookDir = -currentWallNormal;
                    lookDir.y = 0;
                    
                    if (lookDir.sqrMagnitude > 0.001f)
                    {
                        targetWallRotation = Quaternion.LookRotation(lookDir, Vector3.up);
                    }
                    else
                    {
                        targetWallRotation = cachedTransform.rotation;
                    }
                }

                cachedTransform.rotation = Quaternion.Slerp(
                    cachedTransform.rotation, 
                    targetWallRotation, 
                    Time.deltaTime * wallRotationSpeed
                );
            }
        else
        {
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                Quaternion xRotation = Quaternion.Euler(wallAnimationRotationX, 0, 0);
                
                if (!isWallRotated)
                {
                    originalRotation = cachedTransform.rotation;
                    isWallRotated = true;
                }

                cachedTransform.rotation = Quaternion.Slerp(
                    cachedTransform.rotation,
                    originalRotation * xRotation,
                    Time.deltaTime * wallRotationSpeed
                );
            }
        }
    }

    /// <summary>
    /// Bloqueia ou desbloqueia a rotação do PlayerMovement para evitar conflito
    /// </summary>
    private void SetPlayerRotationLocked(bool locked)
    {
        if (playerMovement == null || isRotationLockedField == null)
            return;

        try
        {
            isRotationLockedField.SetValue(playerMovement, locked);
            if (locked)
            {
                var timerField = playerMovement.GetType().GetField("rotationLockTimer", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (timerField != null)
                    timerField.SetValue(playerMovement, 999f);
            }
        }
        catch { }
    }

    // ================================================================
    // PULOS E CANCELAMENTO
    // ================================================================

    /// <summary>
    /// Executa o pulo da parede (cancelamento manual com Espaço ou automático pelo timer).
    /// Impulso para trás.
    /// </summary>
    private void PerformWallJump()
    {
        Vector3 backwardDir = currentWallNormal != Vector3.zero ? currentWallNormal : -cachedTransform.forward;
        
        Vector3 jumpDir = backwardDir * backwardJumpForce + Vector3.up * backwardJumpUpForce;
        
        playerMovement.moveDirection = jumpDir;
        playerMovement.isJumping = true;
        playerMovement.isFalling = false;
        playerMovement.currentSpeed = backwardJumpForce;

        if (backwardDir != Vector3.zero)
        {
            cachedTransform.rotation = Quaternion.LookRotation(backwardDir);
        }

        // Restaura a rotação original e desbloqueia
        SetPlayerRotationLocked(false);
        isWallRotated = false;

        ExitAllWallStates();

        playerMovement.SetWallCancelLock();

        // ✅ APLICA BLOQUEIO DE AIR DASH APÓS PULO DE CANCELAMENTO
        ApplyAirDashLock(airDashLockAfterWallCancel);

        if (animator != null)
        {
            animator.SetBool("IsJumping", true);
            animator.SetTrigger("WallCancelJump");
        }

        if (showDebugInfo)
            Debug.Log($"Pulo de CANCELAMENTO executado com força {backwardJumpForce}! Direção: {jumpDir}");
    }

    public bool IsWallInteracting()
    {
        return isWallClimbing || isWallSticking || isWallSliding;
    }

    private bool IsStillNearWall()
    {
        if (currentWallNormal == Vector3.zero)
            return false;

        Vector3 wallDirection = -currentWallNormal;
        return Physics.Raycast(cachedTransform.position, wallDirection, out RaycastHit hit, wallRecheckDistance, wallLayer);
    }

    private bool IsWallAtHeight(float heightOffset)
    {
        if (currentWallNormal == Vector3.zero) return false;

        Vector3 rayOrigin = cachedTransform.position + Vector3.up * heightOffset;
        Vector3 wallDirection = -currentWallNormal;
        
        bool hit = Physics.Raycast(rayOrigin, wallDirection, wallRecheckDistance, wallLayer);
        
        if (showDebugInfo)
            Debug.DrawRay(rayOrigin, wallDirection * wallRecheckDistance, hit ? Color.green : Color.yellow, 0.1f);
            
        return hit;
    }

    private void PerformAutoForwardJump()
    {
        Vector3 forwardDir = -currentWallNormal;
        Vector3 jumpDir = forwardDir * autoJumpForwardForce + Vector3.up * autoJumpUpForce;
        
        playerMovement.moveDirection = jumpDir;
        playerMovement.isJumping = true;
        playerMovement.isFalling = false;
        playerMovement.currentSpeed = autoJumpForwardForce;

        // ✅ NOVO: Faz o personagem olhar para a direção do pulo
        if (forwardDir != Vector3.zero)
        {
            cachedTransform.rotation = Quaternion.LookRotation(forwardDir);
        }

        // Restaura a rotação original e desbloqueia
        SetPlayerRotationLocked(false);
        isWallRotated = false;

        ExitAllWallStates();

        // ✅ APLICA BLOQUEIO DE AIR DASH APÓS PULO AUTOMÁTICO
        ApplyAirDashLock(airDashLockAfterAutoJump);

        if (animator != null)
        {
            animator.SetBool("IsJumping", true);
            animator.SetTrigger("WallDashJump");
        }

        if (showDebugInfo)
            Debug.Log($"Pulo automático do topo executado! Direção: {jumpDir}");
    }

    private void ExitAllWallStates()
    {
        isWallClimbing = false;
        isWallSticking = false;
        isWallSliding = false;
        isWallDashing = false;
        currentWallNormal = Vector3.zero;
        wallContactPoint = Vector3.zero;
        hasTargetWallRotation = false;
        currentWallSlideTimer = 0f;

        // ✅ Desbloqueia a rotação do PlayerMovement e restaura a rotação original
        SetPlayerRotationLocked(false);
        isWallRotated = false;

        if (animator != null)
        {
            // Resetar todos os parâmetros de animação de parede
            animator.SetBool("IsWallClimbing", false);
            animator.SetBool("IsWallSticking", false);
            animator.SetBool("IsWallSliding", false);
            // Opcional: CrossFade para um estado padrão, se houver um (ex: "Idle")
            // animator.CrossFade("Idle", 0.15f);
        }
    }

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

    private bool DetectWallDuringDash()
    {
        Vector3 rayOrigin = cachedTransform.position;
        
        Vector3 forward = cachedTransform.forward;
        Vector3 right = cachedTransform.right;

        rayDirections[0] = forward;
        rayDirections[1] = right;
        rayDirections[2] = -right;
        rayDirections[3] = (forward + right).normalized;
        rayDirections[4] = (forward - right).normalized;

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

    private void ExecuteWallDashJump()
    {
        if (isWallDashing)
            return;

        isWallDashing = false;
        isWallSticking = false;
        isWallSliding = false;
        isWallClimbing = false;
        hasTargetWallRotation = false;
        isWallRotated = false;
        currentWallSlideTimer = 0f;

        if (showDebugInfo)
            Debug.Log("WALL DASH JUMP ACIONADO");

        if (dynamicCamera != null)
        {
            dynamicCamera.TriggerWallDashShake();
            if (showDebugInfo) Debug.Log("Camera Shake disparado!");
        }

        StopPlayerDash();
        DisableAirDashParticles();

        // ✅ ATUALIZAÇÃO: Define a normal da parede ANTES de calcular a rotação
        currentWallNormal = raycastHit.normal;
        wallContactPoint = raycastHit.point;

        // ✅ CORREÇÃO: Faz o personagem olhar DIRETAMENTE para a parede (A Hat in Time style)
        // Em vez de ficar paralelo, ele deve ficar de frente para a superfície que está subindo.
        Vector3 lookDir = -currentWallNormal;
        lookDir.y = 0; // Mantém a rotação horizontal para o CharacterController
        
        if (lookDir.sqrMagnitude > 0.001f)
        {
            Quaternion instantTargetRotation = Quaternion.LookRotation(lookDir, Vector3.up);
            cachedTransform.rotation = instantTargetRotation;
        }

        // Salva a rotação original (agora já alinhada) para possíveis interpolações futuras
        originalRotation = cachedTransform.rotation;

        isWallClimbing = true;
        currentWallClimbTimer = wallClimbDuration;

        if (showDebugInfo)
            Debug.Log($"WALL DASH JUMP: Iniciando escalada. Normal: {currentWallNormal}, Ponto: {wallContactPoint}");

        // A animação de Climb já foi iniciada no ExecuteWallDashJump
        // Mantemos os Bools para compatibilidade com a máquina de estados, se houver
        if (animator != null)
        {
            animator.CrossFade("WallClimb", 0.1f); // Inicia a animação de Climb com crossfade
            animator.SetBool("IsWallClimbing", true);
            animator.SetBool("IsWallSticking", false);
            animator.SetBool("IsWallSliding", false);
        }

        if (enableParticles && jumpParticles != null)
        {
            jumpParticles.Play();
        }
    }

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

            if (showDebugInfo) Debug.Log("Dash parado");
        }
        catch (System.Exception e)
        {
            if (showDebugInfo)
                Debug.LogWarning("Erro ao parar dash: " + e.Message);
        }
    }

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
                if (showDebugInfo) Debug.Log("Partículas de Air Dash paradas");
            }
        }
        catch (System.Exception e)
        {
            if (showDebugInfo)
                Debug.LogWarning("Erro ao desativar partículas: " + e.Message);
        }
    }

    // Métodos públicos
    public void SetEnabled(bool enabled) => enableWallDashJump = enabled;
    public bool IsWallDashing() => isWallClimbing || isWallSticking || isWallSliding;

    /// <summary>
    /// Chamado externamente pelo PlayerMovement para ativar o bloqueio de Air Dash.
    /// Útil quando o PlayerMovement precisa notificar o WallDashJump sobre o bloqueio.
    /// </summary>
    public void SetAirDashLock(float duration)
    {
        ApplyAirDashLock(duration);
    }
}
