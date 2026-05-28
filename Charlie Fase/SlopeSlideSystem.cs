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
    public float lateralSpeed = 5f;
    public float slideGravity = 20f; 
    public LayerMask rampLayer; 
    public float raycastDistance = 0.2f; 
    public float slideRaycastDistance = 0.5f; // Distância maior durante o slide para evitar "saltos"
    public float minSlideAngle = 30f; 
    
    [Header("Estabilização (Anti-Jitter)")]
    public float stickToGroundForce = 5f; // Força extra para baixo para manter contato com a rampa
    public float rotationSmoothSpeed = 10f; // Velocidade de rotação/alinhamento com a rampa

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
        // Calcula a direção baseada na inclinação
        Vector3 slideDirection = Vector3.ProjectOnPlane(Vector3.down, hitNormal).normalized;
        slideVelocity = slideDirection * slideSpeed;

        // Movimento lateral
        float h = Input.GetAxis("Horizontal");
        Vector3 lateralAxis = Vector3.Cross(hitNormal, slideDirection).normalized; 
        Vector3 lateralMove = lateralAxis * h * lateralSpeed;

        // Gravidade normal do slide
        slideVelocity.y -= slideGravity * Time.deltaTime;

        // ADERÊNCIA: Adiciona uma força constante na direção oposta à normal da rampa (empurrando contra o chão)
        // Isso ajuda o CharacterController a não perder o contato em mudanças bruscas de ângulo
        Vector3 stickForce = -hitNormal * stickToGroundForce;

        controller.Move((slideVelocity + lateralMove + stickForce) * Time.deltaTime);

        // Rotaciona o personagem para alinhar com a rampa e a direção do movimento
        if (slideDirection != Vector3.zero)
        {
            // 1. Calcula a rotação para olhar na direção do slide
            Quaternion lookRotation = Quaternion.LookRotation(slideDirection, hitNormal);
            
            // 2. Aplica o Slerp para uma transição suave entre a rotação atual e o alinhamento com a rampa
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSmoothSpeed);
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

        if (playerMovement != null)
        {
            playerMovement.enabled = false;
            playerMovement.animatorBusy = true;
        }
        slideVelocity = Vector3.zero;
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
