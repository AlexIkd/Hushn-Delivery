using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class CarregadorDeCena : MonoBehaviour
{
    [Header("Configurações do Marcador")]
    public float velocidadeDeRotacao = 50f;
    [Tooltip("O atraso agora é o tempo para o Fade Out e outras ações antes da transição real.")]
    public float atrasoParaCarregar = 5.0f; 

    [Header("Configurações de Cena")]
    public string nomeDaCenaParaCarregar;

    private bool transicaoIniciada = false;
    private Collider meuColisor;

    private void Start()
    {
        meuColisor = GetComponent<Collider>();
        if (meuColisor == null)
        {
            Debug.LogError("[CarregadorDeCena] ERRO: Este objeto precisa de um componente Collider para funcionar!", this.gameObject);
        }
    }

    void Update()
    {
        transform.Rotate(0f, velocidadeDeRotacao * Time.deltaTime, 0f);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Verifica se o objeto que entrou no gatilho tem a tag "Player"
        if (other.CompareTag("Player") && !transicaoIniciada)
        {
            transicaoIniciada = true;
            if (meuColisor != null)
            {
                meuColisor.enabled = false; // Impede múltiplos acionamentos
            }
            
            Debug.Log($"[CarregadorDeCena] Jogador detectado! Iniciando processo de finalização da fase.");

            // Inicia a coroutine que vai fazer todo o trabalho.
            StartCoroutine(ProcessoDeFinalizacao());
        }
    }

    private IEnumerator ProcessoDeFinalizacao()
    {
        // PASSO 1: ENCONTRAR O GAMEMANAGER E SALVAR OS DADOS
        Debug.Log("[CarregadorDeCena] Procurando pelo GameManager na cena...");
        GameManager gameManager = FindObjectOfType<GameManager>();

        if (gameManager != null)
        {
            Debug.Log("[CarregadorDeCena] GameManager encontrado! Chamando FinalizarFaseEColarDados()...");
            // ESTA É A CHAMADA CRUCIAL QUE SALVA OS DADOS NO DadosDaFase.
            gameManager.FinalizarFaseEColarDados();
        }
        else
        {
            Debug.LogError("[CarregadorDeCena] ERRO CRÍTICO: GameManager não foi encontrado na cena! Os dados da fase NÃO serão salvos.");
        }

        // PASSO 2: INICIAR A TRANSIÇÃO VISUAL (FADE OUT)
        Debug.Log($"[CarregadorDeCena] Iniciando transição visual (Fade Out) com SceneTransitionManager...");
        
        // Verifica se o SceneTransitionManager existe e usa-o para iniciar a transição
        if (SceneTransitionManager.Instance != null)
        {
            // O SceneTransitionManager agora cuidará do Fade Out e do carregamento da cena.
            // O atraso (atrasoParaCarregar) será usado para outras ações, se necessário, 
            // mas o Fade Out é o que realmente "precede" a transição.
            
            // O tempo de espera agora é opcional, dependendo se você quer que o jogador
            // veja a tela escura por um tempo antes da cena carregar.
            // Se o Fade Out for rápido, você pode querer um pequeno atraso aqui.
            // Vamos manter o atraso original para outras ações, mas o Fade Out é o principal.
            
            // Se você quiser que o Fade Out demore o tempo de 'atrasoParaCarregar', 
            // você precisaria ajustar o 'fadeDuration' no SceneTransitionManager.
            
            // Por enquanto, vamos manter o atraso para garantir que o salvamento de dados
            // e outras lógicas tenham tempo de processar antes do Fade Out começar.
            
            // Se o atraso for *antes* do Fade Out, descomente a linha abaixo:
            // yield return new WaitForSeconds(atrasoParaCarregar);
            
            // Se o atraso for *durante* o Fade Out, o SceneTransitionManager já cuida disso.
            
            // Vamos remover o atraso explícito aqui, pois o SceneTransitionManager já tem sua própria duração de Fade.
            // Se você quiser um atraso *antes* do Fade Out, adicione-o aqui.
            
            // Para garantir que o salvamento de dados ocorra antes de qualquer coisa visual:
            yield return new WaitForSeconds(0.5f); // Pequeno atraso para garantir que o salvamento de dados terminou.

            // Chamada correta:
            SceneTransitionManager.Instance.TransitionToScene(nomeDaCenaParaCarregar);
            
            // Como o SceneTransitionManager agora carrega a cena, esta coroutine pode terminar.
        }
        else
        {
            Debug.LogError("[CarregadorDeCena] ERRO CRÍTICO: SceneTransitionManager.Instance não encontrado! A transição de cena será feita de forma abrupta.");
            
            // PASSO 3 (Alternativo): INICIAR A TRANSIÇÃO DE CENA (SEM FADE)
            if (!string.IsNullOrEmpty(nomeDaCenaParaCarregar))
            {
                // Se o manager não for encontrado, ainda esperamos o atraso original
                yield return new WaitForSeconds(atrasoParaCarregar);
                SceneManager.LoadScene(nomeDaCenaParaCarregar);
            }
            else
            {
                Debug.LogError("[CarregadorDeCena] ERRO: O nome da cena para carregar não foi definido no Inspector!");
            }
        }
    }
}
