using UnityEngine;

public class TrailController : MonoBehaviour
{
    [Header("Configurações de Velocidade")]
    [Tooltip("Velocidade mínima para ativar o rasto.")]
    public float velocidadeMinima = 5.0f;

    [Header("Configurações de Fade")]
    [Tooltip("Duração original do rastro quando ativo.")]
    [Range(0.1f, 5.0f)] // Adiciona um slider no Inspector para facilitar o ajuste
    public float trailTimeOriginal = 0.5f;
    [Tooltip("Velocidade com que o rastro desaparece (fade out).")]
    [Range(0.1f, 10.0f)] // Adiciona um slider no Inspector para facilitar o ajuste
    public float fadeSpeed = 2.0f;

    [Header("Referências")]
    [Tooltip("O componente Trail Renderer a ser controlado.")]
    public TrailRenderer trailRenderer;
    
    private Rigidbody rb;
    private PlayerMovement_FrontiersStyle playerMovement;
    private Vector3 ultimaPosicao;
    private float currentTrailTime;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = GetComponentInParent<Rigidbody>();

        playerMovement = GetComponent<PlayerMovement_FrontiersStyle>();
        if (playerMovement == null) playerMovement = GetComponentInParent<PlayerMovement_FrontiersStyle>();

        if (trailRenderer == null)
        {
            Debug.LogError("Trail Renderer não atribuído.");
            enabled = false;
            return;
        }

        trailRenderer.emitting = false;
        trailRenderer.time = trailTimeOriginal;
        currentTrailTime = 0f;
        ultimaPosicao = transform.position;
    }

    void Update()
    {
        float velocidadeAtual = CalcularVelocidade();

        // Verifica se o jogador está em Wall Run
        bool estaEmWallRun = (playerMovement != null) && playerMovement.IsWallRunning;

        // Lógica de Ativação vs Desativação (Fade)
        if (velocidadeAtual >= velocidadeMinima && !estaEmWallRun)
        {
            // Ativa gradualmente o tempo do rastro até o original
            currentTrailTime = Mathf.MoveTowards(currentTrailTime, trailTimeOriginal, fadeSpeed * Time.deltaTime);
            
            if (currentTrailTime > 0)
            {
                trailRenderer.emitting = true;
                trailRenderer.time = currentTrailTime;
            }
        }
        else
        {
            // Diminui gradualmente o tempo do rastro até zero
            currentTrailTime = Mathf.MoveTowards(currentTrailTime, 0f, fadeSpeed * Time.deltaTime);
            trailRenderer.time = currentTrailTime;

            // Se chegou a zero, para de emitir
            if (currentTrailTime <= 0)
            {
                trailRenderer.emitting = false;
            }
        }
    }

    private float CalcularVelocidade()
    {
        if (playerMovement != null) return playerMovement.currentSpeed;
        if (rb != null) return rb.linearVelocity.magnitude;

        Vector3 deslocamento = transform.position - ultimaPosicao;
        float vel = Time.deltaTime > 0 ? deslocamento.magnitude / Time.deltaTime : 0f;
        ultimaPosicao = transform.position;
        return vel;
    }
}
