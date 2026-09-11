using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Move uma plataforma entre as posições inicial e final.
/// Também envia o deslocamento ao PlayerMovement_FrontiersStyle apoiado nela.
/// </summary>
public class LeverPlatformMover : MonoBehaviour
{
    [Header("Pontos da plataforma")]
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform endPoint;

    [Header("Movimento")]
    [SerializeField, Min(0.01f)] private float movementDuration = 1.5f;
    [SerializeField] private AnimationCurve movementCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private bool useUnscaledTime = false;

    [Header("Transportar o jogador")]
    [Tooltip("Envia o deslocamento para o PlayerMovement do jogador apoiado.")]
    [SerializeField] private bool carryPlayer = true;
    [Tooltip("Arraste o PlayerMovement_FrontiersStyle do jogador. Se vazio, tenta encontrar pela tag Player.")]
    [SerializeField] private PlayerMovement_FrontiersStyle playerToCarry;
    [SerializeField] private Collider platformCollider;
    [SerializeField] private LayerMask riderLayers = ~0;
    [SerializeField, Min(0.01f)] private float riderHeightTolerance = 0.3f;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private Vector3 endPosition;
    private Quaternion endRotation;
    private Coroutine movementCoroutine;
    private bool isAtEnd;
    private bool pingPongRequested;

    public bool IsAtEnd => isAtEnd;
    public bool IsMoving { get; private set; }
    public bool IsPingPongActive => pingPongRequested;

    private void Awake()
    {
        if (platformCollider == null)
            platformCollider = GetComponent<Collider>();

        if (playerToCarry == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                playerToCarry = playerObject.GetComponent<PlayerMovement_FrontiersStyle>();
        }

        startPosition = startPoint != null ? startPoint.position : transform.position;
        startRotation = startPoint != null ? startPoint.rotation : transform.rotation;
        endPosition = endPoint != null ? endPoint.position : startPosition;
        endRotation = endPoint != null ? endPoint.rotation : startRotation;

        transform.SetPositionAndRotation(startPosition, startRotation);
        isAtEnd = false;
    }

    public void MoveToEnd()
    {
        pingPongRequested = false;
        StartMovement(endPosition, endRotation, true);
    }

    public void MoveToStart()
    {
        pingPongRequested = false;
        StartMovement(startPosition, startRotation, false);
    }

    public void TogglePosition()
    {
        if (isAtEnd) MoveToStart();
        else MoveToEnd();
    }

    public void StartPingPong()
    {
        pingPongRequested = true;

        if (movementCoroutine != null)
            StopCoroutine(movementCoroutine);

        movementCoroutine = StartCoroutine(PingPongRoutine());
    }

    public void StopPingPongAndReturnToStart()
    {
        pingPongRequested = false;

        if (movementCoroutine != null)
            StopCoroutine(movementCoroutine);

        movementCoroutine = StartCoroutine(MoveRoutine(
            transform.position,
            transform.rotation,
            startPosition,
            startRotation,
            false
        ));
    }

    private void StartMovement(Vector3 targetPosition, Quaternion targetRotation, bool targetIsEnd)
    {
        if (movementCoroutine != null)
            StopCoroutine(movementCoroutine);

        movementCoroutine = StartCoroutine(MoveRoutine(
            transform.position,
            transform.rotation,
            targetPosition,
            targetRotation,
            targetIsEnd
        ));
    }

    private IEnumerator PingPongRoutine()
    {
        IsMoving = false;

        while (pingPongRequested)
        {
            yield return MoveRoutine(transform.position, transform.rotation,
                endPosition, endRotation, true);

            if (!pingPongRequested)
                break;

            yield return MoveRoutine(transform.position, transform.rotation,
                startPosition, startRotation, false);
        }

        IsMoving = false;
        movementCoroutine = null;
    }

    private IEnumerator MoveRoutine(
        Vector3 fromPosition,
        Quaternion fromRotation,
        Vector3 targetPosition,
        Quaternion targetRotation,
        bool targetIsEnd)
    {
        IsMoving = true;
        float elapsed = 0f;

        while (elapsed < movementDuration)
        {
            List<PlayerMovement_FrontiersStyle> riders = FindRidersOnPlatform();
            Vector3 previousPosition = transform.position;

            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / movementDuration);
            float curvedProgress = movementCurve.Evaluate(progress);

            transform.position = Vector3.LerpUnclamped(
                fromPosition, targetPosition, curvedProgress);
            transform.rotation = Quaternion.Slerp(
                fromRotation, targetRotation, curvedProgress);

            Vector3 platformDelta = transform.position - previousPosition;

            foreach (PlayerMovement_FrontiersStyle rider in riders)
            {
                if (rider != null)
                    rider.AddPlatformDisplacement(platformDelta);
            }

            yield return null;
        }

        List<PlayerMovement_FrontiersStyle> finalRiders = FindRidersOnPlatform();
        Vector3 finalDelta = targetPosition - transform.position;
        transform.SetPositionAndRotation(targetPosition, targetRotation);

        foreach (PlayerMovement_FrontiersStyle rider in finalRiders)
        {
            if (rider != null)
                rider.AddPlatformDisplacement(finalDelta);
        }

        isAtEnd = targetIsEnd;
        IsMoving = false;
    }

    private List<PlayerMovement_FrontiersStyle> FindRidersOnPlatform()
    {
        List<PlayerMovement_FrontiersStyle> riders =
            new List<PlayerMovement_FrontiersStyle>();

        if (!carryPlayer || platformCollider == null)
            return riders;

        Bounds bounds = platformCollider.bounds;

        // Caminho principal: usa diretamente o jogador configurado, evitando
        // que a troca de bounds durante o movimento faça a detecção falhar.
        if (playerToCarry != null && IsPlayerStandingOnPlatform(playerToCarry, bounds))
        {
            riders.Add(playerToCarry);
            return riders;
        }

        Collider[] colliders = Physics.OverlapBox(
            bounds.center,
            bounds.extents + Vector3.one * 0.03f,
            Quaternion.identity,
            riderLayers,
            QueryTriggerInteraction.Ignore
        );

        foreach (Collider collider in colliders)
        {
            PlayerMovement_FrontiersStyle movement =
                collider.GetComponentInParent<PlayerMovement_FrontiersStyle>();

            if (movement == null || riders.Contains(movement))
                continue;

            CharacterController characterController =
                movement.GetComponent<CharacterController>();

            if (characterController == null)
                continue;

            if (IsPlayerStandingOnPlatform(movement, bounds))
                riders.Add(movement);
        }

        return riders;
    }

    private bool IsPlayerStandingOnPlatform(
        PlayerMovement_FrontiersStyle movement,
        Bounds platformBounds)
    {
        if (movement == null)
            return false;

        CharacterController rider = movement.GetComponent<CharacterController>();
        if (rider == null || !rider.enabled || !rider.gameObject.activeInHierarchy)
            return false;

        Bounds riderBounds = rider.bounds;
        float platformTop = platformBounds.max.y;
        float riderBottom = riderBounds.min.y;

        bool horizontalOverlap =
            riderBounds.max.x >= platformBounds.min.x &&
            riderBounds.min.x <= platformBounds.max.x &&
            riderBounds.max.z >= platformBounds.min.z &&
            riderBounds.min.z <= platformBounds.max.z;

        bool heightCloseEnough =
            riderBottom >= platformTop - riderHeightTolerance &&
            riderBottom <= platformTop + riderHeightTolerance;

        return horizontalOverlap && heightCloseEnough;
    }
}
