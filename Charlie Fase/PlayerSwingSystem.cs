using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PlayerSwingSystem : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private PlayerMovement_FrontiersStyle playerMovement; // Referência ao script principal de movimento
    [SerializeField] private Transform cameraTransform; // Referência à câmera para raycast
    [SerializeField] private LineRenderer ropeRenderer; // Para desenhar a corda
    [SerializeField] private Transform handTransform; // Posição da mão para o início da corda

    [Header("Configurações de Swing (Estilo TLOU2)")]
    [SerializeField] private float anchorDetectionRadius = 20f; // Raio de detecção para SwingAnchors
    [SerializeField] private float anchorDetectionAngle = 75f; // Ângulo de visão para detecção
    [SerializeField] private LayerMask obstacleLayer; // Layer de obstáculos para Raycast
    [SerializeField] private LayerMask groundLayer; // Layer do chão para bloqueio
    [SerializeField] private float groundCheckRadius = 0.5f; // Raio da esfera de detecção do chão (ajustável)
    [SerializeField] private float minRopeLength = 2f; // Comprimento mínimo da corda
    [SerializeField] private float maxRopeLength = 35f; // Comprimento máximo da corda
    [SerializeField] private float ropeAdjustSpeed = 15f; // Velocidade para encurtar/alongar a corda
    
    [Header("Física do Pêndulo")]
    [SerializeField] private float gravityMultiplier = 2.0f; // Gravidade forte para dar peso ao personagem
    [SerializeField] private float airResistance = 0.1f; // Resistência do ar natural
    [SerializeField] private float swingPushForce = 20f; // Força aplicada pelo jogador para ganhar momento
    [SerializeField] private float lateralControlForce = 10f; // Controle lateral sutil
    [SerializeField] private float maxSwingSpeed = 30f;
    [SerializeField] private float ropeTensionStrength = 180f; // Força da tensão da corda
    
    [Header("Escalada na Corda")]
    [SerializeField] private float climbSpeed = 5f; // Velocidade de subir/descer na corda
    [SerializeField] private float climbMomentumDamping = 1.5f; // Amortecimento ao escalar
    
    [Header("Visualização")]
    [SerializeField] private RectTransform warpIcon;
    [SerializeField] private Color activeIconColor = Color.white;
    [SerializeField] private Color blockedIconColor = Color.red; // Cor para quando estiver no chão
    [SerializeField] private int ropeSegments = 15; // Número de segmentos para a simulação da corda
    [SerializeField] private float ropeCurvature = 0.5f; // Intensidade da curvatura (catenária)
    [SerializeField] private float launchDuration = 0.15f; // Duração do efeito de lançamento
    [SerializeField] private float ropeWaveSpeed = 15f; // Velocidade da oscilação
    [SerializeField] private float ropeWaveAmplitude = 0.08f; // Amplitude da oscilação

    // --- NOVO: Referências de Animação do Swing ---
    [Header("Animação do Swing")]
    [SerializeField] private Animator swingAnimator; // Animator do jogador para controlar animações de swing
    [SerializeField] private float swingIdleThreshold = 2f; // Velocidade abaixo disso = animação de swing parado
    [SerializeField] private float swingSpeedMaxForAnimation = 20f; // Normaliza a velocidade para o Animator (0~1)
    [SerializeField] private string swingAnimationStateName = "Swing_Start_Animation_State_Name"; // Nome do estado no Animator
    [SerializeField] private float animationCrossfadeDuration = 0.1f; // Duração da transição suave
    [SerializeField] private float swingAngleLimit = 60f; // Ângulo máximo para normalizar a animação (ex: 60 graus)
    [SerializeField] private float maxRotationTilt = 45f; // Inclinação máxima do corpo em graus
    [SerializeField] private float rotationSmoothSpeed = 10f; // Velocidade do Lerp para a rotação

    // Estado Interno
    private SwingAnchor currentTargetAnchor;
    private bool _isSwingingInternal = false;
    private Vector3 anchorPoint;
    private float currentRopeLength;
    private Vector3 swingVelocity;
    private bool isLaunching = false;
    private float launchTimer = 0f;
    private float ropeWaveTimer = 0f;
    private bool isClimbing = false;

    // Variável para controle do estado de bloqueio
    private bool isTouchingGround = false;

    // --- NOVO: Cache para otimização do Animator ---
    private float cachedSwingSpeed = -1f;
    private bool cachedIsClimbing = false;
    private const float SWING_SPEED_CHANGE_THRESHOLD = 0.01f;

    private void Awake()
    {
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement_FrontiersStyle>();
        if (cameraTransform == null) cameraTransform = Camera.main.transform;
        if (ropeRenderer == null) ropeRenderer = GetComponent<LineRenderer>();
        if (handTransform == null) handTransform = transform;
        // Auto-detecta o Animator se não for atribuído
        if (swingAnimator == null) swingAnimator = GetComponentInChildren<Animator>();

        if (ropeRenderer != null)
        {
            ropeRenderer.enabled = false;
            ropeRenderer.positionCount = ropeSegments;
        }
    }

    private void Update()
    {
        // Agora checamos apenas a colisão física com a groundLayer
        // Isso ignora o estado interno 'isGrounded' do playerMovement que pode estar bugado
        isTouchingGround = Physics.CheckSphere(transform.position, groundCheckRadius, groundLayer);

        HandleInput();

        if (_isSwingingInternal)
        {
            if (isLaunching)
            {
                launchTimer += Time.deltaTime;
                if (launchTimer >= launchDuration) isLaunching = false;
            }
            
            // O balanço assume o controle total da física do jogador
            ProcessAdvancedSwing();
            DrawRope();
            
            // --- NOVO: Atualiza as animações do swing ---
            UpdateSwingAnimations();

            if (warpIcon != null && warpIcon.gameObject.activeSelf) warpIcon.gameObject.SetActive(false);
        }
        else
        {
            currentTargetAnchor = FindNearestSwingAnchor();
            UpdateUI();

            // Limpa os parâmetros do Animator quando não está balançando
            ClearSwingAnimationState();
        }
    }

    // ======================================================
    // ANIMAÇÕES DO SWING
    // ======================================================

    private void UpdateSwingAnimations()
    {
        if (swingAnimator == null) return;

        // 1. Controle de Velocidade e Escalada (Lógica Anterior)
        float currentSwingSpeed = swingVelocity.magnitude;
        float normalizedSwingSpeed = Mathf.Clamp01(currentSwingSpeed / swingSpeedMaxForAnimation);

        if (Mathf.Abs(normalizedSwingSpeed - cachedSwingSpeed) > SWING_SPEED_CHANGE_THRESHOLD)
        {
            swingAnimator.SetFloat("SwingSpeed", normalizedSwingSpeed);
            cachedSwingSpeed = normalizedSwingSpeed;
        }

        if (isClimbing != cachedIsClimbing)
        {
            swingAnimator.SetBool("IsSwingClimbing", isClimbing);
            cachedIsClimbing = isClimbing;
        }

        // 2. Sincronização da Animação com o Ângulo do Pêndulo (Motion Time)
        // Calcula a direção do jogador em relação à âncora
        Vector3 playerToAnchor = anchorPoint - transform.position;
        Vector3 playerDir = -playerToAnchor.normalized;

        // Calcula o ângulo no plano de movimento (frente/trás)
        // Usamos a câmera como referência para saber o que é "frente"
        Vector3 swingPlaneForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        float angle = Vector3.SignedAngle(Vector3.down, playerDir, Vector3.Cross(Vector3.down, swingPlaneForward));

        // Normaliza o ângulo para um valor entre 0 e 1 (0.5 é o centro/pivô)
        // Se angle for -swingAngleLimit, t = 0. Se for +swingAngleLimit, t = 1.
        float normalizedAngle = Mathf.InverseLerp(-swingAngleLimit, swingAngleLimit, angle);
        
        // Passa para o Animator controlar o tempo da animação manualmente
        swingAnimator.SetFloat("SwingMotionTime", normalizedAngle);

        // 3. Rotação Dinâmica do Corpo (Inclinação)
        ApplyDynamicRotation(angle);
    }

    private void ApplyDynamicRotation(float currentAngle)
    {
        // Define a rotação alvo baseada no ângulo atual do pêndulo
        // Invertemos o ângulo para que o jogador se incline na direção do movimento
        float targetTilt = -currentAngle * (maxRotationTilt / swingAngleLimit);
        targetTilt = Mathf.Clamp(targetTilt, -maxRotationTilt, maxRotationTilt);

        // Calcula a direção para onde o jogador deve estar olhando (frente da câmera)
        Vector3 camF = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        if (camF == Vector3.zero) camF = transform.forward;
        
        Quaternion baseRotation = Quaternion.LookRotation(camF, Vector3.up);
        Quaternion tiltRotation = Quaternion.Euler(targetTilt, 0, 0);
        
        // Combina a rotação base (olhando pra frente) com a inclinação (tilt)
        Quaternion finalRotation = baseRotation * tiltRotation;

        // Aplica a rotação suavemente via Lerp
        transform.rotation = Quaternion.Lerp(transform.rotation, finalRotation, Time.deltaTime * rotationSmoothSpeed);
    }

    private void ClearSwingAnimationState()
    {
        if (swingAnimator == null) return;

        // Quando sai do swing, reseta os parâmetros para evitar estados travados
        swingAnimator.SetFloat("SwingSpeed", 0f);
            swingAnimator.SetBool("IsSwingClimbing", false);
            swingAnimator.SetBool("IsSwinging", false); // Desativa o estado de balanço
            // Garante que o parâmetro de queda seja resetado ou ativado conforme necessário ao sair do swing
            // A lógica de queda deve ser gerenciada pelo PlayerMovement_FrontiersStyle após sair do swing.
        cachedSwingSpeed = -1f;
        cachedIsClimbing = false;
    }

    // ======================================================
    // INPUT E LÓGICA DE SWING
    // ======================================================

    private void HandleInput()
    {
        // O swing só pode começar se NÃO estiver tocando o chão (groundLayer)
        bool canStart = !isTouchingGround;

        if (Input.GetMouseButtonDown(0) && !_isSwingingInternal && canStart)
        {
            TryStartSwing();
        }
        else if ((Input.GetMouseButtonUp(0) || Input.GetButtonDown("Jump")) && _isSwingingInternal)
        {
            ExitSwing(Input.GetButtonDown("Jump"));
        }
    }

    private void TryStartSwing()
    {
        currentTargetAnchor = FindNearestSwingAnchor();
        if (currentTargetAnchor != null)
        {
            anchorPoint = currentTargetAnchor.GetAnchorPosition();
            currentRopeLength = Vector3.Distance(handTransform.position, anchorPoint);
            currentRopeLength = Mathf.Clamp(currentRopeLength, minRopeLength, maxRopeLength);
            
            // Transfere o momento inicial do jogador
            swingVelocity = playerMovement.moveDirection;
            
            _isSwingingInternal = true;
            playerMovement.isSwinging = true;
            // A animação de queda deve ser gerenciada pelo PlayerMovement_FrontiersStyle. O SwingSystem apenas garante que não estamos caindo enquanto balançamos.
            if (swingAnimator != null)
            {
                swingAnimator.SetBool("IsFalling", false); // Garante que não estamos caindo
                swingAnimator.SetBool("IsSwinging", true); // Ativa o estado de balanço imediatamente
                // Força a reprodução da animação de swing com transição suave (Crossfade)
                swingAnimator.CrossFade(swingAnimationStateName, animationCrossfadeDuration);
            }
            isClimbing = false;
            isLaunching = true;
            launchTimer = 0f;
            
            if (ropeRenderer != null) ropeRenderer.enabled = true;
        }
    }

    private void ProcessAdvancedSwing()
    {
        Vector3 playerPos = handTransform.position;
        Vector3 playerToAnchor = anchorPoint - playerPos;
        float currentDistance = playerToAnchor.magnitude;
        Vector3 dirToAnchor = playerToAnchor.normalized;

        // Inputs
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        bool holdingBoost = Input.GetKey(KeyCode.LeftShift) || Input.GetMouseButton(1);

        // 1. Lógica de Escalada vs Balanço
        if (!holdingBoost && Mathf.Abs(v) > 0.1f && swingVelocity.magnitude < 8f)
        {
            isClimbing = true;
            swingVelocity = Vector3.Lerp(swingVelocity, Vector3.zero, Time.deltaTime * climbMomentumDamping);
            currentRopeLength -= v * climbSpeed * Time.deltaTime;
            currentRopeLength = Mathf.Clamp(currentRopeLength, minRopeLength, maxRopeLength);
        }
        else
        {
            isClimbing = false;
        }

        // 2. Aplicação de Gravidade Própria
        swingVelocity += Vector3.down * (playerMovement.gravity * gravityMultiplier) * Time.deltaTime;

        // 3. Input de Balanço (Ganho de Momento)
        if (holdingBoost || (!isClimbing && Mathf.Abs(v) > 0.1f))
        {
            Vector3 camF = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            Vector3 camR = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
            Vector3 inputDir = (camF * v + camR * h).normalized;

            if (inputDir.magnitude > 0.1f)
            {
                Vector3 tangentDir = Vector3.ProjectOnPlane(inputDir, dirToAnchor).normalized;
                float alignment = Vector3.Dot(swingVelocity.normalized, tangentDir);
                float accel = (alignment > 0 || swingVelocity.magnitude < 2f) ? swingPushForce : swingPushForce * 0.5f;
                
                swingVelocity += tangentDir * accel * Time.deltaTime;
                
                // Controle lateral
                Vector3 lateralDir = Vector3.Cross(dirToAnchor, Vector3.up).normalized;
                swingVelocity += lateralDir * (h * lateralControlForce * Time.deltaTime);
            }
        }

        // 4. Restrição Rígida de Pêndulo (Impede a corda de esticar)
        if (currentDistance > currentRopeLength)
        {
            // Força de tensão imediata
            float overshoot = currentDistance - currentRopeLength;
            swingVelocity += dirToAnchor * (overshoot * ropeTensionStrength) * Time.deltaTime;

            // Remove componente de velocidade que foge do centro
            float velDot = Vector3.Dot(swingVelocity, dirToAnchor);
            if (velDot < 0) swingVelocity -= dirToAnchor * velDot;
        }

        // 5. Amortecimento e Limites
        swingVelocity *= (1f - airResistance * Time.deltaTime);
        if (swingVelocity.magnitude > maxSwingSpeed) swingVelocity = swingVelocity.normalized * maxSwingSpeed;

        // Aplica ao PlayerMovement
        playerMovement.moveDirection = swingVelocity;
        playerMovement.currentSpeed = swingVelocity.magnitude;
    }

    private void ExitSwing(bool isJump)
    {
        _isSwingingInternal = false;
            playerMovement.isSwinging = false;
            // A lógica de queda deve ser gerenciada pelo PlayerMovement_FrontiersStyle. O SwingSystem não deve forçar IsFalling aqui.
            // No entanto, precisamos garantir que o Animator possa transicionar para a queda se o jogador estiver no ar.
            // Isso será tratado pelo PlayerMovement_FrontiersStyle, que deve definir IsFalling com base em isGrounded.
            if (swingAnimator != null) swingAnimator.SetBool("IsSwinging", false); // Desativa o estado de balanço ao sair
        
        // --- RESET DE HABILIDADES AO SAIR ---
        playerMovement.doubleJumpCharges = playerMovement.maxDoubleJumpCharges;
        playerMovement.airDashCharges = playerMovement.maxAirDashCharges;
        
        if (isJump)
        {
            Vector3 jumpVel = swingVelocity;
            Vector3 camForwardFlat = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            jumpVel += camForwardFlat * 4f; // Pequeno bônus frontal
            jumpVel.y = Mathf.Max(jumpVel.y + 3f, playerMovement.jumpForce); 
            
            playerMovement.moveDirection = jumpVel;
            playerMovement.isJumping = true;
        }
        else
        {
            playerMovement.moveDirection = swingVelocity;
        }

        if (ropeRenderer != null) ropeRenderer.enabled = false;
    }

    private void DrawRope()
    {
        if (ropeRenderer == null) return;
        ropeRenderer.positionCount = ropeSegments;
        ropeWaveTimer += Time.deltaTime * ropeWaveSpeed;

        Vector3 start = handTransform.position;
        Vector3 end = isLaunching ? Vector3.Lerp(start, anchorPoint, launchTimer / launchDuration) : anchorPoint;
        
        float dist = Vector3.Distance(start, end);
        float slack = Mathf.Max(0, currentRopeLength - dist);
        float actualCurvature = (slack > 0.1f) ? ropeCurvature * slack : 0.1f;

        for (int i = 0; i < ropeSegments; i++)
        {
            float t = (float)i / (ropeSegments - 1);
            Vector3 point = Vector3.Lerp(start, end, t);

            // Curvatura Catenária
            float curve = Mathf.Sin(t * Mathf.PI) * actualCurvature;
            point -= Vector3.up * curve;

            // Ondulação Dinâmica
            float wave = Mathf.Sin(t * Mathf.PI) * Mathf.Sin(ropeWaveTimer + t * 10f) * ropeWaveAmplitude;
            Vector3 ropeDir = (end - start).normalized;
            Vector3 waveDir = Vector3.Cross(ropeDir, Vector3.up).normalized;
            if (waveDir == Vector3.zero) waveDir = Vector3.right;
            point += waveDir * wave;

            ropeRenderer.SetPosition(i, point);
        }
    }

    private void UpdateUI()
    {
        if (warpIcon == null) return;
        bool show = currentTargetAnchor != null;
        warpIcon.gameObject.SetActive(show);
        if (show)
        {
            Vector3 anchorPos = currentTargetAnchor.GetAnchorPosition();
            warpIcon.position = cameraTransform.GetComponent<Camera>().WorldToScreenPoint(anchorPos);
            
            UnityEngine.UI.Image iconImage = warpIcon.GetComponent<UnityEngine.UI.Image>();
            if (iconImage != null)
            {
                // O ícone agora reflete apenas se estamos tocando a groundLayer
                iconImage.color = isTouchingGround ? blockedIconColor : activeIconColor;
            }
        }
    }

    private SwingAnchor FindNearestSwingAnchor()
    {
        SwingAnchor best = null;
        float closestScore = 1000000f;
        Camera mainCam = cameraTransform.GetComponent<Camera>();
        if (mainCam == null) mainCam = Camera.main;

        foreach (var anchor in SwingAnchor.allAnchors)
        {
            if (anchor == null || !anchor.gameObject.activeInHierarchy) continue;
            Vector3 pos = anchor.GetAnchorPosition();
            
            // 1. Distância
            float d = Vector3.Distance(handTransform.position, pos);
            if (d > anchorDetectionRadius) continue;
            
            // 2. Campo de Visão
            Vector3 screenPos = mainCam.WorldToScreenPoint(pos);
            if (screenPos.z <= 0) continue;
            
            // 3. Raycast de Obstáculos (Mantendo a lógica original)
            if (Physics.Linecast(handTransform.position, pos, out RaycastHit hit, obstacleLayer))
            {
                if (hit.transform != anchor.transform && !hit.transform.IsChildOf(anchor.transform)) continue;
            }
            
            // 4. Priorização por centro de tela
            float distFromCenter = (new Vector2(screenPos.x, screenPos.y) - new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)).sqrMagnitude;
            Vector3 dirToAnchor = (pos - handTransform.position).normalized;
            float alignment = Vector3.Dot(cameraTransform.forward, dirToAnchor);
            float score = distFromCenter - (alignment * 5000f);

            if (score < closestScore) { closestScore = score; best = anchor; }
        }
        return best;
    }

    // Opcional: Desenha a esfera de detecção no editor para facilitar o ajuste do raio
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, groundCheckRadius);
    }
}
