using UnityEngine;
using System;
using System.Collections.Generic; // Adicionado para usar Dictionary

/// <summary>
/// Sistema de Ranking de Estilo inspirado em Devil May Cry.
/// Gerencia a pontuação, o ranking (D, C, B, A, S), o decaimento (decay) e o tempo de "hold" (estático).
/// </summary>
public class StyleRankSystem : MonoBehaviour
{
    // Enumeração para os rankings de estilo
    public enum StyleRank { D, C, B, A, S }

    [Header("Configurações de Pontuação")]
    [Tooltip("Pontuação atual de estilo.")]
    [SerializeField] public float styleScore = 0f;
    [Tooltip("Taxa de decaimento da pontuação por segundo.")]
    [SerializeField] public float decayRate = 10f;
    [Tooltip("Duração em segundos que o ranking fica estático após um aumento.")]
    [SerializeField] public float rankHoldDuration = 20f;
    [Tooltip("Pontuação máxima que o ranking pode atingir.")]
    [SerializeField] public float maxScore = 500f;

    [Header("Limiares de Ranking")]
    // Limiares de pontuação para cada ranking (mínimo para atingir)
    [SerializeField] public int scoreThresholdC = 100;
    [SerializeField] public int scoreThresholdB = 200;
    [SerializeField] public int scoreThresholdA = 300;
    [SerializeField] public int scoreThresholdS = 400;

    [Header("Multiplicadores de Tips")]
    [Tooltip("Mapeamento de multiplicadores de tips por Rank. O multiplicador é aplicado quando o rank é atingido.")]
    private readonly Dictionary<StyleRank, float> rankMultipliers = new Dictionary<StyleRank, float>()
    {
        { StyleRank.D, 1.0f }, // Base
        { StyleRank.C, 1.2f }, // Multiplicador quando atinge C
        { StyleRank.B, 1.4f }, // Multiplicador quando atinge B
        { StyleRank.A, 1.6f }, // Multiplicador quando atinge A
        { StyleRank.S, 1.8f }  // Multiplicador quando atinge S
    };

    // Estado interno
    public StyleRank currentRank = StyleRank.D;
    public float holdTimer = 0f;
    public bool isHoldingRank = false;
    public bool isRankUpHolding = false;
    public StyleRank nextRankToAchieve = StyleRank.D; // Armazena o próximo rank a ser alcançado
    
    // NOVO: Multiplicador de tips atual
    private float currentTipMultiplier = 1.0f;

    // Eventos para notificar a UI ou outros sistemas
    public static event Action<StyleRank> OnRankChanged;
    public static event Action<float> OnScoreChanged;
    public static event Action<StyleRank> OnRankUpHoldStart;
    public static event Action OnRankUpHoldEnd;
    public static event Action OnRankD;

    /// <summary>
    /// Retorna o ranking de estilo atual.
    /// </summary>
    public StyleRank CurrentRank => currentRank;

    /// <summary>
    /// Retorna a pontuação de estilo atual.
    /// </summary>
    public float StyleScore => styleScore;

    /// <summary>
    /// Indica se o sistema está em estado de espera para subir de rank.
    /// </summary>
    public bool IsRankUpHolding => isRankUpHolding;

    /// <summary>
    /// NOVO: Retorna o multiplicador de tips atual.
    /// </summary>
    public float CurrentTipMultiplier => currentTipMultiplier;

    void Start()
    {
        // Garante que o ranking inicial seja D e o multiplicador seja 1.0
        UpdateRank(true);
        UpdateTipMultiplier(); // Inicializa o multiplicador
    }

    void Update()
    {
        // O decaimento e o hold normal só ocorrem se não estiver em Rank Up Hold
        if (!isRankUpHolding)
        {
            HandleRankHold();
            HandleScoreDecay();
        }
    }

    /// <summary>
    /// Adiciona pontos à pontuação de estilo e atualiza o ranking.
    /// Este é o método que os scripts de movimento devem chamar.
    /// </summary>
    /// <param name="points">A quantidade de pontos a ser adicionada.</param>
    public void AddStylePoints(int points)
    {
        StyleRank previousRank = currentRank;

        // Se estiver em Rank Up Hold, uma nova ação do jogador força a subida de rank
        if (isRankUpHolding)
        {
            // 1. Encerra o hold visual
            isRankUpHolding = false;
            OnRankUpHoldEnd?.Invoke();
            
            // 2. Força a subida de rank para o rank que estava esperando
            currentRank = nextRankToAchieve;
            OnRankChanged?.Invoke(currentRank); // Notifica a UI para transicionar
            
            // 3. Adiciona os pontos e atualiza o score
            styleScore = Mathf.Min(styleScore + points, maxScore);
            OnScoreChanged?.Invoke(styleScore);
            
            // 4. ATUALIZA O MULTIPLICADOR
            UpdateTipMultiplier();
            
            // 5. Verifica se o novo score já atinge o próximo rank (para um hold imediato)
            UpdateRank(false);
            
            // 6. Inicia o hold normal do novo rank
            StartRankHold();
            return;
        }

        // Adiciona a pontuação
        styleScore = Mathf.Min(styleScore + points, maxScore);
        OnScoreChanged?.Invoke(styleScore);

        // Atualiza o ranking e verifica se houve aumento
        UpdateRank(false);

        // Se o ranking aumentou, ativa o hold normal
        if (currentRank > previousRank)
        {
            // O multiplicador é atualizado dentro de UpdateRank() antes de chegar aqui
            StartRankHold();
        }
        // Se o ranking não aumentou, mas o jogador fez um movimento, reinicia o hold timer
        else if (isHoldingRank)
        {
            holdTimer = rankHoldDuration;
        }
    }

    /// <summary>
    /// Inicia o timer de hold (estático) do ranking.
    /// </summary>
    private void StartRankHold()
    {
        isHoldingRank = true;
        holdTimer = rankHoldDuration;
        Debug.Log($"Novo Ranking: {currentRank}. Hold ativado por {rankHoldDuration}s. Multiplicador de Tips: {currentTipMultiplier}x");
    }

    /// <summary>
    /// Gerencia o timer de hold do ranking.
    /// </summary>
    private void HandleRankHold()
    {
        if (isHoldingRank)
        {
            holdTimer -= Time.deltaTime;
            if (holdTimer <= 0f)
            {
                isHoldingRank = false;
                Debug.Log("Hold de Ranking finalizado. Decay ativado.");
            }
        }
    }

    /// <summary>
    /// Gerencia o decaimento da pontuação se o hold não estiver ativo.
    /// </summary>
    private void HandleScoreDecay()
    {
        if (!isHoldingRank)
        {
            // Salva o ranking anterior para verificar se houve queda
            StyleRank previousRank = currentRank;

            // Aplica o decaimento
            styleScore = Mathf.Max(styleScore - decayRate * Time.deltaTime, 0f);
            OnScoreChanged?.Invoke(styleScore);

            // Atualiza o ranking
            UpdateRank(false);

            // Se o ranking caiu, notifica e atualiza o multiplicador
            if (currentRank < previousRank)
            {
                Debug.Log($"Queda de Ranking: {previousRank} -> {currentRank}");
                UpdateTipMultiplier(); // Atualiza o multiplicador ao cair de rank
            }
        }
    }

    /// <summary>
    /// Determina o ranking atual com base na pontuação.
    /// </summary>
    /// <param name="forceUpdate">Força a notificação de mudança de ranking, mesmo que não tenha mudado.</param>
    private void UpdateRank(bool forceUpdate)
    {
        StyleRank newRank;

        if (styleScore >= scoreThresholdS)
        {
            newRank = StyleRank.S;
        }
        else if (styleScore >= scoreThresholdA)
        {
            newRank = StyleRank.A;
        }
        else if (styleScore >= scoreThresholdB)
        {
            newRank = StyleRank.B;
        }
        else if (styleScore >= scoreThresholdC)
        {
            newRank = StyleRank.C;
        }
        else
        {
            newRank = StyleRank.D;
        }

        if (newRank != currentRank || forceUpdate)
        {
            // Se o novo rank for maior que o atual E a pontuação atingiu o limite
            if (newRank > currentRank && styleScore >= GetThreshold(newRank))
            {
                // Entra no estado de Rank Up Hold
                isRankUpHolding = true;
                nextRankToAchieve = newRank; // Armazena o rank que será alcançado
                OnRankUpHoldStart?.Invoke(currentRank); // Notifica a UI para iniciar o efeito no rank atual
                Debug.Log($"Rank Up Hold iniciado. Próximo Rank: {newRank}.");
                
                // NOVO: Atualiza o multiplicador para o rank que está prestes a ser alcançado
                UpdateTipMultiplier(newRank);
                
                return;
            }
            
            // Se não for um Rank Up Hold, atualiza normalmente
            currentRank = newRank;
            OnRankChanged?.Invoke(currentRank);
            
            // NOVO: Atualiza o multiplicador para o rank atual
            UpdateTipMultiplier();
            
            // Se o novo rank for D, dispara o evento OnRankD
            if (currentRank == StyleRank.D)
            {
                OnRankD?.Invoke();
            }
        }
    }

    /// <summary>
    /// NOVO: Atualiza o multiplicador de tips baseado no rank fornecido ou no rank atual.
    /// </summary>
    private void UpdateTipMultiplier(StyleRank? rankToUse = null)
    {
        StyleRank rank = rankToUse ?? currentRank;
        
        if (rankMultipliers.TryGetValue(rank, out float multiplier))
        {
            currentTipMultiplier = multiplier;
        }
        else
        {
            currentTipMultiplier = 1.0f; // Fallback
        }
        
        Debug.Log($"Multiplicador de Tips atualizado para: {currentTipMultiplier}x (Baseado no Rank: {rank})");
    }

    /// <summary>
    /// Retorna o limite de pontuação para um determinado ranking.
    /// </summary>
    private int GetThreshold(StyleRank rank)
    {
        switch (rank)
        {
            case StyleRank.C: return scoreThresholdC;
            case StyleRank.B: return scoreThresholdB;
            case StyleRank.A: return scoreThresholdA;
            case StyleRank.S: return scoreThresholdS;
            default: return 0;
        }
    }

    // =================================================================================================
    // Métodos de Integração (Para serem chamados por outros scripts)
    // =================================================================================================

    // Mapeamento dos movimentos para a pontuação definida na Fase 1
    private const int POINTS_AIR_TRICK = 75;
    private const int POINTS_GRIND_RAIL = 50;
    private const int POINTS_BOOST = 40;
    private const int POINTS_WALLRUN = 30;
    private const int POINTS_SWITCH_RAIL = 60;
    private const int POINTS_AIR_DASH = 35; // Novo: Pontos para o Air Dash

    public void OnAirTrickUsed()
    {
        AddStylePoints(POINTS_AIR_TRICK);
        Debug.Log("Air Trick usado! Pontos adicionados.");
    }

    public void OnGrindRailStart()
    {
        // Adiciona pontos ao iniciar o Grind Rail
        AddStylePoints(POINTS_GRIND_RAIL);
        Debug.Log("Grind Rail iniciado! Pontos adicionados.");
    }

    public void OnBoostUsed()
    {
        AddStylePoints(POINTS_BOOST);
        Debug.Log("Boost usado! Pontos adicionados.");
    }

    public void OnWallRunStart()
    {
        // Adiciona pontos ao iniciar o Wall Run
        AddStylePoints(POINTS_WALLRUN);
        Debug.Log("Wall Run iniciado! Pontos adicionados.");
    }

    public void OnSwitchRailUsed()
    {
        AddStylePoints(POINTS_SWITCH_RAIL);
        Debug.Log("Switch Rail usado! Pontos adicionados.");
    }

    public void OnAirDashUsed()
    {
        AddStylePoints(POINTS_AIR_DASH);
        Debug.Log("Air Dash usado! Pontos adicionados.");
    }
}
