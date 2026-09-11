using UnityEngine;

/// <summary>
/// Anel de impulso aéreo. Quando o jogador atravessa o trigger,
/// recebe velocidade na direção forward do anel.
/// </summary>
[RequireComponent(typeof(Collider))]
public class AirDashRing : MonoBehaviour
{
    [Header("Impulso")]
    [Tooltip("Velocidade aplicada na direção forward do anel.")]
    [SerializeField, Min(0f)] private float forwardImpulse = 24f;

    [Tooltip("Impulso vertical opcional aplicado junto com o avanço.")]
    [SerializeField] private float upwardImpulse = 0f;

    [Tooltip("Se ativado, o jogador precisa estar no ar para ativar o anel.")]
    [SerializeField] private bool requirePlayerInAir = true;

    [Tooltip("Permite ativar o mesmo anel novamente depois deste tempo.")]
    [SerializeField, Min(0f)] private float reactivationDelay = 0.15f;

    [Tooltip("Tempo em que o input de movimento fica bloqueado após atravessar o anel.")]
    [SerializeField, Min(0f)] private float movementLockDuration = 0.15f;

    [Header("Câmera")]
    [SerializeField] private DynamicFollowCamera gameplayCamera;

    [SerializeField] private LayerMask playerLayers = ~0;
    [SerializeField] private bool useRingUpAxisForVerticalImpulse = false;

    private Collider ringCollider;
    private float nextActivationTime;

    private void Awake()
    {
        ringCollider = GetComponent<Collider>();
        ringCollider.isTrigger = true;

        if (gameplayCamera == null)
            gameplayCamera = FindFirstObjectByType<DynamicFollowCamera>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Time.time < nextActivationTime)
            return;

        if (!IsInPlayerLayer(other.gameObject.layer))
            return;

        PlayerMovement_FrontiersStyle movement =
            other.GetComponentInParent<PlayerMovement_FrontiersStyle>();

        if (movement == null)
            return;

        if (requirePlayerInAir && movement.isGrounded)
            return;

        ActivateFor(movement);
    }

    private void ActivateFor(PlayerMovement_FrontiersStyle movement)
    {
        Vector3 direction = transform.forward;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            direction = Vector3.forward;
        else
            direction.Normalize();

        Vector3 impulse = direction * forwardImpulse;

        if (useRingUpAxisForVerticalImpulse)
            impulse += transform.up * upwardImpulse;
        else
            impulse.y += upwardImpulse;

        movement.LaunchAirDashFromRing(
            direction,
            forwardImpulse,
            impulse.y,
            movementLockDuration,
            true
        );

        if (gameplayCamera != null)
        {
            gameplayCamera.TriggerRingHeightBoost();
            gameplayCamera.TriggerRingCameraCenter();
        }
        nextActivationTime = Time.time + reactivationDelay;

        // O anel não ativa os efeitos visuais/sonoros do air dash.
    }

    private bool IsInPlayerLayer(int layer)
    {
        return (playerLayers.value & (1 << layer)) != 0;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(
            transform.position,
            transform.position + transform.forward * 2f
        );
    }
}
