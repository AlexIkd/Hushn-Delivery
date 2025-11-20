using UnityEngine;
using TMPro; // Usando a biblioteca TextMeshPro

public class ScoreManager : MonoBehaviour
{
    [Header("Configurações de Pontuação")]
    [Tooltip("O texto da UI que exibirá a pontuação final.")]
    public TMP_Text textoDoScore;

    [Tooltip("O multiplicador aplicado aos tips para calcular o score.")]
    public int multiplicadorDePontos = 50;

    private int scoreAtual = 0;

    // ADICIONADO: Propriedade pública para que o GameManager possa ler o score.
    public int ScoreAtual
    {
        get { return scoreAtual; }
    }

    void Start()
    {
        // Garante que o score comece zerado na tela.
        AtualizarTextoDoScore(0);
    }

    public void CalcularScore(int totalDeTips)
    {
        // Calcula o score multiplicando os tips pelo valor definido.
        scoreAtual = totalDeTips * multiplicadorDePontos;

        // Atualiza o texto na tela.
        AtualizarTextoDoScore(scoreAtual);
    }

    private void AtualizarTextoDoScore(int valor)
    {
        if (textoDoScore != null)
        {
            // Converte o número do score para texto (string).
            textoDoScore.text = valor.ToString();
        }
    }
}
