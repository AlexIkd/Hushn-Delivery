using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement_FrontiersStyle : MonoBehaviour
{
    // Referência ao sistema de ranking de estilo
    private StyleRankSystem styleRankSystem;
    [Header("Ground Slide Settings")]
    [SerializeField] private float groundSlideHeight = 0.8f;
    [SerializeField] private float groundSlideMinSpeed = 3f;
    [SerializeField] private float minGroundSlideDuration = 0.5f; // Duração mínima obrigatória do slide
    [SerializeField] private float maxGroundSlideDuration = 1.0f; // Duração total do slide
    [SerializeField] private float groundSlideTransitionSpeed = 10f;
    [SerializeField] private float maxGroundSlideTurnAngle = 35f; // Limite de 35 graus
    [SerializeField] private float groundSlideCooldown = 0.5f; // Cooldown para usar o slide novamente
    [SerializeField] private ParticleSystem groundSlideParticles;
    private float groundSlideTimer = 0f;
    private float groundSlideCooldownTimer = 0f;
    private float groundSlideLockedSpeed = 0f;
    private Vector3 groundSlideInitialDirection; // Direção inicial do slide no chão
    private float originalHeight;
    private float originalCenterY;
    private bool isGroundSliding = false;
    private float currentColliderHeight;
    [Header("Configurações de Velocidade")]
    [SerializeField] public float maxSpeed = 15f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float airAcceleration = 5f; // Taxa de aceleração reduzida no ar
    [SerializeField] private float deceleration = 15f;
    [SerializeField] private float turnSpeed = 500f;
    [SerializeField] private float rotationSmoothing = 10f; // Controla a fluidez da curva

    [Header("Quick Turn")]
    [SerializeField] private float quickTurnThreshold = 5.0f;
    [SerializeField] private float quickTurnAngle = 165f;
    [SerializeField] private float quickTurnDeceleration = 8f;
    [SerializeField] private float quickTurnMinSpeedMultiplier = 0.4f;
    [SerializeField] private KeyCode quickTurnKey = KeyCode.Q; // Tecla dedicada para Quick Turn
    [SerializeField] private float quickTurnCooldown = 1.0f; // Cooldown entre usos do Quick Turn
    private float quickTurnCooldownTimer = 0f;

    [Header("Desaceleração Brusca")]
    [SerializeField] private float sharpDecelerationOnReverse = 30f; // Fator de desaceleração ao inverter direção
    [SerializeField] private float reverseDirectionAngleThreshold = 135f; // Ângulo para considerar inversão de direção (ex: 135 graus)

    [Header("Skid Settings")]
    [Tooltip("Velocidade de frenagem durante o skid. Valores maiores param o personagem mais rápido.")]
    [SerializeField] private float skidBrakeSpeed = 30f;
    [Tooltip("Velocidade mínima que o personagem precisa ter para que o skid possa ser ativado.")]
    [SerializeField] private float skidMinActivationSpeed = 2f;
    [Tooltip("Tempo que o jogador fica impossibilitado de se mover após o skid terminar.")]
    [SerializeField] private float skidLockDuration = 0.3f;
    private float skidLockTimer = 0f;

    [Header("Configurações de Salto e Gravidade")]
    [SerializeField] public float jumpForce = 8f;
    [SerializeField] public float gravity = 20f;

    [Header("Stomp (Queda Rápida)")]
    [SerializeField] private float stompForce = 30f;
    [SerializeField] private float stompMinHeight = 2f;
    [SerializeField] private float wallJumpOutwardForce = 12f;
    [SerializeField] private float wallJumpForwardMomentum = 0.75f;
    [SerializeField] private KeyCode stompKey = KeyCode.LeftControl;
    [SerializeField] private ParticleSystem stompParticles;
    [SerializeField] private float stompCooldownAfterDoubleJump = 0.5f; // Cooldown após pulo duplo
    private float stompCooldownTimer = 0f;
    private bool isStomping = false;

    // Métodos públicos para cancelamento de estados
    public void CancelWallRun() { if (isWallRunning) ExitWallRun(); }
    public void CancelGlide() { if (isGliding) StopGlide(); }
    public void CancelAirDash() { if (isDashing) StopAirDash(); }
    public void CancelStomp() { if (isStomping) isStomping = false; if (animator != null) animator.SetBool("IsStomping", false); if (isGroundSliding) StopGroundSlide(); } // Stomp não tem um método Stop dedicado, então resetamos a flag e o animator diretamente.

    public void CancelGroundSlideImmediate()
    {
        if (isGroundSliding)
        {
            StopGroundSlide();
            // Reseta o colisor instantaneamente para o tamanho normal
            controller.height = originalHeight;
            controller.center = new Vector3(controller.center.x, originalCenterY, controller.center.z);
            currentColliderHeight = originalHeight;
        }
    }

    [Header("Air Movement")]
    [SerializeField] private float airDashForce = 15f;
    [SerializeField] private float airDashDuration = 0.1f;
    [SerializeField] public int maxDoubleJumpCharges = 1;
    public int doubleJumpCharges = 0;
    [SerializeField] public int maxAirDashCharges = 1;
    public int airDashCharges = 0;
    private bool isDashing = false;
    private float airDashTimer = 0f;

    [Header("Air Dash Cooldowns")]
    [SerializeField] private float airDashCooldownAfterDoubleJump = 0.3f; // Cooldown após pulo duplo
    [SerializeField] private float airDashCooldownAfterAirTrick = 0.3f;   // Cooldown após air trick
    [SerializeField] private float minAirTrickDuration = 0.5f;           // Duração mínima do air trick antes de iniciar o cooldown do dash
    private float airDashCooldownTimer = 0f;

    [Header("Idle Prolongado")]
    [SerializeField] private float prolongedIdleTime = 15f;
    private float idleTimer = 0f;
    private bool isProlongedIdle = false;

    [Header("Trail Effect Integration")]
    [SerializeField] private SpeedTrailEffect wallRunTrailEffect;

    [Header("Configurações de Wall Run")]
    [SerializeField] private float wallRunGravity = 2f;
    [SerializeField] private float wallRunSpeedIncrease = 8f;
    [SerializeField] private float wallRunSpeedDecrease = 4f;
    [SerializeField] private float wallRunDuration = 2.5f;
    [SerializeField] private float minDistanceToGroundForWallRun = 1.0f;
    [SerializeField] private LayerMask wallMask;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float wallDistance = 1.0f;
    [SerializeField] private LayerMask railLayer; // Nova Layer para Rails
    [SerializeField] private float maxWallRunAngle = 45f;

    [Header("Air Trick Settings")]
    [SerializeField] private float airTrickRotationLockTime = 1f;
    [SerializeField] private float minHeightForAirTrick = 3f;
    [SerializeField] private float airTrickCooldown = 0.5f;
    private bool isRotationLocked = false;
    private float rotationLockTimer = 0f;
    private float airTrickCooldownTimer = 0f;
    private Quaternion lockedRotation;

    private float wallRunTimer = 0f;

    [Header("Particulas de Movimento")]
    [SerializeField] private ParticleSystem airDashParticles;
    [SerializeField] private bool enableAirDashParticles = true;
    [SerializeField] private ParticleSystem jumpParticles;
    [SerializeField] private bool enableJumpParticles = true;
    [SerializeField] private ParticleSystem doubleJumpParticles;
    [SerializeField] private bool enableDoubleJumpParticles = true;
    [SerializeField] private ParticleSystem airTrickParticles;
    [SerializeField] private bool enableAirTrickParticles = true;
    [SerializeField] private ParticleSystem wallRunLeftParticles;
    [SerializeField] private ParticleSystem wallRunRightParticles;
    [SerializeField] private bool enableWallRunParticles = true;

    [Header("Glide Settings")]
    [SerializeField] private bool canGlide = true;
    [SerializeField] private float glideGravity = 0.8f; // Gravidade mais suave para um glide prolongado
    [SerializeField] public float glideForwardSpeed = 25f; // Velocidade frontal aumentada para manter o momentum
    [SerializeField] private float glideTurnSpeed = 8f; // Aumenta a responsividade da curva durante o glide
    [SerializeField] private float glideLiftForce = 2.0f; // Força de sustentação para prolongar o tempo no ar
    [SerializeField] private float glideEntryBoost = 8f; // Impulso inicial mais forte para uma entrada suave
    [SerializeField] private float maxGlideFallSpeed = -1.5f; // Queda ainda mais lenta para simular melhor o glide
    [SerializeField] private float glideDeceleration = 5f; // Taxa de desaceleração do glide
    [SerializeField] private float minGlideSpeed = 10f; // Velocidade mínima do glide
    [SerializeField] private float glideGraceTime = 0.15f; // Tempo de carência para não desativar imediatamente
    [SerializeField] private float minGlideDuration = 0.5f; // Duração mínima do glide antes de poder ser cancelado

    [SerializeField] private float minHeightForGlide = 5f; // Altura mínima para ativar o glide
    
    [SerializeField] private ParticleSystem glideParticles;
    [SerializeField] private float glideCooldown = 3.0f;
    private float glideCooldownTimer = 0f;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false; // ✅ DESATIVADO por padrão

    [Header("Recuperação após Wall Run")]
    [SerializeField] private float wallRunRecoveryTime = 0.4f;
    private bool recoveringFromWallRun = false;
    private float wallRunRecoveryTimer = 0f;

    [Header("Bar Cooldown")]
    [SerializeField] private float barWallRunCooldown = 0.5f;
    private float barWallRunCooldownTimer = 0f;

    // Estados internos
    [HideInInspector] public bool IsGliding => isGliding;
    [HideInInspector] public bool IsGrabbingBar { get; set; } = false; // Nova propriedade para indicar se o jogador está agarrado à barra
    [HideInInspector] public bool isSwinging = false; // NOVO: Flag para o sistema de balanço
    public bool IsInNarrowPassage { get; set; } = false;
    public bool IsWallRunning => isWallRunning; // Propriedade pública para verificar se está em wall run
    [HideInInspector] public bool OnLeftWall => onLeftWall;
    [HideInInspector] public bool OnRightWall => onRightWall;
    private bool isWallRunning = false;
    private bool hasWallRun = false;
    [HideInInspector] public bool isGrounded = false;
    private bool onLeftWall = false;
    private bool onRightWall = false;
    private Vector3 wallNormal;
    private Vector3 lastWallNormal;
    private bool isGliding = false;
    private bool glideButtonHeld = false;
    private float glideActiveTimer = 0f; // Tempo que o glide está ativo

    private float glideGraceTimer = 0f;
    private float currentGlideSpeed; // Velocidade atual do glide

    // Componentes
    private CharacterController controller;
    private Animator animator;
    private Transform cameraTransform;
    private Transform cachedTransform;
    private PlayerRailRide_SonicStyle_Spline railRide;
    private DynamicFollowCamera followCamera;
    private bool wasGrindingLastFrame = false; // Rastreia o estado anterior para detectar saída do rail

    private void ExecuteRailJump()
    {
        // Avisa o sistema de Rail que o jogador quer sair (pular)
        if (railRide != null)
        {
            railRide.ExitRailForced();
        }

        // Força o estado de pulo padrão
        isGrounded = false; // Garante que o estado de chão seja limpo no pulo
        moveDirection.y = jumpForce;
        isJumping = true;
        isFalling = false;
        canJumpAfterGrind = false;

        // Reseta as cargas para permitir pulo duplo/dash após o pulo do rail
        doubleJumpCharges = maxDoubleJumpCharges;
        airDashCharges = maxAirDashCharges;
        wasGrindingLastFrame = false; // Garante que a lógica de saída do Update não rode duas vezes

        if (animator != null)
        {
            animator.SetBool("IsJumping", true);
            animator.SetBool("IsGrounded", false);
            animator.SetTrigger("Jump"); // Se houver um trigger específico
        }

        if (enableJumpParticles && jumpParticles != null)
        {
            StartJumpParticles();
        }

        if (showDebugInfo) Debug.Log("🚀 Pulo executado diretamente do Rail!");
    }

    // ✅ NOVO: Referência ao WallDashJump para bloquear Wall Run
    private WallDashJump wallDashJump;

    // Movimento
    public Vector3 moveDirection = Vector3.zero;
    private Vector3 lastMoveDirection = Vector3.zero;
    // Controla se existe input direcional real neste frame.
    // Isso impede que a rotação continue sendo recalculada durante a desaceleração ao soltar o direcional.
    private bool hasMovementInput = false;
    public float currentSpeed;
    private float originalSpeedBeforeQuickTurn;
    private bool isQuickTurning = false; // Novo estado para Quick Turn
    private bool isSkidding = false; // Estado para frenagem brusca (skid)
    private float minSpeedDuringQuickTurn;


    // Controle de animação
    [HideInInspector] public bool animatorBusy = false;
    [HideInInspector] public Quaternion targetRotation;

    // Variáveis para controle de estado de animação de pulo
    public bool isJumping = false;
    private bool isFalling = false;
    private bool canJumpAfterGrind = false; // Permite um pulo normal ao sair do rail mesmo no ar

    // Velocidade externa
    private Vector3 externalVelocity = Vector3.zero;

    // ✅ CACHE - Evita alocações repetidas
    private Vector3 inputVector;
    private Vector3 cameraForward;
    private Vector3 cameraRight;
    private Vector3 desiredMoveDirection;
    private Vector3 desiredMove;
    private Vector3 horizontalMove;
    private RaycastHit raycastHit;
    private const float GROUND_CHECK_RADIUS = 0.1f;
    private const float RAYCAST_MAX_DISTANCE = float.MaxValue;

    // ✅ OTIMIZAÇÕES - CACHE DE ANIMATOR
    private float cachedSpeed = -1f;
    private bool cachedIsGrounded = false;
    private bool cachedIsWallRunning = false;
    private bool cachedIsJumping = false;
    private bool cachedIsFalling = false;
    private bool cachedIsStomping = false;
    private bool cachedOnLeftWall = false;
    private bool cachedOnRightWall = false;
    private bool cachedProlongedIdle = false;
    private bool cachedIsGroundSliding = false;
    private bool cachedIsGliding = false;
    private bool cachedIsSkidding = false;
    private bool cachedIsSwinging = false; // NOVO: Cache para o Animator de balanço
    private const float SPEED_CHANGE_THRESHOLD = 0.01f;

    // ✅ OTIMIZAÇÕES - CONTROLE DE RAYCASTS
    private int raycastFrameCounter = 0;
    private const int RAYCAST_CHECK_INTERVAL = 2;

    // ✅ OTIMIZAÇÕES - CACHE DE ROTAÇÃO
    private Quaternion cachedTargetRotation = Quaternion.identity;
    private float rotationUpdateTimer = 0f;
    private const float ROTATION_UPDATE_INTERVAL = 0.05f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        cachedTransform = transform;
        cameraTransform = Camera.main ? Camera.main.transform : null;
        railRide = GetComponent<PlayerRailRide_SonicStyle_Spline>();

        if (animator == null)
            Debug.LogWarning("Animator não encontrado no PlayerMovement_FrontiersStyle!");

        if (controller == null)
            Debug.LogError("CharacterController não encontrado no PlayerMovement_FrontiersStyle!");

        // ✅ OTIMIZADO: FindObjectOfType uma vez
        styleRankSystem = FindObjectOfType<StyleRankSystem>();
        if (styleRankSystem == null && showDebugInfo)
        {
            Debug.LogWarning("StyleRankSystem não encontrado na cena.");
        }
        
        // ✅ NOVO: Inicializa referência ao WallDashJump
        wallDashJump = GetComponent<WallDashJump>();
        if (wallDashJump == null && showDebugInfo)
        {
            Debug.LogWarning("WallDashJump não encontrado no mesmo GameObject.");
        }

        // Validação do groundCheck
        if (groundCheck == null)
        {
            Debug.LogError("groundCheck não está atribuído!");

            GameObject groundCheckObj = new GameObject("GroundCheck");
            groundCheckObj.transform.SetParent(cachedTransform);
            groundCheckObj.transform.localPosition = new Vector3(0, -controller.height * 0.5f, 0);
            groundCheck = groundCheckObj.transform;

            if (showDebugInfo)
                Debug.Log("GroundCheck criado automaticamente.");
        }

        originalHeight = controller.height;
        originalCenterY = controller.center.y;
        currentColliderHeight = originalHeight;

        airDashCharges = maxAirDashCharges;
        doubleJumpCharges = maxDoubleJumpCharges;

        // Garante que as partículas de glide não comecem tocando
        if (glideParticles != null)
        {
            glideParticles.Stop();
        }
    }

    void Update()
    {
        // ✅ NOVO: Detecta saída do rail para resetar pulos
        if (railRide != null)
        {
            if (railRide.isGrinding)
            {
                // Executa apenas no frame de entrada no rail
                if (!wasGrindingLastFrame)
                {
                    if (isGliding) StopGlide();
                    if (isDashing) StopAirDash();
                    
                    // Força a limpeza das partículas de dash ao entrar no rail
                    StopAirDashParticles(true);
                }
                
                // ✅ NOVO: Garante que o estado de chão seja limpo imediatamente ao entrar/estar no rail
                // Isso evita que o pulo padrão seja ignorado por causa de um isGrounded "sujo" vindo do frame anterior
                isGrounded = false;
                
                wasGrindingLastFrame = true;

        // ✅ NOVO: Pulo direto do Rail
                if (Input.GetButtonDown("Jump") && !animatorBusy)
                {
                    // Interrompe o idle prolongado ao pular do rail
                    if (isProlongedIdle)
                    {
                        isProlongedIdle = false;
                        idleTimer = 0f;
                        if (animator != null) animator.SetBool("ProlongedIdle", false);
                    }
                    ExecuteRailJump();
                }
                return;
            }

            else if (wasGrindingLastFrame)
            {
                // Acabou de sair do rail: reseta as cargas e permite pulo normal
                canJumpAfterGrind = true;
                doubleJumpCharges = maxDoubleJumpCharges;
                originalHeight = controller.height;
                originalCenterY = controller.center.y;
                currentColliderHeight = originalHeight;
                airDashCharges = maxAirDashCharges;
                wasGrindingLastFrame = false;
                if (showDebugInfo) Debug.Log("✅ Saiu do Rail: Pulos resetados!");
            }
        }

        // Bloqueia o movimento normal se estiver agarrado à barra ou em passagem estreita
        if (IsGrabbingBar || IsInNarrowPassage)
        {
            return;
        }

        // Timer de bloqueio após lançamento da barra
        if (barLaunchLockTimer > 0)
        {
            barLaunchLockTimer -= Time.deltaTime;
        }

        // 1. Pré-processamento e Verificações de Estado
        HandleGroundSlideInput();
        UpdateColliderHeight();
        CheckGround();
        CheckWallRun();
        HandleStomp();
        HandleAirDash();
        HandleAirInput();
        UpdateRotationLock();
        UpdateAirTrickCooldown();
        HandleProlongedIdle();

        // Atualiza o timer de carência do glide
        if (glideGraceTimer > 0) glideGraceTimer -= Time.deltaTime;
        if (isGliding) glideActiveTimer += Time.deltaTime; // Incrementa o timer de duração do glide

        if (glideCooldownTimer > 0) glideCooldownTimer -= Time.deltaTime;
        if (stompCooldownTimer > 0) stompCooldownTimer -= Time.deltaTime;
        if (barWallRunCooldownTimer > 0) barWallRunCooldownTimer -= Time.deltaTime;
        if (airDashCooldownTimer > 0) airDashCooldownTimer -= Time.deltaTime;
        
        // IMPORTANTE: HandleGlide deve vir DEPOIS de processar o pulo duplo no ApplyGravity
        // mas antes de aplicar o movimento final.
        // No entanto, para capturar o GetButtonDown corretamente, vamos manter a ordem
        HandleGlide();

        // 2. Lógica de Recuperação
        if (recoveringFromWallRun)
        {
            wallRunRecoveryTimer -= Time.deltaTime;
            if (wallRunRecoveryTimer <= 0f)
            {
                recoveringFromWallRun = false;
                if (showDebugInfo)
                    Debug.Log("✅ Recuperação do Wall Run finalizada.");
            }
        }

        // 3. ✅ OTIMIZADO: Aplica velocidade externa
        if (externalVelocity.sqrMagnitude > 0.01f)
        {
            moveDirection += externalVelocity;
            externalVelocity = Vector3.Lerp(externalVelocity, Vector3.zero, Time.deltaTime * 5f);
        }

        // 4. MOVIMENTO
        // Lógica de movimento terrestre
        if (animatorBusy || isQuickTurning || isSkidding)
        {
            // Se estiver skidding, apenas aplica a gravidade e deixa a velocidade ser controlada pela lógica de skid
            if (isSkidding)
            {
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0, skidBrakeSpeed * Time.deltaTime);
                if (currentSpeed <= 0.5f) // Sai do skid quando a velocidade for muito baixa
                {
                    isSkidding = false;
                    skidLockTimer = skidLockDuration; // Inicia o tempo de espera para voltar a se mover
                }
                ApplyGravity();
            }
            else
            {
                ApplyQuickTurnDeceleration();
                ApplyGravity();
            }
        }
        else if (isWallRunning)
        {
            HandleWallRunInput();

            if (isWallRunning)
            {
                WallRunMovement();
            }
        }
        // NOVO: Prioriza o sistema de balanço se ativo
        else if (isSwinging)
        {
            // O PlayerSwingSystem já está manipulando moveDirection e currentSpeed.
            // Não aplique gravidade ou input de movimento normal aqui.
            // Apenas certifique-se de que o CharacterController.Move() seja chamado no final.
        }
        else if (isGliding)
        {
            GlideMovement();
        }
        else
        {
            HandleInputAndMovement();
            HandleRotation();
            ApplyGravity();

            if (isDashing)
            {
                AirDashMovement();
            }
        }

        // 5. Aplicação Final do Movimento Terrestre
        if (controller.enabled)
        {
            controller.Move(moveDirection * Time.deltaTime);
        }

        // ✅ OTIMIZADO: Mover UpdateAnimator para o final do Update
        // Isso garante que todas as variáveis de estado sejam atualizadas antes de serem passadas para o Animator.
        // Isso ajuda a evitar "flicadas" visuais causadas por estados inconsistentes entre o script e o Animator.
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
    }

    // ======================================================
    // MOVIMENTO NORMAL
    // ======================================================

    private void HandleInputAndMovement()
    {
        // Atualiza o timer de trava do skid
        if (skidLockTimer > 0)
        {
            skidLockTimer -= Time.deltaTime;
            // Se ainda estiver em lock, aplicamos apenas a desaceleração passiva e saímos
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0, deceleration * Time.deltaTime);
            moveDirection.x = lastMoveDirection.x * currentSpeed;
            moveDirection.z = lastMoveDirection.z * currentSpeed;
            return;
        }

        // Atualiza o cooldown do Quick Turn
        if (quickTurnCooldownTimer > 0) quickTurnCooldownTimer -= Time.deltaTime;

        if (recoveringFromWallRun || isDashing || barLaunchLockTimer > 0) return; // Bloqueia input se estiver em lock de lançamento

        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        // ✅ OTIMIZADO: Reutiliza Vector3 em cache
        inputVector.x = horizontalInput;
        inputVector.y = 0;
        inputVector.z = verticalInput;
                float inputMagnitude = inputVector.magnitude;
        hasMovementInput = inputMagnitude > 0.1f;

        if (cameraTransform != null)
        {
            cameraForward = cameraTransform.forward;
            cameraRight = cameraTransform.right;
            cameraForward.y = 0;
            cameraRight.y = 0;
            cameraForward.Normalize();
            cameraRight.Normalize();

            desiredMoveDirection = (cameraForward * verticalInput + cameraRight * horizontalInput).normalized;
        }
        // Garante que desiredMoveDirection seja calculada antes de ser usada na lógica de desaceleração
        if (desiredMoveDirection == Vector3.zero && inputVector.magnitude > 0.01f) // Fallback para quando a câmera não está disponível
        {
            desiredMoveDirection = inputVector.normalized;
        }

        if (hasMovementInput)
        {
            if (isProlongedIdle)
            {
                isProlongedIdle = false;
                if (animator != null)
                    animator.SetBool("ProlongedIdle", false);
            }

            // Usa aceleração normal no chão e reduzida no ar
            float currentAccel = controller.isGrounded ? acceleration : airAcceleration;

            // Lógica de aceleração normal
            if (controller.isGrounded && !isQuickTurning)
            {
                Vector3 flatMoveDirection = new Vector3(moveDirection.x, 0, moveDirection.z).normalized;
                Vector3 flatInputDirection = new Vector3(desiredMoveDirection.x, 0, desiredMoveDirection.z).normalized;
                if (inputMagnitude < 0.1f) flatInputDirection = flatMoveDirection;
                float angleBetween = Vector3.Angle(flatMoveDirection, flatInputDirection);

                if (angleBetween > reverseDirectionAngleThreshold && currentSpeed > skidMinActivationSpeed && !isSkidding) // Inicia o skid
                {
                    isSkidding = true;
                    // A desaceleração será tratada no Update
                }
                else if (!isSkidding) // Aceleração normal se não estiver skidding e não houver inversão brusca
                {
                    currentSpeed = Mathf.MoveTowards(currentSpeed, maxSpeed, currentAccel * Time.deltaTime);
                }
            }
            else // Fora do chão ou Quick Turning, usa aceleração normal
            {
                currentSpeed = Mathf.MoveTowards(currentSpeed, maxSpeed, currentAccel * Time.deltaTime);
            }

            if (cameraTransform != null)
            {
                // ✅ OTIMIZADO: Calcula uma vez


                if (isGroundSliding)
                {
                    float angle = Vector3.Angle(groundSlideInitialDirection, desiredMoveDirection);
                    if (angle > maxGroundSlideTurnAngle)
                    {
                        desiredMoveDirection = Vector3.RotateTowards(groundSlideInitialDirection, desiredMoveDirection, maxGroundSlideTurnAngle * Mathf.Deg2Rad, 0f);
                    }
                }

                // Se estiver no chão, projeta a direção de movimento na superfície da rampa.
                // Isso é crucial para que o jogador se mova *ao longo* da rampa e não "quique".
                if (controller.isGrounded)
                {
                    RaycastHit hit;
                    if (Physics.Raycast(cachedTransform.position + Vector3.up * 0.1f, Vector3.down, out hit, controller.height / 2f + 0.3f, groundMask))
                    {
                        desiredMoveDirection = Vector3.ProjectOnPlane(desiredMoveDirection, hit.normal).normalized;
                    }
                }

                // Calcula desiredMove com base na lógica de skid
                if (isSkidding)
                {
                    // Durante o skid, a direção de movimento é a direção anterior do jogador, mas a velocidade é reduzida.
                    // Isso cria o efeito de "skid" sem virar imediatamente.
                    desiredMove = new Vector3(lastMoveDirection.x, 0, lastMoveDirection.z).normalized * currentSpeed;
                }
                else
                {
                    desiredMove = desiredMoveDirection * currentSpeed;
                }

                if (controller.isGrounded && lastMoveDirection.sqrMagnitude > 0.01f && !animatorBusy)
                {
                    float angle = Vector3.Angle(lastMoveDirection, desiredMoveDirection);

                }

                // Atualiza lastMoveDirection apenas se não estiver skidding, para manter a direção durante a frenagem
                if (!isSkidding)
                {
                    lastMoveDirection = desiredMoveDirection;
                }

                // Verifica input para Quick Turn dedicado
                if (Input.GetKeyDown(quickTurnKey) && controller.isGrounded && currentSpeed >= quickTurnThreshold && !animatorBusy && quickTurnCooldownTimer <= 0)
                {
                    TriggerQuickTurn();
                    return;
                }

                moveDirection.x = desiredMove.x;
                moveDirection.z = desiredMove.z;
            }
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0, deceleration * Time.deltaTime);

            // Mantém a última direção real do jogador durante a desaceleração.
            // Antes, cada eixo era zerado separadamente; isso fazia uma diagonal "escorregar"
            // visualmente para frente/trás/esquerda/direita quando um eixo chegava a zero antes do outro.
            if (lastMoveDirection.sqrMagnitude > 0.01f)
            {
                moveDirection.x = lastMoveDirection.x * currentSpeed;
                moveDirection.z = lastMoveDirection.z * currentSpeed;
            }
            else
            {
                moveDirection.x = Mathf.MoveTowards(moveDirection.x, 0, deceleration * Time.deltaTime);
                moveDirection.z = Mathf.MoveTowards(moveDirection.z, 0, deceleration * Time.deltaTime);
            }
        }
    }

    // ======================================================
    // GROUND CHECK
    // ======================================================

    private void CheckGround()
    {
        isGrounded = controller.isGrounded;

        // ✅ OTIMIZADO: Verificação adicional apenas se necessário
        if (!isGrounded && groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, GROUND_CHECK_RADIUS, groundMask);
        }

        // Nova verificação de proximidade do rail

        if (isGrounded)
        {
            isStomping = false;
            hasWallRun = false;
            canJumpAfterGrind = false;
            // Se estiver no chão e planando, e não houver tempo de carência, para o glide.
            // O glideGraceTimer é para evitar desativações prematuras logo após um pulo, por exemplo.
            if (isGliding && glideGraceTimer <= 0f) StopGlide();
        }
    }


    // ======================================================
    // WALL RUN
    // ======================================================

    private void CheckWallRun()
    {
        // ✅ NOVO: Bloqueia Wall Run se Wall Dash Jump está ativo
        if (wallDashJump != null && wallDashJump.IsWallDashing())
        {
            if (showDebugInfo)
                Debug.Log("🚫 Wall Run BLOQUEADO - Wall Dash Jump em progresso");
            return;
        }
        
        // ✅ OTIMIZADO: Raycasts apenas a cada 2 frames
        raycastFrameCounter++;
        if (raycastFrameCounter % RAYCAST_CHECK_INTERVAL != 0)
            return;

        Vector3 position = cachedTransform.position;

        // ✅ OTIMIZADO: Raycasts com reutilização de hit
        onLeftWall = Physics.Raycast(position, -cachedTransform.right, out raycastHit, wallDistance + controller.radius, wallMask);
        Vector3 leftHitNormal = raycastHit.normal;

        onRightWall = Physics.Raycast(position, cachedTransform.right, out raycastHit, wallDistance + controller.radius, wallMask);
        Vector3 rightHitNormal = raycastHit.normal;

        if (isWallRunning)
        {
            if (!onLeftWall && !onRightWall)
            {
                ExitWallRun(true);
                return;
            }
        }

        if ((onLeftWall || onRightWall) && !isWallRunning && !controller.isGrounded && !recoveringFromWallRun && barWallRunCooldownTimer <= 0)
        {
            if (Physics.Raycast(position, Vector3.down, out raycastHit, RAYCAST_MAX_DISTANCE, groundMask))
            {
                if (raycastHit.distance < minDistanceToGroundForWallRun)
                {
                    return;
                }
            }

            wallNormal = onLeftWall ? leftHitNormal : rightHitNormal;

            // ✅ OTIMIZADO: Cálculo simplificado
            horizontalMove.x = moveDirection.x;
            horizontalMove.y = 0;
            horizontalMove.z = moveDirection.z;
            horizontalMove.Normalize();

            float angleToWall = Vector3.Angle(-horizontalMove, wallNormal);

            if (angleToWall > maxWallRunAngle || currentSpeed < quickTurnThreshold)
            {
                return;
            }

            StartWallRun();
        }
    }

    private void StartWallRun()
    {
        if (isWallRunning) return;

        // Se estiver planando, para o glide ao iniciar o wall run
        if (isGliding) StopGlide();

        styleRankSystem?.OnWallRunStart();

        // --- INTEGRAÇÃO SPEED TRAIL ---
        if (wallRunTrailEffect != null)
        {
            wallRunTrailEffect.StartTrail();
        }
        // ------------------------------

        isWallRunning = true;
        wallRunTimer = 0f;

        if (airDashCharges < maxAirDashCharges)
        {
            airDashCharges = maxAirDashCharges;
            if (showDebugInfo)
                Debug.Log($"✅ Dash Aéreo resetado ao iniciar Wall Run. Cargas: {airDashCharges}");
        }

        if (doubleJumpCharges < maxDoubleJumpCharges)
        {
            doubleJumpCharges = maxDoubleJumpCharges;
            if (showDebugInfo)
                Debug.Log($"✅ Pulo Duplo resetado ao iniciar Wall Run. Cargas: {doubleJumpCharges}");
        }

        hasWallRun = true;

        Vector3 forwardDir = Vector3.Cross(wallNormal, Vector3.up);
        if (Vector3.Dot(forwardDir, cachedTransform.forward) < 0)
            forwardDir = -forwardDir;

        currentSpeed = Mathf.Max(currentSpeed, maxSpeed);
        moveDirection = forwardDir * (currentSpeed + wallRunSpeedIncrease);
        moveDirection.y = 0;

        if (animator != null)
        {
            animator.ResetTrigger("QuickTurn");
            animator.SetBool("IsGrounded", false);
            animator.SetBool("ProlongedIdle", false);
            animator.SetFloat("Speed", 0);
            animator.SetBool("IsWallRunning", true);
        }

        isJumping = false;
        isFalling = false;

        if (animator != null)
        {
            animator.SetBool("IsJumping", false);
            animator.SetBool("IsFalling", false);
        }

        if (enableWallRunParticles)
        {
            StartWallRunParticles();
        }

        if (showDebugInfo)
            Debug.Log("🏃‍♂️ Wall Run iniciado!");
    }

    private void ExitWallRun(bool jumpAway = false)
    {
        if (!isWallRunning) return;

        isWallRunning = false;

        // --- INTEGRAÇÃO SPEED TRAIL ---
        if (wallRunTrailEffect != null)
        {
            wallRunTrailEffect.StopTrail();
        }
        // ------------------------------
        lastWallNormal = wallNormal;
        currentSpeed = Mathf.Max(currentSpeed - wallRunSpeedDecrease, 0);

        if (animator != null)
            animator.SetBool("IsWallRunning", false);

        if (jumpAway)
        {
            Vector3 outwardImpulse = wallNormal * wallJumpOutwardForce;

            horizontalMove.x = moveDirection.x;
            horizontalMove.y = 0;
            horizontalMove.z = moveDirection.z;
            Vector3 forwardMomentum = horizontalMove.normalized * horizontalMove.magnitude * wallJumpForwardMomentum;

            Vector3 verticalImpulse = Vector3.up * jumpForce;

            moveDirection.x = 0;
            moveDirection.z = 0;

            moveDirection += outwardImpulse;
            moveDirection += forwardMomentum;
            moveDirection.y = verticalImpulse.y;
            recoveringFromWallRun = true;
            wallRunRecoveryTimer = wallRunRecoveryTime;
            if (showDebugInfo)
                Debug.Log($"🌀 Pulou e entrou em recuperação após Wall Run!");
        }
        else
        {
            moveDirection.y = 0;
        }

        if (enableWallRunParticles)
        {
            StopWallRunParticles();
        }

        if (showDebugInfo)
            Debug.Log("Wall Run encerrado!");
    }

    private void WallRunMovement()
    {
        wallRunTimer += Time.deltaTime;

        // ✅ OTIMIZADO: Raycast com reutilização
        Vector3 rayDirection = onLeftWall ? -cachedTransform.right : cachedTransform.right;
        float rayLength = controller.radius + 0.5f;

        if (Physics.Raycast(cachedTransform.position, rayDirection, out raycastHit, rayLength, wallMask))
        {
            float distanceToWall = raycastHit.distance;
            float desiredDistance = controller.radius + 0.05f;

            float correctionAmount = desiredDistance - distanceToWall;

            if (Mathf.Abs(correctionAmount) > 0.001f)
            {
                Vector3 correctionVector = raycastHit.normal * correctionAmount;

                if (controller != null && controller.enabled)
                {
                    controller.enabled = false;
                    cachedTransform.position += correctionVector;
                    controller.enabled = true;
                }
                else
                {
                    cachedTransform.position += correctionVector;
                }
            }
        }

        if (wallRunTimer >= wallRunDuration)
        {
            ExitWallRun(true);
            return;
        }

        Vector3 forwardDir = Vector3.Cross(wallNormal, Vector3.up);
        if (Vector3.Dot(forwardDir, cachedTransform.forward) < 0)
            forwardDir = -forwardDir;

        moveDirection = Vector3.Lerp(moveDirection, forwardDir * currentSpeed, Time.deltaTime * 5f);
        moveDirection.y = 0;
    }

    private void HandleWallRunInput()
    {
        if (Input.GetButtonDown("Jump"))
        {
            // Interrompe o idle prolongado ao pular da parede
            if (isProlongedIdle)
            {
                isProlongedIdle = false;
                idleTimer = 0f;
                if (animator != null) animator.SetBool("ProlongedIdle", false);
            }
            ExitWallRun(true);
        }
    }

    // ======================================================
    // QUICK TURN
    // ======================================================

    private void TriggerQuickTurn()
    {
        originalSpeedBeforeQuickTurn = currentSpeed;
        minSpeedDuringQuickTurn = originalSpeedBeforeQuickTurn * quickTurnMinSpeedMultiplier;
        targetRotation = Quaternion.LookRotation(-cachedTransform.forward);

        if (animator != null)
            animator.SetTrigger("QuickTurn");

        animatorBusy = true;
        isQuickTurning = true;
        quickTurnCooldownTimer = quickTurnCooldown; // Inicia o cooldown
        if (isGliding) StopGlide(); // Cancela glide ao iniciar quick turn

        // Notifica a câmera para girar 180 graus
        if (followCamera == null) followCamera = Camera.main.GetComponent<DynamicFollowCamera>();
        if (followCamera != null) followCamera.OnQuickTurn();
    }

    public void CompleteQuickTurn()
    {
        cachedTransform.rotation = targetRotation;
        moveDirection.x = -moveDirection.x;
        moveDirection.z = -moveDirection.z;
        lastMoveDirection = -lastMoveDirection;
        animatorBusy = false;
        isQuickTurning = false;
    }

    // Variáveis para controle de lançamento da barra
    private float barLaunchLockTimer = 0f;
    private const float BAR_LAUNCH_LOCK_DURATION = 0.5f;

    // Método para receber o impulso da barra horizontal
    public void SetMovementFromBar(Vector3 velocity)
    {
        // Força a velocidade diretamente
        moveDirection = velocity;
        
        // Calcula a velocidade horizontal para o sistema de movimento
        Vector3 horizontalVel = new Vector3(velocity.x, 0, velocity.z);
        currentSpeed = horizontalVel.magnitude;
        
        if (velocity.sqrMagnitude > 0.1f)
        {
            // Estado de pulo forçado
            isJumping = true;
            isFalling = false;
            barLaunchLockTimer = BAR_LAUNCH_LOCK_DURATION;
            
            // Se houver velocidade vertical positiva, garante que o estado de pulo seja reconhecido pelo Animator
            if (velocity.y > 0.1f)
            {
                if (animator != null)
                {
                    animator.SetBool("IsJumping", true);
                    animator.SetBool("IsGrounded", false);
                }
            }

            // Faz o jogador olhar para a direção do lançamento
            if (horizontalVel.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(horizontalVel);
                lastMoveDirection = horizontalVel.normalized;
            }
        }
        
        // Reset de cargas de pulo ao usar a barra
        airDashCharges = maxAirDashCharges;
        doubleJumpCharges = maxDoubleJumpCharges;

        // Inicia o cooldown do Wall Run ao sair da barra
        barWallRunCooldownTimer = barWallRunCooldown;
    }

    private void ApplyQuickTurnDeceleration()
    {
        float targetSpeed = Mathf.Max(minSpeedDuringQuickTurn, 0);
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, quickTurnDeceleration * Time.deltaTime);

        // ✅ OTIMIZADO: Reutiliza Vector3
        horizontalMove.x = moveDirection.x;
        horizontalMove.y = 0;
        horizontalMove.z = moveDirection.z;
        horizontalMove.Normalize();

        moveDirection.x = horizontalMove.x * currentSpeed;
        moveDirection.z = horizontalMove.z * currentSpeed;
        UpdateAnimator();
    }

    // ======================================================
    // AIR TRICK
    // ======================================================

    private void HandleAirInput()
    {
        if (controller.isGrounded || isWallRunning || animatorBusy || airTrickCooldownTimer > 0f) return;

        if (!IsHighEnoughForAirTrick())
        {
            return;
        }

        if (Input.GetKey(KeyCode.Q))
        {
            string triggerName = string.Empty;

            if (Input.GetKeyDown(KeyCode.W))
            {
                triggerName = "AirInput_Forward";
            }
            else if (Input.GetKeyDown(KeyCode.S))
            {
                triggerName = "AirInput_Backward";
            }
            else if (Input.GetKeyDown(KeyCode.A))
            {
                triggerName = "AirInput_Left";
            }
            else if (Input.GetKeyDown(KeyCode.D))
            {
                triggerName = "AirInput_Right";
            }

            if (!string.IsNullOrEmpty(triggerName))
            {
                TriggerAirTrick();
                if (animator != null)
                {
                    animator.SetTrigger(triggerName);
                }
            }
        }
    }

    private bool IsHighEnoughForAirTrick()
    {
        // Usamos um Raycast com distância limitada pela altura mínima.
        // Se o raio NÃO atingir o chão dentro dessa distância, significa que o jogador
        // está alto o suficiente (ou sobre um vão), então permitimos o truque.
        bool groundTooClose = Physics.Raycast(cachedTransform.position, Vector3.down, out raycastHit, minHeightForAirTrick, groundMask);

        if (showDebugInfo)
        {
            Debug.DrawRay(cachedTransform.position, Vector3.down * minHeightForAirTrick,
                         groundTooClose ? Color.red : Color.green);
        }

        // Se NÃO houver chão perto, então está "alto o suficiente"
        return !groundTooClose;
    }

    private bool IsHighEnoughForGlide()
    {
        // Mesma lógica do Air Trick: se não houver chão detectado dentro da altura mínima, permite o glide.
        bool groundTooClose = Physics.Raycast(cachedTransform.position, Vector3.down, out raycastHit, minHeightForGlide, groundMask);

        if (showDebugInfo)
        {
            Debug.DrawRay(cachedTransform.position, Vector3.down * minHeightForGlide,
                         groundTooClose ? Color.red : Color.cyan);
        }

        return !groundTooClose;
    }

    private void TriggerAirTrick()
    {
        styleRankSystem?.OnAirTrickUsed();

        isRotationLocked = true;
        // O rotationLockTimer agora usa o valor máximo entre o tempo de trava de rotação e a duração mínima definida manualmente
        rotationLockTimer = Mathf.Max(airTrickRotationLockTime, minAirTrickDuration);
        airTrickCooldownTimer = airTrickCooldown;
        animatorBusy = true;

        if (enableAirTrickParticles && airTrickParticles != null)
        {
            StartAirTrickParticles();
        }

        if (showDebugInfo)
            Debug.Log($"🌀 Air Trick - Rotação travada por {airTrickRotationLockTime}s");

        if (isGliding) StopGlide(); // Cancela glide ao iniciar air trick
    }

    private float GetCurrentHeight()
    {
        if (Physics.Raycast(cachedTransform.position, Vector3.down, out raycastHit, RAYCAST_MAX_DISTANCE, groundMask))
        {
            return raycastHit.distance;
        }
        return float.MaxValue;
    }

    private void UpdateRotationLock()
    {
        if (isRotationLocked && rotationLockTimer > 0)
        {
            rotationLockTimer -= Time.deltaTime;
            if (rotationLockTimer <= 0f)
            {
                isRotationLocked = false;
                animatorBusy = false;
                if (showDebugInfo)
                    Debug.Log("✅ Rotação destravada após Air Trick");

                // Adiciona cooldown para o Air Dash APÓS o término do Air Trick
                airDashCooldownTimer = airDashCooldownAfterAirTrick;
            }
        }
    }

    public void LockRotation(bool lockRotation)
    {
        isRotationLocked = lockRotation;
        if (lockRotation)
        {
            lockedRotation = cachedTransform.rotation;
        }
        else
        {
            rotationLockTimer = 0f;
        }
    }

    private void UpdateAirTrickCooldown()
    {
        if (airTrickCooldownTimer > 0f)
        {
            airTrickCooldownTimer -= Time.deltaTime;
        }
    }

    // ======================================================
    // ROTAÇÃO
    // ======================================================

    private void HandleRotation()
    {
        if (isRotationLocked)
        {
            cachedTransform.rotation = lockedRotation;
            return;
        }

        if (animatorBusy) return;

        // Se não há input direcional, não recalcula a rotação usando a velocidade residual.
        // Assim, ao soltar uma diagonal, o jogador permanece olhando para a última diagonal usada.
        if (!hasMovementInput)
        {
            rotationUpdateTimer = ROTATION_UPDATE_INTERVAL; // força atualizar o alvo assim que o input voltar
            return;
        }

        // ✅ OTIMIZADO: Reutiliza Vector3
        horizontalMove.x = moveDirection.x;
        horizontalMove.y = 0;
        horizontalMove.z = moveDirection.z;

        if (horizontalMove.sqrMagnitude > 0.01f && !isQuickTurning && !isSkidding)
        {
            // Uncharted Style: Rotação dinâmica baseada na velocidade e ângulo
            // Quanto mais rápido o jogador, mais "pesada" é a curva para evitar mudanças bruscas instantâneas
            
            // 1. Define o alvo de rotação
            Quaternion targetRot = Quaternion.LookRotation(horizontalMove);
            
            // 2. Calcula a velocidade de rotação dinâmica
            // Se o ângulo for muito grande (mudança brusca), a rotação é levemente mais lenta para parecer mais orgânica
            float angleDiff = Quaternion.Angle(cachedTransform.rotation, targetRot);
            float dynamicTurnSpeed = turnSpeed;
            
            // Suaviza a velocidade de giro baseada no ângulo (curvas largas são mais fluidas)
            float speedFactor = Mathf.Clamp01(currentSpeed / maxSpeed);
            float smoothness = Mathf.Lerp(rotationSmoothing, rotationSmoothing * 0.5f, speedFactor);
            
            // 3. Aplica Slerp (Spherical Linear Interpolation) para uma trajetória de arco mais natural
            cachedTransform.rotation = Quaternion.Slerp(
                cachedTransform.rotation, 
                targetRot, 
                Time.deltaTime * smoothness
            );
        }
    }

    // ======================================================
    // GRAVIDADE
    // ======================================================

    private void ApplyGravity()
    {
        if (isDashing || recoveringFromWallRun || isStomping) return;
        if (isGliding) return; // Gravidade do glide é aplicada em GlideMovement()

        float effectiveGravity = isWallRunning ? wallRunGravity : gravity;

        bool canDoNormalJump = controller.isGrounded || canJumpAfterGrind;

        if (canDoNormalJump)
        {
            if (controller.isGrounded)
            {
                moveDirection.y = -controller.stepOffset;
                isFalling = false;
            }

        if (Input.GetButtonDown("Jump") && !animatorBusy)
        {
            // Interrompe o idle prolongado ao pular
            if (isProlongedIdle)
            {
                isProlongedIdle = false;
                idleTimer = 0f;
                if (animator != null) animator.SetBool("ProlongedIdle", false);
            }

            moveDirection.y = jumpForce;
            isJumping = true;
            isFalling = false; // Garante que não esteja caindo ao pular
            canJumpAfterGrind = false; // Consome o pulo do rail

                if (airDashCharges < maxAirDashCharges)
                {
                    airDashCharges = maxAirDashCharges;
                    if (showDebugInfo)
                        Debug.Log($"✅ Dash Aéreo resetado ao iniciar o pulo. Cargas: {airDashCharges}");
                }
                if (doubleJumpCharges < maxDoubleJumpCharges)
                {
                    doubleJumpCharges = maxDoubleJumpCharges;
                    if (showDebugInfo)
                        Debug.Log($"✅ Pulo Duplo resetado ao iniciar o pulo. Cargas: {doubleJumpCharges}");
                }

                if (enableJumpParticles && jumpParticles != null)
                {
                    StartJumpParticles();
                }

                if (isWallRunning)
                    ExitWallRun(true);
            }

            // ✅ CORREÇÃO: Aplica gravidade quando não está no chão, mesmo com canJumpAfterGrind=true
            // Isso corrige a flutuação causada pelo autojump do rail
            if (!controller.isGrounded)
            {
                moveDirection.y -= effectiveGravity * Time.deltaTime;

                // ✅ OTIMIZADO: Verificações simplificadas
                if (moveDirection.y < -0.1f)
                {
                    isJumping = false;
                    isFalling = true;
                }
                else if (moveDirection.y > 0.1f)
                {
                    isJumping = true;
                    isFalling = false;
                }
                else
                {
                    isJumping = false;
                    isFalling = true;
                }
            }
        }
        else
        {
            // Lógica Unificada: Pulo Duplo OU Glide
            if (Input.GetButtonDown("Jump") && !animatorBusy)
            {
                // Interrompe o idle prolongado ao pular (pulo duplo)
                if (isProlongedIdle)
                {
                    isProlongedIdle = false;
                    idleTimer = 0f;
                    if (animator != null) animator.SetBool("ProlongedIdle", false);
                }

                if (doubleJumpCharges > 0)
                {
                    // Executa Pulo Duplo
                    moveDirection.y = jumpForce;
                    doubleJumpCharges--;
                    isJumping = true;
                    isFalling = false;

                    if (animator != null)
                        animator.SetTrigger("DoubleJump");

                    if (enableDoubleJumpParticles && doubleJumpParticles != null)
                        StartDoubleJumpParticles();

                    // Adiciona cooldown para o stomp após o pulo duplo
                    stompCooldownTimer = stompCooldownAfterDoubleJump;

                    // Adiciona cooldown para o Air Dash após o pulo duplo
                    airDashCooldownTimer = airDashCooldownAfterDoubleJump;

                    if (showDebugInfo)
                        Debug.Log($"🚀 Pulo Duplo! Cargas restantes: {doubleJumpCharges} | Stomp Cooldown: {stompCooldownAfterDoubleJump}s | Air Dash Cooldown: {airDashCooldownAfterDoubleJump}s");
                }
                else if (canGlide && !isGliding && glideCooldownTimer <= 0f)
                {
                    // Tenta iniciar o Glide se não houver mais pulos
                    bool canStartGlide = !isWallRunning && !isStomping && !isDashing && 
                                        !isRotationLocked && (railRide == null || !railRide.isGrinding);
                    
                    if (canStartGlide)
                    {
                        StartGlide();
                        return; // Sai para evitar aplicar gravidade no frame de início
                    }
                }
            }

            moveDirection.y -= effectiveGravity * Time.deltaTime;

            // ✅ OTIMIZADO: Verificações simplificadas
            if (moveDirection.y < -0.1f)
            {
                isJumping = false;
                isFalling = true;
            }
            else if (moveDirection.y > 0.1f)
            {
                isJumping = true;
                isFalling = false;
            }
            else
            {
                isJumping = false;
                isFalling = true;
            }
        }
    }

    // ======================================================
    // STOMP (QUEDA RÁPIDA)
    // ======================================================

    private void HandleStomp()
    {
        // Stomp só pode ser iniciado se NÃO estiver no chão
        if (!controller.isGrounded && !isDashing && !isStomping && !isWallRunning && !isGroundSliding)
        {
            if (Input.GetKeyDown(stompKey))
            {
                if (stompCooldownTimer <= 0)
                {
                    StartStomp();
                }
                else if (showDebugInfo)
                {
                    Debug.Log($"⏳ Stomp em cooldown após Pulo Duplo! ({stompCooldownTimer:F2}s restantes)");
                }
            }
        }

        if (isStomping && !controller.isGrounded)
        {
            ApplyStompForce();
        }
    }

    private void StartStomp()
    {
        isStomping = true;
        if (isGliding) StopGlide(); // Cancela glide ao iniciar stomp

        moveDirection.x = 0;
        moveDirection.z = 0;
        moveDirection.y = -stompForce;

        if (stompParticles != null)
        {
            stompParticles.Play();
        }

        if (showDebugInfo)
            Debug.Log($"💥 Stomp ativado! Força: {stompForce}");
    }

    private void ApplyStompForce()
    {
        moveDirection.y = -stompForce;
        moveDirection.x = 0;
        moveDirection.z = 0;
    }

    // ======================================================
    // AIR DASH
    // ======================================================

    private void HandleAirDash()
    {
        if (!controller.isGrounded && !isWallRunning && !isDashing && !isStomping && airDashCharges > 0 && airDashCooldownTimer <= 0)
        {
            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                StartAirDash();
            }
        }

        if (isDashing)
        {
            airDashTimer -= Time.deltaTime;
            if (airDashTimer <= 0)
            {
                StopAirDash();
            }
        }
    }

    private void StartAirDash()
    {
        isDashing = true;
        airDashTimer = airDashDuration;
        airDashCharges--;

        if (isGliding) StopGlide(); // Cancela glide ao usar air dash

        // ✅ OTIMIZADO: Reutiliza Vector3
        horizontalMove.x = moveDirection.x;
        horizontalMove.y = 0;
        horizontalMove.z = moveDirection.z;

        Vector3 dashDirection = horizontalMove.sqrMagnitude > 0.01f ? horizontalMove.normalized : cachedTransform.forward;

        moveDirection = dashDirection * airDashForce;
        moveDirection.y = 0;

        styleRankSystem?.OnAirDashUsed();

        if (animator != null)
            animator.SetTrigger("AirDash");

        if (enableAirDashParticles && airDashParticles != null)
        {
            StartAirDashParticles();
        }

        if (showDebugInfo)
            Debug.Log($"💨 Dash Aéreo! Cargas restantes: {airDashCharges}");
    }

    private void StopAirDash()
    {
        isDashing = false;

        if (enableAirDashParticles && airDashParticles != null)
        {
            StopAirDashParticles();
        }
    }

    private void AirDashMovement()
    {
        // O movimento do Dash já foi definido em StartAirDash()
    }

    // ======================================================
    // IDLE PROLONGADO
    // ======================================================

    private void HandleProlongedIdle()
    {
        if (currentSpeed < 0.1f && !animatorBusy)
        {
            idleTimer += Time.deltaTime;
            if (idleTimer >= prolongedIdleTime && !isProlongedIdle)
            {
                isProlongedIdle = true;
                if (animator != null)
                    animator.SetBool("ProlongedIdle", true);
            }
        }
        else
        {
            idleTimer = 0f;
            if (isProlongedIdle)
            {
                isProlongedIdle = false;
                if (animator != null)
                    animator.SetBool("ProlongedIdle", false);
            }
        }
    }

    // ======================================================
    // GLIDE MECHANIC
    // ======================================================

    private void HandleGlide()
    {
        if (!canGlide) return;

        // Verifica se o botão de pulo (barra de espaço) está sendo segurado
        glideButtonHeld = Input.GetButton("Jump");

        // Se já estiver planando, a única coisa que cancela é soltar o botão ou tocar o chão (tratado no CheckGround)
        if (isGliding)
        {
            // Só permite parar o glide se o botão não estiver segurado E a duração mínima já tiver passado
            if (!glideButtonHeld && glideActiveTimer >= minGlideDuration)
            {
                StopGlide();
            }
            return;
        }
        
        // A ativação agora é tratada dentro do ApplyGravity para garantir sincronia com o pulo duplo.
    }

    private void StartGlide()
    {
        if (isGliding) return;

        // Bloqueios
        if (controller.isGrounded || isWallRunning || isStomping || isDashing || animatorBusy || isRotationLocked || (railRide != null && railRide.isGrinding))
        {
            if (showDebugInfo) Debug.Log("🚫 Glide BLOQUEADO - Em estado incompatível.");
            return;
        }

        isGliding = true;
        glideGraceTimer = glideGraceTime; // Inicia o timer de carência
        glideActiveTimer = 0f; // Reseta o timer de duração do glide

        isJumping = false;
        isFalling = true; 

        // Zera a velocidade vertical e adiciona um pequeno impulso para cima para garantir a saída do chão
        moveDirection.y = 2.0f; // Pequeno impulso vertical para iniciar o glide suavemente
        
        // Mantém o momentum atual ao entrar no glide, sem impulso adicional
        currentGlideSpeed = Mathf.Max(new Vector3(moveDirection.x, 0, moveDirection.z).magnitude, minGlideSpeed); // Define a velocidade inicial baseada no momentum atual

        if (animator != null)
        {
            animator.SetBool("IsGliding", true);
        }

        StartGlideParticles();

        if (showDebugInfo)
            Debug.Log("✈️ Glide iniciado!");
    }

    private void StopGlide()
    {
        if (!isGliding) return;

        isGliding = false;

        if (animator != null)
        {
            animator.SetBool("IsGliding", false);
            if (showDebugInfo) Debug.Log("✅ Animator: IsGliding = false");
        }

        StopGlideParticles();
        glideCooldownTimer = glideCooldown;

        if (showDebugInfo)
            Debug.Log($"🛑 Glide encerrado! Cooldown de {glideCooldown}s iniciado.");
    }

    private void GlideMovement()
    {
        if (!isGliding) return;

        // Aplica gravidade reduzida
        moveDirection.y -= glideGravity * Time.deltaTime;

        // Limita a velocidade de queda
        moveDirection.y = Mathf.Max(moveDirection.y, maxGlideFallSpeed);

        // Cancela o glide se a altura for menor que a mínima
        if (GetCurrentHeight() < minHeightForGlide)
        {
            StopGlide();
            if (showDebugInfo) Debug.Log("🛑 Glide cancelado: altura mínima atingida.");
            return; // Sai do método após cancelar o glide
        }

        // Adiciona sustentação se o botão de pulo estiver sendo segurado
        if (glideButtonHeld)
        {
            moveDirection.y += glideLiftForce * Time.deltaTime;
        }

        // Captura o input do jogador para controle direcional durante o glide
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        inputVector.x = horizontalInput;
        inputVector.y = 0;
        inputVector.z = verticalInput;
        float inputMagnitude = inputVector.magnitude;

        Vector3 currentHorizontalMove = new Vector3(moveDirection.x, 0, moveDirection.z);
        Vector3 targetHorizontalDirection = cachedTransform.forward; // Direção padrão se não houver input

        if (inputMagnitude > 0.1f)
        {
            // Calcula a direção desejada baseada no input e na câmera
            if (cameraTransform != null)
            {
                cameraForward = cameraTransform.forward;
                cameraRight = cameraTransform.right;
                cameraForward.y = 0;
                cameraRight.y = 0;
                cameraForward.Normalize();
                cameraRight.Normalize();

                targetHorizontalDirection = (cameraForward * verticalInput + cameraRight * horizontalInput).normalized;
            }
            else
            {
                targetHorizontalDirection = cachedTransform.forward; // Fallback se a câmera não estiver disponível
            }
        }

        // Interpola suavemente a direção horizontal atual para a direção desejada
        Vector3 newHorizontalDirection = Vector3.Slerp(currentHorizontalMove.normalized, targetHorizontalDirection, Time.deltaTime * glideTurnSpeed).normalized;

        // Desacelera a velocidade atual do glide
        currentGlideSpeed = Mathf.MoveTowards(currentGlideSpeed, minGlideSpeed, glideDeceleration * Time.deltaTime);

        // Aplica a velocidade frontal na nova direção horizontal
        moveDirection.x = newHorizontalDirection.x * currentGlideSpeed;
        moveDirection.z = newHorizontalDirection.z * currentGlideSpeed;

        // Rotação suave do jogador para a direção do movimento
        if (newHorizontalDirection.sqrMagnitude > 0.01f && !isRotationLocked)
        {
            Quaternion targetGlideRotation = Quaternion.LookRotation(newHorizontalDirection);
            cachedTransform.rotation = Quaternion.Slerp(cachedTransform.rotation, targetGlideRotation, Time.deltaTime * glideTurnSpeed * 0.75f); // Rotação mais ágil
        }

        // Atualiza currentSpeed para animação, se necessário
        currentSpeed = currentGlideSpeed;
    }

    // ======================================================
    // ANIMAÇÃO
    // ======================================================

    private void UpdateAnimator()
    {
        if (animator == null) return;

        float normalizedSpeed = Mathf.Clamp01(currentSpeed / maxSpeed);
        
        // ✅ OTIMIZADO: Só atualiza Speed se mudou significativamente
        if (Mathf.Abs(normalizedSpeed - cachedSpeed) > SPEED_CHANGE_THRESHOLD)
        {
            animator.SetFloat("Speed", normalizedSpeed);
            cachedSpeed = normalizedSpeed;
        }

        // ✅ CORREÇÃO CIRÚRGICA: Impedimos que o Animator receba IsGrounded = true se estivermos no meio de um Stomp.
        // Isso resolve o bug da animação de aterrissagem tocar prematuramente.
        bool animatorGrounded = controller.isGrounded && !isStomping;

        if (animatorGrounded != cachedIsGrounded)
        {
            animator.SetBool("IsGrounded", animatorGrounded);
            cachedIsGrounded = animatorGrounded;
        }

        // ✅ OTIMIZADO: Só atualiza IsWallRunning se mudou
        if (isWallRunning != cachedIsWallRunning)
        {
            animator.SetBool("IsWallRunning", isWallRunning);
            cachedIsWallRunning = isWallRunning;
        }

        // ✅ OTIMIZADO: Só atualiza IsJumping se mudou
        if (isJumping != cachedIsJumping)
        {
            animator.SetBool("IsJumping", isJumping);
            cachedIsJumping = isJumping;
        }

        // ✅ OTIMIZADO: Só atualiza IsFalling se mudou
        if (isFalling != cachedIsFalling)
        {
            animator.SetBool("IsFalling", isFalling);
            cachedIsFalling = isFalling;
        }

        // ✅ OTIMIZADO: Só atualiza IsStomping se mudou
        if (isStomping != cachedIsStomping)
        {
            animator.SetBool("IsStomping", isStomping);
            cachedIsStomping = isStomping;
        }

        // ✅ OTIMIZADO: Só atualiza OnLeftWall se mudou
        if (onLeftWall != cachedOnLeftWall)
        {
            animator.SetBool("OnLeftWall", onLeftWall);
            cachedOnLeftWall = onLeftWall;
        }

        // ✅ OTIMIZADO: Só atualiza OnRightWall se mudou
        if (onRightWall != cachedOnRightWall)
        {
            animator.SetBool("OnRightWall", onRightWall);
            cachedOnRightWall = onRightWall;
        }

        // ✅ OTIMIZADO: Só atualiza ProlongedIdle se mudou
        if (isProlongedIdle != cachedProlongedIdle)
        {
            animator.SetBool("ProlongedIdle", isProlongedIdle);
            cachedProlongedIdle = isProlongedIdle;
        }

        // ✅ NOVO: Só atualiza IsGroundSliding se mudou
        if (isGroundSliding != cachedIsGroundSliding)
        {
            animator.SetBool("IsGroundSliding", isGroundSliding);
            cachedIsGroundSliding = isGroundSliding;
        }

        // ✅ NOVO: Só atualiza IsGliding se mudou
            if (isGliding != cachedIsGliding)
            {
                animator.SetBool("IsGliding", isGliding);
                cachedIsGliding = isGliding;
            }

            if (isSkidding != cachedIsSkidding)
            {
                animator.SetBool("IsSkidding", isSkidding);
                cachedIsSkidding = isSkidding;
            }

            // ✅ NOVO: Só atualiza IsSwinging se mudou
            if (isSwinging != cachedIsSwinging)
            {
                animator.SetBool("IsSwinging", isSwinging);
                cachedIsSwinging = isSwinging;
            }
    }

    // ======================================================
    // MÉTODOS PÚBLICOS PARA INTEGRAÇÃO
    // ======================================================

    public void AddExternalVelocity(Vector3 velocity)
    {
        externalVelocity = velocity;
        if (showDebugInfo)
            Debug.Log($"⚡ Velocidade externa adicionada: {velocity}");
    }

    public void ForceExitWallRun()
    {
        if (isWallRunning)
        {
            ExitWallRun(false);
        }
    }

    public void ResetAirCharges()
    {
        doubleJumpCharges = maxDoubleJumpCharges;
        airDashCharges = maxAirDashCharges;
        
        // Reseta o estado de pulo e queda para permitir um novo pulo normal ao sair do rail
        isJumping = false;
        isFalling = false;
        canJumpAfterGrind = true;

        if (showDebugInfo)
            Debug.Log($"✅ Cargas aéreas e estados de pulo resetados. Double Jump: {doubleJumpCharges}, Air Dash: {airDashCharges}");
    }

    // Método para executar um pulo forçado (usado pelo Rail)
    public void ExecuteJump(Vector3 velocity)
    {
        moveDirection.y = velocity.y;
        
        // Se a velocidade tiver componentes horizontais, aplica como externalVelocity
        Vector3 horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
        if (horizontalVelocity.magnitude > 0)
        {
            AddExternalVelocity(horizontalVelocity);
        }

        isJumping = true;
        isFalling = false;
        canJumpAfterGrind = false; // Consome o pulo normal
        
        if (enableJumpParticles && jumpParticles != null)
            StartJumpParticles();
            
        if (showDebugInfo)
            Debug.Log("🚀 Pulo executado via script externo (Rail)");
    }

    // ======================================================
    // CONTROLE DE PARTICULAS
    // ======================================================

    private void StartAirDashParticles()
    {
        if (airDashParticles == null) return;

        // Para e limpa as partículas antes de tocar novamente para garantir que reiniciem instantaneamente
        airDashParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        airDashParticles.Play();

        if (showDebugInfo)
            Debug.Log("Particulas de air dash iniciadas.");
    }

    private void StopAirDashParticles(bool clearInstant = false)
    {
        if (airDashParticles == null) return;

        if (clearInstant)
        {
            // Para e limpa instantaneamente
            airDashParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        else
        {
            // Para de emitir novas partículas, mas deixa as existentes sumirem aos poucos
            airDashParticles.Stop();
        }

        if (showDebugInfo)
            Debug.Log(clearInstant ? "Particulas de air dash limpas instantaneamente." : "Particulas de air dash paradas.");
    }

    private void StartJumpParticles()
    {
        if (jumpParticles == null) return;

        if (!jumpParticles.isPlaying)
        {
            jumpParticles.Play();

            if (showDebugInfo)
                Debug.Log("Particulas de pulo iniciadas.");
        }
    }

    private void StopJumpParticles()
    {
        if (jumpParticles == null) return;

        if (jumpParticles.isPlaying)
        {
            jumpParticles.Stop();

            if (showDebugInfo)
                Debug.Log("Particulas de pulo paradas.");
        }
    }

    private void StartDoubleJumpParticles()
    {
        if (doubleJumpParticles == null) return;

        if (!doubleJumpParticles.isPlaying)
        {
            doubleJumpParticles.Play();

            if (showDebugInfo)
                Debug.Log("Particulas de duplo pulo iniciadas.");
        }
    }

    private void StopDoubleJumpParticles()
    {
        if (doubleJumpParticles == null) return;

        if (doubleJumpParticles.isPlaying)
        {
            doubleJumpParticles.Stop();

            if (showDebugInfo)
                Debug.Log("Particulas de duplo pulo paradas.");
        }
    }

    private void StartAirTrickParticles()
    {
        if (airTrickParticles == null) return;

        // Para e limpa as partículas antes de tocar novamente para garantir que reiniciem instantaneamente
        // Isso resolve o problema de não ativar quando executado em sucessão rápida
        airTrickParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        airTrickParticles.Play();

        if (showDebugInfo)
            Debug.Log("Particulas de air trick iniciadas (reiniciadas).");
    }

    private void StopAirTrickParticles()
    {
        if (airTrickParticles == null) return;

        if (airTrickParticles.isPlaying)
        {
            // Para de emitir novas partículas, mas deixa as existentes sumirem aos poucos
            airTrickParticles.Stop();

            if (showDebugInfo)
                Debug.Log("Particulas de air trick paradas.");
        }
    }

    private void StartWallRunParticles()
    {
        if (onLeftWall && wallRunLeftParticles != null)
        {
            if (!wallRunLeftParticles.isPlaying)
            {
                wallRunLeftParticles.Play();

                if (showDebugInfo)
                    Debug.Log("Particulas de wall run esquerdo iniciadas.");
            }
        }
        else if (onRightWall && wallRunRightParticles != null)
        {
            if (!wallRunRightParticles.isPlaying)
            {
                wallRunRightParticles.Play();

                if (showDebugInfo)
                    Debug.Log("Particulas de wall run direito iniciadas.");
            }
        }
    }

    private void StopWallRunParticles()
    {
        if (wallRunLeftParticles != null && wallRunLeftParticles.isPlaying)
        {
            wallRunLeftParticles.Stop();

            if (showDebugInfo)
                Debug.Log("Particulas de wall run esquerdo paradas.");
        }

        if (wallRunRightParticles != null && wallRunRightParticles.isPlaying)
        {
            wallRunRightParticles.Stop();

            if (showDebugInfo)
                Debug.Log("Particulas de wall run direito paradas.");
        }
    }

    private void StartGlideParticles()
    {
        if (glideParticles == null) return;

        // Para e limpa as partículas antes de tocar novamente para garantir que reiniciem instantaneamente
        glideParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        glideParticles.Play();

        if (showDebugInfo)
            Debug.Log("Partículas de glide iniciadas (reiniciadas).");
    }

    private void StopGlideParticles()
    {
        if (glideParticles == null) return;

        if (glideParticles.isPlaying)
        {
            // Para de emitir novas partículas, mas deixa as existentes sumirem aos poucos (comportamento padrão do Stop)
            glideParticles.Stop();

            if (showDebugInfo)
                Debug.Log("Partículas de glide paradas.");
        }
    }

    public void ResetMovementDirection()
    {
        moveDirection = Vector3.zero;
    }

    // ======================================================
    // DEBUG E PROPRIEDADES
    // ======================================================

    private void OnGUI()
    {
        if (!showDebugInfo) return;

        GUI.Label(new Rect(10, 10, 300, 20), $"WallRun: {isWallRunning}");
        GUI.Label(new Rect(10, 30, 300, 20), $"Speed: {currentSpeed:F2}");
        GUI.Label(new Rect(10, 50, 300, 20), $"Animator Busy: {animatorBusy}");
        GUI.Label(new Rect(10, 70, 300, 20), $"Idle Timer: {idleTimer:F1}");
        GUI.Label(new Rect(10, 90, 300, 20), $"Rotation Locked: {isRotationLocked} ({rotationLockTimer:F1}s)");
        GUI.Label(new Rect(10, 110, 300, 20), $"Height: {GetCurrentHeight():F1}m / Min Air Trick: {minHeightForAirTrick}m / Min Glide: {minHeightForGlide}m");
        GUI.Label(new Rect(10, 130, 300, 20), $"Air Trick Cooldown: {airTrickCooldownTimer:F2}s");
        GUI.Label(new Rect(10, 150, 300, 20), $"Air Dash: {isDashing} (Charges: {airDashCharges}/{maxAirDashCharges})");
        GUI.Label(new Rect(10, 170, 300, 20), $"Double Jump: (Charges: {doubleJumpCharges}/{maxDoubleJumpCharges})");
        GUI.Label(new Rect(10, 190, 300, 20), $"Air Dash Cooldown: {airDashCooldownTimer:F2}s");
        GUI.Label(new Rect(10, 210, 300, 20), $"On Rail: {(railRide != null && railRide.isGrinding ? "Yes" : "No")}");
        GUI.Label(new Rect(10, 230, 300, 20), $"Stomp: {isStomping}");
        GUI.Label(new Rect(10, 250, 300, 20), $"Glide: {isGliding}");
        if (glideCooldownTimer > 0)
        {
            GUI.Label(new Rect(10, 270, 300, 20), $"Glide Cooldown: {glideCooldownTimer:F2}s");
        }
    }

    public float CurrentSpeed => currentSpeed;
    public bool IsGrounded => controller.isGrounded;
    public bool IsRotationLocked => isRotationLocked;
    public bool IsStomping => isStomping;
    public bool IsGroundSliding => isGroundSliding;

    /// <summary>
    /// Zera a velocidade vertical (gravidade acumulada).
    /// Útil ao entrar em estados que ignoram a gravidade, como o grind rail.
    /// </summary>
    public void ResetVerticalVelocity()
    {
        moveDirection.y = 0;
    }

    private void HandleGroundSlideInput()
    {
        // Atualiza o cooldown do slide
        if (groundSlideCooldownTimer > 0) groundSlideCooldownTimer -= Time.deltaTime;

        // Só permite iniciar o slide se estiver REALMENTE no chão (grounded), não estiver em Quick Turn e o cooldown expirou
        if (Input.GetKeyDown(stompKey) && controller.isGrounded && currentSpeed > groundSlideMinSpeed && !isGroundSliding && !isQuickTurning && groundSlideCooldownTimer <= 0)
        {
            StartGroundSlide();
        }

        if (isGroundSliding)
        {
            groundSlideTimer += Time.deltaTime;
            currentSpeed = groundSlideLockedSpeed;

            // O slide agora tem uma duração mínima obrigatória.
            // Após essa duração, ele pode ser cancelado soltando o botão.
            
            bool reachedMaxDuration = groundSlideTimer >= maxGroundSlideDuration;
            bool manualCancel = groundSlideTimer >= minGroundSlideDuration && Input.GetKeyUp(stompKey);
            bool lostSpeed = groundSlideTimer >= minGroundSlideDuration && currentSpeed < 2f;
            bool lostGround = groundSlideTimer >= minGroundSlideDuration && !isGrounded;

            if (reachedMaxDuration || manualCancel || lostSpeed || lostGround)
            {
                StopGroundSlide();
            }
        }
    }

    private void UpdateColliderHeight()
    {
        float targetHeight = isGroundSliding ? groundSlideHeight : originalHeight;
        
        if (Mathf.Abs(currentColliderHeight - targetHeight) > 0.001f)
        {
            currentColliderHeight = Mathf.Lerp(currentColliderHeight, targetHeight, Time.deltaTime * groundSlideTransitionSpeed);
            float heightDifference = originalHeight - currentColliderHeight;
            float newCenterY = originalCenterY - (heightDifference / 2f);
            controller.height = currentColliderHeight;
            controller.center = new Vector3(controller.center.x, newCenterY, controller.center.z);
        }
    }

    private void StartGroundSlide()
    {
        if (isGroundSliding) return;
        isGroundSliding = true;
        groundSlideTimer = 0f;
        groundSlideLockedSpeed = currentSpeed;
        groundSlideInitialDirection = lastMoveDirection.normalized; // Salva a direção inicial

        if (animator != null) animator.SetBool("IsUnleashedSlide", true); // Reutilizando a animação existente
        if (groundSlideParticles != null && !groundSlideParticles.isPlaying) groundSlideParticles.Play();
        
        // Não interage com SlopeSlideSystem aqui, pois é um slide de chão
    }

    private void StopGroundSlide()
    {
        if (!isGroundSliding) return;
        isGroundSliding = false;
        
        groundSlideCooldownTimer = groundSlideCooldown; // Inicia o cooldown ao parar o slide

        if (animator != null) animator.SetBool("IsUnleashedSlide", false);
        if (groundSlideParticles != null) groundSlideParticles.Stop();
        
        // Não interage com SlopeSlideSystem aqui, pois é um slide de chão
    }
}
