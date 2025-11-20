using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI; // Adicionado para usar o componente Image

public class RankingUIController : MonoBehaviour
{
    [Header("Referências da UI")]
    public TMP_Text textoTempo;
    public TMP_Text textoScore;
    public TMP_Text textoTips;
    // Alterado de TMP_Text para Image
    [Tooltip("Componente Image que exibirá o Sprite do Rank.")]
    public Image imagemRankFinal; 
    public GameObject painelDeRanking;

    [Header("Configurações de Animação")]
    [Tooltip("Duração da animação de contagem em segundos.")]
    public float duracaoDaContagem = 1.5f;
    [Tooltip("Pausa entre a contagem de cada elemento.")]
    public float pausaEntreContagens = 0.5f;

    [Header("Efeitos de Revelação do Rank")]
    [Tooltip("Sistema de partículas para tocar na revelação do Rank.")]
    public ParticleSystem particulasDeRank;
    [Tooltip("Fonte de áudio para tocar o som de revelação.")]
    public AudioSource audioSource;
    [Tooltip("Clip de áudio para tocar na revelação do Rank.")]
    public AudioClip somDeRevelacao;

    // Referência estática para o PlayerRankAnimator
    public static RankingUIController Instance;

    [Header("Critérios de Ranking (Defina do melhor para o pior)")]
    public RankCriterio[] criterios;

    [Header("Sprites de Rank")]
    [Tooltip("Sprites de imagem para cada rank. O índice deve corresponder ao índice do RankCriterio.")]
    public Sprite[] spritesDeRank;

    [System.Serializable]
    public class RankCriterio
    {
        public string rank;
        public int scoreMinimo;
        public float tempoMaximo;
        [Tooltip("Índice do Sprite correspondente no array 'spritesDeRank'.")]
        public int spriteIndex; // Novo campo para mapear o sprite
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        painelDeRanking.SetActive(false);
        // Garante que a imagem do rank esteja invisível no início
        if (imagemRankFinal != null)
        {
            imagemRankFinal.enabled = false;
        }
        StartCoroutine(ExibirResultados());
    }

    private IEnumerator ExibirResultados()
    {
        // Espera para o fade in
        yield return new WaitForSeconds(1.5f);

        DadosDaFase dados = DadosDaFase.Instance;
        if (dados == null)
        {
            Debug.LogError("DadosDaFase não encontrados! Não é possível exibir o ranking.");
            yield break;
        }

        painelDeRanking.SetActive(true);

        // 1. Contagem de TIPS (Instantâneo) - Removendo prefixo
        textoTips.text = $"{dados.tipsColetados}";
        yield return new WaitForSeconds(pausaEntreContagens);

        // 2. Contagem de SCORE (Animado) - Removendo prefixo
        yield return StartCoroutine(ContarScore(dados.scoreFinal));
        yield return new WaitForSeconds(pausaEntreContagens);

        // 3. Contagem de TEMPO (Animado) - Removendo prefixo
        yield return StartCoroutine(ContarTempo(dados.tempoFinal));
        yield return new WaitForSeconds(pausaEntreContagens);

        // 4. Revelação do RANK
        // A função agora retorna o índice do sprite, não o nome do rank
        int spriteIndexFinal = CalcularRankSpriteIndex(dados.scoreFinal, dados.tempoFinal);
        
        // Exibe a imagem do rank
        if (imagemRankFinal != null && spriteIndexFinal >= 0 && spriteIndexFinal < spritesDeRank.Length)
        {
            imagemRankFinal.sprite = spritesDeRank[spriteIndexFinal];
            imagemRankFinal.enabled = true; // Torna a imagem visível
        }
        else
        {
            Debug.LogError($"[RankingUIController] Erro ao exibir o Sprite do Rank. Índice: {spriteIndexFinal}. Verifique se o 'imagemRankFinal' está atribuído e se o 'spritesDeRank' tem o índice correto.");
        }
        
        // Toca o efeito visual e sonoro
        TocarEfeitosDeRevelacao();

        // ** NOVO: Comunicação com PlayerRankAnimator **
        // O nome do rank é o campo 'rank' do critério encontrado.
        string rankName = GetRankName(dados.scoreFinal, dados.tempoFinal);
        if (!string.IsNullOrEmpty(rankName))
        {
            PlayerRankAnimator.Instance?.SetRankTrigger(rankName);
        }
        // ********************************************
    }

    private void TocarEfeitosDeRevelacao()
    {
        // Toca o sistema de partículas, se estiver configurado
        if (particulasDeRank != null)
        {
            particulasDeRank.Play();
        }

        // Toca o som, se estiver configurado
        if (audioSource != null && somDeRevelacao != null)
        {
            audioSource.PlayOneShot(somDeRevelacao);
        }
        else if (audioSource == null)
        {
            Debug.LogWarning("AudioSource não configurado no RankingUIController. O som de revelação não será tocado.");
        }
    }

    // Coroutine para animar a contagem do Score
    private IEnumerator ContarScore(int scoreFinal)
    {
        float tempoDecorrido = 0f;
        int scoreInicial = 0;

        while (tempoDecorrido < duracaoDaContagem)
        {
            tempoDecorrido += Time.deltaTime;
            float progresso = tempoDecorrido / duracaoDaContagem;
            
            // Interpola o valor do score
            int scoreAtual = (int)Mathf.Lerp(scoreInicial, scoreFinal, progresso);
            
            // Removendo prefixo
            textoScore.text = $"{scoreAtual}";
            yield return null;
        }

        // Garante que o valor final seja exibido exatamente - Removendo prefixo
        textoScore.text = $"{scoreFinal}";
    }

    // Coroutine para animar a contagem do Tempo
    private IEnumerator ContarTempo(float tempoFinal)
    {
        float tempoDecorrido = 0f;
        float tempoInicial = 0f;

        while (tempoDecorrido < duracaoDaContagem)
        {
            tempoDecorrido += Time.deltaTime;
            float progresso = tempoDecorrido / duracaoDaContagem;
            
            // Interpola o valor do tempo
            float tempoAtual = Mathf.Lerp(tempoInicial, tempoFinal, progresso);
            
            // Formata o tempo para Minutos:Segundos.Milissegundos (NOVO)
            int minutos = Mathf.FloorToInt(tempoAtual / 60);
            int segundos = Mathf.FloorToInt(tempoAtual % 60);
            int milissegundos = Mathf.FloorToInt((tempoAtual * 100) % 100); // Pega os dois primeiros dígitos
            
            textoTempo.text = $"{minutos:00}:{segundos:00}.{milissegundos:00}";
            yield return null;
        }

        // Garante que o valor final seja exibido exatamente (NOVO)
        int minutosFinais = Mathf.FloorToInt(tempoFinal / 60);
        int segundosFinais = Mathf.FloorToInt(tempoFinal % 60);
        int milissegundosFinais = Mathf.FloorToInt((tempoFinal * 100) % 100);
        textoTempo.text = $"{minutosFinais:00}:{segundosFinais:00}.{milissegundosFinais:00}";
    }

    // Função auxiliar para obter o nome do rank
    private string GetRankName(int score, float tempo)
    {
        for (int i = 0; i < criterios.Length; i++)
        {
            var criterio = criterios[i];
            if (score >= criterio.scoreMinimo && tempo <= criterio.tempoMaximo)
            {
                return criterio.rank;
            }
        }
        return null; // Retorna null se nenhum rank for encontrado
    }

    // Função modificada para retornar o índice do Sprite
    private int CalcularRankSpriteIndex(int score, float tempo)
    {
        for (int i = 0; i < criterios.Length; i++)
        {
            var criterio = criterios[i];
            if (score >= criterio.scoreMinimo && tempo <= criterio.tempoMaximo)
            {
                // Retorna o índice do sprite definido no critério
                return criterio.spriteIndex;
            }
        }
        // Retorna -1 ou um índice padrão para o rank "D" (ou o pior rank)
        // Você deve garantir que o Rank "D" também tenha um critério ou um índice padrão.
        // Por segurança, vamos retornar 0 (assumindo que o índice 0 é o pior rank, ou você pode ajustar)
        // Ou você pode adicionar um critério para o rank "D" com spriteIndex.
        return -1; // Retorna -1 para indicar que nenhum rank foi alcançado (ou use um índice padrão)
    }
}
