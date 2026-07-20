using UnityEngine;
using System.Collections.Generic;

public class SpeedTrailEffect : MonoBehaviour
{
    [Header("Configurações Artísticas (Estilo Sunset Overdrive)")]
    [SerializeField] private List<TrailRenderer> trailRenderers;
    
    [Tooltip("Duração do rastro. Sunset Overdrive usa rastros curtos e rápidos.")]
    [SerializeField] private float trailTime = 0.3f;

    [Tooltip("Curva de largura: Use isso para fazer o rastro começar grosso e terminar em ponta fina (estilo pincel).")]
    [SerializeField] private AnimationCurve widthCurve = AnimationCurve.Linear(0, 1, 1, 0);

    [Tooltip("Gradiente de cor: Use cores vibrantes como Laranja, Roxo ou Neon.")]
    [SerializeField] private Gradient trailGradient;

    [Header("Efeito de Movimento")]
    [Tooltip("Distância mínima que o jogador deve percorrer para gerar rastro (evita rastro parado).")]
    [SerializeField] private float minVertexDistance = 0.1f;

    private bool isTrailActive = false;

    void Awake()
    {
        if (trailRenderers == null || trailRenderers.Count == 0)
        {
            trailRenderers = new List<TrailRenderer>(GetComponentsInChildren<TrailRenderer>());
        }

        SetupTrails();
    }

    private void SetupTrails()
    {
        foreach (TrailRenderer tr in trailRenderers)
        {
            tr.time = 0f;
            tr.widthCurve = widthCurve;
            tr.colorGradient = trailGradient;
            tr.minVertexDistance = minVertexDistance;
            tr.emitting = false;
            
            // Configurações importantes para o visual:
            tr.alignment = LineAlignment.View; // Faz o rastro sempre encarar a câmera
            tr.textureMode = LineTextureMode.Stretch; // Estica a textura ao longo do rastro
        }
    }

    public void StartTrail()
    {
        if (!isTrailActive)
        {
            foreach (TrailRenderer tr in trailRenderers)
            {
                tr.Clear(); // Limpa rastros antigos
                tr.emitting = true;
                tr.time = trailTime;
            }
            isTrailActive = true;
        }
    }

    public void StopTrail()
    {
        if (isTrailActive)
        {
            foreach (TrailRenderer tr in trailRenderers)
            {
                tr.emitting = false;
            }
            isTrailActive = false;
        }
    }
}
