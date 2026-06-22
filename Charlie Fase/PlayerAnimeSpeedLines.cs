using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerAnimeSpeedLines : MonoBehaviour
{
    [Header("Configurações Gerais")]
    [SerializeField] private bool enableEffect = true;
    [ColorUsage(true, true)] [SerializeField] private Color startColor = new Color(0.1f, 0.6f, 1f, 1f) * 5f;
    [ColorUsage(true, true)] [SerializeField] private Color endColor = new Color(0.5f, 0.1f, 0.8f, 0.5f) * 3f;
    [SerializeField] private float effectDuration = 0.3f;

    [Header("Rastro de Silhuetas (Ghost Trail)")]
    [SerializeField] private bool enableGhostTrail = true;
    [SerializeField] private float ghostLifetime = 0.5f;
    [SerializeField] private float ghostSpawnInterval = 0.05f;
    [SerializeField] private Material ghostMaterial;
    [ColorUsage(true, true)] [SerializeField] private Color ghostStartColor = new Color(0f, 0.5f, 1f, 0.5f);
    [ColorUsage(true, true)] [SerializeField] private Color ghostEndColor = new Color(0.5f, 0f, 1f, 0f);

    [Header("Rastro Contínuo (Particles)")]
    [SerializeField] private float trailLifetime = 0.4f;
    [SerializeField] private float trailStartSize = 0.1f;
    [SerializeField] private float trailEmissionRate = 100f;
    
    [Header("Partículas de Impacto (Burst)")]
    [SerializeField] private int burstCount = 30;
    [SerializeField] private float burstSpeed = 5f;
    [SerializeField] private float burstLifetime = 0.6f;

    private ParticleSystem trailParticleSystem;
    private ParticleSystem burstParticleSystem;
    private Material particleMaterial;
    private SkinnedMeshRenderer[] meshRenderers;
    private float lastGhostSpawnTime;
    private bool isGhostActive = false;

    private List<GameObject> activeGhosts = new List<GameObject>();

    void Start()
    {
        if (!enableEffect) return;
        
        CreateParticleMaterial();
        CreateTrailParticleSystem();
        CreateBurstParticleSystem();

        // Garante que ghostMaterial não seja nulo
        if (ghostMaterial == null)
        {
            Shader ghostShader = Shader.Find("Standard"); // Shader padrão para o ghost
            if (ghostShader == null) ghostShader = Shader.Find("Unlit/Color");
            ghostMaterial = new Material(ghostShader);
            ghostMaterial.color = ghostStartColor; // Define uma cor inicial para o fallback
        }
        
        isGhostActive = false;
        meshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
    }

    void Update()
    {
        if (isGhostActive && Time.time - lastGhostSpawnTime >= ghostSpawnInterval)
        {
            SpawnGhost();
            lastGhostSpawnTime = Time.time;
        }
    }

    private void SpawnGhost()
    {
        if (!enableGhostTrail || ghostMaterial == null || meshRenderers == null) return;

        foreach (var smr in meshRenderers)
        {
            if (smr == null || !smr.gameObject.activeInHierarchy) continue;

            GameObject ghostObj = new GameObject("GhostFrame");
            ghostObj.transform.SetPositionAndRotation(smr.transform.position, smr.transform.rotation);
            ghostObj.transform.localScale = Vector3.one; 

            MeshFilter mf = ghostObj.AddComponent<MeshFilter>();
            MeshRenderer mr = ghostObj.AddComponent<MeshRenderer>();

            Mesh mesh = new Mesh();
            smr.BakeMesh(mesh);
            mf.mesh = mesh;

            Material instanceMat = new Material(ghostMaterial);
            instanceMat.SetColor("_Color", ghostStartColor);
            if (instanceMat.HasProperty("_BaseColor")) instanceMat.SetColor("_BaseColor", ghostStartColor);
            instanceMat.SetFloat("_Alpha", 1.0f);
            mr.material = instanceMat;

            activeGhosts.Add(ghostObj);
            StartCoroutine(FadeGhost(ghostObj, mr, mesh, instanceMat));
        }
    }

    private IEnumerator FadeGhost(GameObject ghostObj, MeshRenderer renderer, Mesh mesh, Material mat)
    {
        float elapsed = 0;
        while (elapsed < ghostLifetime)
        {
            if (mat == null || ghostObj == null) yield break;
            
            elapsed += Time.deltaTime;
            float t = elapsed / ghostLifetime;

            Color currentColor = Color.Lerp(ghostStartColor, ghostEndColor, t);
            
            mat.SetColor("_Color", currentColor);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", currentColor);
            if (mat.HasProperty("_Alpha")) mat.SetFloat("_Alpha", currentColor.a);
            
            yield return null;
        }

        CleanupGhost(ghostObj, mesh, mat);
    }

    private void CleanupGhost(GameObject ghostObj, Mesh mesh, Material mat)
    {
        if (activeGhosts.Contains(ghostObj)) activeGhosts.Remove(ghostObj);
        
        if (ghostObj != null) Destroy(ghostObj);
        if (mat != null) Destroy(mat);
        if (mesh != null) Destroy(mesh);
    }

    private void CreateParticleMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Additive");

        if (shader != null)
        {
            particleMaterial = new Material(shader);
        }
        else
        {
            // Fallback para um material básico se nenhum shader for encontrado
            particleMaterial = new Material(Shader.Find("Unlit/Color"));
        }
        particleMaterial.color = startColor;
    }

    private void CreateTrailParticleSystem()
    {
        GameObject psObj = new GameObject("TrailPS");
        psObj.transform.SetParent(transform);
        psObj.transform.localPosition = Vector3.zero;
        
        trailParticleSystem = psObj.AddComponent<ParticleSystem>();
        var main = trailParticleSystem.main;
        main.playOnAwake = false;
        main.startLifetime = trailLifetime;
        main.startSpeed = 0f;
        main.startSize = trailStartSize;
        main.startColor = startColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        
        var colorOverLifetime = trailParticleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(new GradientColorKey[] { new GradientColorKey(startColor, 0.0f), new GradientColorKey(endColor, 1.0f) }, new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        var emission = trailParticleSystem.emission;
        emission.enabled = false;
        emission.rateOverTime = trailEmissionRate;

        var renderer = trailParticleSystem.GetComponent<ParticleSystemRenderer>();
        renderer.material = particleMaterial;
        
        trailParticleSystem.Stop();
    }

    private void CreateBurstParticleSystem()
    {
        GameObject psObj = new GameObject("BurstPS");
        psObj.transform.SetParent(transform);
        psObj.transform.localPosition = Vector3.zero;
        
        burstParticleSystem = psObj.AddComponent<ParticleSystem>();
        var main = burstParticleSystem.main;
        main.playOnAwake = false;
        main.startLifetime = burstLifetime;
        main.startSpeed = burstSpeed;
        main.startSize = trailStartSize * 2f;
        main.startColor = startColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = false;
        
        var colorOverLifetime = burstParticleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient burstGradient = new Gradient();
        burstGradient.SetKeys(new GradientColorKey[] { new GradientColorKey(startColor, 0.0f), new GradientColorKey(endColor, 1.0f) }, new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(burstGradient);

        var emission = burstParticleSystem.emission;
        emission.enabled = false;

        var renderer = burstParticleSystem.GetComponent<ParticleSystemRenderer>();
        renderer.material = particleMaterial;
        
        burstParticleSystem.Stop();
    }

    public void EnableEffect(Vector3 direction)
    {
        if (!enableEffect) return;
        
        // Ativa partículas contínuas (Trail)
        if (trailParticleSystem != null) {
            var emission = trailParticleSystem.emission;
            emission.enabled = true;
            trailParticleSystem.Play();
        }
        
        // REMOVIDO: burstParticleSystem.Emit(burstCount) do início
        
        isGhostActive = true;
        StartCoroutine(DisableEffectAfterDuration(effectDuration));
    }

    public void DisableEffect()
    {
        // Desativa partículas contínuas
        if (trailParticleSystem != null) {
            var emission = trailParticleSystem.emission;
            emission.enabled = false;
        }
        
        // MANTIDO: Burst de partículas apenas no final do efeito
        if (burstParticleSystem != null) burstParticleSystem.Emit(burstCount);
        
        isGhostActive = false;
    }

    private IEnumerator DisableEffectAfterDuration(float duration)
    {
        yield return new WaitForSeconds(duration);
        DisableEffect();
    }

    void OnDestroy()
    {
        if (particleMaterial != null) Destroy(particleMaterial);
        
        foreach (var ghost in activeGhosts)
        {
            if (ghost != null) Destroy(ghost);
        }
        activeGhosts.Clear();
    }
}
