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

    [Header("Mesh Collider (Opcional)")]
    [SerializeField] private MeshCollider meshCollider;
    [SerializeField] private float colliderRadius = 0.5f;
    [SerializeField] private int colliderSegments = 20;

    private SplineContainer splineContainer;
    private Spline spline;

    private void Awake()
    {
        splineContainer = GetComponent<SplineContainer>();
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

    public Rail_SonicStyle_Spline GetAdjacentRail(bool isLeft)
    {
        return isLeft ? leftRail : rightRail;
    }

    /// <summary>
    /// Gera mesh collider automaticamente baseado no spline
    /// </summary>
    [ContextMenu("Generate Mesh Collider")]
    public void GenerateMeshCollider()
    {
        if (spline == null)
        {
            Debug.LogError("Spline nao encontrado!");
            return;
        }

        if (meshCollider == null)
        {
            meshCollider = GetComponent<MeshCollider>();
            if (meshCollider == null)
            {
                meshCollider = gameObject.AddComponent<MeshCollider>();
            }
        }

        Mesh mesh = GenerateTubeMesh();
        meshCollider.sharedMesh = mesh;
        meshCollider.convex = false;
        meshCollider.isTrigger = true;

        Debug.Log("Mesh Collider gerado para '" + gameObject.name + "' com " + colliderSegments + " segmentos");
    }

    private Mesh GenerateTubeMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "RailCollider_" + gameObject.name;

        int segments = colliderSegments;
        int sides = 8; // Octogono para o tubo
        int vertexCount = segments * sides;
        int triangleCount = (segments - 1) * sides * 6;

        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[triangleCount];

        // Gera vertices ao longo do spline
        for (int i = 0; i < segments; i++)
        {
            float t = (float)i / (segments - 1);
            Vector3 position = GetPositionAtT(t);
            Vector3 tangent = GetTangentAtT(t);
            
            // Cria circulo perpendicular ao spline
            Vector3 normal = Vector3.Cross(tangent, Vector3.up);
            if (normal.magnitude < 0.1f)
                normal = Vector3.Cross(tangent, Vector3.right);
            normal.Normalize();
            
            Vector3 binormal = Vector3.Cross(tangent, normal).normalized;

            for (int j = 0; j < sides; j++)
            {
                float angle = (float)j / sides * Mathf.PI * 2f;
                Vector3 offset = (Mathf.Cos(angle) * normal + Mathf.Sin(angle) * binormal) * colliderRadius;
                
                int vertexIndex = i * sides + j;
                vertices[vertexIndex] = transform.InverseTransformPoint(position + offset);
            }
        }

        // Gera triangulos
        int triIndex = 0;
        for (int i = 0; i < segments - 1; i++)
        {
            for (int j = 0; j < sides; j++)
            {
                int current = i * sides + j;
                int next = i * sides + (j + 1) % sides;
                int currentNext = (i + 1) * sides + j;
                int nextNext = (i + 1) * sides + (j + 1) % sides;

                // Primeiro triangulo
                triangles[triIndex++] = current;
                triangles[triIndex++] = currentNext;
                triangles[triIndex++] = next;

                // Segundo triangulo
                triangles[triIndex++] = next;
                triangles[triIndex++] = currentNext;
                triangles[triIndex++] = nextNext;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    private void OnDrawGizmos()
    {
        if (spline == null && splineContainer != null && splineContainer.Splines.Count > 0)
        {
            spline = splineContainer.Splines[0];
        }

        if (spline == null) return;

        Gizmos.color = railColor;

        // Desenha o spline
        Vector3 previousPoint = GetPositionAtT(0);
        for (int i = 1; i <= visualizationSegments; i++)
        {
            float t = (float)i / visualizationSegments;
            Vector3 currentPoint = GetPositionAtT(t);
            Gizmos.DrawLine(previousPoint, currentPoint);
            previousPoint = currentPoint;
        }

        // Desenha inicio e fim
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(GetPositionAtT(0), 0.3f);
        
        if (!isLoop)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(GetPositionAtT(1), 0.3f);
        }

        // Desenha conexoes com rails adjacentes
        if (showAdjacentConnections)
        {
            if (leftRail != null)
            {
                Gizmos.color = Color.cyan;
                Vector3 startPos = GetPositionAtT(0);
                Vector3 leftPos = leftRail.GetPositionAtT(0);
                Gizmos.DrawLine(startPos, leftPos);
            }

            if (rightRail != null)
            {
                Gizmos.color = Color.magenta;
                Vector3 startPos = GetPositionAtT(0);
                Vector3 rightPos = rightRail.GetPositionAtT(0);
                Gizmos.DrawLine(startPos, rightPos);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (spline == null) return;

        // Desenha setas indicando direcao
        Gizmos.color = Color.white;
        for (int i = 0; i < 10; i++)
        {
            float t = (float)i / 10f + 0.05f;
            Vector3 position = GetPositionAtT(t);
            Vector3 tangent = GetTangentAtT(t);
            DrawArrow(position, tangent, 0.5f);
        }

#if UNITY_EDITOR
        // Label com informacoes
        Vector3 startPos = GetPositionAtT(0);
        string info = gameObject.name + "\n";
        info += "Comprimento: " + GetSplineLength().ToString("F1") + "m\n";
        info += "Velocidade: " + recommendedSpeed.ToString("F1") + "\n";
        info += "Loop: " + (isLoop ? "Sim" : "Nao");
        
        UnityEditor.Handles.Label(startPos + Vector3.up * 0.5f, info);
#endif
    }

    private void DrawArrow(Vector3 position, Vector3 direction, float size)
    {
        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + 20, 0) * Vector3.forward;
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - 20, 0) * Vector3.forward;
        
        Gizmos.DrawRay(position, right * size);
        Gizmos.DrawRay(position, left * size);
    }
}
