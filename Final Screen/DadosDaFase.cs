using UnityEngine;

/// <summary>
/// Este script funciona como um contêiner de dados que persiste entre as cenas.
/// Seu único propósito é armazenar os resultados finais da fase (tempo, score e tips)
/// para que a cena de ranking possa acessá-los.
/// </summary>
public class DadosDaFase : MonoBehaviour
{
    // Instância estática (Singleton) para ser facilmente acessível de qualquer script.
    public static DadosDaFase Instance;

    // --- DADOS A SEREM TRANSPORTADOS ---
    public float tempoFinal;      // Armazena o tempo final obtido do StopwatchTimer.
    public int scoreFinal;        // Armazena o score final calculado pelo ScoreManager.
    public int tipsColetados;     // Armazena a contagem total de tips do GameManager.

    private void Awake()
    {
        // Implementação do padrão Singleton para garantir que apenas uma instância deste objeto exista.
        if (Instance == null)
        {
            // Se nenhuma instância existir, esta se torna a instância principal.
            Instance = this;
            // Impede que este objeto seja destruído ao carregar uma nova cena.
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // Se uma instância já existir, destrói este objeto para evitar duplicatas.
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Método público para que o GameManager (ou outro script) possa registrar os dados
    /// antes de fazer a transição para a cena de ranking.
    /// </summary>
    /// <param name="tempo">O tempo final da fase.</param>
    /// <param name="score">O score final da fase.</param>
    /// <param name="tips">A quantidade final de tips coletados.</param>
    public void RegistrarDados(float tempo, int score, int tips)
    {
        this.tempoFinal = tempo;
        this.scoreFinal = score;
        this.tipsColetados = tips;

        // Mensagem de log para confirmar que os dados foram salvos corretamente.
        Debug.Log($"[DadosDaFase] Dados registrados com sucesso: Tempo={tempo}, Score={score}, Tips={tips}");
    }
}
