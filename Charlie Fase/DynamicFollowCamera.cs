using UnityEngine;

public class DynamicFollowCamera : MonoBehaviour // Nome alterado para refletir a nova funcionalidade
{
    [Tooltip("O Transform do jogador que a câmera deve seguir e orbitar.")]
    public Transform target;

    [Header("Configurações de Posição Normal")]
    [Tooltip("Distância base da câmera em relação ao jogador.")]
    public float baseDistance = 10.0f;
    [Tooltip("Altura da câmera em relação ao jogador.")]
    public float height = 5.0f;
    [Tooltip("Quão suavemente a câmera se move para a posição desejada.")]
    public float positionSmoothSpeed = 0.125f;

    [Header("Configurações de Rotação Manual")]
    [Tooltip("Velocidade de rotação da câmera com o input do mouse.")]
    public float rotationSpeed = 5.0f;
    [Tooltip("Velocidade de retorno ao centro (auto-centramento) quando não há input.")]
    public float autoCenterSpeed = 1.0f;
    [Tooltip("Ângulo mínimo vertical (olhando para baixo).")]
    public float minYAngle = -10f;
    [Tooltip("Ângulo máximo vertical (olhando para cima).")]
    public float maxYAngle = 80f;

    [Header("Configurações de Zoom (Roda do Mouse)")]
    public float zoomSpeed = 2.0f;
    public float minDistance = 5.0f;
    public float maxDistance = 15.0f;

    // --- NOVO: Configurações de Colisão ---
    [Header("Configurações de Colisão")]
    [Tooltip("As layers que a câmera deve considerar como obstáculos.")]
    public LayerMask collisionLayers;
    [Tooltip("Um pequeno recuo para evitar que a câmera fique exatamente na superfície da parede.")]
    public float collisionPadding = 0.2f;
    [Tooltip("Quão suavemente a câmera se move para a posição de colisão.")]
    public float collisionSmoothSpeed = 0.05f;
    [Tooltip("Offset vertical a partir do pivô do jogador para iniciar o raio de colisão (para evitar colidir com o chão).")]
    public float collisionRaycastOffset = 1.0f;
    // --- FIM NOVO ---

    [Header("Configurações Durante Wall Run")]
    public float wallRunTiltAmount = 10f;
    public float wallRunTiltSpeed = 8f;

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

    private float currentDistance;
    private float currentX = 0.0f;
    private float currentY = 0.0f;
    private float currentTilt = 0f;
    private Camera cam;
    private float currentFOV;

    private PlayerMovement_FrontiersStyle playerMovement;
    private PlayerRailRide_SonicStyle_Spline railRideSpline;
    private PlayerRailRide_SonicStyle_Spline railRide;

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

            currentDistance = baseDistance;
            currentX = target.eulerAngles.y;
            currentY = 15f; // Um ângulo inicial razoável
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
        {
            HandleRotationInput(grindRotationSpeed);
        }
        else if (!isGrinding)
        {
            HandleRotationInput(rotationSpeed);
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        bool isGrinding = IsPlayerGrinding();
        bool isWallRunning = IsPlayerWallRunning();
        bool isBoosting = IsPlayerBoosting();

        float targetHeight = isGrinding ? grindHeight : height;
        float idealDistance = isGrinding ? grindDistance : currentDistance;
        float smoothSpeed = isGrinding ? grindSmoothSpeed : positionSmoothSpeed;
        float targetFOV = normalFOV;

        if (isGrinding) targetFOV = grindFOV;
        if (isBoosting) targetFOV = boostFOV;

        currentFOV = Mathf.Lerp(currentFOV, targetFOV, Time.deltaTime * fovTransitionSpeed);
        if (cam != null) cam.fieldOfView = currentFOV;

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

        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);

        float targetTilt = 0f;
        if (isGrinding)
        {
            targetTilt = target.right.y * grindTiltAmount;
            currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * 5f);
        }
        else if (isWallRunning)
        {
            targetTilt = IsPlayerOnLeftWall() ? wallRunTiltAmount : -wallRunTiltAmount;
            currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * wallRunTiltSpeed);
        }
        else
        {
            currentTilt = Mathf.Lerp(currentTilt, 0, Time.deltaTime * 5f);
        }
        rotation *= Quaternion.Euler(0, 0, currentTilt);

        // --- NOVO: Lógica de Colisão ---
        Vector3 targetHeadPosition = target.position + Vector3.up * collisionRaycastOffset;
        Vector3 idealCameraPosition = targetHeadPosition + rotation * (Vector3.back * idealDistance + Vector3.up * (targetHeight - collisionRaycastOffset));
        
        float finalDistance = idealDistance;
        RaycastHit hit;
        // Lança um raio da cabeça do jogador para a posição ideal da câmera
        if (Physics.Raycast(targetHeadPosition, idealCameraPosition - targetHeadPosition, out hit, idealDistance, collisionLayers))
        {
            // Se colidiu, ajusta a distância final para o ponto de colisão, com um pequeno recuo
            finalDistance = hit.distance - collisionPadding;
        }
        // --- FIM NOVO ---

        // Calcula a Posição Desejada usando a distância final (ajustada pela colisão ou não)
        Vector3 desiredPosition = targetHeadPosition + rotation * (Vector3.back * finalDistance + Vector3.up * (targetHeight - collisionRaycastOffset));

        if (isBoosting)
        {
            desiredPosition += Random.insideUnitSphere * boostShakeAmount;
        }

        // Usa um Lerp diferente para a colisão para uma resposta mais rápida
        float finalSmoothSpeed = (finalDistance < idealDistance) ? collisionSmoothSpeed : smoothSpeed;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, finalSmoothSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, smoothSpeed);
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

    // Métodos auxiliares (sem alterações)
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

    private void OnDrawGizmosSelected()
    {
        if (target == null) return;

        Gizmos.color = Color.blue;
        Vector3 normalPos = target.position - transform.forward * baseDistance + Vector3.up * height;
        Gizmos.DrawWireSphere(normalPos, 0.3f);
        Gizmos.DrawLine(target.position, normalPos);

        Gizmos.color = Color.yellow;
        Vector3 grindPos = target.position - transform.forward * grindDistance + Vector3.up * grindHeight;
        Gizmos.DrawWireSphere(grindPos, 0.3f);
        Gizmos.DrawLine(target.position, grindPos);

        // NOVO: Gizmo para a colisão
        Gizmos.color = Color.red;
        Vector3 headPos = target.position + Vector3.up * collisionRaycastOffset;
        Gizmos.DrawLine(headPos, transform.position);
    }
}
