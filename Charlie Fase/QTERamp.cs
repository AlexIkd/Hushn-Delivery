using UnityEngine;

public class QTERamp : MonoBehaviour
{
    [Header("Configurações do QTE")]
    public float qteDuration = 1.5f;
    public int sequenceLength = 3;
    
    [Header("Impulso Inicial (Obrigatório)")]
    public Vector3 initialLaunchDirection = new Vector3(0, 0.5f, 1);
    public float initialLaunchForce = 25f;
    public float initialLockDuration = 0.3f;

    [Header("Bônus de Sucesso (QTE)")]
    public Vector3 successExtraDirection = new Vector3(0, 1, 1);
    public float successExtraForce = 25f;
    public float successLockDuration = 1.0f;

    [Header("Trajetória de Falha")]
    [Tooltip("Tempo de trava de input caso erre o QTE (mantém o impulso inicial)")]
    public float failLockDuration = 0.5f;

    [Header("Efeitos")]
    public ParticleSystem successParticles;
    public ParticleSystem failParticles;
    public AudioClip successSFX;
    public AudioClip failSFX;

    private bool playerInRamp = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !playerInRamp)
        {
            playerInRamp = true;
            StartRampQTE(other.gameObject);
        }
    }

    private void StartRampQTE(GameObject player)
    {
        // 1. Aplica o Impulso Inicial IMEDIATAMENTE
        ApplyInitialLaunch(player);

        if (QTEHandler.Instance == null)
        {
            Debug.LogError("QTEHandler não encontrado na cena!");
            playerInRamp = false;
            return;
        }

        // 2. Inicia o QTE em Slow Motion
        QTEHandler.Instance.StartQTE(qteDuration, sequenceLength, (success) => {
            if (success)
            {
                ApplySuccessBonus(player);
            }
            else
            {
                ApplyFailState(player);
            }
            playerInRamp = false;
        });
    }

    private void ApplyInitialLaunch(GameObject player)
    {
        PlayerMovement_FrontiersStyle movement = player.GetComponent<PlayerMovement_FrontiersStyle>();
        if (movement == null) return;

        Vector3 worldDir = transform.TransformDirection(initialLaunchDirection.normalized);
        movement.ExecuteJump(worldDir * initialLaunchForce);
        movement.SetSpringLaunchLock(initialLockDuration);
        movement.ResetAirCharges();
        
        Debug.Log("QTE: Impulso Inicial Aplicado.");
    }

    private void ApplySuccessBonus(GameObject player)
    {
        PlayerMovement_FrontiersStyle movement = player.GetComponent<PlayerMovement_FrontiersStyle>();
        if (movement == null) return;

        Vector3 worldDir = transform.TransformDirection(successExtraDirection.normalized);
        
        // Aplica o bônus (ExecuteJump substitui a velocidade atual, dando o novo impulso)
        movement.ExecuteJump(worldDir * (initialLaunchForce + successExtraForce));
        movement.SetSpringLaunchLock(successLockDuration);

        if (successParticles) successParticles.Play();
        if (successSFX) AudioSource.PlayClipAtPoint(successSFX, transform.position);
        Debug.Log("QTE SUCESSO! Bônus de Lançamento Aplicado.");
    }

    private void ApplyFailState(GameObject player)
    {
        PlayerMovement_FrontiersStyle movement = player.GetComponent<PlayerMovement_FrontiersStyle>();
        if (movement == null) return;

        // Apenas mantém a trava de input por mais um tempo, sem dar novo impulso
        movement.SetSpringLaunchLock(failLockDuration);

        if (failParticles) failParticles.Play();
        if (failSFX) AudioSource.PlayClipAtPoint(failSFX, transform.position);
        Debug.Log("QTE FALHA! Mantendo impulso padrão.");
    }

    private void OnDrawGizmos()
    {
        // Visualização das trajetórias no Editor
        Gizmos.color = Color.yellow; // Inicial
        DrawArrow(transform.position, transform.TransformDirection(initialLaunchDirection.normalized) * 3f);

        Gizmos.color = Color.green; // Sucesso (Total)
        DrawArrow(transform.position, transform.TransformDirection(successExtraDirection.normalized) * 5f);
    }

    private void DrawArrow(Vector3 pos, Vector3 direction)
    {
        Gizmos.DrawRay(pos, direction);
        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + 20, 0) * Vector3.forward;
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - 20, 0) * Vector3.forward;
        Gizmos.DrawRay(pos + direction, right * 0.5f);
        Gizmos.DrawRay(pos + direction, left * 0.5f);
    }
}
