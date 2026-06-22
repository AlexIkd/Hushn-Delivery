using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class CinematicCameraTrigger : MonoBehaviour
{
    [Header("Configurações de Câmera")]
    [SerializeField] private GameObject cinematicCamera; // Referência à câmera cinematográfica (ex: Cinemachine Virtual Camera)
    
    [Header("Tempos de Transição")]
    [Tooltip("Tempo para a câmera transicionar da principal para a cinematográfica.")]
    [SerializeField] private float transitionInDuration = 0.5f;
    [Tooltip("Tempo para a câmera transicionar da cinematográfica de volta para a principal.")]
    [SerializeField] private float transitionOutDuration = 0.5f;

    [Header("Configurações de Uso")]
    [SerializeField] private bool triggerOnce = true; // Se deve disparar apenas uma vez
    [SerializeField] private float cooldownDuration = 2.0f; // Cooldown entre ativações se triggerOnce for false
    
    private bool hasTriggered = false;
    private float lastDeactivationTime = -Mathf.Infinity;
    private bool isActive = false;

    private void Awake()
    {
        // Garante que o collider seja um trigger
        BoxCollider box = GetComponent<BoxCollider>();
        box.isTrigger = true;

        // Garante que a câmera comece desativada
        if (cinematicCamera != null)
        {
            cinematicCamera.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (triggerOnce && hasTriggered) return;
            if (Time.time < lastDeactivationTime + cooldownDuration) return;

            ActivateCamera();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && isActive)
        {
            DeactivateCamera();
        }
    }

    private void ActivateCamera()
    {
        if (cinematicCamera == null || isActive) return;

        if (CameraRailManager.Instance != null)
        {
            // Inicia a transição suave usando o sistema que você já possui
            CameraRailManager.Instance.StartTransitionToRail(cinematicCamera, transitionInDuration);
        }
        else
        {
            // Fallback para troca instantânea
            if (Camera.main != null) Camera.main.gameObject.SetActive(false);
            cinematicCamera.SetActive(true);
        }

        isActive = true;
        hasTriggered = true;
    }

    private void DeactivateCamera()
    {
        if (cinematicCamera == null || !isActive) return;

        if (CameraRailManager.Instance != null)
        {
            // IMPORTANTE: Para o seu CameraRailManager, o primeiro parâmetro do StartTransitionToMain 
            // deve ser o Transform de destino (geralmente o jogador ou o ponto onde a câmera principal deveria estar).
            // Vamos passar o próprio jogador como alvo para garantir que o manager saiba para onde voltar.
            
            Transform mainTarget = null;
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) mainTarget = player.transform;

            // Inicia a transição de volta usando o seu manager
            CameraRailManager.Instance.StartTransitionToMain(mainTarget, cinematicCamera, transitionOutDuration);
        }
        else
        {
            // Fallback para troca instantânea
            if (Camera.main != null) Camera.main.gameObject.SetActive(true);
            cinematicCamera.SetActive(false);
        }

        isActive = false;
        lastDeactivationTime = Time.time;
    }
}
