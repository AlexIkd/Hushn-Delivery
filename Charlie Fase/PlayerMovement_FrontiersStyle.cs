using UnityEngine;
using UnityEngine.InputSystem;
using System;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement_FrontiersStyle : MonoBehaviour
{
    // Referência ao sistema de ranking de estilo
    private StyleRankSystem styleRankSystem;
    [Header("Configurações de Velocidade")]
    [SerializeField] public float maxSpeed = 15f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 15f;
    [SerializeField] private float turnSpeed = 500f;

    [Header("Quick Turn")]
    [SerializeField] private float quickTurnThreshold = 5.0f;
    [SerializeField] private float quickTurnAngle = 165f;
    [SerializeField] private float quickTurnDeceleration = 8f;
    [SerializeField] private float quickTurnMinSpeedMultiplier = 0.4f;

    [Header("Configurações de Salto e Gravidade")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float gravity = 20f;

    [Header("Stomp (Queda Rápida)")]
    [SerializeField] private float stompForce = 30f;
    [SerializeField] private float stompMinHeight = 2f;
    [SerializeField] private float wallJumpOutwardForce = 12f;
    [SerializeField] private float wallJumpForwardMomentum = 0.75f;
    [SerializeField] private KeyCode stompKey = KeyCode.LeftControl;
    [SerializeField] private ParticleSystem stompParticles;
    private bool isStomping = false;

    [Header("Air Movement")]
    [SerializeField] private float airDashForce = 15f;
    [SerializeField] private float airDashDuration = 0.1f;
    [SerializeField] private int maxDoubleJumpCharges = 1;
    private int doubleJumpCharges = 0;
    [SerializeField] private int maxAirDashCharges = 1;
    private int airDashCharges = 0;
    private bool isDashing = false;
    private float airDashTimer = 0f;

    [Header("Idle Prolongado")]
    [SerializeField] private float prolongedIdleTime = 15f;
    private float idleTimer = 0f;
    private bool isProlongedIdle = false;

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

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false; // ✅ DESATIVADO por padrão

    [Header("Recuperação após Wall Run")]
    [SerializeField] private float wallRunRecoveryTime = 0.4f;
    private bool recoveringFromWallRun = false;
    private float wallRunRecoveryTimer = 0f;

    // Estados internos
    [HideInInspector] public bool IsWallRunning => isWallRunning;
    [HideInInspector] public bool OnLeftWall => onLeftWall;
    [HideInInspector] public bool OnRightWall => onRightWall;
    private bool isWallRunning = false;
    private bool hasWallRun = false;
    private bool isGrounded = false;
    private bool onLeftWall = false;
    private bool onRightWall = false;
    private Vector3 wallNormal;
    private Vector3 lastWallNormal;

    // Componentes
    private CharacterController controller;
    private Animator animator;
    private Transform cameraTransform;
    private Transform cachedTransform;
    private PlayerRailRide_SonicStyle_Spline railRide;
    
    // ✅ NOVO: Referência ao WallDashJump para bloquear Wall Run
    private WallDashJump wallDashJump;

    // Movimento
    private Vector3 moveDirection = Vector3.zero;
    private Vector3 lastMoveDirection = Vector3.zero;
    public float currentSpeed;
    private float originalSpeedBeforeQuickTurn;
    private float minSpeedDuringQuickTurn;

    // Controle de animação
    [HideInInspector] public bool animatorBusy = false;
    [HideInInspector] public Quaternion targetRotation;

    // Variáveis para controle de estado de animação de pulo
    private bool isJumping = false;
    private bool isFalling = false;

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

        airDashCharges = maxAirDashCharges;
        doubleJumpCharges = maxDoubleJumpCharges;
    }

    void Update()
    {
        // ✅ OTIMIZADO: Early exit se estiver no rail
        if (railRide != null && railRide.isGrinding)
            return;

        // 1. Pré-processamento e Verificações de Estado
        CheckGround();
        CheckWallRun();
        HandleStomp();
        HandleAirDash();
        HandleAirInput();
        UpdateRotationLock();
        UpdateAirTrickCooldown();
        HandleProlongedIdle();
        UpdateAnimator();

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
        if (animatorBusy)
        {
            ApplyQuickTurnDeceleration();
            ApplyGravity();
        }
        else if (isWallRunning)
        {
            HandleWallRunInput();

            if (isWallRunning)
            {
                WallRunMovement();
            }
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

        // 5. Aplicação Final do Movimento
        if (controller.enabled)
        {
            controller.Move(moveDirection * Time.deltaTime);
        }
    }

    private void FixedUpdate()
    {
    }

    // ======================================================
    // MOVIMENTO NORMAL
    // ======================================================

    private void HandleInputAndMovement()
    {
        if (recoveringFromWallRun || isDashing) return;

        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        // ✅ OTIMIZADO: Reutiliza Vector3 em cache
        inputVector.x = horizontalInput;
        inputVector.y = 0;
        inputVector.z = verticalInput;
        float inputMagnitude = inputVector.magnitude;

        if (inputMagnitude > 0.1f)
        {
            if (isProlongedIdle)
            {
                isProlongedIdle = false;
                if (animator != null)
                    animator.SetBool("ProlongedIdle", false);
            }

            currentSpeed = Mathf.MoveTowards(currentSpeed, maxSpeed, acceleration * Time.deltaTime);

            if (cameraTransform != null)
            {
                // ✅ OTIMIZADO: Calcula uma vez
                cameraForward = cameraTransform.forward;
                cameraRight = cameraTransform.right;
                cameraForward.y = 0;
                cameraRight.y = 0;
                cameraForward.Normalize();
                cameraRight.Normalize();

                desiredMoveDirection = (cameraForward * verticalInput + cameraRight * horizontalInput).normalized;
                desiredMove = desiredMoveDirection * currentSpeed;

                if (controller.isGrounded && lastMoveDirection.sqrMagnitude > 0.01f && !animatorBusy)
                {
                    float angle = Vector3.Angle(lastMoveDirection, desiredMoveDirection);
                    if (angle >= quickTurnAngle && currentSpeed >= quickTurnThreshold)
                    {
                        TriggerQuickTurn();
                        return;
                    }
                }

                lastMoveDirection = desiredMoveDirection;

                moveDirection.x = desiredMove.x;
                moveDirection.z = desiredMove.z;
            }
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0, deceleration * Time.deltaTime);
            moveDirection.x = Mathf.MoveTowards(moveDirection.x, 0, deceleration * Time.deltaTime);
            moveDirection.z = Mathf.MoveTowards(moveDirection.z, 0, deceleration * Time.deltaTime);
        }
    }

    // ======================================================
    // WALL RUN
    // ======================================================

    private void CheckGround()
    {
        isGrounded = controller.isGrounded;

        // ✅ OTIMIZADO: Verificação adicional apenas se necessário
        if (!isGrounded && groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, GROUND_CHECK_RADIUS, groundMask);
        }

        if (isGrounded)
        {
            isStomping = false;
            hasWallRun = false;
        }
    }

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

        if ((onLeftWall || onRightWall) && !isWallRunning && !controller.isGrounded && !recoveringFromWallRun)
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

        styleRankSystem?.OnWallRunStart();

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
    }

    public void CompleteQuickTurn()
    {
        cachedTransform.rotation = targetRotation;
        moveDirection.x = -moveDirection.x;
        moveDirection.z = -moveDirection.z;
        lastMoveDirection = -lastMoveDirection;
        animatorBusy = false;
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
        if (Physics.Raycast(cachedTransform.position, Vector3.down, out raycastHit, RAYCAST_MAX_DISTANCE, groundMask))
        {
            float distanceToGround = raycastHit.distance;
            bool isHighEnough = distanceToGround >= minHeightForAirTrick;

            if (showDebugInfo)
            {
                Debug.DrawRay(cachedTransform.position, Vector3.down * distanceToGround,
                             isHighEnough ? Color.green : Color.yellow);
            }

            return isHighEnough;
        }

        return true;
    }

    private void TriggerAirTrick()
    {
        styleRankSystem?.OnAirTrickUsed();

        isRotationLocked = true;
        rotationLockTimer = airTrickRotationLockTime;
        airTrickCooldownTimer = airTrickCooldown;
        animatorBusy = true;

        if (enableAirTrickParticles && airTrickParticles != null)
        {
            StartAirTrickParticles();
        }

        if (showDebugInfo)
            Debug.Log($"🌀 Air Trick - Rotação travada por {airTrickRotationLockTime}s");
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

        // ✅ OTIMIZADO: Reutiliza Vector3
        horizontalMove.x = moveDirection.x;
        horizontalMove.y = 0;
        horizontalMove.z = moveDirection.z;

        if (horizontalMove.magnitude > 0.1f)
        {
            // ✅ OTIMIZADO: Atualizar target apenas periodicamente
            rotationUpdateTimer += Time.deltaTime;
            if (rotationUpdateTimer >= ROTATION_UPDATE_INTERVAL)
            {
                cachedTargetRotation = Quaternion.LookRotation(horizontalMove);
                rotationUpdateTimer = 0f;
            }
            
            // ✅ OTIMIZADO: Usar Lerp é mais rápido que RotateTowards
            cachedTransform.rotation = Quaternion.Lerp(
                cachedTransform.rotation,
                cachedTargetRotation,
                Time.deltaTime * turnSpeed * 0.1f
            );
        }
    }

    // ======================================================
    // GRAVIDADE
    // ======================================================

    private void ApplyGravity()
    {
        if (isDashing || recoveringFromWallRun || isStomping) return;

        float effectiveGravity = isWallRunning ? wallRunGravity : gravity;

        if (controller.isGrounded)
        {
            moveDirection.y = -controller.stepOffset;
            isFalling = false;

            if (Input.GetButtonDown("Jump") && !animatorBusy)
            {
                moveDirection.y = jumpForce;
                isJumping = true;

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
        }
        else
        {
            if (Input.GetButtonDown("Jump") && !animatorBusy && doubleJumpCharges > 0)
            {
                moveDirection.y = jumpForce;
                doubleJumpCharges--;
                isJumping = true;
                isFalling = false;

                if (animator != null)
                    animator.SetTrigger("DoubleJump");

                if (enableDoubleJumpParticles && doubleJumpParticles != null)
                {
                    StartDoubleJumpParticles();
                }

                if (showDebugInfo)
                    Debug.Log($"🚀 Pulo Duplo! Cargas restantes: {doubleJumpCharges}");
            }

            moveDirection.y -= effectiveGravity * Time.deltaTime;

            // ✅ OTIMIZADO: Verificações simplificadas
            if (moveDirection.y < -0.1f)
            {
                isFalling = true;
                isJumping = false;
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
        if (!controller.isGrounded && !isDashing && !isStomping && !isWallRunning)
        {
            if (Input.GetKeyDown(stompKey))
            {
                StartStomp();
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
        if (!controller.isGrounded && !isWallRunning && !isDashing && !isStomping && airDashCharges > 0)
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

        // ✅ OTIMIZADO: Só atualiza IsGrounded se mudou
        if (controller.isGrounded != cachedIsGrounded)
        {
            animator.SetBool("IsGrounded", controller.isGrounded);
            cachedIsGrounded = controller.isGrounded;
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
        if (showDebugInfo)
            Debug.Log($"✅ Cargas aéreas resetadas no grind. Double Jump: {doubleJumpCharges}, Air Dash: {airDashCharges}");
    }

    // ======================================================
    // CONTROLE DE PARTICULAS
    // ======================================================

    private void StartAirDashParticles()
    {
        if (airDashParticles == null) return;

        if (!airDashParticles.isPlaying)
        {
            airDashParticles.Play();

            if (showDebugInfo)
                Debug.Log("Particulas de air dash iniciadas.");
        }
    }

    private void StopAirDashParticles()
    {
        if (airDashParticles == null) return;

        if (airDashParticles.isPlaying)
        {
            airDashParticles.Stop();

            if (showDebugInfo)
                Debug.Log("Particulas de air dash paradas.");
        }
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

        if (!airTrickParticles.isPlaying)
        {
            airTrickParticles.Play();

            if (showDebugInfo)
                Debug.Log("Particulas de air trick iniciadas.");
        }
    }

    private void StopAirTrickParticles()
    {
        if (airTrickParticles == null) return;

        if (airTrickParticles.isPlaying)
        {
            airTrickParticles.Stop();

            if (showDebugInfo)
                Debug.Log("Particulas de air trick paradas.");
        }
    }

    public void ResetMovementDirection()
    {
        moveDirection = Vector3.zero;
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
        GUI.Label(new Rect(10, 110, 300, 20), $"Height: {GetCurrentHeight():F1}m / Min: {minHeightForAirTrick}m");
        GUI.Label(new Rect(10, 130, 300, 20), $"Air Trick Cooldown: {airTrickCooldownTimer:F2}s");
        GUI.Label(new Rect(10, 150, 300, 20), $"Air Dash: {isDashing} (Charges: {airDashCharges}/{maxAirDashCharges})");
        GUI.Label(new Rect(10, 170, 300, 20), $"Double Jump: (Charges: {doubleJumpCharges}/{maxDoubleJumpCharges})");
        GUI.Label(new Rect(10, 190, 300, 20), $"On Rail: {(railRide != null && railRide.isGrinding ? "Yes" : "No")}");
        GUI.Label(new Rect(10, 210, 300, 20), $"Stomp: {isStomping}");
    }

    public float CurrentSpeed => currentSpeed;
    public bool IsGrounded => controller.isGrounded;
    public bool IsRotationLocked => isRotationLocked;
    public bool IsStomping => isStomping;
}
