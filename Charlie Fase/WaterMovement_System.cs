using UnityEngine;
using System;

/// <summary>
/// Sistema de movimentação na água para Unity 6.0
/// Gerencia dois estados: deslizamento rápido inicial e afundamento com movimentação limitada
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class WaterMovement_System : MonoBehaviour
{
    [Header("Configurações de Deslizamento")]
    [SerializeField] private float initialSlideSpeed = 25f;
    [SerializeField] private float slideDeceleration = 8f;
    [SerializeField] private float minSpeedToSink = 3f;
    [SerializeField] private ParticleSystem slideParticles;
    [SerializeField] private bool enableSlideParticles = true;

    [Header("Configurações de Afundamento")]
    [SerializeField] private float sinkSpeed = 5f;
    [SerializeField] private float walkSpeed = 6f;
    [SerializeField] private float sinkAcceleration = 3f;
    [SerializeField] private float sinkDeceleration = 5f;
    [SerializeField] private float turnSpeed = 300f;
    [SerializeField] private ParticleSystem sinkParticles;
    [SerializeField] private bool enableSinkParticles = true;

    [Header("Configurações de Pulo")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float gravity = 20f;

    [Header("Detecção de Água")]
    [SerializeField] private LayerMask waterLayer;
    [SerializeField] private float waterCheckRadius = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    // Estados
    public enum WaterState
    {
        Sliding,    // Deslizando rápido na água
        Sinking,    // Afundando na água
        Exiting     // Saindo da água
    }

    private WaterState currentState = WaterState.Sliding;
    private bool isInWater = false;
    private bool isGroundedInWater = false;

    // Componentes
    private CharacterController controller;
    private Animator animator;
    private Transform cachedTransform;
    private Transform cameraTransform;

    // Movimento
    private Vector3 moveDirection = Vector3.zero;
    private Vector3 inputVector = Vector3.zero;
    private float currentSpeed = 0f;
    private float verticalVelocity = 0f;

    // Referência ao sistema de movimentação normal
    private PlayerMovement_FrontiersStyle normalMovement;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        cachedTransform = transform;
        cameraTransform = Camera.main ? Camera.main.transform : null;
        normalMovement = GetComponent<PlayerMovement_FrontiersStyle>();

        if (controller == null)
            Debug.LogError("CharacterController não encontrado em WaterMovement_System!");

        if (normalMovement == null)
            Debug.LogWarning("PlayerMovement_FrontiersStyle não encontrado no mesmo GameObject!");

        // Começa desativado
        enabled = false;
    }

    void Update()
    {
        if (!isInWater)
            return;

        // Verificar se ainda está na água
        CheckWaterPresence();

        if (!isInWater)
        {
            ExitWater();
            return;
        }

        // Atualizar estado baseado na velocidade
        UpdateWaterState();

        // Processar entrada
        HandleInput();

        // Aplicar movimento baseado no estado
        switch (currentState)
        {
            case WaterState.Sliding:
                HandleSliding();
                break;
            case WaterState.Sinking:
                HandleSinking();
                break;
        }

        // Aplicar gravidade
        ApplyGravity();

        // Aplicar movimento final
        if (controller.enabled)
        {
            controller.Move(moveDirection * Time.deltaTime);
        }
    }

    /// <summary>
    /// Inicia o sistema de movimentação na água
    /// </summary>
    public void EnterWater(Vector3 slideDirection, float slideSpeed)
    {
        isInWater = true;
        currentState = WaterState.Sliding;
        currentSpeed = Mathf.Min(slideSpeed, initialSlideSpeed);
        moveDirection = slideDirection.normalized * currentSpeed;
        verticalVelocity = 0f;

        // Desativar movimentação normal
        if (normalMovement != null)
            normalMovement.enabled = false;

        // Ativar este script
        enabled = true;

        if (enableSlideParticles && slideParticles != null)
            slideParticles.Play();

        if (showDebugInfo)
            Debug.Log($"🌊 Entrando na água com velocidade: {currentSpeed}");
    }

    /// <summary>
    /// Sai da água e retorna ao sistema de movimentação normal
    /// </summary>
    private void ExitWater()
    {
        isInWater = false;
        enabled = false;

        // Reativar movimentação normal
        if (normalMovement != null)
        {
            normalMovement.enabled = true;
            // Transferir velocidade horizontal para o sistema normal
            normalMovement.currentSpeed = currentSpeed;
        }

        if (slideParticles != null)
            slideParticles.Stop();

        if (sinkParticles != null)
            sinkParticles.Stop();

        if (showDebugInfo)
            Debug.Log("🌊 Saindo da água");
    }

    /// <summary>
    /// Verifica se o personagem ainda está na água
    /// </summary>
    private void CheckWaterPresence()
    {
        // Verificar se há água na posição atual
        Collider[] waterColliders = Physics.OverlapSphere(
            cachedTransform.position,
            waterCheckRadius,
            waterLayer
        );

        isInWater = waterColliders.Length > 0;
    }

    /// <summary>
    /// Atualiza o estado baseado na velocidade
    /// </summary>
    private void UpdateWaterState()
    {
        if (currentState == WaterState.Sliding && currentSpeed <= minSpeedToSink)
        {
            currentState = WaterState.Sinking;

            if (enableSinkParticles && sinkParticles != null)
            {
                sinkParticles.Play();
            }

            if (showDebugInfo)
                Debug.Log("💧 Começando a afundar");
        }
    }

    /// <summary>
    /// Processa entrada do jogador
    /// </summary>
    private void HandleInput()
    {
        inputVector = Vector3.zero;

        // Obter entrada do novo Input System
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed) inputVector.z += 1f;
            if (keyboard.sKey.isPressed) inputVector.z -= 1f;
            if (keyboard.aKey.isPressed) inputVector.x -= 1f;
            if (keyboard.dKey.isPressed) inputVector.x += 1f;
        }

        // Normalizar entrada
        if (inputVector.sqrMagnitude > 1f)
            inputVector.Normalize();
    }

    /// <summary>
    /// Gerencia o estado de deslizamento rápido
    /// </summary>
    private void HandleSliding()
    {
        // Aplicar desaceleração
        currentSpeed = Mathf.Max(currentSpeed - slideDeceleration * Time.deltaTime, 0f);

        // Manter direção de movimento
        moveDirection = moveDirection.normalized * currentSpeed;

        // Permitir rotação leve baseada na entrada
        if (inputVector.sqrMagnitude > 0.01f)
        {
            Vector3 desiredDirection = GetDesiredDirection(inputVector);
            moveDirection = Vector3.Lerp(moveDirection.normalized, desiredDirection, Time.deltaTime * 2f) * currentSpeed;
        }

        // Atualizar animador
        UpdateAnimator();
    }

    /// <summary>
    /// Gerencia o estado de afundamento
    /// </summary>
    private void HandleSinking()
    {
        // Movimento horizontal limitado
        Vector3 horizontalInput = Vector3.zero;

        if (inputVector.sqrMagnitude > 0.01f)
        {
            horizontalInput = GetDesiredDirection(inputVector);
            currentSpeed = Mathf.Lerp(currentSpeed, walkSpeed, sinkAcceleration * Time.deltaTime);
        }
        else
        {
            currentSpeed = Mathf.Lerp(currentSpeed, 0f, sinkDeceleration * Time.deltaTime);
        }

        // Aplicar movimento horizontal
        Vector3 horizontalMove = horizontalInput * currentSpeed;
        moveDirection.x = horizontalMove.x;
        moveDirection.z = horizontalMove.z;

        // Rotacionar para a direção desejada
        if (inputVector.sqrMagnitude > 0.01f)
        {
            Vector3 desiredDirection = GetDesiredDirection(inputVector);
            Quaternion desiredRotation = Quaternion.LookRotation(desiredDirection);
            cachedTransform.rotation = Quaternion.RotateTowards(
                cachedTransform.rotation,
                desiredRotation,
                turnSpeed * Time.deltaTime
            );
        }

        // Aplicar afundamento
        moveDirection.y = -sinkSpeed;

        // Atualizar animador
        UpdateAnimator();
    }

    /// <summary>
    /// Aplica gravidade ao movimento vertical
    /// </summary>
    private void ApplyGravity()
    {
        if (currentState == WaterState.Sliding)
        {
            // Durante o deslizamento, aplicar gravidade reduzida
            verticalVelocity -= gravity * 0.5f * Time.deltaTime;
            moveDirection.y = verticalVelocity;
        }
        // Durante o afundamento, a gravidade é aplicada em HandleSinking
    }

    /// <summary>
    /// Realiza um pulo para sair da água
    /// </summary>
    public void Jump()
    {
        if (!isInWater || currentState != WaterState.Sinking)
            return;

        verticalVelocity = jumpForce;
        moveDirection.y = jumpForce;

        if (showDebugInfo)
            Debug.Log("⬆️ Pulando da água");

        // Sair da água após o pulo
        ExitWater();
    }

    /// <summary>
    /// Calcula a direção desejada baseada na entrada e câmera
    /// </summary>
    private Vector3 GetDesiredDirection(Vector3 input)
    {
        if (cameraTransform == null)
            return cachedTransform.forward;

        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;

        // Ignorar componente Y da câmera
        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 desiredDirection = (cameraForward * input.z + cameraRight * input.x).normalized;
        return desiredDirection == Vector3.zero ? cachedTransform.forward : desiredDirection;
    }

    /// <summary>
    /// Atualiza parâmetros do animador
    /// </summary>
    private void UpdateAnimator()
    {
        if (animator == null)
            return;

        float speed = new Vector3(moveDirection.x, 0f, moveDirection.z).magnitude;
        animator.SetFloat("Speed", speed);
        animator.SetBool("IsInWater", true);
        animator.SetBool("IsSinking", currentState == WaterState.Sinking);
    }

    // Propriedades públicas para acesso externo
    public bool IsInWater => isInWater;
    public WaterState CurrentState => currentState;
    public float CurrentSpeed => currentSpeed;
}
