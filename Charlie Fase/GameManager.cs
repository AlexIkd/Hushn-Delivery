using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Contador de Tips")]
    public int pontuacaoTotal = 0; // Representa os tips coletados
    public TMP_Text textoDePontuacao; // Texto que mostra os tips na UI da fase

    [Header("Conexões")]
    [Tooltip("Referência ao ScoreManager na cena para calcular a pontuação final.")]
    public ScoreManager scoreManager; 
    
    [Tooltip("Referência ao cronômetro da fase (StopwatchTimer).")]
    public StopwatchTimer stopwatchTimer; 

    void Start()
    {
        AtualizarTextoPontuacao();

        // Tenta encontrar os componentes automaticamente se não forem definidos no Inspector
        if (scoreManager == null)
        {
            scoreManager = FindObjectOfType<ScoreManager>();
        }
        if (stopwatchTimer == null)
        {
            stopwatchTimer = FindObjectOfType<StopwatchTimer>();
        }

        if (scoreManager != null)
        {
            scoreManager.CalcularScore(pontuacaoTotal);
        }
    }

    public void AdicionarPontos(int pontosParaAdicionar)
    {
        pontuacaoTotal += pontosParaAdicionar;
        Debug.Log("Tips coletados: " + pontuacaoTotal);
        AtualizarTextoPontuacao();

        if (scoreManager != null)
        {
            scoreManager.CalcularScore(pontuacaoTotal);
        }
    }

    private void AtualizarTextoPontuacao()
    {
        if (textoDePontuacao != null)
        {
            textoDePontuacao.text = pontuacaoTotal.ToString();
        }
    }

    /// <summary>
    /// Coleta todos os dados finais da fase (tempo, score, tips) e os registra
    /// no contêiner de dados persistente (DadosDaFase).
    /// Este método deve ser chamado ANTES da transição de cena.
    /// </summary>
    public void FinalizarFaseEColarDados()
    {
        float tempoFinal = 0f;
        int scoreFinal = 0;

        // 1. Pega o TEMPO final do StopwatchTimer
        if (stopwatchTimer != null)
        {
            // Puxa o valor atual e para o cronômetro.
            tempoFinal = stopwatchTimer.StopStopwatch();
        }
        else
        {
            Debug.LogWarning("GameManager: StopwatchTimer não encontrado. O tempo final será 0.");
        }

        // 2. Pega o SCORE final do ScoreManager
        if (scoreManager != null)
        {
            // Puxa o valor atual da propriedade ScoreAtual.
            scoreFinal = scoreManager.ScoreAtual; 
        }
        else
        {
            Debug.LogWarning("GameManager: ScoreManager não encontrado. O score final será 0.");
        }

        // 3. Pega os TIPS finais (que é a variável pontuacaoTotal)
        int tipsFinais = this.pontuacaoTotal;

        // 4. "Cola" todos os dados no contêiner DadosDaFase
        if (DadosDaFase.Instance != null)
        {
            DadosDaFase.Instance.RegistrarDados(tempoFinal, scoreFinal, tipsFinais);
        }
        else
        {
            Debug.LogError("GameManager: Instância de DadosDaFase não encontrada! Os dados não serão salvos para a cena de ranking.");
        }
    }
}
