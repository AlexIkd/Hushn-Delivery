using UnityEngine;

public class TrailController : MonoBehaviour
{
    [Header("Configurações de Velocidade")]
    [Tooltip("Velocidade mínima para ativar o rasto.")]
    public float velocidadeMinima = 5.0f;

    [Header("Referências")]
    [Tooltip("O componente Trail Renderer a ser controlado.")]
    public TrailRenderer trailRenderer;
    
    private Rigidbody rb;
    private PlayerMovement_FrontiersStyle playerMovement;
    private Vector3 ultimaPosicao;

    void Start()
    {
        // Tenta obter o Rigidbody e o script de movimento no mesmo GameObject ou em um pai.
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = GetComponentInParent<Rigidbody>();
        }

        playerMovement = GetComponent<PlayerMovement_FrontiersStyle>();
        if (playerMovement == null)
        {
            playerMovement = GetComponentInParent<PlayerMovement_FrontiersStyle>();
        }

        if (trailRenderer == null)
        {
            Debug.LogError("Trail Renderer não atribuído no script TrailController.");
            enabled = false; // Desativa o script se a referência estiver faltando.
            return;
        }

        // Inicialmente, desativa o rasto.
        trailRenderer.emitting = false;
        ultimaPosicao = transform.position;
    }

    void Update()
    {
        // 1. Calcular a velocidade
        float velocidadeAtual;

        if (playerMovement != null)
        {
            // Se o script de movimento estiver presente, usa a velocidade calculada por ele (mais preciso para CharacterController)
            velocidadeAtual = playerMovement.currentSpeed;
        }
        else if (rb != null)
        {
            // Se houver Rigidbody, usa sua velocidade (mais preciso para física)
            velocidadeAtual = rb.linearVelocity.magnitude;
        }
        else
        {
            // Se não houver Rigidbody nem script de movimento, calcula a velocidade manualmente (para transform-based movement)
            Vector3 deslocamento = transform.position - ultimaPosicao;
            // Usamos Time.deltaTime para calcular a velocidade (distância/tempo)
            if (Time.deltaTime > 0)
            {
                velocidadeAtual = deslocamento.magnitude / Time.deltaTime;
            }
            else
            {
                velocidadeAtual = 0f;
            }
            ultimaPosicao = transform.position;
        }

        // 2. Controlar a emissão do rasto
        if (velocidadeAtual >= velocidadeMinima)
        {
            // Ativa a emissão se a velocidade for maior ou igual à mínima
            if (!trailRenderer.emitting)
            {
                trailRenderer.emitting = true;
            }
        }
        else
        {
            // Desativa a emissão se a velocidade for menor que a mínima
            if (trailRenderer.emitting)
            {
                trailRenderer.emitting = false;
            }
        }
    }
}
