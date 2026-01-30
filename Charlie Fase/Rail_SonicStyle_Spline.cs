using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

[RequireComponent(typeof(SplineContainer))]
public class Rail_SonicStyle_Spline : MonoBehaviour
{
    [Header("Configuracao do Rail")]
    [SerializeField] private float recommendedSpeed = 20f;
    [SerializeField] private bool isLoop = false;
    
    [Header("Rails Adjacentes")]
    [SerializeField] private Rail_SonicStyle_Spline leftRail;
    [SerializeField] private Rail_SonicStyle_Spline rightRail;
    [SerializeField] private float adjacentRailDistance = 3f;
    
    [Header("Visualizacao")]
    [SerializeField] private Color railColor = Color.yellow;
    [SerializeField] private bool showAdjacentConnections = true;
    [SerializeField] private int visualizationSegments = 50;

    [Header("Configuracoes de Camera Cinematografica")]
    [SerializeField] private bool enableCinematicCamera = false;
    [SerializeField] private float cinematicCameraDistance = 0.8f; // Distancia (0-1) no rail para ativar a camera cinematografica
    [SerializeField] private GameObject cinematicCamera; // Referencia a camera cinematografica (ex: Cinemachine Virtual Camera)
    
    [Header("Tempos de Transicao (Individual)")]
    [Tooltip("Tempo para a camera transicionar da principal para a do rail.")]
    [SerializeField] private float transitionInDuration = 0.5f;
    [Tooltip("Tempo para a camera transicionar da do rail de volta para a principal.")]
    [SerializeField] private float transitionOutDuration = 0.5f;

    [Header("Configuracoes de Cooldown")]
    [Tooltip("Tempo (em segundos) que a camera cinematografica ficara desativada apos ser desativada.")]
    [SerializeField] private float cameraCooldownDuration = 2.0f;
    private float lastCameraDeactivationTime = -Mathf.Infinity;
    private bool isCinematicCameraActive = false;

    [Header("Mesh Collider (Opcional)")]
    [SerializeField] private MeshCollider meshCollider;
    [SerializeField] private float colliderRadius = 0.5f;
    [SerializeField] private int colliderSegments = 20;

    private SplineContainer splineContainer;
    private Spline spline;

    private void Awake()
    {
        splineContainer = GetComponent<SplineContainer>();
        // Garante que a camera cinematografica esteja desativada ao iniciar
        if (cinematicCamera != null)
        {
            cinematicCamera.SetActive(false);
        }
        if (splineContainer != null && splineContainer.Splines.Count > 0)
        {
            spline = splineContainer.Splines[0];
        }
    }

    private void OnValidate()
    {
        if (splineContainer == null)
            splineContainer = GetComponent<SplineContainer>();

        if (splineContainer == null || splineContainer.Splines.Count == 0)
        {
            Debug.LogWarning("Rail '" + gameObject.name + "' nao possui SplineContainer ou spline configurado!");
        }
    }

    /// <summary>
    /// Obtem a posicao em um ponto normalizado do spline (0-1)
    /// </summary>
    public Vector3 GetPositionAtT(float t)
    {
        if (spline == null) return transform.position;

        t = Mathf.Clamp01(t);
        float3 localPos = SplineUtility.EvaluatePosition(spline, t);
        return transform.TransformPoint(localPos);
    }

    /// <summary>
    /// Obtem a tangente (direcao) em um ponto normalizado do spline (0-1)
    /// </summary>
    public Vector3 GetTangentAtT(float t)
    {
        if (spline == null) return Vector3.forward;

        t = Mathf.Clamp01(t);
        float3 localTangent = SplineUtility.EvaluateTangent(spline, t);
        return transform.TransformDirection(localTangent).normalized;
    }

    /// <summary>
    /// Obtem a posicao mais proxima no spline a partir de uma posicao no espaco
    /// </summary>
    public float GetClosestT(Vector3 worldPosition)
    {
        if (spline == null) return 0f;

        float3 localPos = transform.InverseTransformPoint(worldPosition);
        SplineUtility.GetNearestPoint(spline, localPos, out float3 nearest, out float t);
        return t;
    }

    /// <summary>
    /// Obtem a distancia ate o ponto mais proximo no spline
    /// </summary>
    public float GetDistanceToSpline(Vector3 worldPosition)
    {
        if (spline == null) return float.MaxValue;

        float t = GetClosestT(worldPosition);
        Vector3 closestPoint = GetPositionAtT(t);
        return Vector3.Distance(worldPosition, closestPoint);
    }

    /// <summary>
    /// Determina a direcao do movimento baseado na velocidade do jogador
    /// </summary>
    public bool GetMovementDirection(Vector3 position, Vector3 velocity)
    {
        if (spline == null) return true;

        float t = GetClosestT(position);
        Vector3 tangent = GetTangentAtT(t);
        
        return Vector3.Dot(velocity.normalized, tangent) >= 0;
    }

    /// <summary>
    /// Calcula a curvatura do spline em um ponto (para inclinacao)
    /// </summary>
    public float GetCurvatureAtT(float t)
    {
        if (spline == null) return 0f;

        t = Mathf.Clamp01(t);
        
        // Calcula curvatura usando tangentes proximas
        float delta = 0.01f;
        float t1 = Mathf.Max(0, t - delta);
        float t2 = Mathf.Min(1, t + delta);
        
        Vector3 tangent1 = GetTangentAtT(t1);
        Vector3 tangent2 = GetTangentAtT(t2);
        
        float angle = Vector3.SignedAngle(tangent1, tangent2, Vector3.up);
        return angle / (t2 - t1);
    }

    /// <summary>
    /// Obtem o comprimento total do spline
    /// </summary>
    public float GetSplineLength()
    {
        if (spline == null) return 0f;
        return spline.GetLength();
    }

    /// <summary>
    /// Avanca no spline por uma distancia especifica
    /// </summary>
    public float AdvanceByDistance(float currentT, float distance, bool forward = true)
    {
        if (spline == null) return currentT;

        float splineLength = GetSplineLength();
        if (splineLength <= 0) return currentT;

        float deltaT = distance / splineLength;
        if (!forward) deltaT = -deltaT;

        float newT = currentT + deltaT;

        if (isLoop)
        {
            // Em loops, faz wrap around
            while (newT > 1f) newT -= 1f;
            while (newT < 0f) newT += 1f;
        }
        else
        {
            // Em rails normais, clamp
            newT = Mathf.Clamp01(newT);
        }

        return newT;
    }

    /// <summary>
    /// Verifica se chegou ao fim do spline
    /// </summary>
    public bool IsAtEnd(float t, bool movingForward)
    {
        if (isLoop) return false; // Loops nunca terminam

        if (movingForward)
            return t >= 0.99f;
        else
            return t <= 0.01f;
    }

    // Propriedades publicas
    public float RecommendedSpeed { get { return recommendedSpeed; } }
    public bool IsLoop { get { return isLoop; } }
    public Rail_SonicStyle_Spline LeftRail { get { return leftRail; } }
    public Rail_SonicStyle_Spline RightRail { get { return rightRail; } }
    public float AdjacentRailDistance { get { return adjacentRailDistance; } }

    public bool HasAdjacentRail(bool isLeft)
    {
        return isLeft ? leftRail != null : rightRail != null;
    }

    /// <summary>
    /// Verifica se a camera cinematografica deve ser ativada/desativada
    /// </summary>
    public void CheckCinematicCamera(float currentT, bool movingForward)
    {
        if (!enableCinematicCamera || isLoop) return;

        // Verifica se esta perto do fim (1.0) ou do inicio (0.0)
        bool nearEnd = movingForward ? currentT >= cinematicCameraDistance : currentT <= (1f - cinematicCameraDistance);
        
        // Adiciona uma verificacao para garantir que nao estamos em transicao
        if (CameraRailManager.Instance != null && CameraRailManager.Instance.IsTransitioning())
        {
            return;
        }

        // Verifica se o cooldown ainda esta ativo antes de tentar ativar a camera
        bool isCooldownActive = Time.time < lastCameraDeactivationTime + cameraCooldownDuration;
        
        if (nearEnd && !isCinematicCameraActive && !isCooldownActive)
        {
            ActivateCinematicCamera();
        }
        else if (!nearEnd && isCinematicCameraActive)
        {
            DeactivateCinematicCamera();
        }
    }

    /// <summary>
    /// Ativa a camera cinematografica e desativa a camera padrao
    /// AGORA INICIA A TRANSICAO SUAVE COM TEMPO INDIVIDUAL
    /// </summary>
    public void ActivateCinematicCamera()
    {
        if (cinematicCamera == null || isCinematicCameraActive) return;

        if (CameraRailManager.Instance != null)
        {
            // Inicia a transicao suave para a camera do rail usando o tempo deste rail
            CameraRailManager.Instance.StartTransitionToRail(cinematicCamera, transitionInDuration);
        }
        else
        {
            // Fallback para a logica original (troca instantanea)
            if (Camera.main != null)
            {
                Camera.main.gameObject.SetActive(false);
            }
            cinematicCamera.SetActive(true);
        }
        
        isCinematicCameraActive = true;
    }

    /// <summary>
    /// Desativa a camera cinematografica e ativa a camera padrao
    /// AGORA INICIA A TRANSICAO SUAVE DE VOLTA COM TEMPO INDIVIDUAL
    /// </summary>
    public void DeactivateCinematicCamera()
    {
        if (cinematicCamera == null || !isCinematicCameraActive) return;

        if (CameraRailManager.Instance != null)
        {
            // Inicia a transicao suave de volta para a camera principal usando o tempo deste rail
            CameraRailManager.Instance.StartTransitionToMain(Camera.main.transform, cinematicCamera, transitionOutDuration);
        }
        else
        {
            // Fallback para a logica original (troca instantanea)
            if (Camera.main != null)
            {
                Camera.main.gameObject.SetActive(true);
            }
            cinematicCamera.SetActive(false);
        }
        
        isCinematicCameraActive = false;
        lastCameraDeactivationTime = Time.time;
        
        // Desativa o flag para que a camera cinematografica nao seja ativada novamente
        // a menos que seja reativada manualmente no Inspector.
        enableCinematicCamera = false;
    }

    public Rail_SonicStyle_Spline GetAdjacentRail(bool isLeft)
    {
        return isLeft ? leftRail : rightRail;
    }
}
