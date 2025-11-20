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
    [SerializeField] private KeyCode stompKey = KeyCode.LeftControl;
    [SerializeField] private ParticleSystem stompParticles; // Sistema de partículas ao ativar stomp
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
    private Quaternion lockedRotation; // NOVO: Rotação travada para o rail ride

    private float wallRunTimer = 0f;

    [Header("Particulas de Movimento")]
    [SerializeField] private ParticleSystem airDashParticles; // Sistema de particulas para air dash
    [SerializeField] private bool enableAirDashParticles = true; // Habilita particulas durante air dash
    [SerializeField] private ParticleSystem jumpParticles; // Sistema de particulas para pulo
    [SerializeField] private bool enableJumpParticles = true; // Habilita particulas durante pulo
    [SerializeField] private ParticleSystem doubleJumpParticles; // Sistema de particulas para duplo pulo
    [SerializeField] private bool enableDoubleJumpParticles = true; // Habilita particulas durante duplo pulo
    [SerializeField] private ParticleSystem airTrickParticles; // Sistema de particulas para air trick
    [SerializeField] private bool enableAirTrickParticles = true; // Habilita particulas durante air trick
    [SerializeField] private ParticleSystem wallRunLeftParticles; // Sistema de particulas para wall run esquerdo
    [SerializeField] private ParticleSystem wallRunRightParticles; // Sistema de particulas para wall run direito
    [SerializeField] private bool enableWallRunParticles = true; // Habilita particulas durante wall run
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true; // Mostra informacoes de debug no console

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
    private PlayerRailRide_SonicStyle_Spline railRide;

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

    // Velocidade externa (para transições suaves)
    private Vector3 externalVelocity = Vector3.zero;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        cameraTransform = Camera.main ? Camera.main.transform : null;
        railRide = GetComponent<PlayerRailRide_SonicStyle_Spline>();

        if (animator == null)
            Debug.LogWarning("Animator não encontrado no PlayerMovement_FrontiersStyle!");

        if (controller == null)
            Debug.LogError("CharacterController não encontrado no PlayerMovement_FrontiersStyle!");

        // Inicializa a referência ao StyleRankSystem
        styleRankSystem = FindObjectOfType<StyleRankSystem>();
        if (styleRankSystem == null)
        {
            Debug.LogWarning("StyleRankSystem não encontrado na cena. O ranking de estilo não funcionará.");
        }

        // Validação do groundCheck
        if (groundCheck == null)
        {
            Debug.LogError("groundCheck não está atribuído! Crie um GameObject filho na posição dos pés do personagem e atribua-o.");
            
            // Cria automaticamente um groundCheck se não existir
            GameObject groundCheckObj = new GameObject("GroundCheck");
            groundCheckObj.transform.SetParent(transform);
            groundCheckObj.transform.localPosition = new Vector3(0, -controller.height / 2f, 0);
            groundCheck = groundCheckObj.transform;
            
            Debug.Log("GroundCheck criado automaticamente. Ajuste a posição se necessário.");
        }

        airDashCharges = maxAirDashCharges;
        doubleJumpCharges = maxDoubleJumpCharges;
    }

    void Update()
    {
        // Não processa movimento se estiver no rail
        if (railRide != null && railRide.isGrinding)
        {
            return;
        }

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

        // 2. Lógica de Recuperação (Wall Run Recovery)
        if (recoveringFromWallRun)
        {
            wallRunRecoveryTimer -= Time.deltaTime;
            if (wallRunRecoveryTimer <= 0f)
            {
                recoveringFromWallRun = false;
                Debug.Log("✅ Recuperação do Wall Run finalizada.");
            }
        }

        // 3. Aplica velocidade externa (para transições suaves)
        if (externalVelocity.magnitude > 0.1f)
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
        if (recoveringFromWallRun || isDashing) return; // CORREÇÃO: Não processar input durante dash

        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        Vector3 inputVector = new Vector3(horizontalInput, 0, verticalInput);
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
                Vector3 cameraForward = Vector3.Scale(cameraTransform.forward, new Vector3(1, 0, 1)).normalized;
                Vector3 cameraRight = Vector3.Scale(cameraTransform.right, new Vector3(1, 0, 1)).normalized;
                Vector3 desiredMoveDirection = (cameraForward * verticalInput + cameraRight * horizontalInput).normalized;
                Vector3 desiredMove = desiredMoveDirection * currentSpeed;

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

        if (!isGrounded && groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, 0.1f, groundMask);
        }

        if (isGrounded)
        {
            isStomping = false;
            hasWallRun = false;
        }
    }

    private void CheckWallRun()
    {
        Vector3 leftRayStart = transform.position;
        Vector3 rightRayStart = transform.position;

        onLeftWall = Physics.Raycast(leftRayStart, -transform.right, out RaycastHit leftHit, wallDistance + controller.radius, wallMask);
        onRightWall = Physics.Raycast(rightRayStart, transform.right, out RaycastHit rightHit, wallDistance + controller.radius, wallMask);

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
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, float.MaxValue, groundMask))
            {
                if (hit.distance < minDistanceToGroundForWallRun)
                {
                    return;
                }
            }
            
            wallNormal = onLeftWall ? leftHit.normal : rightHit.normal;

            Vector3 horizontalMoveDirection = new Vector3(moveDirection.x, 0, moveDirection.z).normalized;
            float angleToWall = Vector3.Angle(-horizontalMoveDirection, wallNormal);

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

        // Adiciona pontos de estilo ao iniciar o Wall Run
        styleRankSystem?.OnWallRunStart();

        isWallRunning = true;
        wallRunTimer = 0f;
        
        if (airDashCharges < maxAirDashCharges)
        {
            airDashCharges = maxAirDashCharges;
            Debug.Log($"✅ Dash Aéreo resetado ao iniciar Wall Run. Cargas: {airDashCharges}");
        }
        
        if (doubleJumpCharges < maxDoubleJumpCharges)
        {
            doubleJumpCharges = maxDoubleJumpCharges;
            Debug.Log($"✅ Pulo Duplo resetado ao iniciar Wall Run. Cargas: {doubleJumpCharges}");
        }

        hasWallRun = true;

        Vector3 forwardDir = Vector3.Cross(wallNormal, Vector3.up);
        if (Vector3.Dot(forwardDir, transform.forward) < 0)
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
        
        // Inicia particulas de wall run
        if (enableWallRunParticles)
        {
            StartWallRunParticles();
        }

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
            Vector3 jumpDirection = (wallNormal + Vector3.up).normalized;
            moveDirection = jumpDirection * jumpForce;
            recoveringFromWallRun = true;
            wallRunRecoveryTimer = wallRunRecoveryTime;
            Debug.Log($"🌀 Pulou e entrou em recuperação após Wall Run!");
        }
        else
        {
            moveDirection.y = 0;
        }
        
        // Para particulas de wall run
        if (enableWallRunParticles)
        {
            StopWallRunParticles();
        }

        Debug.Log("Wall Run encerrado!");
    }

    private void WallRunMovement()
    {
        wallRunTimer += Time.deltaTime;

        RaycastHit hit;
        Vector3 rayDirection = onLeftWall ? -transform.right : transform.right;
        float dist = controller.radius + 0.1f;

        if (Physics.Raycast(transform.position, rayDirection, out hit, dist, wallMask))
        {
            float distanceToWall = hit.distance;
            float desiredDistance = controller.radius + 0.01f;
            float offset = distanceToWall - desiredDistance;

            if (offset < 0)
            {
                transform.position += hit.normal * -offset;
            }
        }

        if (wallRunTimer >= wallRunDuration)
        {
            ExitWallRun(true);
            return;
        }

        Vector3 forwardDir = Vector3.Cross(wallNormal, Vector3.up);
        if (Vector3.Dot(forwardDir, transform.forward) < 0)
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
        targetRotation = Quaternion.LookRotation(-transform.forward);
        
        if (animator != null)
            animator.SetTrigger("QuickTurn");
        
        animatorBusy = true;
    }

    public void CompleteQuickTurn()
    {
        transform.rotation = targetRotation;
        moveDirection.x = -moveDirection.x;
        moveDirection.z = -moveDirection.z;
        lastMoveDirection = -lastMoveDirection;
        animatorBusy = false;
    }

    private void ApplyQuickTurnDeceleration()
    {
        float targetSpeed = Mathf.Max(minSpeedDuringQuickTurn, 0);
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, quickTurnDeceleration * Time.deltaTime);
        Vector3 horizontal = new Vector3(moveDirection.x, 0, moveDirection.z).normalized;
        moveDirection.x = horizontal.x * currentSpeed;
        moveDirection.z = horizontal.z * currentSpeed;
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
                TriggerAirTrick();
            }
            else if (Input.GetKeyDown(KeyCode.S))
            {
                triggerName = "AirInput_Backward";
                TriggerAirTrick();
            }
            else if (Input.GetKeyDown(KeyCode.A))
            {
                triggerName = "AirInput_Left";
                TriggerAirTrick();
            }
            else if (Input.GetKeyDown(KeyCode.D))
            {
                triggerName = "AirInput_Right";
                TriggerAirTrick();
            }

            if (!string.IsNullOrEmpty(triggerName) && animator != null)
            {
                animator.SetTrigger(triggerName);
            }
        }
    }

    private bool IsHighEnoughForAirTrick()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, float.MaxValue, groundMask))
        {
            float distanceToGround = hit.distance;
            bool isHighEnough = distanceToGround >= minHeightForAirTrick;
            
            Debug.DrawRay(transform.position, Vector3.down * distanceToGround, 
                         isHighEnough ? Color.green : Color.yellow);
            
            return isHighEnough;
        }
        
        return true;
    }

    private void TriggerAirTrick()
    {
        // Adiciona pontos de estilo ao usar o Air Trick
        styleRankSystem?.OnAirTrickUsed();

        isRotationLocked = true;
        rotationLockTimer = airTrickRotationLockTime;
        airTrickCooldownTimer = airTrickCooldown;
        animatorBusy = true;
        
        // Inicia particulas de air trick
        if (enableAirTrickParticles && airTrickParticles != null)
        {
            StartAirTrickParticles();
        }
        
        Debug.Log($"🌀 Air Trick - Rotação travada por {airTrickRotationLockTime} segundos e Cooldown iniciado por {airTrickCooldown} segundos (Altura: {GetCurrentHeight():F1}m)");
    }

    private float GetCurrentHeight()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, float.MaxValue, groundMask))
        {
            return hit.distance;
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
                // A trava de rotação do Air Trick é desativada aqui
                isRotationLocked = false;
                animatorBusy = false;
                Debug.Log("✅ Rotação destravada após Air Trick");
            }
        }
    }

    /// <summary>
    /// Trava ou destrava a rotação do jogador (usado por sistemas externos como Rail Ride)
    /// </summary>
    public void LockRotation(bool lockRotation)
    {
        isRotationLocked = lockRotation;
        if (lockRotation)
        {
            // Salva a rotação atual quando a trava é ativada
            lockedRotation = transform.rotation;
        }
        else
        {
            // Se estiver destravando, garante que o timer do Air Trick também seja resetado
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
            // Mantém a rotação travada na última rotação salva (do LockRotation ou Air Trick)
            transform.rotation = lockedRotation;
            return;
        }

        if (animatorBusy) return;
        
        Vector3 horizontalMove = new Vector3(moveDirection.x, 0, moveDirection.z);
        if (horizontalMove.magnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(horizontalMove);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
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
                    Debug.Log($"✅ Dash Aéreo resetado ao iniciar o pulo. Cargas: {airDashCharges}");
                }
                if (doubleJumpCharges < maxDoubleJumpCharges)
                {
                    doubleJumpCharges = maxDoubleJumpCharges;
                    Debug.Log($"✅ Pulo Duplo resetado ao iniciar o pulo. Cargas: {doubleJumpCharges}");
                }
                
                // Inicia particulas de pulo
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
                
                // Inicia particulas de duplo pulo
                if (enableDoubleJumpParticles && doubleJumpParticles != null)
                {
                    StartDoubleJumpParticles();
                }
                
                Debug.Log($"🚀 Pulo Duplo! Cargas restantes: {doubleJumpCharges}");
            }

            moveDirection.y -= effectiveGravity * Time.deltaTime;

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
        // Permite stomp apenas quando está no ar
        if (!controller.isGrounded && !isDashing && !isStomping && !isWallRunning)
        {
            if (Input.GetKeyDown(stompKey))
            {
                StartStomp();
            }
        }
        
        // Aplica a força de stomp continuamente enquanto estiver stompando
        if (isStomping && !controller.isGrounded)
        {
            ApplyStompForce();
        }
    }

    private void StartStomp()
    {
        isStomping = true;
        
        // Cancela qualquer movimento horizontal e aplica impulso forte para baixo
        moveDirection.x = 0;
        moveDirection.z = 0;
        moveDirection.y = -stompForce;
        
        // Ativa o sistema de partículas
        if (stompParticles != null)
        {
            stompParticles.Play();
        }
        
        Debug.Log($"💥 Stomp ativado! Força: {stompForce}");
    }
    
    private void ApplyStompForce()
    {
        // Mantém a força de stomp constante até atingir o chão
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

        Vector3 dashDirection = transform.forward; // Padrão é a direção de visão
        
        // Se houver movimento horizontal, use a direção do movimento
        Vector3 horizontalMove = new Vector3(moveDirection.x, 0, moveDirection.z);
        if (horizontalMove.sqrMagnitude > 0.01f)
        {
            dashDirection = horizontalMove.normalized;
        }

        moveDirection = dashDirection * airDashForce;
        moveDirection.y = 0;


        styleRankSystem?.OnAirDashUsed();
        
        if (animator != null)
            animator.SetTrigger("AirDash");
        
        // Inicia particulas de air dash
        if (enableAirDashParticles && airDashParticles != null)
        {
            StartAirDashParticles();
        }
        
        Debug.Log($"💨 Dash Aéreo! Cargas restantes: {airDashCharges}");
    }

    private void StopAirDash()
    {
        isDashing = false;
        
        // Para particulas de air dash
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
        animator.SetFloat("Speed", normalizedSpeed);
        animator.SetBool("IsGrounded", controller.isGrounded);
        animator.SetBool("IsWallRunning", isWallRunning);
        animator.SetBool("IsJumping", isJumping);
        animator.SetBool("IsFalling", isFalling);
        animator.SetBool("OnLeftWall", onLeftWall);
        animator.SetBool("OnRightWall", onRightWall);
    }

    // ======================================================
    // MÉTODOS PÚBLICOS PARA INTEGRAÇÃO
    // ======================================================

    /// <summary>
    /// Adiciona velocidade externa ao movimento (usado para transições suaves)
    /// </summary>
    public void AddExternalVelocity(Vector3 velocity)
    {
        externalVelocity = velocity;
        Debug.Log($"⚡ Velocidade externa adicionada: {velocity}");
    }

    /// <summary>
    /// Força a saída do wall run (usado pelo sistema de grind)
    /// </summary>
    public void ForceExitWallRun()
    {
        if (isWallRunning)
        {
            ExitWallRun(false);
        }
    }

    /// <summary>
    /// Reseta as cargas de pulo duplo e air dash (usado pelo sistema de grind)
    /// </summary>
    public void ResetAirCharges()
    {
        doubleJumpCharges = maxDoubleJumpCharges;
        airDashCharges = maxAirDashCharges;
        Debug.Log($"✅ Cargas aéreas resetadas no grind. Double Jump: {doubleJumpCharges}, Air Dash: {airDashCharges}");
    }

    // ======================================================
    // CONTROLE DE PARTICULAS
    // ======================================================

    /// <summary>
    /// Inicia particulas de air dash (child do personagem)
    /// </summary>
    private void StartAirDashParticles()
    {
        if (airDashParticles == null) return;
        
        // Ativa o sistema de particulas
        if (!airDashParticles.isPlaying)
        {
            airDashParticles.Play();
            
            if (showDebugInfo)
            {
                Debug.Log("Particulas de air dash iniciadas.");
            }
        }
    }
    
    /// <summary>
    /// Para as particulas de air dash
    /// </summary>
    private void StopAirDashParticles()
    {
        if (airDashParticles == null) return;
        
        // Para o sistema de particulas
        if (airDashParticles.isPlaying)
        {
            airDashParticles.Stop();
            
            if (showDebugInfo)
            {
                Debug.Log("Particulas de air dash paradas.");
            }
        }
    }

    /// <summary>
    /// Inicia particulas de pulo (child do personagem)
    /// </summary>
    private void StartJumpParticles()
    {
        if (jumpParticles == null) return;
        
        // Ativa o sistema de particulas
        if (!jumpParticles.isPlaying)
        {
            jumpParticles.Play();
            
            if (showDebugInfo)
            {
                Debug.Log("Particulas de pulo iniciadas.");
            }
        }
    }
    
    /// <summary>
    /// Para as particulas de pulo
    /// </summary>
    private void StopJumpParticles()
    {
        if (jumpParticles == null) return;
        
        // Para o sistema de particulas
        if (jumpParticles.isPlaying)
        {
            jumpParticles.Stop();
            
            if (showDebugInfo)
            {
                Debug.Log("Particulas de pulo paradas.");
            }
        }
    }

    /// <summary>
    /// Inicia particulas de duplo pulo (child do personagem)
    /// </summary>
    private void StartDoubleJumpParticles()
    {
        if (doubleJumpParticles == null) return;
        
        // Ativa o sistema de particulas
        if (!doubleJumpParticles.isPlaying)
        {
            doubleJumpParticles.Play();
            
            if (showDebugInfo)
            {
                Debug.Log("Particulas de duplo pulo iniciadas.");
            }
        }
    }
    
    /// <summary>
    /// Para as particulas de duplo pulo
    /// </summary>
    private void StopDoubleJumpParticles()
    {
        if (doubleJumpParticles == null) return;
        
        // Para o sistema de particulas
        if (doubleJumpParticles.isPlaying)
        {
            doubleJumpParticles.Stop();
            
            if (showDebugInfo)
            {
                Debug.Log("Particulas de duplo pulo paradas.");
            }
        }
    }

    /// <summary>
    /// Inicia particulas de air trick (child do personagem)
    /// </summary>
    private void StartAirTrickParticles()
    {
        if (airTrickParticles == null) return;
        
        // Ativa o sistema de particulas
        if (!airTrickParticles.isPlaying)
        {
            airTrickParticles.Play();
            
            if (showDebugInfo)
            {
                Debug.Log("Particulas de air trick iniciadas.");
            }
        }
    }
    
    /// <summary>
    /// Para as particulas de air trick
    /// </summary>
    private void StopAirTrickParticles()
    {
        if (airTrickParticles == null) return;
        
        // Para o sistema de particulas
        if (airTrickParticles.isPlaying)
        {
            airTrickParticles.Stop();
            
            if (showDebugInfo)
            {
                Debug.Log("Particulas de air trick paradas.");
            }
        }
    }

    /// <summary>
    /// Inicia particulas de wall run (child do personagem)
    /// </summary>
    private void StartWallRunParticles()
    {
        // Ativa o sistema de particulas correto baseado no lado do wall run
        if (onLeftWall && wallRunLeftParticles != null)
        {
            if (!wallRunLeftParticles.isPlaying)
            {
                wallRunLeftParticles.Play();
                
                if (showDebugInfo)
                {
                    Debug.Log("Particulas de wall run esquerdo iniciadas.");
                }
            }
        }
        else if (onRightWall && wallRunRightParticles != null)
        {
            if (!wallRunRightParticles.isPlaying)
            {
                wallRunRightParticles.Play();
                
                if (showDebugInfo)
                {
                    Debug.Log("Particulas de wall run direito iniciadas.");
                }
            }
        }
    }
    
    /// <summary>
    /// Para as particulas de wall run
    /// </summary>
    private void StopWallRunParticles()
    {
        // Para ambos os sistemas de particulas (caso algum esteja ativo)
        if (wallRunLeftParticles != null && wallRunLeftParticles.isPlaying)
        {
            wallRunLeftParticles.Stop();
            
            if (showDebugInfo)
            {
                Debug.Log("Particulas de wall run esquerdo paradas.");
            }
        }
        
        if (wallRunRightParticles != null && wallRunRightParticles.isPlaying)
        {
            wallRunRightParticles.Stop();
            
            if (showDebugInfo)
            {
                Debug.Log("Particulas de wall run direito paradas.");
            }
        }
    }

    // ======================================================
    // DEBUG E PROPRIEDADES
    // ======================================================

    private void OnGUI()
    {
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
