using UnityEngine;

public class SlopeSlideDebugger : MonoBehaviour
{
    private CharacterController controller;
    private SlopeSlideSystem slideSystem;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        slideSystem = GetComponent<SlopeSlideSystem>();
        
        Debug.Log("=== DIAGNÓSTICO DE SLIDE INICIADO ===");
        if (controller == null) Debug.LogError("ERRO: CharacterController não encontrado no jogador!");
        if (slideSystem == null) Debug.LogError("ERRO: SlopeSlideSystem não encontrado no jogador!");
    }

    void Update()
    {
        if (slideSystem != null)
        {
            // Mostra o estado no console apenas quando mudar
            if (slideSystem.IsSliding())
            {
                Debug.DrawRay(transform.position, transform.forward * 2f, Color.red);
            }
        }
    }

    // Verifica se o Trigger está funcionando
    void OnTriggerEnter(Collider other)
    {
        Debug.Log("ENTROU EM TRIGGER: " + other.gameObject.name + " | Layer: " + LayerMask.LayerToName(other.gameObject.layer));
        
        SlopeRamp ramp = other.GetComponent<SlopeRamp>();
        if (ramp != null)
        {
            Debug.Log("SUCESSO: Componente SlopeRamp encontrado!");
        }
        else
        {
            Debug.LogWarning("AVISO: Objeto tem a Layer certa, mas NÃO tem o script SlopeRamp anexado.");
        }
    }
}
