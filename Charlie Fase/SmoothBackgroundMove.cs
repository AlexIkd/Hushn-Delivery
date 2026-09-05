using UnityEngine;

/// <summary>
/// Script para fazer uma única imagem de background se mover suavemente de um lado para o outro.
/// Ideal para dar vida a menus estáticos com um movimento sutil e contínuo.
/// </summary>
public class SmoothBackgroundMove : MonoBehaviour
{
    [Header("Configurações de Movimento")]
    [Tooltip("A distância máxima que a imagem se deslocará para cada lado.")]
    public float amplitude = 50f;

    [Tooltip("A velocidade do movimento. Valores menores são mais sutis.")]
    public float velocidade = 0.5f;

    [Tooltip("Suavidade ao aplicar o movimento. Ajuda a evitar 'tremores'.")]
    public float suavidade = 5f;

    [Header("Eixos")]
    public bool moverHorizontal = true;
    public bool moverVertical = false;

    private RectTransform rectTransform;
    private Vector2 posicaoInicial;
    private Vector2 posicaoAlvo;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        
        if (rectTransform != null)
        {
            posicaoInicial = rectTransform.anchoredPosition;
        }
        else
        {
            Debug.LogError("SmoothBackgroundMove: Este script precisa de um RectTransform (UI Image).");
            enabled = false;
        }
    }

    void Update()
    {
        // Usamos Mathf.Sin (Seno) para criar um movimento de oscilação suave (vai e vem)
        // Time.time faz com que o valor mude constantemente ao longo do tempo
        float oscilacao = Mathf.Sin(Time.time * velocidade);
        
        float deslocamentoX = moverHorizontal ? oscilacao * amplitude : 0f;
        float deslocamentoY = moverVertical ? oscilacao * amplitude : 0f;

        // Calculamos a nova posição baseada na posição inicial + o deslocamento
        posicaoAlvo = new Vector2(posicaoInicial.x + deslocamentoX, posicaoInicial.y + deslocamentoY);

        // Aplicamos o movimento de forma suave usando Lerp para um visual mais polido
        rectTransform.anchoredPosition = Vector2.Lerp(
            rectTransform.anchoredPosition, 
            posicaoAlvo, 
            Time.deltaTime * suavidade
        );
    }
}
