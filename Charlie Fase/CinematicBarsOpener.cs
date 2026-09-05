using UnityEngine;

/// <summary>
/// Controla duas barras cinematográficas UI.
/// A abertura ocorre em três movimentos: deslocamento principal, avanço extra
/// e retorno do avanço extra. O fechamento leva as barras ao centro e mantém
/// a tela fechada até que AbrirBarras() seja chamado novamente.
/// </summary>
public class CinematicBarsOpener : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private RectTransform barraSuperior;
    [SerializeField] private RectTransform barraInferior;

    [Header("Configurações de Animação")]
    [Tooltip("Velocidade de movimento das barras. Com o valor 2, a transição é suave.")]
    [SerializeField, Min(0.01f)] private float velocidadeAbertura = 2f;

    [Tooltip("Atraso usado apenas na abertura automática inicial da cena.")]
    [SerializeField, Min(0f)] private float atrasoInicial = 0.5f;

    [Tooltip("Primeiro deslocamento para fora do centro.")]
    [SerializeField] private float deslocamentoFinal = 600f;

    [Tooltip("Quanto as barras avançam além do primeiro deslocamento.")]
    [SerializeField] private float deslocamentoFinalExtra = 140f;

    [Tooltip("Pausa antes de começar o avanço extra.")]
    [SerializeField, Min(0f)] private float atrasoSegundaAbertura = 0.18f;

    [Tooltip("Duração do retorno do deslocamento extra até o deslocamento final.")]
    [SerializeField, Min(0.01f)] private float duracaoRetornoDeslocamentoExtra = 0.35f;

    [Tooltip("Duração do deslocamento extra durante a abertura após o respawn.")]
    [SerializeField, Min(0.01f)] private float duracaoAberturaDeslocamentoExtra = 0.8f;

    [Tooltip("Tempo que as barras permanecem no deslocamento final após o retorno do deslocamento extra e antes de fechar.")]
    [SerializeField, Min(0f)] private float holdNoDeslocamentoFinal = 0.25f;

    [Tooltip("Duração do fechamento do deslocamento final até o centro quando o personagem morre.")]
    [SerializeField, Min(0.01f)] private float duracaoFechamentoFinal = 0.6f;

    [Tooltip("Tempo que a tela permanece totalmente fechada antes de iniciar a reabertura após o respawn.")]
    [SerializeField, Min(0f)] private float tempoBarrasFechadas = 0.5f;

    private enum EstadoBarras
    {
        AguardandoAberturaInicial,
        AguardandoReabertura,
        AbrindoDeslocamentoPrincipal,
        AguardandoAvancoExtra,
        AbrindoDeslocamentoExtra,
        RetornandoDeslocamentoExtra,
        HoldNoDeslocamentoFinal,
        Abertas,
        Fechando,
        Fechadas
    }

    private EstadoBarras estado;
    private float timer;
    private float timerEstado;
    private float inicioFechamentoFinalY;
    private bool abrirSemDeslocamentoExtra;

    private void Start()
    {
        if (barraSuperior == null || barraInferior == null)
        {
            Debug.LogError("CinematicBarsOpener: arraste Barra Superior e Barra Inferior no Inspector.");
            enabled = false;
            return;
        }

        SetBarsY(0f);
        estado = EstadoBarras.AguardandoAberturaInicial;
        timer = 0f;
        timerEstado = 0f;
    }

    private void Update()
    {
        if (barraSuperior == null || barraInferior == null)
            return;

        // UnscaledDeltaTime permite que as barras continuem animando mesmo
        // quando a tela de Game Over pausa o Time.timeScale.
        float deltaTime = Time.unscaledDeltaTime;

        switch (estado)
        {
            case EstadoBarras.AguardandoAberturaInicial:
                timer += deltaTime;
                if (timer >= atrasoInicial)
                {
                    timer = 0f;
                    estado = EstadoBarras.AbrindoDeslocamentoPrincipal;
                }
                break;

            case EstadoBarras.AguardandoReabertura:
                // A tela permanece fechada durante este intervalo após o respawn.
                timerEstado += deltaTime;
                if (timerEstado >= tempoBarrasFechadas)
                {
                    timerEstado = 0f;
                    estado = EstadoBarras.AbrindoDeslocamentoPrincipal;
                }
                break;

            case EstadoBarras.AbrindoDeslocamentoPrincipal:
                if (MoveBarsToward(deslocamentoFinal, deltaTime))
                {
                    timerEstado = 0f;

                    if (abrirSemDeslocamentoExtra)
                    {
                        // Abertura especial da tela de Game Over:
                        // termina no deslocamento final, sem avançar para o extra.
                        abrirSemDeslocamentoExtra = false;
                        estado = EstadoBarras.Abertas;
                    }
                    else
                    {
                        estado = atrasoSegundaAbertura > 0f
                            ? EstadoBarras.AguardandoAvancoExtra
                            : EstadoBarras.AbrindoDeslocamentoExtra;
                    }
                }
                break;

            case EstadoBarras.AguardandoAvancoExtra:
                timerEstado += deltaTime;
                if (timerEstado >= atrasoSegundaAbertura)
                {
                    timerEstado = 0f;
                    estado = EstadoBarras.AbrindoDeslocamentoExtra;
                }
                break;

            case EstadoBarras.AbrindoDeslocamentoExtra:
                if (MoveBarsToward(deslocamentoFinal + deslocamentoFinalExtra, deltaTime, duracaoAberturaDeslocamentoExtra))
                {
                    // Durante a abertura normal, permanece no deslocamento extra.
                    estado = EstadoBarras.Abertas;
                }
                break;

            case EstadoBarras.RetornandoDeslocamentoExtra:
                // Este estado só é usado no fechamento após a morte.
                if (MoveBarsToward(deslocamentoFinal, deltaTime, duracaoRetornoDeslocamentoExtra))
                {
                    // Garante que a barra fique exatamente no deslocamento final.
                    SetBarsY(deslocamentoFinal);
                    inicioFechamentoFinalY = deslocamentoFinal;
                    timerEstado = 0f;
                    estado = holdNoDeslocamentoFinal > 0f
                        ? EstadoBarras.HoldNoDeslocamentoFinal
                        : EstadoBarras.Fechando;
                }
                break;

            case EstadoBarras.HoldNoDeslocamentoFinal:
                // Hold solicitado antes de iniciar a duração do fechamento final.
                timerEstado += deltaTime;
                if (timerEstado >= holdNoDeslocamentoFinal)
                {
                    timerEstado = 0f;
                    estado = EstadoBarras.Fechando;
                }
                break;

            case EstadoBarras.Fechando:
                timerEstado += deltaTime;
                float fechamentoProgress = Mathf.Clamp01(timerEstado / Mathf.Max(0.01f, duracaoFechamentoFinal));
                // SmoothStep mantém o controle do tempo, mas suaviza início e fim.
                fechamentoProgress = fechamentoProgress * fechamentoProgress * (3f - 2f * fechamentoProgress);
                SetBarsY(Mathf.Lerp(inicioFechamentoFinalY, 0f, fechamentoProgress));

                if (timerEstado >= duracaoFechamentoFinal)
                {
                    SetBarsY(0f);
                    timerEstado = 0f;
                    estado = EstadoBarras.Fechadas;
                }
                break;

            case EstadoBarras.Fechadas:
                // Mantém a tela fechada até o respawn chamar AbrirBarras().
                break;

            case EstadoBarras.Abertas:
                break;
        }
    }

    private bool MoveBarsToward(float alvoY, float deltaTime, float duracaoOverride = -1f)
    {
        float currentY = barraSuperior.anchoredPosition.y;
        float smoothFactor;

        if (duracaoOverride > 0f)
        {
            // Usa uma duração explícita quando o fechamento precisa ser cronometrado.
            smoothFactor = 1f - Mathf.Exp(-deltaTime / duracaoOverride * 5f);
        }
        else
        {
            // A velocidade configurada no Inspector controla a abertura normal.
            smoothFactor = 1f - Mathf.Exp(-velocidadeAbertura * deltaTime);
        }

        float nextY = Mathf.Lerp(currentY, alvoY, smoothFactor);

        if (Mathf.Abs(nextY - alvoY) < 0.5f)
            nextY = alvoY;

        SetBarsY(nextY);
        return Mathf.Abs(nextY - alvoY) < 0.01f;
    }

    private void SetBarsY(float y)
    {
        barraSuperior.anchoredPosition = new Vector2(barraSuperior.anchoredPosition.x, y);
        barraInferior.anchoredPosition = new Vector2(barraInferior.anchoredPosition.x, -y);
    }

    /// <summary>
    /// Fecha as barras até o centro e mantém a tela fechada.
    /// </summary>
    public void FecharBarras()
    {
        if (barraSuperior == null || barraInferior == null)
            return;

        timerEstado = 0f;
        abrirSemDeslocamentoExtra = false;

        float currentY = Mathf.Abs(barraSuperior.anchoredPosition.y);

        // Se ainda estiverem no deslocamento extra, retornam primeiro ao
        // deslocamento final. O fechamento só começa depois desse retorno.
        if (currentY > deslocamentoFinal + 0.5f)
        {
            estado = EstadoBarras.RetornandoDeslocamentoExtra;
            return;
        }

        // Se o Game Over abriu sem extra, as barras já estão no deslocamento
        // final. Nesse caso, não há retorno intermediário: começa diretamente
        // o hold e depois o fechamento cronometrado até o centro.
        inicioFechamentoFinalY = Mathf.Clamp(currentY, 0f, deslocamentoFinal);
        SetBarsY(inicioFechamentoFinalY);
        estado = holdNoDeslocamentoFinal > 0f
            ? EstadoBarras.HoldNoDeslocamentoFinal
            : EstadoBarras.Fechando;
    }

    /// <summary>
    /// Fechamento exclusivo do botão Restart.
    /// Começa no Deslocamento Final e fecha diretamente até o centro.
    /// Não usa o retorno do deslocamento extra.
    /// </summary>
    public void FecharBarrasDoRestart()
    {
        if (barraSuperior == null || barraInferior == null)
            return;

        timerEstado = 0f;
        abrirSemDeslocamentoExtra = false;

        // No Game Over as barras estão no Deslocamento Final, porque a
        // abertura especial nunca ativa o Deslocamento Extra.
        inicioFechamentoFinalY = Mathf.Clamp(
            Mathf.Abs(barraSuperior.anchoredPosition.y),
            0f,
            deslocamentoFinal);

        SetBarsY(inicioFechamentoFinalY);
        estado = holdNoDeslocamentoFinal > 0f
            ? EstadoBarras.HoldNoDeslocamentoFinal
            : EstadoBarras.Fechando;
    }

    /// <summary>
    /// Inicia a reabertura após manter a tela fechada pelo tempo configurado.
    /// A abertura repete deslocamento final e deslocamento extra.
    /// </summary>
    public void AbrirBarras()
    {
        abrirSemDeslocamentoExtra = false;
        IniciarAbertura();
    }

    /// <summary>
    /// Reabre imediatamente no fluxo normal após o Restart:
    /// Deslocamento Final e depois Deslocamento Extra.
    /// Não aguarda tempoBarrasFechadas.
    /// </summary>
    public void AbrirBarrasNormalImediatamente()
    {
        if (barraSuperior == null || barraInferior == null)
            return;

        abrirSemDeslocamentoExtra = false;
        timerEstado = 0f;
        estado = EstadoBarras.AbrindoDeslocamentoPrincipal;
    }

    /// <summary>
    /// Abre as barras somente até o deslocamento final, sem executar o extra.
    /// Usado para revelar a tela de Game Over.
    /// </summary>
    public void AbrirBarrasSemDeslocamentoExtra()
    {
        abrirSemDeslocamentoExtra = true;
        IniciarAbertura();
    }

    /// <summary>
    /// Abre imediatamente, sem esperar tempoBarrasFechadas, e sem deslocamento extra.
    /// Usado na entrada do menu de Game Over.
    /// </summary>
    public void AbrirBarrasSemDeslocamentoExtraImediato()
    {
        if (barraSuperior == null || barraInferior == null)
            return;

        abrirSemDeslocamentoExtra = true;
        timerEstado = 0f;
        estado = EstadoBarras.AbrindoDeslocamentoPrincipal;
    }

    private void IniciarAbertura()
    {
        if (barraSuperior == null || barraInferior == null)
            return;

        timerEstado = 0f;
        estado = tempoBarrasFechadas > 0f
            ? EstadoBarras.AguardandoReabertura
            : EstadoBarras.AbrindoDeslocamentoPrincipal;
    }

    public bool BarrasEstaoFechadas => estado == EstadoBarras.Fechadas;
    public bool BarrasEstaoAbertas => estado == EstadoBarras.Abertas;

    public float DuracaoFechamentoConfigurada =>
        duracaoRetornoDeslocamentoExtra + duracaoFechamentoFinal;
}

/*
Sequência da abertura:

Abertura normal ou após respawn:

Centro -> deslocamentoFinal -> deslocamentoFinal + deslocamentoFinalExtra
-> permanece no deslocamentoFinal + deslocamentoFinalExtra

Fechamento após a morte:

Deslocamento extra -> deslocamentoFinal
-> holdNoDeslocamentoFinal
-> centro
-> permanece fechada por tempoBarrasFechadas
-> deslocamentoFinal -> deslocamentoFinal + deslocamentoFinalExtra.
*/
