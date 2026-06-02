using UnityEngine;

public class HorizontalBarHandler : MonoBehaviour
{
    [Header("Detecção")]
    [SerializeField] private float detectionRadius = 2.0f;
    [SerializeField] private LayerMask barLayer;
    [SerializeField] private float grabCooldown = 0.5f;
    
    [Header("Configurações Visuais")]
    [SerializeField] private float playerRadiusOffset = 1.2f;
    
    private PlayerMovement_FrontiersStyle playerMovement;
    private CharacterController controller;
    private Animator animator;
    
    private HorizontalBar currentBar;
    private bool isGrabbing = false;
    private float currentAngle = 0f;
    private Vector3 barRight;
    private float cooldownTimer = 0f;
    
    void Start()
    {
        playerMovement = GetComponent<PlayerMovement_FrontiersStyle>();
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        if (playerMovement == null) Debug.LogError("PlayerMovement_FrontiersStyle não encontrado no mesmo GameObject.");
        if (controller == null) Debug.LogError("CharacterController não encontrado no mesmo GameObject.");
    }

    void Update()
    {
        if (cooldownTimer > 0) cooldownTimer -= Time.deltaTime;

        if (isGrabbing)
        {
            HandleSwinging();
        }
        else
        {
            CheckForBar();
        }
    }

    void LateUpdate()
    {
        if (isGrabbing && currentBar != null)
        {
            UpdatePlayerPosition();
        }
    }

    private void CheckForBar()
    {
        // Não tenta pegar a barra se estiver no chão, wall running, ou em cooldown
        if (cooldownTimer > 0 || (controller != null && controller.isGrounded) || (playerMovement != null && playerMovement.IsWallRunning)) return;

        Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRadius, barLayer);
        foreach (var col in colliders)
        {
            HorizontalBar bar = col.GetComponentInParent<HorizontalBar>() ?? col.GetComponentInChildren<HorizontalBar>() ?? col.GetComponent<HorizontalBar>();
            if (bar != null)
            {
                GrabBar(bar);
                break;
            }
        }
    }

    private void GrabBar(HorizontalBar bar)
    {
        if (bar.grabPoint == null) return;

        currentBar = bar;
        isGrabbing = true;
        
        // Desabilita o movimento normal do jogador e o CharacterController
        if (playerMovement != null) 
        {
            playerMovement.IsGrabbingBar = true;
            playerMovement.enabled = false;
        }
        if (controller != null) controller.enabled = false;
        
        // Zera a velocidade do jogador para evitar momentum indesejado
        if (playerMovement != null) playerMovement.SetMovementFromBar(Vector3.zero);
        
        // Calcula a direção inicial do balanço
        barRight = bar.grabPoint.right;
        // Define o ângulo inicial para que o jogador comece na parte inferior do balanço (ou onde for mais natural)
        currentAngle = -90f; 
        
        if (animator != null)
        {
            animator.SetBool("IsHorizontalBar", true);
            animator.SetBool("IsGrounded", false); // Garante que a animação de chão seja desativada
        }
    }

    private void HandleSwinging()
    {
        if (currentBar == null) 
        {
            ReleaseBar();
            return;
        }

        // Aumenta o ângulo de balanço com base na swingSpeed da barra
        currentAngle += currentBar.swingSpeed * Time.deltaTime;
        // Mantém o ângulo dentro de 0-360 graus
        if (currentAngle > 360f) currentAngle -= 360f;

        // Se o botão de pulo for pressionado, lança o jogador baseado no quadrante atual
        if (Input.GetButtonDown("Jump"))
        {
            Vector3 finalLaunchDirection = Vector3.zero;
            
            // Normaliza o ângulo para 0-360
            float normalizedAngle = currentAngle;
            while (normalizedAngle < 0) normalizedAngle += 360;
            while (normalizedAngle >= 360) normalizedAngle -= 360;

            // DETERMINAÇÃO DO QUADRANTE (Estilo Sonic Unleashed)
            // Os ângulos dependem de como o balanço foi iniciado (-90 inicial)
            // Quadrante 1: 315 a 45 graus -> LANÇAMENTO PARA FRENTE
            // Quadrante 2: 45 a 135 graus -> LANÇAMENTO PARA BAIXO
            // Quadrante 3: 135 a 225 graus -> LANÇAMENTO PARA TRÁS
            // Quadrante 4: 225 a 315 graus -> LANÇAMENTO PARA CIMA

            if (normalizedAngle >= 315 || normalizedAngle < 45)
            {
                // FRENTE (Eixo Forward do grabPoint)
                finalLaunchDirection = currentBar.grabPoint.forward;
            }
            else if (normalizedAngle >= 45 && normalizedAngle < 135)
            {
                // BAIXO (Eixo -Up do grabPoint)
                finalLaunchDirection = -currentBar.grabPoint.up;
            }
            else if (normalizedAngle >= 135 && normalizedAngle < 225)
            {
                // TRÁS (Eixo -Forward do grabPoint)
                finalLaunchDirection = -currentBar.grabPoint.forward;
            }
            else // 225 a 315
            {
                // CIMA (Eixo Up do grabPoint)
                finalLaunchDirection = currentBar.grabPoint.up;
            }
            
            LaunchPlayer(finalLaunchDirection);
        }
    }

    private void UpdatePlayerPosition()
    {
        // Calcula a posição do jogador em torno do grabPoint da barra
        Quaternion rotation = Quaternion.AngleAxis(currentAngle, barRight);
        Vector3 offset = rotation * (currentBar.grabPoint.forward * playerRadiusOffset);
        
        transform.position = currentBar.grabPoint.position + offset;
        
        // Orienta o jogador na direção do balanço
        Vector3 tangent = Vector3.Cross(barRight, offset).normalized;
        if (tangent != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(tangent, -offset.normalized);
        }
    }

    private void LaunchPlayer(Vector3 launchDirection)
    {
        isGrabbing = false;
        cooldownTimer = grabCooldown;
        
        // Reabilita o movimento normal do jogador e o CharacterController
        if (controller != null) controller.enabled = true;
        if (playerMovement != null) playerMovement.enabled = true;
        
        // Cálculo da velocidade final usando o novo método da barra
        // Isso garante que o arremesso considere o eixo Y do grabPoint
        Vector3 finalVelocity = currentBar.CalculateLaunchVelocity(launchDirection);

        // Passa a velocidade final para o script de movimento do jogador
        if (playerMovement != null) 
        {
            playerMovement.IsGrabbingBar = false;
            playerMovement.SetMovementFromBar(finalVelocity);
        }

        if (animator != null)
        {
            animator.SetBool("IsHorizontalBar", false);
            animator.SetTrigger("Jump"); // Ativa a animação de pulo
        }

        currentBar = null;
    }

    private void ReleaseBar()
    {
        isGrabbing = false;
        cooldownTimer = grabCooldown;
        if (controller != null) controller.enabled = true;
        if (playerMovement != null) playerMovement.enabled = true;
        if (animator != null) animator.SetBool("IsHorizontalBar", false);
        currentBar = null;
    }

    // Propriedade para que outros scripts possam verificar se o jogador está balançando
    public bool IsGrabbingBar => isGrabbing;
}
