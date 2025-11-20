using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Sistema de trail estilizado de anime com linhas de velocidade
/// Cria efeitos visuais característicos de anime/mangá durante movimentos rápidos
/// </summary>
public class PlayerAnimeSpeedLines : MonoBehaviour
{
    [Header("Configurações de Linhas de Velocidade")]
    [SerializeField] private bool enableEffect = true;
    [SerializeField] private int lineCount = 8; // Número de linhas de velocidade
    [SerializeField] private float lineLength = 3f; // Comprimento das linhas
    [SerializeField] private float lineWidth = 0.15f; // Largura das linhas
    [SerializeField] private float lineDuration = 0.3f; // Duração das linhas
    [SerializeField] private float lineSpread = 2f; // Distância lateral das linhas
    
    [Header("Estilo Visual")]
    [SerializeField] private Color lineColor = new Color(0.3f, 0.7f, 1f, 0.8f); // Azul vibrante
    [SerializeField] private bool useGradient = true; // Usar gradiente de cor
    [SerializeField] private AnimationCurve widthCurve = AnimationCurve.Linear(0, 1, 1, 0.2f); // Curva de largura
    [SerializeField] private bool randomizeLines = true; // Randomizar posição das linhas
    
    [Header("Partículas de Impacto")]
    [SerializeField] private bool enableImpactParticles = true;
    [SerializeField] private int particleCount = 20; // Partículas por burst
    [SerializeField] private float particleSize = 0.3f;
    [SerializeField] private Color particleColor = new Color(1f, 1f, 1f, 0.9f);
    
    // Estado interno
    private bool isEffectActive = false;
    private List<LineRenderer> speedLines = new List<LineRenderer>();
    private ParticleSystem impactParticles;
    private Material lineMaterial;
    private float[] lineOffsets; // Offsets aleatórios para cada linha
    
    void Start()
    {
        CreateLineMaterial();
        CreateSpeedLines();
        
        if (enableImpactParticles)
        {
            CreateImpactParticles();
        }
        
        DisableEffect();
    }
    
    /// <summary>
    /// Cria o material para as linhas
    /// </summary>
    private void CreateLineMaterial()
    {
        Shader shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        
        if (shader != null)
        {
            lineMaterial = new Material(shader);
            lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One); // Additive blending
            lineMaterial.SetInt("_ZWrite", 0);
            lineMaterial.renderQueue = 3000;
            lineMaterial.color = lineColor;
        }
    }
    
    /// <summary>
    /// Cria as linhas de velocidade
    /// </summary>
    private void CreateSpeedLines()
    {
        lineOffsets = new float[lineCount];
        
        for (int i = 0; i < lineCount; i++)
        {
            GameObject lineObj = new GameObject($"SpeedLine_{i}");
            lineObj.transform.SetParent(transform);
            lineObj.transform.localPosition = Vector3.zero;
            
            LineRenderer line = lineObj.AddComponent<LineRenderer>();
            
            // Configurações básicas
            line.positionCount = 2;
            line.startWidth = lineWidth;
            line.endWidth = lineWidth * 0.2f;
            line.widthCurve = widthCurve;
            
            // Material e cor
            line.material = lineMaterial;
            
            // Gradiente de cor
            if (useGradient)
            {
                Gradient gradient = new Gradient();
                gradient.SetKeys(
                    new GradientColorKey[] { 
                        new GradientColorKey(lineColor, 0f),
                        new GradientColorKey(Color.white, 0.3f),
                        new GradientColorKey(lineColor, 1f)
                    },
                    new GradientAlphaKey[] { 
                        new GradientAlphaKey(0f, 0f),
                        new GradientAlphaKey(lineColor.a, 0.2f),
                        new GradientAlphaKey(0f, 1f)
                    }
                );
                line.colorGradient = gradient;
            }
            
            // Sombras
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            
            // Alinhamento
            line.alignment = LineAlignment.View;
            
            // Offset aleatório
            if (randomizeLines)
            {
                lineOffsets[i] = Random.Range(-lineSpread, lineSpread);
            }
            else
            {
                // Distribui uniformemente
                float t = (float)i / (lineCount - 1);
                lineOffsets[i] = Mathf.Lerp(-lineSpread, lineSpread, t);
            }
            
            speedLines.Add(line);
        }
    }
    
    /// <summary>
    /// Cria o sistema de partículas de impacto
    /// </summary>
    private void CreateImpactParticles()
    {
        GameObject psObj = new GameObject("ImpactParticles");
        psObj.transform.SetParent(transform);
        psObj.transform.localPosition = Vector3.zero;
        
        impactParticles = psObj.AddComponent<ParticleSystem>();
        
        var main = impactParticles.main;
        main.startLifetime = 0.4f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 6f);
        main.startSize = particleSize;
        main.startColor = particleColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 100;
        main.loop = false;
        
        var emission = impactParticles.emission;
        emission.enabled = false; // Controlado manualmente
        
        var shape = impactParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 25f;
        shape.radius = 0.5f;
        
        var colorOverLifetime = impactParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(particleColor, 0f),
                new GradientColorKey(particleColor, 1f)
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(particleColor.a, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
        
        var sizeOverLifetime = impactParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        var renderer = impactParticles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = lineMaterial;
    }
    
    /// <summary>
    /// Ativa o efeito de linhas de velocidade
    /// </summary>
    public void EnableEffect()
    {
        if (!enableEffect) return;
        
        isEffectActive = true;
        
        // Ativa as linhas
        foreach (LineRenderer line in speedLines)
        {
            line.enabled = true;
        }
        
        // Emite partículas de impacto inicial
        if (enableImpactParticles && impactParticles != null)
        {
            impactParticles.Emit(particleCount);
        }
        
        // Inicia a animação das linhas
        StartCoroutine(AnimateSpeedLines());
    }
    
    /// <summary>
    /// Desativa o efeito
    /// </summary>
    public void DisableEffect()
    {
        isEffectActive = false;
        
        foreach (LineRenderer line in speedLines)
        {
            line.enabled = false;
        }
        
        StopAllCoroutines();
    }
    
    /// <summary>
    /// Anima as linhas de velocidade
    /// </summary>
    private System.Collections.IEnumerator AnimateSpeedLines()
    {
        float elapsed = 0f;
        
        while (isEffectActive && elapsed < lineDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / lineDuration;
            
            // Atualiza cada linha
            for (int i = 0; i < speedLines.Count; i++)
            {
                UpdateSpeedLine(speedLines[i], i, t);
            }
            
            yield return null;
        }
        
        // Fade out suave
        float fadeTime = 0.1f;
        float fadeElapsed = 0f;
        
        while (fadeElapsed < fadeTime)
        {
            fadeElapsed += Time.deltaTime;
            float alpha = 1f - (fadeElapsed / fadeTime);
            
            foreach (LineRenderer line in speedLines)
            {
                Color color = line.material.color;
                color.a = lineColor.a * alpha;
                line.material.color = color;
            }
            
            yield return null;
        }
        
        DisableEffect();
    }
    
    /// <summary>
    /// Atualiza a posição de uma linha de velocidade
    /// </summary>
    private void UpdateSpeedLine(LineRenderer line, int index, float t)
    {
        // Posição inicial (atrás do jogador)
        Vector3 backward = -transform.forward;
        Vector3 right = transform.right;
        
        // Offset lateral
        float lateralOffset = lineOffsets[index];
        
        // Posição de início da linha (atrás do jogador)
        float distanceBehind = Mathf.Lerp(1f, lineLength, t);
        Vector3 startPos = transform.position + backward * distanceBehind + right * lateralOffset;
        
        // Posição de fim da linha (mais atrás ainda)
        Vector3 endPos = startPos + backward * lineLength * 0.5f;
        
        // Adiciona variação vertical
        float verticalOffset = Mathf.Sin(t * Mathf.PI + index) * 0.5f;
        startPos.y += verticalOffset;
        endPos.y += verticalOffset;
        
        // Atualiza as posições
        line.SetPosition(0, startPos);
        line.SetPosition(1, endPos);
    }
    
    void OnDestroy()
    {
        if (lineMaterial != null)
        {
            Destroy(lineMaterial);
        }
    }
}
