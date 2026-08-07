using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Sistema de Parkour Melhorado - Otimizado para Performance
/// Reduz alocações, Raycasts redundantes e cálculos desnecessários
/// </summary>
public class ParkourSystem : MonoBehaviour
{
    [System.Serializable]
    public class ParkourMove
    {
        public string name;
        public string animationName;
        public float minObstacleHeight;
        public float maxObstacleHeight;
        public float animationDuration = 0.6f;
        public float landingDistance = 1.5f;
        public float jumpPower = 1.5f;
        [Range(0f, 1f)]
        public float transitionDuration = 0.3f;
    }

    [Header("References")]
    private CharacterController controller;
    private Animator animator;
    private PlayerMovement_FrontiersStyle movementScript;
    private Transform cachedTransform;

    [Header("Detection")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float detectionDistance = 3f;
    [SerializeField] private float minObstacleHeight = 0.5f;
    [SerializeField] private float maxObstacleHeight = 10f;
    [SerializeField] private float minSpeedRequired = 0.5f;

    [Header("Parkour Moves")]
    [SerializeField] private List<ParkourMove> parkourMoves = new List<ParkourMove>();

    [Header("Collider Settings")]
    [SerializeField] private float controllerHeightDuringParkour = 0.1f;
    [SerializeField] private float controllerCenterYDuringParkour = 0.05f;

    [Header("Animation Settings")]
    [SerializeField] private float animationCrossFadeDuration = 0.15f;
    [SerializeField] private float preAnimationDelay = 0.1f;

    [Header("Arc Settings")]
    [SerializeField] private float minimumArcHeight = 1.5f;
    [SerializeField] private float landingDistanceMultiplier = 1.5f;

    [Header("Speed Settings")]
    [SerializeField] private bool maintainSpeed = true;
    [SerializeField] private float speedMultiplier = 1.0f;

    [Header("Smoothness & Inertia Settings")]
    [SerializeField] private float positionSmoothSpeed = 12f; // Aumentado para resposta mais rápida mas suave
    [SerializeField] private float rotationSmoothSpeed = 8f;
    [SerializeField] private float inertiaDecay = 0.95f;
    [SerializeField] private float velocityDamping = 0.92f;
    [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool useAdaptiveSmoothing = true;
    
    [Header("Proximity & Landing")]
    [SerializeField] private float targetReachThreshold = 0.1f;
    [SerializeField] private float landingGroundOffset = 0.05f; // Pequeno offset para evitar afundar
    [SerializeField] private LayerMask groundLayer; // Layer específica para o chão

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;


    private bool isParkourActive = false;
    public bool IsParkourActive => isParkourActive;
    private float originalControllerHeight;
    private Vector3 originalControllerCenter;
    private float parkourStartSpeed = 0f;

    private Vector3 velocityInertia = Vector3.zero;
    private float rotationInertia = 0f;
    private Vector3 lastFrameVelocity = Vector3.zero;

    // ✅ CACHE - Evita alocações repetidas
    private Vector3 rayOrigin;
    private Vector3 feetPosition;
    private RaycastHit raycastHit;
    private const float RAYCAST_TOP_DISTANCE = 15f;
    private const float RAYCAST_TOP_MAX_DISTANCE = 20f;
    private const float RAYCAST_LANDING_DISTANCE = 5f;
    private const float RAYCAST_LANDING_MAX_DISTANCE = 10f;
    private const float LANDING_RAY_FORWARD = 3f;
    private const float LANDING_RAY_HEIGHT = 5f;
    private const float GROUND_CHECK_HEIGHT = 2f;
    private const float GROUND_CHECK_DISTANCE = 5f;

    // ✅ CACHE - Pré-alocado para Bezier
    private Vector3 bezierP0, bezierP1, bezierP2;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        movementScript = GetComponent<PlayerMovement_FrontiersStyle>();
        cachedTransform = transform;

        originalControllerHeight = controller.height;
        originalControllerCenter = controller.center;
    }

    void Update()
    {
        // ✅ OTIMIZADO: Verificações mais eficientes
        if (isParkourActive || movementScript.animatorBusy || movementScript.currentSpeed <= minSpeedRequired)
            return;

        if (Input.GetKeyDown(KeyCode.Space) || ShouldAutoParkour())
        {
            AttemptParkour();
        }
    }

    /// <summary>
    /// Verifica se deve fazer parkour automático
    /// </summary>
    private bool ShouldAutoParkour()
    {
        rayOrigin = cachedTransform.position + Vector3.up * 1f;
        return Physics.Raycast(rayOrigin, cachedTransform.forward, detectionDistance, obstacleLayer);
    }

    private void AttemptParkour()
    {
        if (!TryDetectObstacle(out Vector3 obstacleTop, out float obstacleHeight, out Vector3 landingPoint))
        {
            return;
        }

        ParkourMove move = FindAppropriateMove(obstacleHeight);
        if (move == null) return;

        // Capturar velocidade e inércia atual
        parkourStartSpeed = movementScript.currentSpeed;
        velocityInertia = movementScript.currentSpeed * cachedTransform.forward;

        StartCoroutine(ExecuteParkour(move, obstacleTop, obstacleHeight, landingPoint));
    }

    /// <summary>
    /// Detecta obstáculo - OTIMIZADO: menos raycasts e cálculos
    /// </summary>
    private bool TryDetectObstacle(out Vector3 obstacleTop, out float obstacleHeight, out Vector3 landingPoint)
    {
        obstacleTop = Vector3.zero;
        obstacleHeight = 0;
        landingPoint = Vector3.zero;

        // ✅ OTIMIZADO: Calcula uma vez
        feetPosition = cachedTransform.position - Vector3.up * (originalControllerHeight * 0.5f);
        rayOrigin = feetPosition + Vector3.up;

        // Raycast 1: Detecta o obstáculo frontal
        if (!Physics.Raycast(rayOrigin, cachedTransform.forward, out raycastHit, detectionDistance, obstacleLayer))
        {
            return false;
        }

        // Raycast 2: Detecta o topo do obstáculo
        Vector3 topRayOrigin = raycastHit.point + Vector3.up * RAYCAST_TOP_DISTANCE;
        if (!Physics.Raycast(topRayOrigin, Vector3.down, out RaycastHit topHit, RAYCAST_TOP_MAX_DISTANCE, obstacleLayer))
        {
            return false;
        }

        obstacleTop = topHit.point;
        obstacleHeight = obstacleTop.y - feetPosition.y;

        // ✅ OTIMIZADO: Early exit se altura inválida
        if (obstacleHeight < minObstacleHeight || obstacleHeight > maxObstacleHeight)
        {
            return false;
        }

        // Raycast 3: Detecta ponto de pouso
        Vector3 landingRayOrigin = obstacleTop + cachedTransform.forward * LANDING_RAY_FORWARD + Vector3.up * LANDING_RAY_HEIGHT;
        if (!Physics.Raycast(landingRayOrigin, Vector3.down, out RaycastHit landingHit, RAYCAST_LANDING_MAX_DISTANCE, obstacleLayer))
        {
            landingPoint = obstacleTop + cachedTransform.forward * LANDING_RAY_FORWARD;
            landingPoint.y = feetPosition.y;
        }
        else
        {
            landingPoint = landingHit.point;
        }

        return true;
    }

    /// <summary>
    /// Encontra movimento apropriado - OTIMIZADO: loop simples
    /// </summary>
    private ParkourMove FindAppropriateMove(float obstacleHeight)
    {
        // ✅ OTIMIZADO: For loop em vez de foreach
        for (int i = 0; i < parkourMoves.Count; i++)
        {
            ParkourMove move = parkourMoves[i];
            if (obstacleHeight >= move.minObstacleHeight && obstacleHeight <= move.maxObstacleHeight)
            {
                return move;
            }
        }
        return null;
    }

    private IEnumerator ExecuteParkour(ParkourMove move, Vector3 obstacleTop, float obstacleHeight, Vector3 landingPoint)
    {
        isParkourActive = true;
        movementScript.animatorBusy = true;

        animator.CrossFadeInFixedTime(move.animationName, animationCrossFadeDuration);
        yield return new WaitForSeconds(preAnimationDelay);

        controller.height = controllerHeightDuringParkour;
        controller.center = new Vector3(0, controllerCenterYDuringParkour, 0);

        Vector3 startPos = cachedTransform.position;
        Quaternion startRot = cachedTransform.rotation;

        // ✅ OTIMIZADO: Pré-calcula valores
        float arcHeight = Mathf.Max(obstacleHeight * move.jumpPower, obstacleHeight + minimumArcHeight);
        Vector3 midPoint = obstacleTop + Vector3.up * arcHeight;

        float distanceBySpeed = parkourStartSpeed * move.animationDuration;
        float finalLandingDistance = Mathf.Max(move.landingDistance * landingDistanceMultiplier, distanceBySpeed);
        Vector3 endPos = landingPoint + cachedTransform.forward * (finalLandingDistance * 0.5f);

        // Raycast para ground check - Usa obstacleLayer e groundLayer para precisão
        LayerMask combinedLayer = obstacleLayer | groundLayer;
        if (Physics.Raycast(endPos + Vector3.up * GROUND_CHECK_HEIGHT, Vector3.down, out RaycastHit groundHit, GROUND_CHECK_DISTANCE, combinedLayer))
        {
            // Ajusta endPos para a altura real do chão + offset do CharacterController
            endPos.y = groundHit.point.y + (originalControllerHeight * 0.5f) + landingGroundOffset;
        }

        float elapsedTime = 0f;
        float duration = move.animationDuration;
        float inverseDuration = 1f / duration;

        // ✅ OTIMIZADO: Pré-calcula pontos de Bezier
        bezierP0 = startPos;
        bezierP1 = midPoint;
        bezierP2 = endPos;

        Vector3 lastPos = startPos;
        Vector3 smoothVelocity = Vector3.zero;
        Vector3 cachedForward = cachedTransform.forward;

        Vector3 currentVelocity = Vector3.zero;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime * inverseDuration);
            float easedT = movementCurve.Evaluate(t);

            // 1. Calcula a posição teórica na curva de Bezier
            Vector3 bezierTarget = CalculateBezierPoint(easedT, bezierP0, bezierP1, bezierP2);
            


            // 3. MOVIMENTO ULTRA-FLUIDO: Em vez de setar a posição, usamos Move() com interpolação suave
            // Isso permite que o CharacterController resolva colisões laterais enquanto segue a curva
            Vector3 nextPos = Vector3.SmoothDamp(cachedTransform.position, bezierTarget, ref currentVelocity, 0.05f);
            Vector3 moveDiff = nextPos - cachedTransform.position;
            
            if (controller.enabled)
            {
                // Verifica se estamos muito perto do chão para interromper a descida brusca
                if (t > 0.8f)
                {
                    RaycastHit hit;
                    if (Physics.Raycast(cachedTransform.position + Vector3.up * 0.1f, Vector3.down, out hit, 0.5f, combinedLayer))
                    {
                        // Se estivermos prestes a tocar o chão, suavizamos a descida final
                        bezierTarget.y = Mathf.Max(bezierTarget.y, hit.point.y + (originalControllerHeight * 0.5f) + landingGroundOffset);
                    }
                }

                controller.Move(moveDiff);
            }

            // 4. ROTAÇÃO ORGÂNICA
            Quaternion targetRotation = Quaternion.LookRotation(cachedForward);
            cachedTransform.rotation = Quaternion.Slerp(cachedTransform.rotation, targetRotation, Time.deltaTime * rotationSmoothSpeed);

            yield return null;
        }

        // --- FINALIZAÇÃO SUAVE ---
        // Antes de restaurar o collider, garante que a posição final é válida
        RaycastHit finalHit;
        if (Physics.Raycast(cachedTransform.position + Vector3.up * 1f, Vector3.down, out finalHit, 2f, combinedLayer))
        {
            Vector3 finalPos = cachedTransform.position;
            finalPos.y = finalHit.point.y + (originalControllerHeight * 0.5f) + landingGroundOffset;
            cachedTransform.position = finalPos;
        }

        controller.height = originalControllerHeight;
        controller.center = originalControllerCenter;



        // Aplicar inércia residual
        velocityInertia *= inertiaDecay;
        lastFrameVelocity *= velocityDamping;

        yield return new WaitForFixedUpdate();

        movementScript.animatorBusy = false;
        isParkourActive = false;
    }

    /// <summary>
    /// Calcula ponto de Bezier quadrática - OTIMIZADO: sem alocações
    /// </summary>
    private Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float mt = 1f - t;
        float mt2 = mt * mt;
        float t2 = t * t;
        return mt2 * p0 + 2f * mt * t * p1 + t2 * p2;
    }

    /// <summary>
    /// Aplica inércia ao movimento
    /// </summary>
    private Vector3 ApplyInertia(Vector3 currentVelocity, Vector3 targetVelocity, float deltaTime)
    {
        return Vector3.Lerp(currentVelocity, targetVelocity, deltaTime * positionSmoothSpeed);
    }
}
