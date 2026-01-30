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

    [Header("Configurações Durante Wall Run")]
    public float wallRunTiltAmount = 10f;
    public float wallRunTiltSpeed = 8f;
    public float wallRunYawOffset = 25f; 
    public float wallRunYawSpeed = 5f;
    public float wallRunFOV = 75f;

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

    private float currentDistance;
    private float currentX = 0.0f;
    private float currentY = 0.0f;
    private float currentTilt = 0f;
    private float currentWallRunYaw = 0f; 
    private float collisionDistance;
    private Camera cam;
    private float currentFOV;

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
        
        bool isGrinding = IsPlayerGrinding();
        HandleZoomInput();
        
        if (isGrinding && allowGrindCameraRotation)
            HandleRotationInput(grindRotationSpeed);
        else if (!isGrinding)
            HandleRotationInput(rotationSpeed);

        // ✅ NOVO: Atualizar shake
        UpdateShake();
    }

    void LateUpdate()
    {
        if (target == null) return;

        bool isGrinding = IsPlayerGrinding();
        bool isWallRunning = IsPlayerWallRunning();
        bool isBoosting = IsPlayerBoosting();
        bool isWarping = IsPlayerWarping();

        // --- FOV e Altura ---
        float targetHeight = isGrinding ? grindHeight : height;
        float idealDistance = isGrinding ? grindDistance : currentDistance;
        float smoothSpeed = isGrinding ? grindSmoothSpeed : positionSmoothSpeed;
        
        // NOVO: Verificar velocidade usando PlayerMovement.CurrentSpeed
        float playerSpeed = GetPlayerSpeed();
        bool isMovingFast = playerSpeed >= speedThreshold;
        
        // Lógica de FOV com prioridade de estados
        float targetFOV = normalFOV;
        
        if (isWarping)
        {
            targetFOV = warpFOV;
        }
        else if (isBoosting)
        {
            targetFOV = boostFOV;
        }
        else if (isGrinding)
        {
            targetFOV = grindFOV;
        }
        else if (isWallRunning)
        {
            targetFOV = wallRunFOV;
        }
        else if (isMovingFast)
        {
            // NOVO: Se está se movendo rápido, aumentar FOV
            targetFOV = speedFOV;
        }

        currentFOV = Mathf.Lerp(currentFOV, targetFOV, Time.deltaTime * fovTransitionSpeed);
        if (cam != null) cam.fieldOfView = currentFOV;

        // --- Rotação Horizontal (Eixo Y) ---
        bool hasMouseInput = Mathf.Abs(Input.GetAxis("Mouse X")) > 0.1f || Mathf.Abs(Input.GetAxis("Mouse Y")) > 0.1f;
        
        if (!hasMouseInput && !isGrinding)
        {
            currentX = Mathf.LerpAngle(currentX, target.eulerAngles.y, Time.deltaTime * autoCenterSpeed);
        }
        else if (isGrinding && grindAutoCenterStrength > 0f)
        {
            currentX = Mathf.LerpAngle(currentX, target.eulerAngles.y, Time.deltaTime * autoCenterSpeed * grindAutoCenterStrength);
            currentY = Mathf.Lerp(currentY, 10f, Time.deltaTime * autoCenterSpeed * grindAutoCenterStrength);
        }

        // --- Lógica de WallRun ---
        float targetYawOffset = isWallRunning ? (IsPlayerOnLeftWall() ? wallRunYawOffset : -wallRunYawOffset) : 0f;
        float targetTilt = isWallRunning ? (IsPlayerOnLeftWall() ? wallRunTiltAmount : -wallRunTiltAmount) : (isGrinding ? target.right.y * grindTiltAmount : 0f);

        currentWallRunYaw = Mathf.Lerp(currentWallRunYaw, targetYawOffset, Time.deltaTime * wallRunYawSpeed);
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * (isWallRunning ? wallRunTiltSpeed : 5f));

        // --- Construção da Rotação Final ---
        Quaternion finalRotation = Quaternion.Euler(currentY, currentX, 0);
        finalRotation *= Quaternion.Euler(0, currentWallRunYaw, 0);
        finalRotation *= Quaternion.Euler(0, 0, currentTilt);

        // --- COLISÃO APRIMORADA (SphereCast) ---
        Vector3 rayOrigin = target.position + Vector3.up * collisionRaycastOffset;
        Vector3 direction = finalRotation * (Vector3.back * idealDistance + Vector3.up * (targetHeight - collisionRaycastOffset));
        Vector3 normalizedDir = direction.normalized;
        float maxRayDist = direction.magnitude;

        RaycastHit hit;
        float targetCollisionDist = idealDistance;

        if (Physics.SphereCast(rayOrigin, cameraRadius, normalizedDir, out hit, maxRayDist, collisionLayers))
        {
            targetCollisionDist = Mathf.Max(0.5f, hit.distance - collisionPadding);
        }

        collisionDistance = Mathf.Lerp(collisionDistance, targetCollisionDist, Time.deltaTime * collisionResponseSpeed);

        // --- Posicionamento Final ---
        Vector3 desiredPosition = rayOrigin + normalizedDir * collisionDistance;

        if (isBoosting)
        {
            desiredPosition += Random.insideUnitSphere * boostShakeAmount;
        }

        // ✅ NOVO: Aplicar shake offset
        desiredPosition += shakeOffset;

        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, finalRotation, smoothSpeed);
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
            
            // Calcular a intensidade do shake (diminui com o tempo)
            float progress = shakeTimer / shakeDuration;
            float intensity = Mathf.Lerp(shakeAmount, 0f, progress);
            
            // Gerar offset aleatório com frequência
            float shakeX = Mathf.Sin(shakeTimer * shakeFrequency) * intensity;
            float shakeY = Mathf.Cos(shakeTimer * shakeFrequency * 0.7f) * intensity;
            float shakeZ = Mathf.Sin(shakeTimer * shakeFrequency * 0.5f) * intensity;
            
            shakeOffset = new Vector3(shakeX, shakeY, shakeZ);
        }
        else
        {
            shakeOffset = Vector3.zero;
        }
    }

    // NOVO: Método para obter a velocidade do jogador via PlayerMovement
    private float GetPlayerSpeed()
    {
        if (playerMovement != null)
        {
            return playerMovement.CurrentSpeed;
        }
        return 0f;
    }

    private void HandleRotationInput(float speed)
    {
        currentX += Input.GetAxis("Mouse X") * speed;
        currentY -= Input.GetAxis("Mouse Y") * speed;
        currentY = Mathf.Clamp(currentY, minYAngle, maxYAngle);
    }

    private void HandleZoomInput()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        currentDistance -= scrollInput * zoomSpeed;
        currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
    }

    private bool IsPlayerGrinding()
    {
        if (railRideSpline != null) return railRideSpline.IsGrinding;
        if (railRide != null) return railRide.IsGrinding;
        return false;
    }

    private bool IsPlayerWallRunning()
    {
        if (playerMovement != null) return playerMovement.IsWallRunning;
        return false;
    }

    private bool IsPlayerOnLeftWall()
    {
        if (playerMovement != null) return playerMovement.OnLeftWall;
        return false;
    }

    private bool IsPlayerOnRightWall()
    {
        if (playerMovement != null) return playerMovement.OnRightWall;
        return false;
    }

    private bool IsPlayerBoosting()
    {
        if (railRideSpline != null) return railRideSpline.IsBoosting;
        if (railRide != null) return railRide.IsBoosting;
        return false;
    }

    private bool IsPlayerWarping()
    {
        if (warpSystem != null) return warpSystem.IsWarping;
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (target == null) return;
        Gizmos.color = Color.green;
        Vector3 rayOrigin = target.position + Vector3.up * collisionRaycastOffset;
        Gizmos.DrawWireSphere(transform.position, cameraRadius);
        Gizmos.DrawLine(rayOrigin, transform.position);
    }
}
