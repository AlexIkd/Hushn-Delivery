using UnityEngine;

public class SlopeSlideSystem : MonoBehaviour
{
    private CharacterController controller;
    private PlayerMovement_FrontiersStyle playerMovement; 
    private Animator animator;

    [Header("Estado")]
    public bool isSliding = false;

    [Header("Configurações de Movimento")]
    public float slideSpeed = 15f;
    public float lateralSpeed = 10f; // Velocidade lateral para controle total
    public float slideGravity = 25f; // Gravidade forte para manter o fluxo na rampa
    public LayerMask rampLayer; 
    public float raycastDistance = 0.2f; 
    public float slideRaycastDistance = 0.5f; 
    public float minSlideAngle = 30f; 
    
    [Header("Estabilização (Anti-Jitter)")]
    public float stickToGroundForce = 8f; // Aumentado para colar o jogador na rampa
    public float rotationSmoothSpeed = 15f; // Alinhamento mais rápido e preciso

    [Header("Efeitos Visuais")]
    public ParticleSystem slideParticles; // Sistema de partículas de terra/poeira
    public bool enableParticles = true;
    [SerializeField] private bool showDebugInfo = false;

    [Header("Saída do Slide")]
    public float speedOnExit = 20f; 
    public float minSlideDuration = 0.2f; 
    private float slideTimeCounter = 0f;

    [Header("Cooldown")]
    public float slideCooldown = 0.5f; 
    private float cooldownTimer = 0f;

    [Header("Pulo Automático Inteligente")]
    [SerializeField] private bool autoJumpAtEnd = true;
    [SerializeField] private float autoJumpForce = 10f;
    [SerializeField] private float forwardBoost = 5f;
    [SerializeField] private float earlyJumpDistance = 1.5f; 

    private Vector3 slideVelocity; 
    private Vector3 hitNormal; 
    private bool hasTriggeredAutoJump = false; 

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovement_FrontiersStyle>();
        animator = GetComponentInChildren<Animator>(); // Tenta pegar o animator no objeto ou nos filhos

        // Garante que as partículas não comecem tocando (Padrão Frontiers Style)
        if (slideParticles != null)
        {
            slideParticles.Stop();
        }
    }

    void Update()
    {
        if (cooldownTimer > 0) cooldownTimer -= Time.deltaTime;

        if (isSliding)
        {
            slideTimeCounter += Time.deltaTime;
            HandleSlide();
            
            if (autoJumpAtEnd && !hasTriggeredAutoJump)
            {
                CheckForAutoJump();
            }

            if (slideTimeCounter >= minSlideDuration)
            {
                CheckExitSlideConditions();
                if (Input.GetButtonDown("Jump")) ExitSlide();
            }
        }
        else if (cooldownTimer <= 0)
        {
            CheckForRamp();
        }
    }

    public bool IsSliding() => isSliding;

    private void HandleSlide()
    {
        // 1. Direção absoluta de descida (puro vetor da rampa)
        Vector3 slopeDownDirection = Vector3.ProjectOnPlane(Vector3.down, hitNormal).normalized;
        
        // 2. Velocidade de descida fixa e limpa (sem interferência de momentum anterior)
        Vector3 verticalFlow = slopeDownDirection * slideSpeed;

        // 3. Controle lateral absoluto
        float h = Input.GetAxis("Horizontal");
        // O eixo lateral é sempre perpendicular à descida e à normal da rampa
        Vector3 lateralAxis = Vector3.Cross(hitNormal, slopeDownDirection).normalized; 
        Vector3 lateralMove = lateralAxis * h * lateralSpeed;

        // 4. Força de aderência para evitar que o CharacterController "quique" ou perca o estado
        Vector3 stickForce = -hitNormal * stickToGroundForce;

        // 5. Movimento Final: Apenas o fluxo da rampa + seu controle lateral
        // Note que não acumulamos slideVelocity aqui para garantir que o controle seja 1:1 com o seu input
        Vector3 finalMovement = verticalFlow + lateralMove + stickForce;
        
        controller.Move(finalMovement * Time.deltaTime);

        // 6. Alinhamento visual instantâneo e limpo
        if (slopeDownDirection != Vector3.zero)
        {
            // O personagem sempre olha para onde a rampa desce, ajustado pelo seu controle lateral
            Vector3 visualDir = (slopeDownDirection + (lateralAxis * h * 0.5f)).normalized;
            Quaternion targetRot = Quaternion.LookRotation(visualDir, hitNormal);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSmoothSpeed);
        }
    }

    private void CheckForAutoJump()
    {
        Vector3 slideDirection = Vector3.ProjectOnPlane(Vector3.down, hitNormal).normalized;
        Vector3 origin = transform.position + (slideDirection * earlyJumpDistance);
        
        RaycastHit hit;
        bool groundAhead = Physics.Raycast(origin, Vector3.down, out hit, 5f, rampLayer);

        if (!groundAhead)
        {
            TriggerAutoJump(autoJumpForce, forwardBoost);
        }
    }

    public void TriggerAutoJump(float jumpForce, float boost)
    {
        if (!isSliding || hasTriggeredAutoJump) return;

        hasTriggeredAutoJump = true;
        
        Vector3 jumpDir = transform.forward;
        ExitSlide();

        if (playerMovement != null)
        {
            playerMovement.currentSpeed = speedOnExit + boost;
            Vector3 boostVelocity = (jumpDir * (slideSpeed + boost)) + (Vector3.up * jumpForce);
            controller.Move(boostVelocity * Time.deltaTime);
        }
    }

    private void CheckForRamp()
    {
        RaycastHit hit;
        Vector3 raycastOrigin = GetRaycastOrigin();

        if (Physics.Raycast(raycastOrigin, Vector3.down, out hit, raycastDistance, rampLayer))
        {
            float angle = Vector3.Angle(Vector3.up, hit.normal);
            if (angle > minSlideAngle && !isSliding)
            {
                hitNormal = hit.normal;
                EnterSlide();
            }
        }
    }

    private void CheckExitSlideConditions()
    {
        RaycastHit hit;
        Vector3 raycastOrigin = GetRaycastOrigin();

        // Usamos slideRaycastDistance aqui para ser mais tolerante enquanto desliza
        if (Physics.Raycast(raycastOrigin, Vector3.down, out hit, slideRaycastDistance, rampLayer))
        {
            float angle = Vector3.Angle(Vector3.up, hit.normal);
            if (angle < minSlideAngle) ExitSlide();
            else hitNormal = hit.normal;
        }
        else 
        {
            // Se o raycast falhar, tentamos um raio um pouco mais longo para garantir que não é apenas um pequeno degrau
            if (!Physics.Raycast(raycastOrigin, Vector3.down, out hit, slideRaycastDistance * 1.5f, rampLayer))
            {
                ExitSlide();
            }
        }
    }

    private Vector3 GetRaycastOrigin()
    {
        // Origem do raio na base do collider para maior precisão
        return transform.position + Vector3.up * 0.1f;
    }

    private void EnterSlide()
    {
        isSliding = true;
        slideTimeCounter = 0f;
        hasTriggeredAutoJump = false;

        if (animator != null) animator.SetBool("isSliding", true);
        
        if (enableParticles)
        {
            StartSlideParticles();
        }

        // RESET TOTAL DE ESTADO:
        // Limpamos qualquer resquício de movimento anterior para que o slide seja 100% controlado pela rampa
        if (playerMovement != null)
        {
            playerMovement.ResetMovementDirection(); // Zera moveDirection no script principal
            playerMovement.enabled = false;
            playerMovement.animatorBusy = true;
        }
        
        slideVelocity = Vector3.zero;
        
        // Alinhamento inicial imediato com a rampa para evitar o "tranco" visual
        Vector3 initialDown = Vector3.ProjectOnPlane(Vector3.down, hitNormal).normalized;
        if (initialDown != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(initialDown, hitNormal);
        }
    }

    private void ExitSlide()
    {
        isSliding = false;
        cooldownTimer = slideCooldown;

        if (animator != null) animator.SetBool("isSliding", false);
        
        StopSlideParticles();

        if (playerMovement != null)
        {
            playerMovement.enabled = true;
            playerMovement.animatorBusy = false;
            playerMovement.currentSpeed = speedOnExit;
        }
        slideVelocity = Vector3.zero;
    }

    // ======================================================
    // CONTROLE DE PARTICULAS (Padrão Frontiers Style)
    // ======================================================

    private void StartSlideParticles()
    {
        if (slideParticles == null) return;

        if (!slideParticles.isPlaying)
        {
            slideParticles.Play();

            if (showDebugInfo)
                Debug.Log("Particulas de slide iniciadas.");
        }
    }

    private void StopSlideParticles()
    {
        if (slideParticles == null) return;

        if (slideParticles.isPlaying)
        {
            slideParticles.Stop();

            if (showDebugInfo)
                Debug.Log("Particulas de slide paradas.");
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector3 raycastOrigin = GetRaycastOrigin();
        float dist = isSliding ? slideRaycastDistance : raycastDistance;
        Gizmos.DrawLine(raycastOrigin, raycastOrigin + Vector3.down * dist);

        if (isSliding)
        {
            Gizmos.color = Color.yellow;
            Vector3 slideDir = Vector3.ProjectOnPlane(Vector3.down, hitNormal).normalized;
            Vector3 edgeOrigin = transform.position + (slideDir * earlyJumpDistance);
            Gizmos.DrawLine(edgeOrigin, edgeOrigin + Vector3.down * 5f);
        }
    }
}
