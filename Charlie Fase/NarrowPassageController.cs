using UnityEngine;
using System.Collections;

public class NarrowPassageController : MonoBehaviour
{
    [Header("Configurações de Transição")]
    [SerializeField] private float transitionDuration = 0.3f;

    private CharacterController controller;
    private Animator animator;
    private PlayerMovement_FrontiersStyle playerMovement;
    
    private bool isInNarrowPassage = false;
    private bool isTransitioning = false;
    private float exitCooldown = 0.5f;
    private float exitTimer = 0f;
    [SerializeField] private float entryMovementLockDuration = 0.5f; // Duração da trava de movimento após entrar
    private float movementLockTimer = 0f;
    private NarrowPassageTrigger_Frontiers currentTrigger;
    private Vector3 passageDirection;
    private Vector3 currentTargetPoint;

    // Propriedades públicas para verificação externa
    public bool IsInNarrowPassageState => isInNarrowPassage;
    public bool IsTransitioningState => isTransitioning;
    public float CurrentExitTimer => exitTimer;

    private float originalRadius;
    private float originalHeight;
    private Vector3 originalCenter;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        playerMovement = GetComponent<PlayerMovement_FrontiersStyle>();
    }

    public void EnterNarrowPassage(NarrowPassageTrigger_Frontiers trigger, Vector3 entryPoint, Vector3 exitPoint, Vector3 direction)
    {
        if (isInNarrowPassage || isTransitioning || exitTimer > 0) return;
        
        // Se estiver em slide, cancela IMEDIATAMENTE antes de começar a transição
        // Isso garante que o colisor volte ao normal ANTES de salvarmos o originalHeight
        if (playerMovement != null)
        {
            playerMovement.CancelGroundSlideImmediate();
        }

        currentTrigger = trigger;
        passageDirection = direction.normalized;
        
        currentTargetPoint = entryPoint + new Vector3(0, trigger.narrowHeight / 2f, 0);
        
        StartCoroutine(EnterCoroutine());
    }

    public void ExitNarrowPassage(Vector3 exitPointPosition)
    {
        if (!isInNarrowPassage || isTransitioning) return;
        
        Vector3 finalExitPos = exitPointPosition + new Vector3(0, originalHeight / 2f, 0) + new Vector3(0, originalCenter.y, 0);
        StartCoroutine(ExitCoroutine(finalExitPos));
    }

    private IEnumerator EnterCoroutine()
    {
        isTransitioning = true;
        
        if (playerMovement != null) 
        {
            playerMovement.IsInNarrowPassage = true;
            playerMovement.ResetVerticalVelocity();
        }

        originalRadius = controller.radius;
        originalHeight = controller.height;
        originalCenter = controller.center;

        if (animator != null) 
        {
            animator.SetBool("IsInNarrowPassage", true);
            animator.SetBool("IsMovingBackward", false);
            animator.SetFloat("NarrowPassageSpeed", 0f);
        }

        float timer = 0f;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        Quaternion targetRot = Quaternion.LookRotation(passageDirection);

        controller.enabled = false;
        controller.radius = currentTrigger.narrowRadius;
        controller.height = currentTrigger.narrowHeight;
        controller.center = Vector3.zero;

        while (timer < transitionDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / transitionDuration;
            
            transform.position = Vector3.Lerp(startPos, currentTargetPoint, progress);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, progress);
            
            yield return null;
        }

        transform.position = currentTargetPoint;
        transform.rotation = targetRot;

        if (playerMovement != null) 
        {
            playerMovement.currentSpeed = 0f;
            playerMovement.ResetMovementDirection();
        }

        controller.enabled = true;
        isInNarrowPassage = true;
        isTransitioning = false;
        movementLockTimer = entryMovementLockDuration; // Ativa a trava de movimento
    }

    private IEnumerator ExitCoroutine(Vector3 exitPointPosition)
    {
        isTransitioning = true;
        isInNarrowPassage = false;

        if (animator != null) 
        {
            animator.SetBool("IsInNarrowPassage", false);
            animator.SetBool("IsMovingBackward", false);
        }

        float timer = 0f;
        Vector3 startPos = transform.position;

        controller.enabled = false;
        controller.radius = originalRadius;
        controller.height = originalHeight;
        controller.center = originalCenter;

        while (timer < transitionDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / transitionDuration;
            transform.position = Vector3.Lerp(startPos, exitPointPosition, progress);
            yield return null;
        }

        transform.position = exitPointPosition;
        controller.enabled = true;

        if (playerMovement != null) 
        {
            playerMovement.IsInNarrowPassage = false;
            playerMovement.ResetAirCharges();
        }
        
        exitTimer = exitCooldown;
        isTransitioning = false;
    }

    void Update()
    {
        if (exitTimer > 0) exitTimer -= Time.deltaTime;
        if (movementLockTimer > 0) movementLockTimer -= Time.deltaTime;
        
        if (!isInNarrowPassage || isTransitioning) return;

        // Só permite o movimento se a trava tiver expirado
        if (movementLockTimer <= 0)
        {
            HandleMovement();
        }
    }

    private void HandleMovement()
    {
        float verticalInput = Input.GetAxisRaw("Vertical");
        if (Mathf.Abs(verticalInput) < 0.01f) 
        {
            if (animator != null)
            {
                animator.SetFloat("Speed", 0f);
                animator.SetFloat("NarrowPassageSpeed", 0f);
                animator.SetBool("IsMovingBackward", false);
            }
            return;
        }

        // --- LÓGICA DE LIMITE DE MOVIMENTO ---
        // Pegamos os limites reais da passagem
        currentTrigger.GetPassageEnds(out Vector3 p1, out Vector3 p2);
        
        // Determinamos qual ponto é o "início" (atrás do jogador) baseado na direção da passagem
        // entryPoint é p1 se passageDirection aponta de p1 para p2
        Vector3 entryPoint = (Vector3.Dot(passageDirection, p2 - p1) > 0) ? p1 : p2;
        Vector3 exitPoint = (entryPoint == p1) ? p2 : p1;

        // Calculamos a posição futura aproximada
        Vector3 moveDelta = passageDirection * verticalInput * currentTrigger.movementSpeed * Time.deltaTime;
        Vector3 futurePos = transform.position + moveDelta;

        // Vetor do início da passagem até o jogador
        Vector3 fromStart = futurePos - entryPoint;
        float projection = Vector3.Dot(fromStart, passageDirection);
        float totalLength = Vector3.Distance(entryPoint, exitPoint);

        // Se o jogador tentar andar para trás e sair do limite inicial
        if (projection < 0 && verticalInput < 0)
        {
            ExitNarrowPassage(entryPoint);
            return;
        }

        // Se o jogador chegar no fim da passagem (opcional: pode deixar o OnTriggerExit lidar ou travar aqui)
        // Aqui apenas travamos o movimento se ele passar do ponto final, para evitar que ele "ande no ar"
        if (projection > totalLength && verticalInput > 0)
        {
            // Opcional: Poderia chamar ExitNarrowPassage(exitPoint) aqui também se quiser auto-saída no fim
            return; 
        }

        // Movimento real
        Vector3 move = passageDirection * verticalInput * currentTrigger.movementSpeed;
        move.y = -9.81f; 

        controller.Move(move * Time.deltaTime);

        if (animator != null)
        {
            animator.SetFloat("Speed", Mathf.Abs(verticalInput) * currentTrigger.movementSpeed);
            animator.SetFloat("NarrowPassageSpeed", verticalInput);
            animator.SetBool("IsMovingBackward", verticalInput < -0.1f);
        }
    }
}
