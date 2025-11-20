using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Gerencia a exibição visual do ranking de estilo (D, C, B, A, S) no Unity.
/// Implementa o preenchimento em tempo real baseado no score, o desaparecimento do 'D' por opacidade
/// e a lógica de exibição de um efeito visual (pulsação) no estado "Rank Up Hold".
/// </summary>
public class StyleRankUI : MonoBehaviour
{
    [System.Serializable]
    public class RankVisuals
    {
        public StyleRankSystem.StyleRank rank;
        [Tooltip("O objeto pai que contém o contorno e o preenchimento.")]
        public GameObject rankContainer;
        [Tooltip("A imagem que representa o preenchimento do ranking. Deve ser do tipo 'Filled'.")]
        public Image fillImage;
        [Tooltip("A imagem que representa o contorno do ranking.")]
        public Image outlineImage;
    }

    [Header("Configurações de UI")]
    [Tooltip("A imagem que deve acompanhar a visibilidade do 'rank D'.")]
    public Image rankDImageToBlink;
    [Tooltip("Lista de visuais para cada ranking (D, C, B, A, S).")]
    public List<RankVisuals> rankVisuals = new List<RankVisuals>();
    
    [Header("Configurações de Animação")]
    [Tooltip("Pontuação máxima do sistema de ranking (deve ser a mesma do StyleRankSystem).")]
    public float maxScore = 500f;
    [Tooltip("Pontuação limite para o desaparecimento do 'D' começar (ex: 10% do score para o próximo rank).")]
    public float dFadeThreshold = 10f;
    [Tooltip("Taxa de suavização para o alpha do 'D' (maior valor = mais rápido).")]
    public float dFadeSmoothSpeed = 5f;

    [Header("Efeito Rank Up Hold (Pulsação)")]
    [Tooltip("A cor que o ranking irá pulsar durante o Rank Up Hold.")]
    public Color pulseColor = Color.white;
    [Tooltip("A velocidade da pulsação (frequência).")]
    public float pulseSpeed = 5f;
    [Tooltip("O valor mínimo de alpha durante a pulsação.")]
    [Range(0f, 1f)] public float minPulseAlpha = 0.5f;

    private StyleRankSystem.StyleRank currentDisplayedRank = StyleRankSystem.StyleRank.D;
    private Dictionary<StyleRankSystem.StyleRank, RankVisuals> rankMap;
    private StyleRankSystem rankSystem;
    private float currentStyleScore = 0f;
    private Coroutine pulseCoroutine;

    void Awake()
    {
        rankSystem = FindObjectOfType<StyleRankSystem>();
        if (rankSystem == null)
        {
            Debug.LogError("StyleRankSystem não encontrado na cena. O StyleRankUI não funcionará corretamente.");
            return;
        }
        maxScore = rankSystem.maxScore;

        rankMap = new Dictionary<StyleRankSystem.StyleRank, RankVisuals>();
        foreach (var visual in rankVisuals)
        {
            rankMap.Add(visual.rank, visual);
            visual.rankContainer.SetActive(visual.rank == StyleRankSystem.StyleRank.D);
            
            if (visual.fillImage != null)
            {
                visual.fillImage.type = Image.Type.Filled;
                visual.fillImage.fillMethod = Image.FillMethod.Vertical; 
                visual.fillImage.fillAmount = 0f;
            }
        }
    }

    void OnEnable()
    {
        StyleRankSystem.OnRankChanged += OnRankChanged;
        StyleRankSystem.OnScoreChanged += OnScoreChanged;
        StyleRankSystem.OnRankUpHoldStart += OnRankUpHoldStart;
        StyleRankSystem.OnRankUpHoldEnd += OnRankUpHoldEnd;
    }

    void OnDisable()
    {
        StyleRankSystem.OnRankChanged -= OnRankChanged;
        StyleRankSystem.OnScoreChanged -= OnScoreChanged;
        StyleRankSystem.OnRankUpHoldStart -= OnRankUpHoldStart;
        StyleRankSystem.OnRankUpHoldEnd -= OnRankUpHoldEnd;
    }

    void Update()
    {
        // O preenchimento e o fade do 'D' só ocorrem se não estiver em Rank Up Hold
        if (!rankSystem.IsRankUpHolding)
        {
            UpdateRankFill();
            HandleDFade();
        }
    }

    /// <summary>
    /// Define o valor alpha (transparência) de um componente Graphic.
    /// </summary>
    private void SetAlpha(Graphic graphic, float alpha)
    {
        if (graphic != null)
        {
            Color color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }
    }

    private void OnScoreChanged(float newScore)
    {
        currentStyleScore = newScore;
    }

    /// <summary>
    /// Chamado quando o ranking de estilo muda (após o Rank Up Hold).
    /// </summary>
    private void OnRankChanged(StyleRankSystem.StyleRank newRank)
    {
        if (newRank == currentDisplayedRank) return;

        // Oculta o rank anterior
        if (rankMap.ContainsKey(currentDisplayedRank))
        {
            // Reseta a cor do rank anterior antes de ocultar (caso tenha saído do hold)
            ResetRankColor(currentDisplayedRank);
            rankMap[currentDisplayedRank].rankContainer.SetActive(false);
            
        }

        // Exibe o novo rank
        if (rankMap.ContainsKey(newRank))
        {
            rankMap[newRank].rankContainer.SetActive(true);
            
            // **CORREÇÃO:** Garante que o preenchimento do novo rank comece em 0%
            if (rankMap[newRank].fillImage != null)
            {
                rankMap[newRank].fillImage.fillAmount = 0f;
            }

            currentDisplayedRank = newRank;
        }
    }

    /// <summary>
    /// Chamado quando o preenchimento de um rank é completado e o sistema entra em Rank Up Hold.
    /// </summary>
    private void OnRankUpHoldStart(StyleRankSystem.StyleRank currentRank)
    {
        // Garante que o rank atual esteja 100% preenchido visualmente
        if (rankMap.ContainsKey(currentRank))
        {
            rankMap[currentRank].fillImage.fillAmount = 1f;
        }

        // Inicia o efeito de pulsação no rank atual
        if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
        pulseCoroutine = StartCoroutine(PulseRank(currentRank));
    }

    /// <summary>
    /// Chamado quando o Rank Up Hold é encerrado (jogador fez uma nova ação).
    /// </summary>
    private void OnRankUpHoldEnd()
    {
        // Finaliza o efeito de pulsação e reseta a cor
        if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
        pulseCoroutine = null;
        // A cor será resetada no OnRankChanged antes de ocultar o container
    }

    /// <summary>
    /// Coroutine para fazer o rank atual pulsar.
    /// </summary>
    private IEnumerator PulseRank(StyleRankSystem.StyleRank rank)
    {
        if (!rankMap.ContainsKey(rank)) yield break;

        RankVisuals visuals = rankMap[rank];
        
        while (true)
        {
            // Usa a função seno para criar um efeito de pulsação suave entre minPulseAlpha e 1
            float alpha = Mathf.Lerp(minPulseAlpha, 1f, (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f);
            
            // Aplica a cor de pulsação com o alpha calculado
            Color pulse = pulseColor;
            pulse.a = alpha;

            // Aplica a cor ao preenchimento e ao outline (se existir)
            if (visuals.fillImage != null)
            {
                visuals.fillImage.color = pulse;
            }
            if (visuals.outlineImage != null)
            {
                visuals.outlineImage.color = pulse;
            }

            yield return null;
        }
    }

    /// <summary>
    /// Reseta a cor do rank para a cor original (totalmente opaca).
    /// </summary>
    private void ResetRankColor(StyleRankSystem.StyleRank rank)
    {
        if (!rankMap.ContainsKey(rank)) return;

        RankVisuals visuals = rankMap[rank];
        Color resetColor = pulseColor; // Usamos a pulseColor como base, mas com alpha 1
        resetColor.a = 1f;

        if (visuals.fillImage != null)
        {
            visuals.fillImage.color = resetColor;
        }
        if (visuals.outlineImage != null)
        {
            visuals.outlineImage.color = resetColor;
        }
    }

    /// <summary>
    /// Atualiza o fillAmount da imagem de preenchimento em tempo real.
    /// </summary>
    private void UpdateRankFill()
    {
        if (!rankMap.ContainsKey(currentDisplayedRank)) return;

        RankVisuals currentVisuals = rankMap[currentDisplayedRank];
        if (currentVisuals.fillImage == null) return;

        float lowerThreshold = GetLowerThreshold(currentDisplayedRank);
        float upperThreshold = GetUpperThreshold(currentDisplayedRank);

        float scoreRange = upperThreshold - lowerThreshold;
        float scoreInRank = currentStyleScore - lowerThreshold;

        float fillAmount = scoreRange > 0 ? Mathf.Clamp01(scoreInRank / scoreRange) : 0f;

        currentVisuals.fillImage.fillAmount = fillAmount;
    }

    /// <summary>
    /// Gerencia o desaparecimento por opacidade do ranking 'D'.
    /// </summary>
    private void HandleDFade()
    {
        // A imagem só deve ser afetada pelo fade quando o rank for D
        if (currentDisplayedRank != StyleRankSystem.StyleRank.D)
        {
            // Garante que a imagem esteja totalmente visível (alpha = 1) quando o rank não for D
            SetAlpha(rankDImageToBlink, 1f);
            return;
        }

        RankVisuals dVisuals = rankMap[StyleRankSystem.StyleRank.D];
        if (dVisuals.outlineImage == null || dVisuals.fillImage == null) return;

        float targetAlpha;
        if (currentStyleScore <= 0f)
        {
            targetAlpha = 0f;
        }
        else if (currentStyleScore < dFadeThreshold)
        {
            targetAlpha = Mathf.Lerp(0f, 1f, currentStyleScore / dFadeThreshold);
        }
        else
        {
            targetAlpha = 1f;
        }

        float currentAlpha = dVisuals.outlineImage.color.a;
        float newAlpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.deltaTime * dFadeSmoothSpeed);

        Color outlineColor = dVisuals.outlineImage.color;
        outlineColor.a = newAlpha;
        dVisuals.outlineImage.color = outlineColor;

        Color fillColor = dVisuals.fillImage.color;
        fillColor.a = newAlpha;
        dVisuals.fillImage.color = fillColor;
        
        // Aplica o mesmo alpha à imagem
        SetAlpha(rankDImageToBlink, newAlpha);
    }

    // =================================================================================================
    // Métodos Auxiliares para obter os thresholds do StyleRankSystem
    // =================================================================================================

    private float GetLowerThreshold(StyleRankSystem.StyleRank rank)
    {
        switch (rank)
        {
            case StyleRankSystem.StyleRank.C: return rankSystem.scoreThresholdC;
            case StyleRankSystem.StyleRank.B: return rankSystem.scoreThresholdB;
            case StyleRankSystem.StyleRank.A: return rankSystem.scoreThresholdA;
            case StyleRankSystem.StyleRank.S: return rankSystem.scoreThresholdS;
            case StyleRankSystem.StyleRank.D: return 0f;
            default: return 0f;
        }
    }

    private float GetUpperThreshold(StyleRankSystem.StyleRank rank)
    {
        switch (rank)
        {
            case StyleRankSystem.StyleRank.D: return rankSystem.scoreThresholdC;
            case StyleRankSystem.StyleRank.C: return rankSystem.scoreThresholdB;
            case StyleRankSystem.StyleRank.B: return rankSystem.scoreThresholdA;
            case StyleRankSystem.StyleRank.A: return rankSystem.scoreThresholdS;
            case StyleRankSystem.StyleRank.S: return rankSystem.maxScore;
            default: return rankSystem.maxScore;
        }
    }
}
