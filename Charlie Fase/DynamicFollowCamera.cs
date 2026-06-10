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
    [Tooltip("O raio da esfera de colisão da câmera. Evita que a câmera atravesse quinas.")]
    public float cameraRadius = 0.4f;
    [Tooltip("Um pequeno recuo extra para evitar que a câmera fique colada na superfície.")]
    public float collisionPadding = 0.15f;
    [Tooltip("Velocidade de resposta quando a câmera entra em colisão (deve ser rápida).")]
    public float collisionResponseSpeed = 20f;
    [Tooltip("Offset vertical para o ponto de origem do raio (geralmente a cabeça do jogador).")]
    public float collisionRaycastOffset = 1.2f;

    [Header("Configurações Durante Wall Run (Estilo Jedi Survivor)")]
    public float wallRunTiltAmount = 15f;
    public float wallRunTiltSpeed = 6f;
    public float wallRunYawOffset = 15f; 
    public float wallRunYawSpeed = 4f;
    public float wallRunFOV = 75f;
    [Tooltip("Distância extra para trás durante o Wall Run")]
    public float wallRunDistanceMultiplier = 1.2f;
    [Tooltip("Deslocamento lateral da câmera para 'abrir' a visão da pista")]
    public float wallRunSideOffset = 2.5f;
    [Tooltip("Velocidade de transição para o Side Offset ao entrar/sair do Wall Run")]
    public float wallRunSideOffsetSpeed = 3.0f;
    [Tooltip("Velocidade de transição para o multiplicador de distância ao entrar/sair do Wall Run")]
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
    [Tooltip("Offset lateral da câmera durante o Grind (Estilo Sonic Frontiers)")]
    public float grindSideOffset = 2.0f;
    [Tooltip("Velocidade de transição para o Side Offset do Grind")]
    public float grindSideOffsetSpeed = 3.0f;
    [Tooltip("Ângulo horizontal extra (Yaw) durante o Grind para visão lateral")]
    public float grindYawOffset = 10f;
    [Tooltip("Velocidade de transição para o Yaw Offset do Grind")]
    public float grindYawSpeed = 3.0f;

    [Header("Configurações Durante Warp")]
    public float warpFOV = 85f;

    // NOVO: Configuração simples de FOV por velocidade
    [Header("Configurações de FOV por Velocidade")]
    [Tooltip("Velocidade mínima para ativar FOV aumentado")]
    public float speedThreshold = 15f;
    [Tooltip("FOV quando velocidade >= speedThreshold")]
    public float speedFOV = 65f;

    // ✅ NOVO: Configurações de Camera Shake
    [Header("Configurações de Camera Shake")]
    [SerializeField] private float wallDashShakeAmount = 0.15f;
    [SerializeField] private float wallDashShakeDuration = 0.3f;
    [SerializeField] private float wallDashShakeFrequency = 25f;
    [SerializeField] private bool enableWallDashShake = true;

    // ✅ NOVO: Configurações de Slide Tilt
    [Header("Configurações Durante Slide")]
    public float slideFOV = 75f; // NOVO: FOV customizável para o slide
    public float slideTiltAngle = 15.0f; // Ângulo de inclinação da câmera durante o slide
    public float slideTiltSpeed = 7.0f; // Velocidade de inclinação da câmera durante o slide
    public float slideLensDistortion = -0.3f; // NOVO: Intensidade do efeito Olho de Gato
    public float distortionTransitionSpeed = 5f; // NOVO: Velocidade de transição da distorção

    [Header("Configurações Durante Slide (Motion Blur)")]
    public float slideMotionBlurIntensity = 0.5f; // Intensidade do Motion Blur durante o slide
    public float motionBlurTransitionSpeed = 7.0f; // Velocidade de transição do Motion Blur

    [Header("Configurações de Pós-Processamento Adicionais")]
    public float slideChromaticAberration = 0.2f; // Intensidade da Aberração Cromática no slide
    public float slideVignetteIntensity = 0.3f; // Intensidade da Vinheta no slide
    public float ppTransitionSpeed = 5.0f; // Velocidade de transição para estes efeitos

    private float currentDistance;
    private float currentX = 0.0f;
    private float currentY = 0.0f;
    private float currentTilt = 0f;
    private float currentWallRunYaw = 0f; 
    private float collisionDistance;
    private Camera cam;
    private float currentFOV;
    private float currentDistortion = 0f; // NOVO: Valor atual da distorção
    private float currentMotionBlur = 0f; // NOVO: Valor atual do Motion Blur
    private float currentChromaticAberration = 0f;
    private float currentVignette = 0f;
    private float currentWallRunSideOffset = 0f; // Offset lateral dinâmico
    private float currentGrindSideOffset = 0f; // NOVO: Offset lateral para o grind
    private float currentGrindYaw = 0f; // NOVO: Yaw extra para o grind
    private float currentWallRunDistanceMultiplier = 1.0f; // Multiplicador de distância dinâmico
    private bool wasTransitioning = false; // NOVO: Flag para detectar o fim da transição

    // ✅ NOVO: Variáveis de Camera Shake
    private float shakeTimer = 0f;
    private float shakeDuration = 0f;
    private float shakeAmount = 0f;
    private float shakeFrequency = 0f;
    private Vector3 shakeOffset = Vector3.zero;

    private PlayerMovement_FrontiersStyle playerMovement;
    private PlayerRailRide_SonicStyle_Spline railRideSpline;
    private PlayerRailRide_SonicStyle_Spline railRide;
    private WarpSystem warpSystem;
    private SlopeSlideSystem slopeSlideSystem; // Referência ao novo sistema de slide
    private CameraRailManager cameraRailManager; // Referência ao CameraRailManager

    // ✅ NOVO: Propriedade pública para ler a distorção atual
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
            slopeSlideSystem = target.GetComponent<SlopeSlideSystem>(); // Obtém a referência ao SlopeSlideSystem
            cameraRailManager = FindObjectOfType<CameraRailManager>(); // Obtém a referência ao CameraRailManager

            currentDistance = baseDistance;
            collisionDistance = baseDistance;
            currentX = target.eulerAngles.y;
            currentY = 15f;
        }

        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;

        currentFOV = normalFOV;
        if (cam != null) cam.fieldOfView = currentFOV;
    }

    void Update()
    {
        if (target == null) return;
        
        // Lógica de input e shake que pode ser processada no Update
        bool isGrinding = IsPlayerGrinding();
        HandleZoomInput();
        
        if (isGrinding && allowGrindCameraRotation)
            HandleRotationInput(grindRotationSpeed);
        else if (!isGrinding)
            HandleRotationInput(rotationSpeed);

        UpdateShake();

        // Lógica de FOV e Pós-Processamento que pode ser processada no Update
        bool isWallRunning = IsPlayerWallRunning();
        bool isBoosting = IsPlayerBoosting();
        bool isWarping = IsPlayerWarping();
        bool isSliding = IsPlayerSliding();

        float playerSpeed = GetPlayerSpeed();
        bool isMovingFast = playerSpeed >= speedThreshold;
        
        float targetFOV = normalFOV;
        float targetDistortion = 0f;
        
        if (isWarping) targetFOV = warpFOV;
        else if (isBoosting) targetFOV = boostFOV;
        else if (isGrinding) targetFOV = grindFOV;
        else if (isWallRunning) targetFOV = wallRunFOV;
        else if (isSliding) { targetFOV = slideFOV; targetDistortion = slideLensDistortion; }
        else if (isMovingFast) targetFOV = speedFOV;

        currentFOV = Mathf.Lerp(currentFOV, targetFOV, Time.deltaTime * fovTransitionSpeed);
        if (cam != null) cam.fieldOfView = currentFOV;

        currentDistortion = Mathf.Lerp(currentDistortion, targetDistortion, Time.deltaTime * distortionTransitionSpeed);

        float targetMotionBlur = isSliding ? slideMotionBlurIntensity : 0f;
        currentMotionBlur = Mathf.Lerp(currentMotionBlur, targetMotionBlur, Time.deltaTime * motionBlurTransitionSpeed);

        float targetChromatic = isSliding ? slideChromaticAberration : 0f;
        float targetVignette = isSliding ? slideVignetteIntensity : 0f;
        currentChromaticAberration = Mathf.Lerp(currentChromaticAberration, targetChromatic, Time.deltaTime * ppTransitionSpeed);
        currentVignette = Mathf.Lerp(currentVignette, targetVignette, Time.deltaTime * ppTransitionSpeed);
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Verifica se o CameraRailManager está controlando esta câmera
        bool isBeingTransitioned = cameraRailManager != null && cameraRailManager.IsThisCameraTransitioning(transform);

        if (isBeingTransitioned)
        {
            // Se a câmera está em transição, atualizamos currentX e currentY para a rotação atual
            // para que, quando a transição terminar, a câmera não "pule" de volta para a rotação antiga.
            currentX = transform.eulerAngles.y;
            currentY = transform.eulerAngles.x;
            wasTransitioning = true;
            return; // Interrompe o LateUpdate aqui, pois o CameraRailManager está movendo a câmera
        }

        // Se a transição acabou de terminar, precisamos garantir que a câmera comece a interpolar
        // a partir da sua posição e rotação atuais (que foram definidas pelo CameraRailManager).
        if (wasTransitioning)
        {
            currentX = transform.eulerAngles.y;
            currentY = transform.eulerAngles.x;
            wasTransitioning = false;
        }

        bool isGrinding = IsPlayerGrinding();
        bool isWallRunning = IsPlayerWallRunning();
        bool isBoosting = IsPlayerBoosting();
        bool isSliding = IsPlayerSliding();

        // --- FOV e Altura ---
        float targetHeight = isGrinding ? grindHeight : height;
        float idealDistance = isGrinding ? grindDistance : currentDistance;
        
        float targetDistMultiplier = isWallRunning ? wallRunDistanceMultiplier : 1.0f;
        currentWallRunDistanceMultiplier = Mathf.Lerp(currentWallRunDistanceMultiplier, targetDistMultiplier, Time.deltaTime * wallRunDistanceTransitionSpeed);
        idealDistance *= currentWallRunDistanceMultiplier;
        
        float smoothSpeed = isGrinding ? grindSmoothSpeed : positionSmoothSpeed;
        
        // --- Rotação Horizontal (Eixo Y) ---
        bool hasMouseInput = Mathf.Abs(Input.GetAxis("Mouse X")) > 0.1f || Mathf.Abs(Input.GetAxis("Mouse Y")) > 0.1f;
        
        if (!hasMouseInput && !isGrinding && !isSliding && !isWallRunning)
            currentX = Mathf.LerpAngle(currentX, target.eulerAngles.y, Time.deltaTime * autoCenterSpeed);
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
        else if (isSliding)
        {
             currentX = Mathf.LerpAngle(currentX, target.eulerAngles.y, Time.deltaTime * autoCenterSpeed * 0.5f);
             currentY = Mathf.Lerp(currentY, 15f, Time.deltaTime * autoCenterSpeed * 0.5f);
        }

        // --- Lógica de WallRun, Grind e Slide Tilt ---
        float targetWallYaw = isWallRunning ? (IsPlayerOnLeftWall() ? wallRunYawOffset : -wallRunYawOffset) : 0f;
        float targetWallSideOffset = isWallRunning ? (IsPlayerOnLeftWall() ? -wallRunSideOffset : wallRunSideOffset) : 0f;
        
        float targetGrindYaw = isGrinding ? grindYawOffset : 0f;
        float targetGrindSideOffset = isGrinding ? grindSideOffset : 0f;
        
        float targetTilt = 0f;

        if (isWallRunning) targetTilt = IsPlayerOnLeftWall() ? wallRunTiltAmount : -wallRunTiltAmount;
        else if (isGrinding) targetTilt = target.right.y * grindTiltAmount;
        else if (isSliding) targetTilt = slideTiltAngle;

        // Transições
        currentWallRunYaw = Mathf.Lerp(currentWallRunYaw, targetWallYaw, Time.deltaTime * (isWallRunning ? wallRunYawSpeed : 2.5f));
        currentWallRunSideOffset = Mathf.Lerp(currentWallRunSideOffset, targetWallSideOffset, Time.deltaTime * (isWallRunning ? wallRunYawSpeed : wallRunSideOffsetSpeed));
        
        currentGrindYaw = Mathf.Lerp(currentGrindYaw, targetGrindYaw, Time.deltaTime * grindYawSpeed);
        currentGrindSideOffset = Mathf.Lerp(currentGrindSideOffset, targetGrindSideOffset, Time.deltaTime * grindSideOffsetSpeed);
        
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * (isWallRunning ? wallRunTiltSpeed : (isSliding ? slideTiltSpeed : 5f)));

        // --- Construção da Rotação Final ---
        Quaternion finalRotation = Quaternion.Euler(currentY, currentX, 0);
        finalRotation *= Quaternion.Euler(0, currentWallRunYaw + currentGrindYaw, 0);
        finalRotation *= Quaternion.Euler(0, 0, currentTilt);

        // --- COLISÃO APRIMORADA (SphereCast) ---
        Vector3 rayOrigin = target.position + Vector3.up * collisionRaycastOffset;
        
        // Aplica os offsets laterais (WallRun e Grind)
        Vector3 combinedSideOffset = finalRotation * Vector3.right * (currentWallRunSideOffset + currentGrindSideOffset);
        rayOrigin += combinedSideOffset;

        Vector3 direction = finalRotation * (Vector3.back * idealDistance + Vector3.up * (targetHeight - collisionRaycastOffset));
        Vector3 normalizedDir = direction.normalized;
        float maxRayDist = direction.magnitude;

        RaycastHit hit;
        float targetCollisionDist = idealDistance;

        if (Physics.SphereCast(rayOrigin, cameraRadius, normalizedDir, out hit, maxRayDist, collisionLayers))
            targetCollisionDist = Mathf.Max(0.5f, hit.distance - collisionPadding);

        collisionDistance = Mathf.Lerp(collisionDistance, targetCollisionDist, Time.deltaTime * collisionResponseSpeed);

        // --- Posicionamento Final ---
        Vector3 desiredPosition = rayOrigin + normalizedDir * collisionDistance;
        if (isBoosting) desiredPosition += Random.insideUnitSphere * boostShakeAmount;
        desiredPosition += shakeOffset;

        // Apenas atualiza a posição e rotação se não estiver sendo controlada pelo CameraRailManager
        if (!isBeingTransitioned)
        {
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, finalRotation, smoothSpeed);
        }
    }

    // ✅ NOVO: Método para ativar camera shake
    public void TriggerWallDashShake()
    {
        if (!enableWallDashShake) return;
        shakeTimer = 0f;
        shakeDuration = wallDashShakeDuration;
        shakeAmount = wallDashShakeAmount;
        shakeFrequency = wallDashShakeFrequency;
    }

    // ✅ NOVO: Atualizar o shake a cada frame
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
        currentY -= Input.GetAxis("Mouse Y") * speed;
        currentY = Mathf.Clamp(currentY, minYAngle, maxYAngle);
    }

    private bool IsPlayerGrinding()
    {
        if (railRideSpline != null) return railRideSpline.isGrinding;
        if (railRide != null) return railRide.isGrinding;
        return false;
    }

    private bool IsPlayerWallRunning()
    {
        return playerMovement != null && playerMovement.IsWallRunning;
    }

    private bool IsPlayerOnLeftWall()
    {
        return playerMovement != null && playerMovement.OnLeftWall;
    }

    private bool IsPlayerBoosting()
    {
        // Se houver lógica de boost no futuro
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

    private float GetPlayerSpeed()
    {
        return playerMovement != null ? playerMovement.currentSpeed : 0f;
    }
}
