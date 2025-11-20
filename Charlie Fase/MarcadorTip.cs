using UnityEngine;

public class MarcadorTip : MonoBehaviour
{
    [Header("Configurações")]
    public float velocidadeDeRotacao = 50f;
    [Tooltip("Valor base do tip antes de aplicar o multiplicador do Rank.")]
    public int valorBaseDoTip = 10;

    // Referência para o StyleRankSystem na cena
    private StyleRankSystem styleRankSystem;

    void Start()
    {
        // Tenta encontrar o StyleRankSystem na cena
        styleRankSystem = FindObjectOfType<StyleRankSystem>();
        if (styleRankSystem == null)
        {
            Debug.LogWarning("StyleRankSystem não encontrado na cena. O multiplicador de tips não será aplicado.");
        }
    }

    void Update()
    {
        // Faz o objeto girar no eixo Y para dar um efeito visual
        transform.Rotate(0f, velocidadeDeRotacao * Time.deltaTime, 0f);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Verifica se o objeto que entrou no gatilho tem a tag "Player"
        if (other.CompareTag("Player"))
        {
            // 1. Calcula o valor final do tip
            int valorFinalDoTip = valorBaseDoTip;
            float multiplicador = 1.0f;

            if (styleRankSystem != null)
            {
                // Obtém o multiplicador atual do sistema de rank
                multiplicador = styleRankSystem.CurrentTipMultiplier;
                
                // Aplica o multiplicador e arredonda para o inteiro mais próximo
                valorFinalDoTip = Mathf.RoundToInt(valorBaseDoTip * multiplicador);
            }

            // 2. Procura pelo GameManager na cena
            GameManager gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                // Envia o valor final (base * multiplicador) para o GameManager
                gameManager.AdicionarPontos(valorFinalDoTip);
                Debug.Log($"Tip coletado! Base: {valorBaseDoTip}, Multiplicador: {multiplicador}x, Total: {valorFinalDoTip}");
            }

            // 3. Destroi este objeto (o marcador)
            Destroy(gameObject);
        }
    }
}
