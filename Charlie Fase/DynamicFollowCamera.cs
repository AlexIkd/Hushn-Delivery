using UnityEngine;

public class DynamicFollowCamera : MonoBehaviour
{
    [Tooltip("O Transform do jogador que a câmera deve seguir e orbitar.")]
    public Transform target;

    [Header("Configurações de Posição Normal")]
    public float baseDistance = 10.0f;
    public float height = 5.0f;
    public float positionSmoothSpeed = 0.125f;

    [Header("Configurações de Rotação Manual")]
    public float rotationSpeed = 5.0f;
    public float autoCenterSpeed = 1.0f;
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
    public float collisionResponseSpeed = 20f;
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

    [Header("Configurações de FOV por Velocidade")]
    public float speedThreshold = 15f;
    public float speedFOV = 65f;

    [Header("Configurações de Camera Shake")]
    [SerializeField] private float wallDashShakeAmount = 0.15f;
    [SerializeField] private float wallDashShakeDuration = 0.3f;
    [SerializeField] private float wallDashShakeFrequency = 25f;
    [SerializeField] private bool enableWallDashShake = true;

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
    private float currentWallRunSideOffset = 0f;
    private float currentGrindSideOffset = 0f;
    private float currentGrindYaw = 0f;
    private float currentNarrowSideOffset = 0f;
    private float currentBarSideOffset = 0f;
    private float currentSlideSideOffset = 0f;
    private float currentWallRunDistanceMultiplier = 1.0f;
    private bool wasTransitioning = false;

    private int narrowLayerIndex;
    private float narrowExitTimer = 0f;

    private float smoothNarrowHeight;
    private float smoothNarrowDistance;

    private float shakeTimer = 0f;
    private float shakeDuration = 0f;
    private float shakeAmount = 0f;
    private float shakeFrequency = 0f;
    private Vector3 shakeOffset = Vector3.zero;

    private PlayerMovement_FrontiersStyle playerMovement;
    private PlayerRailRide_SonicStyle_Spline railRideSpline;
    private PlayerRailRide_SonicStyle_Spline railRide;
    private WarpSystem warpSystem;
    private SlopeSlideSystem slopeSlideSystem;
    private CameraRailManager cameraRailManager;
    private HorizontalBarHandler horizontalBarHandler; 

    private bool wasOnBar = false;
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
        
        if (!isInNarrow && !isOnBar)
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
        
        if (isInNarrow) targetFOV = narrowFOV;
        else if (isOnBar) targetFOV = barFOV;
        else if (isWarping) targetFOV = warpFOV;
        else if (isBoosting) targetFOV = boostFOV;
        else if (isGrinding) targetFOV = grindFOV;
        else if (isWallRunning) targetFOV = wallRunFOV;
        else if (isSliding) { targetFOV = slideFOV; targetDistortion = slideLensDistortion; }
        else if (isGroundSliding) { targetFOV = groundSlideFOV; targetDistortion = slideLensDistortion; }
        else if (isMovingFast) targetFOV = speedFOV;

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
    }

    void LateUpdate()
    {
        if (target == null) return;

        bool isBeingTransitioned = cameraRailManager != null && cameraRailManager.IsThisCameraTransitioning(transform);

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
        
        if (isInNarrow)
        {
            narrowExitTimer = narrowCollisionReturnDelay;
        }
        else if (narrowExitTimer > 0)
        {
            narrowExitTimer -= Time.deltaTime;
        }

        float tSpeed = isInNarrow ? narrowEnterSpeed : (isOnBar ? barTransitionSpeed : (isSliding ? slideTransitionSpeed : (isGroundSliding ? groundSlideTransitionSpeed : narrowExitSpeed)));

        float targetBaseHeight = isInNarrow ? narrowHeight : (isOnBar ? barHeight : (isSliding ? slideHeight : (isGroundSliding ? groundSlideHeight : (isGrinding ? grindHeight : height))));
        float targetBaseDistance = isInNarrow ? narrowDistance : (isOnBar ? barDistance : (isSliding ? slideDistance : (isGroundSliding ? groundSlideDistance : (isGrinding ? grindDistance : currentDistance))));
        
        smoothNarrowHeight = Mathf.Lerp(smoothNarrowHeight, targetBaseHeight, Time.deltaTime * tSpeed);
        smoothNarrowDistance = Mathf.Lerp(smoothNarrowDistance, targetBaseDistance, Time.deltaTime * tSpeed);
        
        float targetDistMultiplier = isWallRunning ? wallRunDistanceMultiplier : 1.0f;
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
        else
        {
            bool hasMouseInput = Mathf.Abs(Input.GetAxis("Mouse X")) > 0.1f || Mathf.Abs(Input.GetAxis("Mouse Y")) > 0.1f;
            float rotSpeed = autoCenterSpeed;

            if (!hasMouseInput && !isGrinding && !isSliding && !isWallRunning && !isOnBar)
                currentX = Mathf.LerpAngle(currentX, target.eulerAngles.y, Time.deltaTime * rotSpeed);
            else if (isWallRunning && wallRunAutoCenterStrength > 0f)
            {
                float strength = hasMouseInput ? wallRunAutoCenterStrength * 0.3f : wallRunAutoCenterStrength;
                currentX = Mathf.LerpAngle(currentX, target.eulerAngles.y, Time.deltaTime * autoCenterSpeed * strength);
                currentY = Mathf.Lerp(currentY, 10f, Time.deltaTime * autoCenterSpeed * strength);
            }
            else if (isGrinding && grindAutoCenterStrength > 0f)
            {
                currentX = Mathf.LerpAngle(currentX, target.eulerAngles.y, Time.deltaTime * autoCenterSpeed * grindAutoCenterStrength);
                currentY = Mathf.Lerp(currentY, 10f, Time.deltaTime * autoCenterSpeed * grindAutoCenterStrength);
            }
        else if (isSliding || isGroundSliding)
        {
             currentX = Mathf.LerpAngle(currentX, target.eulerAngles.y, Time.deltaTime * autoCenterSpeed * 0.5f);
             currentY = Mathf.Lerp(currentY, 15f, Time.deltaTime * autoCenterSpeed * 0.5f);
        }
        }

        float targetWallYaw = isWallRunning ? (IsPlayerOnLeftWall() ? wallRunYawOffset : -wallRunYawOffset) : 0f;
        float targetWallSideOffset = isWallRunning ? (IsPlayerOnLeftWall() ? -wallRunSideOffset : wallRunSideOffset) : 0f;
        float targetGrindYaw = isGrinding ? grindYawOffset : 0f;
        float targetGrindSideOffset = isGrinding ? grindSideOffset : 0f;
        float targetNarrowSideOffset = isInNarrow ? narrowSideOffset : 0f;
        float targetBarSideOffset = isOnBar ? barEntrySideOffset : 0f;
        float targetSlideSideOffset = isSliding ? slideSideOffset : (isGroundSliding ? groundSlideSideOffset : 0f);
        
        float targetTilt = 0f;
        if (isWallRunning) targetTilt = IsPlayerOnLeftWall() ? wallRunTiltAmount : -wallRunTiltAmount;
        else if (isGrinding) targetTilt = target.right.y * grindTiltAmount;
        else if (isSliding) targetTilt = slideTiltAngle;
        else if (isGroundSliding) targetTilt = groundSlideTiltAngle;
        
        currentWallRunYaw = Mathf.Lerp(currentWallRunYaw, targetWallYaw, Time.deltaTime * (isWallRunning ? wallRunYawSpeed : 2.5f));
        currentWallRunSideOffset = Mathf.Lerp(currentWallRunSideOffset, targetWallSideOffset, Time.deltaTime * wallRunSideOffsetSpeed);
        
        currentGrindYaw = Mathf.Lerp(currentGrindYaw, targetGrindYaw, Time.deltaTime * grindYawSpeed);
        currentGrindSideOffset = Mathf.Lerp(currentGrindSideOffset, targetGrindSideOffset, Time.deltaTime * grindSideOffsetSpeed);
        
        currentNarrowSideOffset = Mathf.Lerp(currentNarrowSideOffset, targetNarrowSideOffset, Time.deltaTime * tSpeed);
        currentBarSideOffset = Mathf.Lerp(currentBarSideOffset, targetBarSideOffset, Time.deltaTime * tSpeed);
        currentSlideSideOffset = Mathf.Lerp(currentSlideSideOffset, targetSlideSideOffset, Time.deltaTime * tSpeed);
        
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * (isWallRunning ? wallRunTiltSpeed : (isSliding ? slideTiltSpeed : (isGroundSliding ? groundSlideTiltSpeed : 5f))));

        Quaternion finalRotation = Quaternion.Euler(currentY, currentX, 0);
        finalRotation *= Quaternion.Euler(0, currentWallRunYaw + currentGrindYaw, 0);
        finalRotation *= Quaternion.Euler(0, 0, currentTilt);

        Vector3 rayOrigin = target.position + Vector3.up * collisionRaycastOffset;
        
        Vector3 sideDirection = finalRotation * Vector3.right;
        Vector3 combinedSideOffset = sideDirection * (currentWallRunSideOffset + currentGrindSideOffset + currentNarrowSideOffset + currentBarSideOffset + currentSlideSideOffset);
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

        Vector3 desiredPosition = rayOrigin + normalizedDir * collisionDistance;
        if (IsPlayerBoosting()) desiredPosition += Random.insideUnitSphere * boostShakeAmount;
        desiredPosition += shakeOffset;

        if (!isBeingTransitioned)
        {
            transform.position = Vector3.Lerp(transform.position, desiredPosition, positionSmoothSpeed * (Time.deltaTime * 60f));
            transform.rotation = Quaternion.Slerp(transform.rotation, finalRotation, positionSmoothSpeed * (Time.deltaTime * 60f));
        }
    }

    public void TriggerWallDashShake()
    {
        if (!enableWallDashShake) return;
        shakeTimer = 0f;
        shakeDuration = wallDashShakeDuration;
        shakeAmount = wallDashShakeAmount;
        shakeFrequency = wallDashShakeFrequency;
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
