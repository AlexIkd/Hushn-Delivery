using UnityEngine;
using System.Collections;

public class SonicSpring : MonoBehaviour
{
    [Header("Configurações de Lançamento")]
    [SerializeField] private float launchForce = 25f;
    [SerializeField] private bool useForwardDirection = true;
    [SerializeField] private Vector3 customDirection = Vector3.up;
    [SerializeField] private float groundCheckCooldown = 0.15f; // Evita que o player detecte chão no frame do lançamento
    [SerializeField] private float lockInputDuration = 0.3f; // Trava o input para garantir a trajetória da mola

    [Header("Visual & Áudio")]
    [SerializeField] private string animationTriggerName = "SpringTrigger";
    [SerializeField] private ParticleSystem springParticles;
    [SerializeField] private AudioClip springSound;
    [SerializeField] private float cooldown = 0.5f;

    private Animator animator;
    private AudioSource audioSource;
    private float lastLaunchTime = -1f;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && springSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        CheckLaunch(other);
    }

    private void OnTriggerStay(Collider other)
    {
        CheckLaunch(other);
    }

    private void CheckLaunch(Collider other)
    {
        if (Time.time < lastLaunchTime + cooldown) return;

        // Tenta encontrar o script de movimento no objeto ou em seus pais
        PlayerMovement_FrontiersStyle player = other.GetComponentInParent<PlayerMovement_FrontiersStyle>();
        
        if (player != null)
        {
            LaunchPlayer(player);
        }
    }

    private void LaunchPlayer(PlayerMovement_FrontiersStyle player)
    {
        lastLaunchTime = Time.time;

        // 1. Determina a direção do lançamento
        Vector3 launchDir = useForwardDirection ? transform.forward : customDirection.normalized;
        Vector3 finalVelocity = launchDir * launchForce;

        // 2. Cancela estados conflitantes
        player.CancelStomp();
        player.CancelAirDash();
        player.CancelGlide();
        player.CancelWallRun();

        // 3. Reseta cargas de pulo/dash (Estilo Sonic)
        player.ResetAirCharges();

        // 4. Aplica o lançamento usando o novo método do PlayerMovement
        player.ExecuteJump(finalVelocity, groundCheckCooldown);
        player.SetSpringLaunchLock(lockInputDuration);

        // 5. Feedback visual e sonoro na mola
        if (animator != null) animator.SetTrigger(animationTriggerName);
        if (springParticles != null) springParticles.Play();
        if (audioSource != null && springSound != null) audioSource.PlayOneShot(springSound);

        Debug.Log($"[SonicSpring] Jogador lançado com força {launchForce} na direção {launchDir}");
    }

    private void OnDrawGizmos()
    {
        // Desenha a direção do pulo no Editor
        Vector3 launchDir = useForwardDirection ? transform.forward : customDirection.normalized;
        
        Gizmos.color = Color.cyan;
        Vector3 start = transform.position;
        Vector3 end = start + launchDir * 2f;

        // Desenha a linha principal
        Gizmos.DrawLine(start, end);

        // Desenha uma pequena ponta de flecha
        float arrowHeadSize = 0.5f;
        Vector3 right = Quaternion.LookRotation(launchDir) * Quaternion.Euler(0, 160, 0) * Vector3.forward;
        Vector3 left = Quaternion.LookRotation(launchDir) * Quaternion.Euler(0, -160, 0) * Vector3.forward;
        Gizmos.DrawRay(end, right * arrowHeadSize);
        Gizmos.DrawRay(end, left * arrowHeadSize);

        // Desenha uma esfera no ponto de origem
        Gizmos.DrawWireSphere(start, 0.2f);
    }
}
