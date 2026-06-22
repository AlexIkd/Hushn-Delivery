using UnityEngine;
using System.Collections;

public class HorizontalBarHandler : MonoBehaviour
{
    [Header("Detecção")]
    [SerializeField] private float detectionRadius = 2.0f;
    [SerializeField] private LayerMask barLayer;
    [SerializeField] private float grabCooldown = 0.5f;
    
    [Header("Configurações de Balanço Automático")]
    [SerializeField] private float maxSwingAngle = 75f; 
    [SerializeField] private float swingSpeed = 4.5f;   
    [Tooltip("Distância do ponto de grab da barra até o ponto de pivô do jogador (geralmente o centro do corpo).")]
    [SerializeField] private float playerDistanceFromBar = 1.2f; 
    [Tooltip("Ajuste vertical do ponto de pivô do jogador em relação ao ponto de grab da barra.")]
    [SerializeField] private float playerVerticalOffset = 0.0f; 
    [Tooltip("Ajuste horizontal do ponto de pivô do jogador ao longo da barra.")]
    [SerializeField] private float playerHorizontalOffset = 0.0f; 
    [Tooltip("Offset local das mãos do jogador em relação ao seu próprio pivô (transform.position).")]
    [SerializeField] private Vector3 playerHandsLocalOffset = new Vector3(0, 1.5f, 0.5f);

    [Header("Ajustes Manuais de Entrada")]
    [Tooltip("Offset Forward usado quando o jogador entra de frente (olhando para o forward da barra).")]
    [SerializeField] private float forwardEntryOffset = 0.0f;
    [Tooltip("Offset Forward usado quando o jogador entra de costas (olhando contra o forward da barra).")]
    [SerializeField] private float backwardEntryOffset = 0.0f;
    [Tooltip("Velocidade de suavização ao encaixar na barra (0 = instantâneo, maior = mais lento).")]
    [SerializeField] private float entryLerpSpeed = 10f;

    [Header("Configurações de Lançamento")]
    [SerializeField] private float horizontalForce = 25f;
    [SerializeField] private float verticalForce = 15f;

    [Header("Suavização")]
    [SerializeField] private float angleSmoothTime = 0.1f;

    [Header("Animação")]
    [SerializeField] private string swingAnimationName = "Swing";
    [SerializeField] private bool syncAnimationWithSwing = true;

    [Header("Efeitos")]
    [SerializeField] private PlayerAnimeSpeedLines jumpTrail;
    
    private PlayerMovement_FrontiersStyle playerMovement;
    private CharacterController controller;
    private Animator animator;
    
    private HorizontalBar currentBar;
    private bool isGrabbing = false;
    public bool EnteredFromBack { get; private set; } = false;
    private float swingTimer = 0f; 
    private Vector3 playerInitialForwardOnGrab; 
    private float cooldownTimer = 0f;
    private float activeForwardOffset = 0f;

    private float smoothSwingAngle;
    private float angleVelocity;

    private float entryLerpFactor = 0f;
    private Vector3 positionAtGrab;
    private Quaternion rotationAtGrab;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement_FrontiersStyle>();
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (cooldownTimer > 0) cooldownTimer -= Time.deltaTime;

        if (isGrabbing)
        {
            HandleSwinging();
            if (entryLerpFactor < 1f)
            {
                entryLerpFactor = Mathf.MoveTowards(entryLerpFactor, 1f, Time.deltaTime * entryLerpSpeed);
            }
        }
        else
        {
            CheckForBar();
            entryLerpFactor = 0f;
        }
    }

    void LateUpdate()
    {
        if (isGrabbing && currentBar != null)
        {
            UpdatePlayerPositionAndRotation();
            UpdateAnimationSync();
        }
    }

    private void GrabBar(HorizontalBar bar)
    {
        if (bar.grabPoint == null) return;

        currentBar = bar;
        isGrabbing = true;
        
        positionAtGrab = transform.position;
        rotationAtGrab = transform.rotation;
        entryLerpFactor = 0f;
        
        if (playerMovement != null) 
        {
            playerMovement.IsGrabbingBar = true;
            playerMovement.enabled = false;
            playerMovement.CancelWallRun();
            playerMovement.CancelGlide();
            playerMovement.CancelAirDash();
            playerMovement.CancelStomp();
        }
        if (controller != null) controller.enabled = false;
        
        float lookDot = Vector3.Dot(transform.forward, currentBar.grabPoint.forward);
        
        // Mantém a direção que o jogador estava olhando ao entrar
        if (lookDot >= 0)
        {
            playerInitialForwardOnGrab = currentBar.grabPoint.forward;
            activeForwardOffset = forwardEntryOffset;
        }
        else
        {
            playerInitialForwardOnGrab = -currentBar.grabPoint.forward;
            activeForwardOffset = backwardEntryOffset;
        }

        Vector3 directionToPlayer = (transform.position - bar.grabPoint.position).normalized;
        float entryDot = Vector3.Dot(playerInitialForwardOnGrab, directionToPlayer);

        if (entryDot > 0.1f) 
        {
            swingTimer = Mathf.PI * 1.5f; 
            EnteredFromBack = true;
        }
        else if (entryDot < -0.1f)
        {
            swingTimer = Mathf.PI * 0.5f; 
            EnteredFromBack = false;
        }
        else
        {
            swingTimer = 0f;
            EnteredFromBack = false;
        }

        smoothSwingAngle = Mathf.Sin(swingTimer) * maxSwingAngle;
        angleVelocity = 0;

        if (animator != null)
        {
            animator.SetBool("IsHorizontalBar", true);
            animator.SetBool("IsGrounded", false);
            if (syncAnimationWithSwing)
            {
                float normalizedAngle = (Mathf.Sin(swingTimer) * maxSwingAngle / maxSwingAngle); 
                float normalizedTime = (normalizedAngle + 1f) / 2f; 
                animator.Play(swingAnimationName, 0, normalizedTime);
                animator.speed = 0f;
            }
        }
    }

    private void HandleSwinging()
    {
        if (currentBar == null) 
        {
            ReleaseBar();
            return;
        }

        swingTimer += Time.deltaTime * swingSpeed;
        
        if (Input.GetButtonDown("Jump"))
        {
            Vector3 vectorToPlayer = transform.position - currentBar.grabPoint.position;
            float dot = Vector3.Dot(vectorToPlayer, playerInitialForwardOnGrab);

            Vector3 jumpDir;

            if (dot > 0)
            {
                // Jogador está na frente do pivô -> Pula para frente
                jumpDir = playerInitialForwardOnGrab;
            }
            else
            {
                // Jogador está atrás do pivô -> Pula para trás
                jumpDir = -playerInitialForwardOnGrab;
            }

            jumpDir.y = 0;
            jumpDir.Normalize();

            Vector3 finalVelocity = (jumpDir * horizontalForce) + (Vector3.up * verticalForce);
            LaunchPlayer(finalVelocity);
        }
    }

    private void UpdatePlayerPositionAndRotation()
    {
        float targetAngle = Mathf.Sin(swingTimer) * maxSwingAngle;
        smoothSwingAngle = Mathf.SmoothDamp(smoothSwingAngle, targetAngle, ref angleVelocity, angleSmoothTime);

        Vector3 desiredHandsWorldPosition = currentBar.grabPoint.position +
                                            currentBar.grabPoint.right * playerHorizontalOffset +
                                            currentBar.grabPoint.forward * activeForwardOffset +
                                            Vector3.up * playerVerticalOffset;

        Quaternion baseRotation = Quaternion.LookRotation(playerInitialForwardOnGrab, Vector3.up);
        Quaternion swingBodyRotation = Quaternion.AngleAxis(smoothSwingAngle, currentBar.grabPoint.right);
        
        Quaternion targetRotation = swingBodyRotation * baseRotation;
        Vector3 targetPosition = desiredHandsWorldPosition - (targetRotation * playerHandsLocalOffset);

        if (entryLerpFactor < 1f)
        {
            transform.position = Vector3.Lerp(positionAtGrab, targetPosition, entryLerpFactor);
            transform.rotation = Quaternion.Slerp(rotationAtGrab, targetRotation, entryLerpFactor);
        }
        else
        {
            transform.position = targetPosition;
            transform.rotation = targetRotation;
        }
    }

    private void UpdateAnimationSync()
    {
        if (animator != null && syncAnimationWithSwing)
        {
            float normalizedAngle = (smoothSwingAngle / maxSwingAngle); 
            float normalizedTime = (normalizedAngle + 1f) / 2f; 
            animator.Play(swingAnimationName, 0, normalizedTime);
        }
    }

    private void LaunchPlayer(Vector3 finalVelocity)
    {
        isGrabbing = false;
        cooldownTimer = grabCooldown;
        if (controller != null) controller.enabled = true;
        if (playerMovement != null) 
        {
            playerMovement.enabled = true;
            playerMovement.IsGrabbingBar = false;
            playerMovement.SetMovementFromBar(finalVelocity);
        }
        if (animator != null)
        {
            animator.speed = 1f; 
            animator.SetBool("IsHorizontalBar", false);
            animator.SetTrigger("Jump");
        }

        if (jumpTrail != null)
        {
            jumpTrail.EnableEffect(finalVelocity);
            // Para o rastro após um pequeno tempo para simular o impulso inicial
            Invoke(nameof(StopJumpTrail), 0.5f);
        }

        currentBar = null;
    }

    private void StopJumpTrail()
    {
        if (jumpTrail != null) jumpTrail.DisableEffect();
    }

    private void ReleaseBar()
    {
        isGrabbing = false;
        cooldownTimer = grabCooldown;
        if (controller != null) controller.enabled = true;
        if (playerMovement != null) playerMovement.enabled = true;
        if (animator != null) 
        {
            animator.speed = 1f; 
            animator.SetBool("IsHorizontalBar", false);
        }
        currentBar = null;
    }

    private void CheckForBar()
    {
        if (cooldownTimer > 0 || (controller != null && controller.isGrounded)) return;
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
}
