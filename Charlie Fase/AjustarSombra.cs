using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteAlways] // Faz o script funcionar mesmo sem dar Play
public class ControladorDeSombra : MonoBehaviour
{
    [Header("Configurações de Sombra")]
    [Range(0, 2000)] // Cria a barra deslizante de 0 a 2000
    public float distanciaDaSombra = 150f;

    // Atualiza quando você move a barra no Inspector
    void OnValidate()
    {
        AtualizarSombra();
    }

    // Atualiza durante o jogo
    void Update()
    {
        // ExecuteAlways garante que isso rode no Editor também
        if (!Application.isPlaying) 
        {
            AtualizarSombra();
        }
    }

    void AtualizarSombra()
    {
        // Pega o Asset do URP que está sendo usado agora
        var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        
        if (pipeline != null)
        {
            pipeline.shadowDistance = distanciaDaSombra;
        }
    }
}
