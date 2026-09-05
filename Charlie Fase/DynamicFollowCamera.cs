using UnityEngine;

public class DynamicFollowCamera : MonoBehaviour
{
    [Tooltip("O Transform do jogador que a câmera deve seguir e orbitar.")]
    public Transform target;

    [Header("Configurações de Posição Normal")]
    public float baseDistance = 10.0f;
    public float height = 5.0f;
    public float positionSmoothSpeed = 0.075f; // Reduzido para maior suavidade

    [Header("Configurações de Rotação Manual")]
    public float rotationSpeed = 5.0f;
    public float autoCenterSpeed = 0.75f; // Reduzido para auto-centralização mais suave
    public float minYAngle = -10f;
    public float maxYAngle = 80f;

    [Header("Configurações de Zoom")]
    public float zoomSpeed = 2.0f;
    public float minDistance = 5.0f;
    public float maxDistance = 15.0f;

    [Header("Configurações de Colisão Aprimorada")]
    public LayerMask collisionLayers;
    public float cameraRadius = 0.4f;
    public float collisionPadding = 0.15f;
    public float collisionResponseSpeed = 15f; // Reduzido para resposta de colisão mais suave
    public float collisionRaycastOffset = 1.2f;

    [Header("Configurações Durante Horizontal Bar")]
    public float barDistance = 8.0f;
    public float barHeight = 2.5f;
    public float barSideOffset = 1.5f;
    public float barFOV = 70f;
    public float barTransitionSpeed = 4.0f;
    public float barPitch = 15f;
    [Range(0f, 2f)]
    public float barAutoCenterStrength = 0.5f;

    [Header("Configurações Durante Narrow Passage (Estilo TLOU2)")]
    public float narrowDistance = 2.5f;
    public float narrowHeight = 1.5f;
    public float narrowSideOffset = 0.6f;
    public float narrowFOV = 45f;
    [Tooltip("Velocidade de transição ao ENTRAR no modo Narrow")]
    public float narrowEnterSpeed = 2.0f;
    [Tooltip("Velocidade de transição ao SAIR do modo Narrow")]
    public float narrowExitSpeed = 1.5f;
    public float narrowPitch = 10f; 
    [Tooltip("Tempo em segundos para a colisão da layer Narrow voltar após sair do modo Narrow")]
    public float narrowCollisionReturnDelay = 0.5f;

    [Header("Configurações Durante Wall Run (Estilo Jedi Survivor)")]
    public float wallRunTiltAmount = 15f;
    public float wallRunTiltSpeed = 6f;
    public float wallRunYawOffset = 15f; 
    public float wallRunYawSpeed = 4f;
    public float wallRunFOV = 75f;
    public float wallRunDistanceMultiplier = 1.2f;
    public float wallRunSideOffset = 2.5f;
    public float wallRunSideOffsetSpeed = 3.0f;
    public float wallRunDistanceTransitionSpeed = 2.0f;
    [Range(0f, 5f)]
    public float wallRunAutoCenterStrength = 2.5f;

    [Header("Configurações Durante Rail Grind")]
    public float grindDistance = 8.0f;
    public float grindHeight = 3.0f;
    public float grindSmoothSpeed = 0.2f;
    public float normalFOV = 60f;
    public float grindFOV = 70f;
    public float boostFOV = 80f;
    public float fovTransitionSpeed = 5f;
    public float grindTiltAmount = 5f;
    public float boostShakeAmount = 0.1f;
    public bool allowGrindCameraRotation = true;
    public float grindRotationSpeed = 3.0f;
    [Range(0f, 2f)]
    public float grindAutoCenterStrength = 0.3f;
    public float grindSideOffset = 2.0f;
    public float grindSideOffsetSpeed = 3.0f;
    public float grindYawOffset = 10f;
    public float grindYawSpeed = 3.0f;

    [Header("Configurações Durante Warp")]
    public float warpFOV = 85f;

    [Header("Configurações Durante Diálogo com NPC")]
    public float dialogueHeight = 1.6f;
    public float dialogueDistance = 3.5f;
    public float dialoguePitch = 12f;
    public float dialogueSideOffset = 0f;
    public float dialogueFOV = 50f;
    public float dialogueTransitionSpeed = 3.0f;
    private float currentDialogueSideOffset = 0f;
    private bool isTransitioningToDialogue = false;
    private bool wasInDialogueLastFrame = false;

    [Header("Configurações Durante Sentado (Bench)")]
    public float sitDistance = 4.0f;
    public float sitHeight = 1.2f;
    public float sitPitch = 10f;
    public float sitTransitionSpeed = 2.0f;
    public float sitAutoCenterDelay = 2.0f;
    public float sitAutoCenterSpeed = 1.0f;
    [Tooltip("Limite de rotação horizontal para os lados (em graus)")]
    public float sitMaxYawAngle = 60f; 
    [Tooltip("Limite de rotação vertical (mínimo e máximo)")]
    public Vector2 sitMinMaxPitch = new Vector2(-5f, 30f);
    private float lastSitInputTime;
    private bool isTransitioningToSit = false;
    private bool wasSittingLastFrame = false;

    [Header("Configurações Durante Glide")]
    [Tooltip("Altura adicional da câmera enquanto o jogador está planando.")]
    public float glideHeightOffset = -1.5f;
    [Tooltip("Distância adicional da câmera enquanto o jogador está planando.")]
    public float glideDistanceOffset = 2.0f;
    [Tooltip("Velocidade de transição para os valores de Glide.")]
    public float glideTransitionSpeed = 3.0f;

    [Header("Configurações Durante Swing (Spider-Man Style)")]
    public float swingMinFOV = 60f;
    public float swingMaxFOV = 95f;
    public float swingMaxSpeedForFOV = 35f;
    [Tooltip("Altura base da câmera durante o swing.")]
    public float swingBaseHeight = 4.0f;
    [Tooltip("Distância base da câmera durante o swing.")]
    public float swingBaseDistance = 8.0f;
    [Tooltip("Ajuste de altura ADICIONAL baseado na velocidade vertical (mergulho).")]
    public float swingDiveHeightOffset = 2.0f;
    [Tooltip("Distância ADICIONAL baseada na velocidade horizontal.")]
    public float swingSpeedDistanceMultiplier = 1.5f;
    public float swingTransitionSpeed = 5f;
    [Header("Spider-Man Swing Tilt Settings")]
    [Tooltip("Inclinação máxima da câmera durante o balanço lateral.")]
    public float swingTiltAmount = 15f;
    [Tooltip("Velocidade de inclinação da câmera.")]
    public float swingTiltSpeed = 5f;
    [Tooltip("Sensibilidade do movimento lateral para o tilt.")]
    public float swingTiltSensitivity = 2f;

    [Header("Configurações de Spin Dash (Cinematográfico)")]
    [Tooltip("Duração do congelamento total (o impacto inicial).")]
    public float spinDashFreezeDuration = 0.15f;
    [Tooltip("Quanto tempo a câmera leva para alcançar o jogador totalmente.")]
    public float spinDashCatchUpDuration = 0.6f;
    [Tooltip("Curva de velocidade do retorno. Comece com uma curva que sobe rápido (Ease Out).")]
    public AnimationCurve catchUpCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    private float spinDashTimer = 0f;
    private bool isSpinDashFrozen = false;
    private bool isCatchingUp = false;
    private float catchUpProgress = 0f;
    
    private Vector3 startCatchUpPos;
    private Quaternion startCatchUpRot;

    [Header("Configurações de FOV por Velocidade")]
    public float speedThreshold = 15f;
    public float speedFOV = 65f;

    [Header("Configurações de Camera Shake")]
    [SerializeField] private float wallDashShakeAmount = 0.15f;
    [SerializeField] private float wallDashShakeDuration = 0.3f;
    [SerializeField] private float wallDashShakeFrequency = 25f;
    [SerializeField] private bool enableWallDashShake = true;

    [Header("Camera Shake ao Receber Dano")]
    [SerializeField] private bool enableDamageShake = true;
    [SerializeField, Min(0f)] private float damageShakeAmount = 0.18f;
    [SerializeField, Min(0f)] private float damageShakeDuration = 0.22f;
    [SerializeField, Min(0f)] private float damageShakeFrequency = 28f;
    [Header("Impacto de Dano no Chão")]
    [SerializeField, Min(0f)] private float damageRecoilDistance = 0.12f;
    [Tooltip("Pitch do impacto no chão. Positivo inclina para baixo; negativo inclina para cima.")]
    [SerializeField] private float damagePitchAmount = -4f;
    [Tooltip("Velocidade de transição do pitch do Hit no chão.")]
    [SerializeField, Min(0.01f)] private float damagePitchTransitionSpeed = 8f;
    [Tooltip("Variação vertical no chão. Positivo sobe; negativo desce.")]
    [SerializeField] private float damageHeightOffset = -0.12f;
    [SerializeField, Min(0.001f)] private float damageHeightTransitionIn = 0.08f;
    [SerializeField, Min(0f)] private float damageHeightHoldDuration = 0.04f;
    [SerializeField, Min(0.001f)] private float damageHeightTransitionOut = 0.16f;
    [SerializeField, Min(0f)] private float damageImpactDuration = 0.22f;

    [Header("Alinhamento da Câmera após Hit")]
    [Tooltip("Alinha suavemente a câmera atrás do jogador sempre que ele recebe dano.")]
    [SerializeField] private bool alignBehindOnDamage = true;
    [Tooltip("Tempo durante o qual a câmera acompanha o alinhamento após o hit.")]
    [SerializeField, Min(0f)] private float damageAlignmentDuration = 0.45f;
    [Tooltip("Velocidade do alinhamento horizontal atrás do jogador.")]
    [SerializeField, Min(0.01f)] private float damageAlignmentSpeed = 8f;

    [Header("Impacto de AirHit")]
    [SerializeField, Min(0f)] private float airDamageRecoilDistance = 0.16f;
    [Tooltip("Pitch do AirHit. Positivo inclina para baixo; negativo inclina para cima.")]
    [SerializeField] private float airDamagePitchAmount = 5f;
    [Tooltip("Velocidade de transição do pitch do AirHit.")]
    [SerializeField, Min(0.01f)] private float airDamagePitchTransitionSpeed = 10f;
    [Tooltip("Variação vertical do AirHit. Positivo sobe; negativo desce.")]
    [SerializeField] private float airDamageHeightOffset = 0.08f;
    [SerializeField, Min(0.001f)] private float airDamageHeightTransitionIn = 0.05f;
    [SerializeField, Min(0f)] private float airDamageHeightHoldDuration = 0.08f;
    [SerializeField, Min(0.001f)] private float airDamageHeightTransitionOut = 0.24f;
    [SerializeField, Min(0f)] private float airDamageImpactDuration = 0.30f;

        [Header("Câmera Cinematográfica de Morte")]
    [Tooltip("Altura final da câmera acima do jogador durante a morte.")]
    [SerializeField, Min(0f)] private float deathCameraHeight = 8f;
    [Tooltip("Distância final da câmera atrás do jogador durante a morte.")]
    [SerializeField, Min(0f)] private float deathCameraDistance = 14f;
    [Tooltip("Deslocamento lateral opcional da câmera durante a morte.")]
    [SerializeField] private float deathCameraSideOffset = 2f;
    [Tooltip("Velocidade de aproximação da posição cinematográfica.")]
    [SerializeField, Min(0.01f)] private float deathCameraPositionSpeed = 2.2f;
    [Tooltip("Velocidade de rotação para olhar para o jogador.")]
    [SerializeField, Min(0.01f)] private float deathCameraRotationSpeed = 3.5f;
    [Tooltip("Altura do ponto que a câmera observa no jogador.")]
    [SerializeField, Min(0f)] private float deathCameraLookAtHeight = 1.1f;
    [Tooltip("FOV usado durante a câmera de morte.")]
    [SerializeField, Min(1f)] private float deathCameraFOV = 58f;
    [Tooltip("Usa tempo não escalado para continuar a transição mesmo se o jogo estiver pausado.")]
    [SerializeField] private bool deathCameraUsesUnscaledTime = true;

    [Header("Configurações Durante Slope Slide")]

    public float slideDistance = 12.0f;
    public float slideHeight = 6.0f;
    public float slideSideOffset = 2.0f;
    public float slideFOV = 75f;
    public float slideTiltAngle = 15.0f;
    public float slideTiltSpeed = 7.0f;
    public float slideLensDistortion = -0.3f;
    public float distortionTransitionSpeed = 5f;
    public float slideTransitionSpeed = 4.0f;
    public float slidePitch = 15f; 
    [Range(0f, 180f)] public float slideRotationLimit = 90f;

    [Header("Configurações Durante Ground Slide (Mirror's Edge Style)")]
    public float groundSlideDistance = 8.0f;
    public float groundSlideHeight = 3.0f;
    public float groundSlideSideOffset = 1.5f;
    public float groundSlideFOV = 70f;
    public float groundSlideTiltAngle = 10.0f;
    public float groundSlideTiltSpeed = 10.0f;
    public float groundSlideTransitionSpeed = 6.0f;
    public float groundSlidePitch = 15f;
    [Range(0f, 180f)] public float groundSlideRotationLimit = 90f;

    [Header("Configurações Durante Slide (Motion Blur)")]
    public float slideMotionBlurIntensity = 0.5f;
    public float motionBlurTransitionSpeed = 7.0f;

    [Header("Configurações de Radial Blur")]
    public bool enableRadialBlur = true;
    public float radialBlurActivationSpeed = 25f;
    public float maxRadialBlurIntensity = 0.7f;
    public float radialBlurTransitionSpeed = 3f;

    [Header("Configurações de Pós-Processamento Adicionais")]
    public float slideChromaticAberration = 0.2f;
    public float slideVignetteIntensity = 0.3f;
    public float ppTransitionSpeed = 5.0f;

    private float currentDistance;
    private float currentX = 0.0f;
    private float currentY = 0.0f;
    private float currentTilt = 0f;
    private float currentWallRunYaw = 0f; 
    private float collisionDistance;
    private Camera cam;
    private float currentFOV;
    private float currentDistortion = 0f;
    private float currentMotionBlur = 0f;
    private float currentChromaticAberration = 0f;
    private float currentVignette = 0f;
    private float currentRadialBlur = 0f;
    private float currentWallRunSideOffset = 0f;
    private float currentGrindSideOffset = 0f;
    private float currentGrindYaw = 0f;
    private float currentNarrowSideOffset = 0f;
    private float currentBarSideOffset = 0f;
    private float currentSlideSideOffset = 0f;
    private float currentWallRunDistanceMultiplier = 1.0f;
    private bool wasTransitioning = false;
    
    [Header("QTE Settings")]
    private bool isQTELocked = false;
    private bool isQTECentered = false;
    public float qteTransitionSpeed = 5f;
    
    [Header("QTE Camera Override Values")]
    public float qtePitch = 15f;        // Ângulo vertical durante o QTE
    public float qteFOV = 70f;          // FOV durante o QTE
    public float qteHeight = 2.0f;      // Altura da câmera durante o QTE (relative to player)
    public float qteDistance = 5.0f;    // Distância da câmera durante o QTE (relative to player)
    
    private float qteCurrentFOV = -1f;  // FOV atual do QTE para Lerp suave
    
    private float currentGlideHeightFactor = 0f;
    private float currentGlideDistanceFactor = 0f;
    
    private float currentSwingFOV = 60f;
    private float currentSwingHeightOffset = 0f;
    private float currentSwingDistanceMultiplier = 1f;

    private int narrowLayerIndex;
    private float narrowExitTimer = 0f;

    private float smoothNarrowHeight;
    private float smoothNarrowDistance;

    private float shakeTimer = 0f;
    private float shakeDuration = 0f;
    private float shakeAmount = 0f;
    private float shakeFrequency = 0f;
    private Vector3 shakeOffset = Vector3.zero;
    private float damageImpactTimer = float.PositiveInfinity;
    private float damageHeightTimer = float.PositiveInfinity;
    private float damagePitchTimer = float.PositiveInfinity;

    private PlayerMovement_FrontiersStyle playerMovement;
    private PlayerRailRide_SonicStyle_Spline railRideSpline;
    private PlayerRailRide_SonicStyle_Spline railRide;
    private WarpSystem warpSystem;
    private SlopeSlideSystem slopeSlideSystem;
    private CameraRailManager cameraRailManager;
    private HorizontalBarHandler horizontalBarHandler; 

        private bool wasOnBar = false;
    private bool isDeathCameraActive = false;
    private Vector3 deathCameraVelocity = Vector3.zero;
    private float deathCameraOriginalFOV;
    private bool deathCameraOriginalFOVCaptured = false;

    private bool barAutoCenterActive = false;
    private float barEntrySideOffset = 0f;

    [Header("Quick Turn Camera")]
    public float quickTurnRotationSpeed = 10f;
    public float quickTurnBlurIntensity = 0.4f;
    private float quickTurnVelocity = 0f;
    private float quickTurnTargetAdd = 0f;
    private float currentQuickTurnAdd = 0f;
    private bool isQuickTurnActive = false;

    public float CurrentLensDistortion => currentDistortion;
    public float CurrentMotionBlur => currentMotionBlur;
    public float CurrentChromaticAberration => currentChromaticAberration;
    public float CurrentVignette => currentVignette;
    public float CurrentRadialBlur => currentRadialBlur;

    /// <summary>
    /// Sincroniza os ângulos internos com a rotação atual do Transform.
    /// Deve ser chamado depois de uma câmera cinematográfica assumir a câmera
    /// principal, evitando que o controle retome com a orientação antiga.
    /// </summary>
    public void SyncRotationToCurrentTransform()
    {
        currentX = transform.eulerAngles.y;
        currentY = transform.eulerAngles.x;
        wasTransitioning = false;
    }

    void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }

        if (target != null)
        {
            playerMovement = target.GetComponent<PlayerMovement_FrontiersStyle>();
            railRideSpline = target.GetComponent<PlayerRailRide_SonicStyle_Spline>();
            if (railRideSpline == null) railRide = target.GetComponent<PlayerRailRide_SonicStyle_Spline>();
            warpSystem = target.GetComponent<WarpSystem>();
            slopeSlideSystem = target.GetComponent<SlopeSlideSystem>();
            cameraRailManager = FindObjectOfType<CameraRailManager>();
            horizontalBarHandler = target.GetComponent<HorizontalBarHandler>();

            currentDistance = baseDistance;
            collisionDistance = baseDistance;
            currentX = target.eulerAngles.y;
            currentY = 15f;
            
            smoothNarrowHeight = height;
            smoothNarrowDistance = baseDistance;

            narrowLayerIndex = LayerMask.NameToLayer("Narrow");
            if (narrowLayerIndex == -1)
            {
                Debug.LogWarning("Layer 'Narrow' not found. Please ensure it is defined in your project.");
            }
        }

        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;

        currentFOV = normalFOV;
        if (cam != null) cam.fieldOfView = currentFOV;
    }

    void Update()
    {
        if (target == null) return;

        if (isSpinDashFrozen)
        {
            spinDashTimer -= Time.deltaTime;
            if (spinDashTimer <= 0)
            {
                isSpinDashFrozen = false;
                isCatchingUp = true;
                catchUpProgress = 0f;
                startCatchUpPos = transform.position;
                startCatchUpRot = transform.rotation;
            }
            return;
        }
        else if (isCatchingUp)
        {
            catchUpProgress += Time.deltaTime / spinDashCatchUpDuration;
            if (catchUpProgress >= 1f)
            {
                isCatchingUp = false;
            }
        }
        
        bool isGrinding = IsPlayerGrinding();
        bool isInNarrow = IsPlayerInNarrow();
        bool isOnBar = IsPlayerOnBar();
        
        if (isOnBar && !wasOnBar)
        {
            barAutoCenterActive = true;

            if (horizontalBarHandler != null)
            {
                // A lógica corrigida:
                // Quando o jogador entra por trás (EnteredFromBack = true), a câmera vai rotacionar 180 graus 
                // para ficar atrás do novo forward do jogador.
                // Como o side offset é aplicado usando finalRotation * Vector3.right, 
                // e a rotação da câmera inverteu 180 graus, o "lado direito" da câmera no mundo agora é o oposto.
                // Portanto, para manter o jogador no mesmo lado visual da tela, o "sinal" do offset DEVE ser invertido.
                
                if (horizontalBarHandler.EnteredFromBack)
                {
                    barEntrySideOffset = -barSideOffset;
                }
                else
                {
                    barEntrySideOffset = barSideOffset;
                }
            }
            else
            {
                barEntrySideOffset = barSideOffset;
            }
        }
        else if (!isOnBar)
        {
            barAutoCenterActive = false;
        }
        wasOnBar = isOnBar;

        HandleZoomInput();

        bool isWallRunning = IsPlayerWallRunning();
        bool isBoosting = IsPlayerBoosting();
        bool isWarping = IsPlayerWarping();
        bool isSliding = IsPlayerSliding();
        bool isGroundSliding = IsPlayerGroundSliding();
        bool isGliding = IsPlayerGliding();
        bool isSwinging = IsPlayerSwinging();
        bool isSitting = IsPlayerSitting();
        bool isInDialogue = NPCDialogueManager.Instance != null && NPCDialogueManager.Instance.IsDialogueActive;

        // ✅ NOVO: Detecta o início/fim do diálogo (para transição da câmera)
        if (isInDialogue && !wasInDialogueLastFrame)
        {
            isTransitioningToDialogue = true;
        }
        else if (!isInDialogue)
        {
            isTransitioningToDialogue = false;
        }
        wasInDialogueLastFrame = isInDialogue;

        // Detecta o início da transição ao sentar
        if (isSitting && !wasSittingLastFrame)
        {
            isTransitioningToSit = true;
        }
        else if (!isSitting)
        {
            isTransitioningToSit = false;
        }
        wasSittingLastFrame = isSitting;

        // Suaviza os fatores de Glide
        float targetGlideFactor = isGliding ? 1f : 0f;
        currentGlideHeightFactor = Mathf.Lerp(currentGlideHeightFactor, targetGlideFactor, Time.deltaTime * glideTransitionSpeed);
        currentGlideDistanceFactor = Mathf.Lerp(currentGlideDistanceFactor, targetGlideFactor, Time.deltaTime * glideTransitionSpeed);

        // Lógica de Swing (Spider-Man Style)
        float targetFOVFromSwing = -1f; // -1 indica que não está em swing

        if (isSwinging)
        {
            float swingSpeed = GetPlayerSpeed();
            float verticalVel = playerMovement.moveDirection.y;
            
            // 1. FOV Dinâmico baseado na velocidade
            float targetSwingFOV = Mathf.Lerp(swingMinFOV, swingMaxFOV, swingSpeed / swingMaxSpeedForFOV);
            currentSwingFOV = Mathf.Lerp(currentSwingFOV, targetSwingFOV, Time.deltaTime * swingTransitionSpeed);
            targetFOVFromSwing = currentSwingFOV;

            // 2. Altura Dinâmica (Mergulho)
            // Se estiver caindo rápido (dive), a câmera sobe um pouco para mostrar o chão
            float diveFactor = Mathf.Clamp01(-verticalVel / 20f);
            float targetHeightOffset = diveFactor * swingDiveHeightOffset;
            currentSwingHeightOffset = Mathf.Lerp(currentSwingHeightOffset, targetHeightOffset, Time.deltaTime * swingTransitionSpeed);

            // 3. Distância Dinâmica
            float speedFactor = Mathf.Clamp01(swingSpeed / swingMaxSpeedForFOV);
            float targetDistMult = 1f + (speedFactor * (swingSpeedDistanceMultiplier - 1f));
            currentSwingDistanceMultiplier = Mathf.Lerp(currentSwingDistanceMultiplier, targetDistMult, Time.deltaTime * swingTransitionSpeed);
        }
        else
        {
            currentSwingHeightOffset = Mathf.Lerp(currentSwingHeightOffset, 0f, Time.deltaTime * swingTransitionSpeed);
            currentSwingDistanceMultiplier = Mathf.Lerp(currentSwingDistanceMultiplier, 1f, Time.deltaTime * swingTransitionSpeed);
        }
        
        if (!isInNarrow && !isOnBar && !isSitting && !isInDialogue)
        {
            if (isGrinding && allowGrindCameraRotation)
                HandleRotationInput(grindRotationSpeed);
            else if (isSliding)
            {
                HandleRotationInput(rotationSpeed);
                // Restringe a rotação em relação ao jogador (Sonic Unleashed Style)
                float targetY = target.eulerAngles.y;
                float angleDiff = Mathf.DeltaAngle(targetY, currentX);
                currentX = targetY + Mathf.Clamp(angleDiff, -slideRotationLimit, slideRotationLimit);
            }
            else if (isGroundSliding)
            {
                HandleRotationInput(rotationSpeed);
                // Restringe a rotação para o Ground Slide
                // Transição suave para o ângulo restrito
                float targetYAngle = target.eulerAngles.y;
                float desiredX = targetYAngle + Mathf.Clamp(Mathf.DeltaAngle(targetYAngle, currentX), -groundSlideRotationLimit, groundSlideRotationLimit);
                currentX = Mathf.LerpAngle(currentX, desiredX, Time.deltaTime * groundSlideTransitionSpeed);
            }
            else if (!isGrinding)
                HandleRotationInput(rotationSpeed);
        }

        UpdateShake();

        // Lógica de Quick Turn Aditiva
        if (Mathf.Abs(currentQuickTurnAdd - quickTurnTargetAdd) > 0.01f)
        {
            isQuickTurnActive = true;
            float prevAdd = currentQuickTurnAdd;
            currentQuickTurnAdd = Mathf.MoveTowards(currentQuickTurnAdd, quickTurnTargetAdd, Time.deltaTime * quickTurnRotationSpeed * 180f);
            float delta = currentQuickTurnAdd - prevAdd;
            currentX += delta;
        }
        else
        {
            isQuickTurnActive = false;
            currentQuickTurnAdd = 0f;
            quickTurnTargetAdd = 0f;
        }

        float playerSpeed = GetPlayerSpeed();
        bool isMovingFast = playerSpeed >= speedThreshold;
        
        float targetFOV = normalFOV;
        float targetDistortion = 0f;
        
        if (targetFOVFromSwing > 0) targetFOV = targetFOVFromSwing;
        else if (isInNarrow) targetFOV = narrowFOV;
        else if (isOnBar) targetFOV = barFOV;
        else if (isInDialogue) targetFOV = dialogueFOV;
        else if (isWarping) targetFOV = warpFOV;
        else if (isBoosting) targetFOV = boostFOV;
        else if (isGrinding) targetFOV = grindFOV;
        else if (isWallRunning) targetFOV = wallRunFOV;
        else if (isSliding) { targetFOV = slideFOV; targetDistortion = slideLensDistortion; }
        else if (isGroundSliding) { targetFOV = groundSlideFOV; targetDistortion = slideLensDistortion; }
        else if (isMovingFast) targetFOV = speedFOV;

        // ✅ QTE OVERRIDE: Durante o QTE, força o FOV configurável em tempo real
        if (isQTELocked)
        {
            targetFOV = qteFOV;
        }

        float fovSpeed = isInNarrow ? narrowEnterSpeed : (isOnBar ? barTransitionSpeed : narrowExitSpeed);
        currentFOV = Mathf.Lerp(currentFOV, targetFOV, Time.deltaTime * fovSpeed);
        if (cam != null) cam.fieldOfView = currentFOV;

        currentDistortion = Mathf.Lerp(currentDistortion, targetDistortion, Time.deltaTime * distortionTransitionSpeed);

        float targetMotionBlur = (isSliding || isQuickTurnActive) ? (isQuickTurnActive ? quickTurnBlurIntensity : slideMotionBlurIntensity) : 0f;
        currentMotionBlur = Mathf.Lerp(currentMotionBlur, targetMotionBlur, Time.deltaTime * motionBlurTransitionSpeed);

        float targetChromatic = isSliding ? slideChromaticAberration : 0f;
        float targetVignette = isSliding ? slideVignetteIntensity : 0f;
        currentChromaticAberration = Mathf.Lerp(currentChromaticAberration, targetChromatic, Time.deltaTime * ppTransitionSpeed);
        currentVignette = Mathf.Lerp(currentVignette, targetVignette, Time.deltaTime * ppTransitionSpeed);

        // Lógica do Radial Blur baseada na velocidade
        float targetRadialBlur = 0f;
        if (enableRadialBlur && playerSpeed >= radialBlurActivationSpeed)
        {
            // Calcula a intensidade baseada no quão acima da velocidade de ativação o jogador está
            // Você pode ajustar a fórmula conforme necessário (ex: normalizar entre a velocidade de ativação e a velocidade máxima)
            float speedFactor = Mathf.Clamp01((playerSpeed - radialBlurActivationSpeed) / 10f); // Supondo que 10 unidades acima da ativação seja o máximo
            targetRadialBlur = maxRadialBlurIntensity * speedFactor;
        }
        currentRadialBlur = Mathf.Lerp(currentRadialBlur, targetRadialBlur, Time.deltaTime * radialBlurTransitionSpeed);
    }

    void LateUpdate()
    {
        if (target == null) return;

        bool isBeingTransitioned = cameraRailManager != null && cameraRailManager.IsThisCameraTransitioning(transform);

        // A câmera de morte tem prioridade sobre rail, QTE, diálogo e controles manuais.
        if (isDeathCameraActive)
        {
            UpdateDeathCamera();
            return;
        }

        if (isBeingTransitioned)
        {
            currentX = transform.eulerAngles.y;
            currentY = transform.eulerAngles.x;
            wasTransitioning = true;
            return;
        }

        if (wasTransitioning)
        {
            currentY = transform.eulerAngles.x;
            wasTransitioning = false;
        }

        bool isGrinding = IsPlayerGrinding();
        bool isWallRunning = IsPlayerWallRunning();
        bool isSliding = IsPlayerSliding();
        bool isGroundSliding = IsPlayerGroundSliding();
        bool isInNarrow = IsPlayerInNarrow();
        bool isOnBar = IsPlayerOnBar();
        bool isSwinging = IsPlayerSwinging();
        bool isSitting = IsPlayerSitting();
        bool isInDialogue = NPCDialogueManager.Instance != null && NPCDialogueManager.Instance.IsDialogueActive;
        
        if (isQTELocked)
        {
            // ✅ QTE: A câmera se centraliza igual ao sistema de "Sitting"
            // Força o alinhamento atrás do jogador com a velocidade de transição do QTE
            currentX = Mathf.LerpAngle(currentX, target.eulerAngles.y, Time.unscaledDeltaTime * qteTransitionSpeed * 3f);
            currentY = Mathf.Lerp(currentY, qtePitch, Time.unscaledDeltaTime * qteTransitionSpeed * 2f);
        }
        else if (isInNarrow)
        {
            narrowExitTimer = narrowCollisionReturnDelay;
        }
        else if (narrowExitTimer > 0)
        {
            narrowExitTimer -= Time.deltaTime;
        }

        float tSpeed = isSitting ? sitTransitionSpeed : (isInNarrow ? narrowEnterSpeed : (isOnBar ? barTransitionSpeed : (isInDialogue ? dialogueTransitionSpeed : (isSliding ? slideTransitionSpeed : (isGroundSliding ? groundSlideTransitionSpeed : narrowExitSpeed)))));

        float targetBaseHeight = isSitting ? sitHeight : (isInNarrow ? narrowHeight : (isOnBar ? barHeight : (isInDialogue ? dialogueHeight : (isSliding ? slideHeight : (isGroundSliding ? groundSlideHeight : (isGrinding ? grindHeight : (isSwinging ? swingBaseHeight : height)))))));
        float targetBaseDistance = isSitting ? sitDistance : (isInNarrow ? narrowDistance : (isOnBar ? barDistance : (isInDialogue ? dialogueDistance : (isSliding ? slideDistance : (isGroundSliding ? groundSlideDistance : (isGrinding ? grindDistance : (isSwinging ? swingBaseDistance : currentDistance)))))));
        
        // ✅ QTE OVERRIDE: Durante o QTE, força altura e distância configuráveis em tempo real
        if (isQTELocked)
        {
            targetBaseHeight = qteHeight;
            targetBaseDistance = qteDistance;
        }
        
        // Aplica o offset de Glide
        targetBaseHeight += glideHeightOffset * currentGlideHeightFactor;
        targetBaseDistance += glideDistanceOffset * currentGlideDistanceFactor;
        
        // Aplica o offset de Swing (Spider-Man Style - Dive e Outros)
        targetBaseHeight += currentSwingHeightOffset;

        smoothNarrowHeight = Mathf.Lerp(smoothNarrowHeight, targetBaseHeight, Time.deltaTime * tSpeed);
        smoothNarrowDistance = Mathf.Lerp(smoothNarrowDistance, targetBaseDistance, Time.deltaTime * tSpeed);
        
        float targetDistMultiplier = isWallRunning ? wallRunDistanceMultiplier : 1.0f;
        // Combina o multiplicador de Wall Run com o de Swing
        targetDistMultiplier *= currentSwingDistanceMultiplier;

        currentWallRunDistanceMultiplier = Mathf.Lerp(currentWallRunDistanceMultiplier, targetDistMultiplier, Time.deltaTime * wallRunDistanceTransitionSpeed);
        
        float finalIdealDistance = smoothNarrowDistance * currentWallRunDistanceMultiplier;
        

        if (isInNarrow)
        {
            currentX = Mathf.LerpAngle(currentX, target.eulerAngles.y, Time.deltaTime * tSpeed);
            currentY = Mathf.Lerp(currentY, narrowPitch, Time.deltaTime * tSpeed);
        }
        else if (isOnBar)
        {
            currentX = Mathf.LerpAngle(currentX, target.eulerAngles.y, Time.deltaTime * barTransitionSpeed);
            currentY = Mathf.Lerp(currentY, barPitch, Time.deltaTime * barTransitionSpeed);
        }
        else if (isSliding)
        {
            // Trava o ângulo vertical (Pitch) durante o slide
            currentY = Mathf.Lerp(currentY, slidePitch, Time.deltaTime * slideTransitionSpeed);
        }
        else if (isGroundSliding)
        {
            // Trava o ângulo vertical (Pitch) durante o ground slide
            currentY = Mathf.Lerp(currentY, groundSlidePitch, Time.deltaTime * groundSlideTransitionSpeed);
        }
        else if (isInDialogue)
        {
            // ✅ NOVO: Durante o diálogo, a câmera se alinha atrás do jogador
            // com o pitch configurado e bloqueia o input manual (igual ao QTE)
            float targetYaw = target.eulerAngles.y;
            currentX = Mathf.LerpAngle(currentX, targetYaw, Time.deltaTime * dialogueTransitionSpeed * 2f);
            currentY = Mathf.Lerp(currentY, dialoguePitch, Time.deltaTime * dialogueTransitionSpeed * 2f);
        }
        else if (isSitting)
        {
            float targetYaw = target.eulerAngles.y;

            if (isTransitioningToSit)
            {
                // Durante a transição inicial, força a câmera para a posição ideal e ignora input
                currentX = Mathf.LerpAngle(currentX, targetYaw, Time.deltaTime * sitTransitionSpeed * 2f);
                currentY = Mathf.Lerp(currentY, sitPitch, Time.deltaTime * sitTransitionSpeed * 2f);

                // Se estiver perto o suficiente do alvo, libera o controle para o jogador
                if (Mathf.Abs(Mathf.DeltaAngle(currentX, targetYaw)) < 1f && Mathf.Abs(currentY - sitPitch) < 1f)
                {
                    isTransitioningToSit = false;
                    lastSitInputTime = Time.time; // Reseta o timer de inatividade
                }
            }
            else
            {
                bool hasMouseInput = Mathf.Abs(Input.GetAxis("Mouse X")) > 0.1f || Mathf.Abs(Input.GetAxis("Mouse Y")) > 0.1f;
                if (hasMouseInput)
                {
                    lastSitInputTime = Time.time;
                    
                    // Rotação manual com limites específicos de sentado
                    currentX += Input.GetAxis("Mouse X") * rotationSpeed;
                    currentY -= Input.GetAxis("Mouse Y") * rotationSpeed;

                    // Limita o Yaw (Horizontal) relativo à frente do banco
                    float diffX = Mathf.DeltaAngle(targetYaw, currentX);
                    diffX = Mathf.Clamp(diffX, -sitMaxYawAngle, sitMaxYawAngle);
                    currentX = targetYaw + diffX;

                    // Limita o Pitch (Vertical)
                    currentY = Mathf.Clamp(currentY, sitMinMaxPitch.x, sitMinMaxPitch.y);
                }
                else if (Time.time - lastSitInputTime > sitAutoCenterDelay)
                {
                    // Auto-centraliza suavemente após o delay de inatividade
                    currentX = Mathf.LerpAngle(currentX, targetYaw, Time.deltaTime * sitAutoCenterSpeed);
                    currentY = Mathf.Lerp(currentY, sitPitch, Time.deltaTime * sitAutoCenterSpeed);
                }
            }
        }
        else
        {
            // Se estiver em QTE, bloqueia o input manual do jogador
            // NÃO usa return aqui! A câmera PRECISA continuar calculando a posição
            bool qteBlockMouse = isQTELocked && !isQTECentered;

            bool hasMouseInput = Mathf.Abs(Input.GetAxis("Mouse X")) > 0.1f || Mathf.Abs(Input.GetAxis("Mouse Y")) > 0.1f;
            float rotSpeed = autoCenterSpeed;

            // Durante o QTE, ignora o mouse completamente (mesmo que o jogador mexa)
            if (qteBlockMouse)
            {
                hasMouseInput = false;
            }

            if (!hasMouseInput && !isGrinding && !isSliding && !isWallRunning && !isOnBar && !isSitting && !IsPlayerInDialogue())
            {
                // Se estiver em QTE, usa unscaledDeltaTime para não ser afetado pelo slow motion
                float smoothFactor = qteBlockMouse ? Time.unscaledDeltaTime : Time.deltaTime;
                currentX = Mathf.LerpAngle(currentX, target.eulerAngles.y, smoothFactor * rotSpeed);
            }
            else if (isWallRunning && wallRunAutoCenterStrength > 0f)
            {
                float strength = hasMouseInput ? wallRunAutoCenterStrength * 0.3f : wallRunAutoCenterStrength;
                float smoothFactor = qteBlockMouse ? Time.unscaledDeltaTime : Time.deltaTime;
                currentX = Mathf.LerpAngle(currentX, target.eulerAngles.y, smoothFactor * autoCenterSpeed * strength);
                currentY = Mathf.Lerp(currentY, 10f, smoothFactor * autoCenterSpeed * strength);
            }
            else if (isGrinding && grindAutoCenterStrength > 0f)
            {
                float smoothFactor = qteBlockMouse ? Time.unscaledDeltaTime : Time.deltaTime;
                currentX = Mathf.LerpAngle(currentX, target.eulerAngles.y, smoothFactor * autoCenterSpeed * grindAutoCenterStrength);
                currentY = Mathf.Lerp(currentY, 10f, smoothFactor * autoCenterSpeed * grindAutoCenterStrength);
            }
            else if (isSliding || isGroundSliding)
            {
                 float smoothFactor = qteBlockMouse ? Time.unscaledDeltaTime : Time.deltaTime;
                 currentX = Mathf.LerpAngle(currentX, target.eulerAngles.y, smoothFactor * autoCenterSpeed * 0.5f);
                 currentY = Mathf.Lerp(currentY, 15f, smoothFactor * autoCenterSpeed * 0.5f);
            }
        }

        float targetWallYaw = isWallRunning ? (IsPlayerOnLeftWall() ? wallRunYawOffset : -wallRunYawOffset) : 0f;
        float targetWallSideOffset = isWallRunning ? (IsPlayerOnLeftWall() ? -wallRunSideOffset : wallRunSideOffset) : 0f;
        float targetGrindYaw = isGrinding ? grindYawOffset : 0f;
        float targetGrindSideOffset = isGrinding ? grindSideOffset : 0f;
        float targetNarrowSideOffset = isInNarrow ? narrowSideOffset : 0f;
        float targetBarSideOffset = isOnBar ? barEntrySideOffset : 0f;
        float targetSlideSideOffset = isSliding ? slideSideOffset : (isGroundSliding ? groundSlideSideOffset : 0f);
        float targetDialogueSideOffset = isInDialogue ? dialogueSideOffset : 0f;
        currentDialogueSideOffset = Mathf.Lerp(currentDialogueSideOffset, targetDialogueSideOffset, Time.deltaTime * dialogueTransitionSpeed);
        
        float targetTilt = 0f;
        if (isWallRunning) targetTilt = IsPlayerOnLeftWall() ? wallRunTiltAmount : -wallRunTiltAmount;
        else if (isGrinding) targetTilt = target.right.y * grindTiltAmount;
        else if (isSliding) targetTilt = slideTiltAngle;
        else if (isGroundSliding) targetTilt = groundSlideTiltAngle;
        else if (isSwinging)
        {
            // ✅ Efeito Spider-Man: Inclina baseado no input lateral ou movimento relativo
            float horizontalInput = Input.GetAxis("Horizontal");
            targetTilt = -horizontalInput * swingTiltAmount;
        }
        
        currentWallRunYaw = Mathf.Lerp(currentWallRunYaw, targetWallYaw, Time.deltaTime * (isWallRunning ? wallRunYawSpeed : 2.5f));
        currentWallRunSideOffset = Mathf.Lerp(currentWallRunSideOffset, targetWallSideOffset, Time.deltaTime * wallRunSideOffsetSpeed);
        
        currentGrindYaw = Mathf.Lerp(currentGrindYaw, targetGrindYaw, Time.deltaTime * grindYawSpeed);
        currentGrindSideOffset = Mathf.Lerp(currentGrindSideOffset, targetGrindSideOffset, Time.deltaTime * grindSideOffsetSpeed);
        
        currentNarrowSideOffset = Mathf.Lerp(currentNarrowSideOffset, targetNarrowSideOffset, Time.deltaTime * tSpeed);
        currentBarSideOffset = Mathf.Lerp(currentBarSideOffset, targetBarSideOffset, Time.deltaTime * tSpeed);
        currentSlideSideOffset = Mathf.Lerp(currentSlideSideOffset, targetSlideSideOffset, Time.deltaTime * tSpeed);
        
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * (isWallRunning ? wallRunTiltSpeed : (isSliding ? slideTiltSpeed : (isGroundSliding ? groundSlideTiltSpeed : (isSwinging ? swingTiltSpeed : 5f)))));

        // Aplica o alinhamento por último para prevalecer sobre a rotação
        // manual e sobre os demais modos de câmera durante o impacto.
        if (damageAlignmentTimer > 0f)
        {
            damageAlignmentTimer -= Time.unscaledDeltaTime;
            currentX = Mathf.LerpAngle(
                currentX,
                target.eulerAngles.y,
                1f - Mathf.Exp(-damageAlignmentSpeed * Time.unscaledDeltaTime)
            );
        }
        
        UpdateDamageImpact();

        Quaternion finalRotation = Quaternion.Euler(currentY, currentX, 0);
        finalRotation *= Quaternion.Euler(0, currentWallRunYaw + currentGrindYaw, 0);
        finalRotation *= Quaternion.Euler(0, 0, currentTilt);
        finalRotation *= Quaternion.Euler(damagePitchOffset, 0f, 0f);

        Vector3 rayOrigin = target.position + Vector3.up * collisionRaycastOffset;
        
        Vector3 sideDirection = finalRotation * Vector3.right;
        Vector3 combinedSideOffset = sideDirection * (currentWallRunSideOffset + currentGrindSideOffset + currentNarrowSideOffset + currentBarSideOffset + currentSlideSideOffset + currentDialogueSideOffset);
        rayOrigin += combinedSideOffset;

        Vector3 direction = finalRotation * (Vector3.back * finalIdealDistance + Vector3.up * (smoothNarrowHeight - collisionRaycastOffset));
        Vector3 normalizedDir = direction.normalized;
        float maxRayDist = direction.magnitude;

        RaycastHit hit;
        float targetCollisionDist = finalIdealDistance;
        bool shouldIgnoreNarrow = isInNarrow || narrowExitTimer > 0;
        LayerMask finalCollisionLayers = shouldIgnoreNarrow ? (collisionLayers & ~(1 << narrowLayerIndex)) : collisionLayers;
        if (Physics.SphereCast(rayOrigin, cameraRadius, normalizedDir, out hit, maxRayDist, finalCollisionLayers))
            targetCollisionDist = Mathf.Max(0.5f, hit.distance - collisionPadding);

        collisionDistance = Mathf.Lerp(collisionDistance, targetCollisionDist, Time.deltaTime * collisionResponseSpeed);
        
        // ✅ QTE: Verifica se a câmera já se centralizou atrás do jogador
        CheckQTECentering();

        Vector3 desiredPosition = rayOrigin + normalizedDir * collisionDistance;
        desiredPosition += finalRotation * (Vector3.back * damageRecoilOffset);
        desiredPosition += Vector3.up * damageHeightOffsetCurrent;
        if (IsPlayerBoosting()) desiredPosition += Random.insideUnitSphere * boostShakeAmount;
        desiredPosition += shakeOffset;

        if (!isBeingTransitioned)
        {
            if (isCatchingUp)
            {
                // Sistema Cinematográfico: Interpola entre a posição onde parou e a posição ideal do jogador
                // usando a AnimationCurve para dar a sensação de aceleração/impulso.
                float curveValue = catchUpCurve.Evaluate(catchUpProgress);
                transform.position = Vector3.Lerp(startCatchUpPos, desiredPosition, curveValue);
                transform.rotation = Quaternion.Slerp(startCatchUpRot, finalRotation, curveValue);
            }
            else
            {
                // Movimento normal de acompanhamento
                transform.position = Vector3.Lerp(transform.position, desiredPosition, positionSmoothSpeed * (Time.deltaTime * 60f));
                transform.rotation = Quaternion.Slerp(transform.rotation, finalRotation, positionSmoothSpeed * (Time.deltaTime * 60f));
            }
        }
    }

        /// <summary>
    /// Entra na câmera cinematográfica de morte.
    /// O controle manual é ignorado até ExitDeathCamera ser chamado.
    /// </summary>
    public void EnterDeathCamera()
    {
        if (target == null)
            return;

        isDeathCameraActive = true;
        deathCameraVelocity = Vector3.zero;

        if (cam != null && !deathCameraOriginalFOVCaptured)
        {
            deathCameraOriginalFOV = cam.fieldOfView;
            deathCameraOriginalFOVCaptured = true;
        }
    }

    /// <summary>
    /// Sai da câmera de morte e devolve o controle ao sistema normal.
    /// </summary>
    public void ExitDeathCamera()
    {
        isDeathCameraActive = false;
        deathCameraVelocity = Vector3.zero;

        if (target != null)
        {
            currentX = transform.eulerAngles.y;
            currentY = transform.eulerAngles.x;
            currentDistance = Mathf.Clamp(Vector3.Distance(transform.position, target.position), minDistance, maxDistance);
            collisionDistance = currentDistance;
        }

        deathCameraOriginalFOVCaptured = false;
    }

    public bool IsDeathCameraActive => isDeathCameraActive;

    private void UpdateDeathCamera()
    {
        if (target == null)
            return;

        float dt = deathCameraUsesUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        dt = Mathf.Max(0.0001f, dt);

        Vector3 targetForward = target.forward;
        targetForward.y = 0f;
        if (targetForward.sqrMagnitude < 0.001f)
            targetForward = transform.forward;
        targetForward.Normalize();

        Vector3 targetRight = Vector3.Cross(Vector3.up, targetForward).normalized;
        Vector3 desiredPosition = target.position
            + Vector3.up * deathCameraHeight
            - targetForward * deathCameraDistance
            + targetRight * deathCameraSideOffset;

        // SmoothDamp produz uma subida e afastamento contínuos, sem trocar de câmera abruptamente.
        float smoothTime = 1f / Mathf.Max(0.01f, deathCameraPositionSpeed);
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref deathCameraVelocity,
            smoothTime,
            Mathf.Infinity,
            dt
        );

        Vector3 lookTarget = target.position + Vector3.up * deathCameraLookAtHeight;
        Vector3 lookDirection = lookTarget - transform.position;
        if (lookDirection.sqrMagnitude > 0.001f)
        {
            Quaternion desiredRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
            float rotationT = 1f - Mathf.Exp(-deathCameraRotationSpeed * dt);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationT);
        }

        if (cam != null)
        {
            float fovT = 1f - Mathf.Exp(-deathCameraRotationSpeed * dt);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, deathCameraFOV, fovT);
        }
    }

    /// <summary>
    /// Ativa o efeito de congelamento da câmera para o Spin Dash.
    /// </summary>

    public void TriggerSpinDashFreeze()
    {
        spinDashTimer = spinDashFreezeDuration;
        isSpinDashFrozen = true;
        isCatchingUp = false;
        // A posição inicial do catch-up será capturada quando o freeze terminar
    }

    public void TriggerWallDashShake()
    {
        if (!enableWallDashShake) return;
        shakeTimer = 0f;
        shakeDuration = wallDashShakeDuration;
        shakeAmount = wallDashShakeAmount;
        shakeFrequency = wallDashShakeFrequency;
    }

    /// <summary>
    /// Dispara um shake específico para o recebimento de dano.
    /// </summary>
    private float damageRecoilOffset = 0f;
    private float damagePitchOffset = 0f;
    private float damagePitchVelocity = 0f;
    private float damageHeightOffsetCurrent = 0f;
    private float damageHeightTransitionInCurrent;
    private float damageHeightHoldDurationCurrent;
    private float damageHeightTransitionOutCurrent;
    private float damageHeightOffsetTargetCurrent;
    private float damageImpactDurationCurrent;
    private float damageRecoilDistanceCurrent;
    private float damagePitchAmountCurrent;
    private float damagePitchTransitionSpeedCurrent;
    private float damageAlignmentTimer;

    public void TriggerDamageShake(bool isAirHit = false)
    {
        if (alignBehindOnDamage && target != null)
            damageAlignmentTimer = damageAlignmentDuration;

        if (isAirHit)
        {
            damageRecoilDistanceCurrent = airDamageRecoilDistance;
            damagePitchAmountCurrent = airDamagePitchAmount;
            damagePitchTransitionSpeedCurrent = airDamagePitchTransitionSpeed;
            damageHeightOffsetTargetCurrent = airDamageHeightOffset;
            damageHeightTransitionInCurrent = airDamageHeightTransitionIn;
            damageHeightHoldDurationCurrent = airDamageHeightHoldDuration;
            damageHeightTransitionOutCurrent = airDamageHeightTransitionOut;
            damageImpactDurationCurrent = airDamageImpactDuration;
        }
        else
        {
            damageRecoilDistanceCurrent = damageRecoilDistance;
            damagePitchAmountCurrent = damagePitchAmount;
            damagePitchTransitionSpeedCurrent = damagePitchTransitionSpeed;
            damageHeightOffsetTargetCurrent = damageHeightOffset;
            damageHeightTransitionInCurrent = damageHeightTransitionIn;
            damageHeightHoldDurationCurrent = damageHeightHoldDuration;
            damageHeightTransitionOutCurrent = damageHeightTransitionOut;
            damageImpactDurationCurrent = damageImpactDuration;
        }

        if (enableDamageShake && damageShakeDuration > 0f && damageShakeAmount > 0f)
        {
            shakeTimer = 0f;
            shakeDuration = damageShakeDuration;
            shakeAmount = damageShakeAmount;
            shakeFrequency = damageShakeFrequency;
        }

        damageImpactTimer = 0f;
        damageHeightTimer = 0f;
        damagePitchTimer = 0f;
        damagePitchVelocity = 0f;
    }

    private void UpdateDamageImpact()
    {
        if (damageImpactTimer < damageImpactDurationCurrent)
        {
            damageImpactTimer += Time.deltaTime;
            float progress = Mathf.Clamp01(damageImpactTimer / Mathf.Max(0.0001f, damageImpactDurationCurrent));
            float envelope = 1f - Mathf.SmoothStep(0f, 1f, progress);

            damageRecoilOffset = damageRecoilDistanceCurrent * envelope;
        }
        else
        {
            damageRecoilOffset = 0f;
        }

        // O pitch usa uma única velocidade e mantém uma curva contínua
        // quando troca do alvo do impacto para o alvo neutro.
        float pitchSpeed = Mathf.Max(0.01f, damagePitchTransitionSpeedCurrent);
        float targetPitch = damagePitchTimer < damageImpactDurationCurrent
            ? damagePitchAmountCurrent
            : 0f;
        float pitchSmoothTime = 1f / pitchSpeed;

        damagePitchOffset = Mathf.SmoothDamp(
            damagePitchOffset,
            targetPitch,
            ref damagePitchVelocity,
            pitchSmoothTime,
            Mathf.Infinity,
            Time.deltaTime
        );

        if (Mathf.Abs(damagePitchOffset) < 0.001f && targetPitch == 0f)
        {
            damagePitchOffset = 0f;
            damagePitchVelocity = 0f;
            damagePitchTimer = float.PositiveInfinity;
        }
        else
        {
            damagePitchTimer += Time.deltaTime;
        }

        // A altura possui sua própria curva para não saltar instantaneamente.
        float heightIn = Mathf.Max(0.001f, damageHeightTransitionInCurrent);
        float heightHold = Mathf.Max(0f, damageHeightHoldDurationCurrent);
        float heightOut = Mathf.Max(0.001f, damageHeightTransitionOutCurrent);
        float heightTotal = heightIn + heightHold + heightOut;

        if (damageHeightTimer < heightTotal)
        {
            damageHeightTimer += Time.deltaTime;

            if (damageHeightTimer <= heightIn)
            {
                float t = Mathf.Clamp01(damageHeightTimer / heightIn);
                damageHeightOffsetCurrent = damageHeightOffsetTargetCurrent * Mathf.SmoothStep(0f, 1f, t);
            }
            else if (damageHeightTimer <= heightIn + heightHold)
            {
                damageHeightOffsetCurrent = damageHeightOffsetTargetCurrent;
            }
            else
            {
                float t = Mathf.Clamp01((damageHeightTimer - heightIn - heightHold) / heightOut);
                damageHeightOffsetCurrent = damageHeightOffsetTargetCurrent * (1f - Mathf.SmoothStep(0f, 1f, t));
            }
        }
        else
        {
            damageHeightOffsetCurrent = 0f;
        }
    }

    private void UpdateShake()
    {
        if (shakeTimer < shakeDuration)
        {
            shakeTimer += Time.deltaTime;
            float progress = shakeTimer / shakeDuration;
            float intensity = Mathf.Lerp(shakeAmount, 0f, progress);
            float shakeX = (Mathf.PerlinNoise(Time.time * shakeFrequency, 0f) * 2f - 1f) * intensity;
            float shakeY = (Mathf.PerlinNoise(0f, Time.time * shakeFrequency) * 2f - 1f) * intensity;
            shakeOffset = new Vector3(shakeX, shakeY, 0f);
        }
        else
        {
            shakeOffset = Vector3.zero;
        }
    }

    private void HandleZoomInput()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            currentDistance -= scrollInput * zoomSpeed;
            currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
        }
    }

    private void HandleRotationInput(float speed)
    {
        if (isQTELocked) return;

        // ✅ NOVO: Durante o diálogo, bloqueia o input manual do mouse (igual ao QTE)
        if (NPCDialogueManager.Instance != null && NPCDialogueManager.Instance.IsDialogueActive)
            return;

        currentX += Input.GetAxis("Mouse X") * speed;

        // Se estiver no slide, ignora o input vertical do mouse para travar o ângulo
        if (IsPlayerSliding())
        {
            // Não aplica o Input.GetAxis("Mouse Y")
            // O valor de currentY será suavizado para slidePitch no LateUpdate
        }
        else
        {
            currentY -= Input.GetAxis("Mouse Y") * speed;
            currentY = Mathf.Clamp(currentY, minYAngle, maxYAngle);
        }
    }

    public void SetQTECameraLock(bool locked)
    {
        isQTELocked = locked;
    }

    // Função pública para bloquear input durante o QTE (chamada pelo QTEHandler)
    public void EnterQTENow()
    {
        isQTELocked = true;
        isQTECentered = false; // Reseta a flag para forçar a centralização
        
        // Força o FOV para o valor do QTE
        if (cam != null)
        {
            cam.fieldOfView = qteFOV;
        }
    }

    public void ExitQTENow()
    {
        isQTELocked = false;
        isQTECentered = false;
    }

    /// <summary>
    /// Verifica se a câmera já está centralizada atrás do jogador durante o QTE
    /// Igual ao sistema de Sitting que verifica quando está perto o suficiente
    /// </summary>
    private void CheckQTECentering()
    {
        if (!isQTELocked) return;
        
        float targetYaw = target.eulerAngles.y;
        float diffX = Mathf.Abs(Mathf.DeltaAngle(currentX, targetYaw));
        float diffY = Mathf.Abs(currentY - qtePitch);
        
        if (diffX < 5f && diffY < 5f)
        {
            isQTECentered = true;
        }
    }

    private bool IsPlayerGrinding()
    {
        if (railRideSpline != null) return railRideSpline.isGrinding;
        if (railRide != null) return railRide.isGrinding;
        return false;
    }

    private bool IsPlayerInNarrow()
    {
        return playerMovement != null && playerMovement.IsInNarrowPassage;
    }

    private bool IsPlayerWallRunning()
    {
        return playerMovement != null && playerMovement.IsWallRunning;
    }

    private bool IsPlayerOnBar()
    {
        return playerMovement != null && playerMovement.IsGrabbingBar;
    }

    private bool IsPlayerOnLeftWall()
    {
        return playerMovement != null && playerMovement.OnLeftWall;
    }

    private bool IsPlayerBoosting()
    {
        return false;
    }

    private bool IsPlayerWarping()
    {
        return warpSystem != null && warpSystem.IsWarping;
    }

    private bool IsPlayerSliding()
    {
        return slopeSlideSystem != null && slopeSlideSystem.IsSliding();
    }

    private bool IsPlayerGroundSliding()
    {
        return playerMovement != null && playerMovement.IsGroundSliding;
    }

    private bool IsPlayerGliding()
    {
        return playerMovement != null && playerMovement.IsGliding;
    }

    private bool IsPlayerSwinging()
    {
        return playerMovement != null && playerMovement.isSwinging;
    }

    private bool IsPlayerSitting()
    {
        return playerMovement != null && playerMovement.IsSitting;
    }

    // ✅ NOVO: helper para detectar diálogo (o IsInDialogue do PlayerMovement é privado,
    // então usamos o NPCDialogueManager diretamente — igual ao bloqueio no PlayerInteractor)
    private bool IsPlayerInDialogue()
    {
        return NPCDialogueManager.Instance != null && NPCDialogueManager.Instance.IsDialogueActive;
    }

    private float GetPlayerSpeed()
    {
        return playerMovement != null ? playerMovement.currentSpeed : 0f;
    }

    /// <summary>
    /// Chamado quando o jogador executa um Quick Turn.
    /// Inicia um impulso de rotação de 180 graus.
    /// </summary>
    public void OnQuickTurn()
    {
        currentQuickTurnAdd = 0f;
        quickTurnTargetAdd = 180f;
    }
}
