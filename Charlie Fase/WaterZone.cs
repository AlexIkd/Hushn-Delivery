using UnityEngine;

/// <summary>
/// Gerencia as zonas de água e controla a transição para o sistema de movimentação na água
/// </summary>
public class WaterZone : MonoBehaviour
{
    [Header("Configurações de Entrada")]
    [SerializeField] private float minSpeedToSlide = 5f;
    [SerializeField] private float slideSpeedMultiplier = 1.2f;

    [Header("Efeitos")]
    [SerializeField] private ParticleSystem splashParticles;
    [SerializeField] private AudioClip splashSound;
    [SerializeField] private bool playEffects = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    private WaterMovement_System waterMovement;
    private AudioSource audioSource;

    void Start()
    {
        // Garantir que há um Collider configurado como trigger
        Collider collider = GetComponent<Collider>();
        if (collider == null)
        {
            Debug.LogError("WaterZone requer um Collider!");
            return;
        }

        if (!collider.isTrigger)
        {
            Debug.LogWarning("WaterZone: Collider não está configurado como trigger. Ajustando...");
            collider.isTrigger = true;
        }

        // Obter AudioSource se houver som de splash
        if (splashSound != null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Verificar se é o jogador
        PlayerMovement_FrontiersStyle playerMovement = other.GetComponent<PlayerMovement_FrontiersStyle>();
        if (playerMovement == null)
            return;

        // Obter o sistema de movimentação na água
        waterMovement = other.GetComponent<WaterMovement_System>();
        if (waterMovement == null)
        {
            Debug.LogError("WaterMovement_System não encontrado no jogador!");
            return;
        }

        // Verificar se o jogador tem velocidade suficiente para deslizar
        float playerSpeed = playerMovement.currentSpeed;

        if (playerSpeed >= minSpeedToSlide)
        {
            // Calcular direção do deslizamento baseada na velocidade atual
            Vector3 slideDirection = other.transform.forward;
            float slideSpeed = playerSpeed * slideSpeedMultiplier;

            // Iniciar movimento na água
            waterMovement.EnterWater(slideDirection, slideSpeed);

            // Reproduzir efeitos
            if (playEffects)
            {
                PlaySplashEffects(other.transform.position);
            }

            if (showDebugInfo)
                Debug.Log($"💧 Jogador entrou na água com velocidade: {playerSpeed}");
        }
        else
        {
            if (showDebugInfo)
                Debug.Log($"⚠️ Jogador entrou na água mas velocidade insuficiente: {playerSpeed} < {minSpeedToSlide}");
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Verificar se é o jogador
        PlayerMovement_FrontiersStyle playerMovement = other.GetComponent<PlayerMovement_FrontiersStyle>();
        if (playerMovement == null)
            return;

        waterMovement = other.GetComponent<WaterMovement_System>();
        if (waterMovement != null && waterMovement.IsInWater)
        {
            // O script de movimentação na água já cuida da saída
            if (showDebugInfo)
                Debug.Log("💧 Jogador saiu da zona de água");
        }
    }

    /// <summary>
    /// Reproduz efeitos de splash
    /// </summary>
    private void PlaySplashEffects(Vector3 position)
    {
        // Reproduzir partículas
        if (splashParticles != null)
        {
            ParticleSystem splash = Instantiate(splashParticles, position, Quaternion.identity);
            splash.Play();
            Destroy(splash.gameObject, 2f);
        }

        // Reproduzir som
        if (audioSource != null && splashSound != null)
        {
            audioSource.PlayOneShot(splashSound);
        }
    }
}
