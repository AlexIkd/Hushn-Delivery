using UnityEngine;

/// <summary>
/// Sistema de motion blur usando Particle System nativo da Unity
/// Cria partículas que seguem o jogador para simular motion blur
/// </summary>
public class PlayerMotionBlurParticles : MonoBehaviour
{
    [Header("Configurações de Motion Blur")]
    [SerializeField] private bool enableMotionBlur = true;
    [SerializeField] private float particleLifetime = 0.2f; // Duração das partículas
    [SerializeField] private float emissionRate = 50f; // Partículas por segundo
    [SerializeField] private float particleSize = 1f; // Tamanho das partículas
    [SerializeField] private Color particleStartColor = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField] private Color particleEndColor = new Color(1f, 1f, 1f, 0f);
    
    [Header("Referências")]
    [SerializeField] private SkinnedMeshRenderer[] playerMeshes;
    
    // Estado interno
    private bool isBlurActive = false;
    private ParticleSystem particleSystem;
    private ParticleSystemRenderer particleRenderer;
    
    void Start()
    {
        // Encontra meshes automaticamente se não especificado
        if (playerMeshes == null || playerMeshes.Length == 0)
        {
            playerMeshes = GetComponentsInChildren<SkinnedMeshRenderer>();
        }
        
        // Cria o particle system
        CreateParticleSystem();
        
        // Desativa inicialmente
        DisableBlur();
    }
    
    /// <summary>
    /// Cria o particle system
    /// </summary>
    private void CreateParticleSystem()
    {
        GameObject psObj = new GameObject("MotionBlur_Particles");
        psObj.transform.SetParent(transform);
        psObj.transform.localPosition = Vector3.zero;
        psObj.transform.localRotation = Quaternion.identity;
        
        particleSystem = psObj.AddComponent<ParticleSystem>();
        particleRenderer = psObj.GetComponent<ParticleSystemRenderer>();
        
        // Configura o módulo principal
        var main = particleSystem.main;
        main.startLifetime = particleLifetime;
        main.startSpeed = 0f; // Partículas ficam paradas onde são criadas
        main.startSize = particleSize;
        main.startColor = particleStartColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World; // World space para não seguir o jogador
        main.maxParticles = 1000;
        main.loop = true;
        
        // Configura emissão
        var emission = particleSystem.emission;
        emission.enabled = true;
        emission.rateOverTime = emissionRate;
        
        // Configura cor ao longo da vida
        var colorOverLifetime = particleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(particleStartColor, 0.0f), 
                new GradientColorKey(particleEndColor, 1.0f) 
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(particleStartColor.a, 0.0f), 
                new GradientAlphaKey(0f, 1.0f) 
            }
        );
        
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
        
        // Configura tamanho ao longo da vida (diminui)
        var sizeOverLifetime = particleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(1f, 0.5f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        // Configura o renderer
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        particleRenderer.receiveShadows = false;
        
        // Tenta usar mesh do jogador como partícula
        if (playerMeshes != null && playerMeshes.Length > 0)
        {
            particleRenderer.renderMode = ParticleSystemRenderMode.Mesh;
            
            // Bake o primeiro mesh para usar como partícula
            Mesh bakedMesh = new Mesh();
            playerMeshes[0].BakeMesh(bakedMesh);
            particleRenderer.mesh = bakedMesh;
        }
        
        // Cria material
        CreateParticleMaterial();
    }
    
    /// <summary>
    /// Cria o material para as partículas
    /// </summary>
    private void CreateParticleMaterial()
    {
        Shader shader = Shader.Find("Particles/Standard Unlit");
        
        if (shader == null)
        {
            shader = Shader.Find("Mobile/Particles/Alpha Blended");
        }
        
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Transparent");
        }
        
        if (shader != null)
        {
            Material mat = new Material(shader);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;
            
            particleRenderer.material = mat;
        }
    }
    
    /// <summary>
    /// Ativa o motion blur
    /// </summary>
    public void EnableBlur()
    {
        if (!enableMotionBlur) return;
        
        isBlurActive = true;
        
        if (particleSystem != null)
        {
            particleSystem.Play();
        }
    }
    
    /// <summary>
    /// Desativa o motion blur
    /// </summary>
    public void DisableBlur()
    {
        isBlurActive = false;
        
        if (particleSystem != null)
        {
            particleSystem.Stop();
            particleSystem.Clear();
        }
    }
    
    /// <summary>
    /// Define a intensidade do blur dinamicamente
    /// </summary>
    public void SetBlurIntensity(float intensity)
    {
        intensity = Mathf.Clamp01(intensity);
        
        Color startColor = particleStartColor;
        startColor.a = intensity;
        
        var main = particleSystem.main;
        main.startColor = startColor;
    }
    
    /// <summary>
    /// Define a taxa de emissão dinamicamente
    /// </summary>
    public void SetEmissionRate(float rate)
    {
        var emission = particleSystem.emission;
        emission.rateOverTime = rate;
    }
    
    void OnDestroy()
    {
        if (particleRenderer != null && particleRenderer.material != null)
        {
            Destroy(particleRenderer.material);
        }
    }
}
