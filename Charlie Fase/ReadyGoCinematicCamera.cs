using System.Collections;
using UnityEngine;

/// <summary>
/// Controla a câmera cinematográfica do READY GO usando dois pontos:
/// Start Point -> End Point.
/// No final, a câmera de gameplay assume a imagem e faz um blend suave
/// da posição cinematográfica até a posição normal do jogador.
/// </summary>
public class ReadyGoCinematicCamera : MonoBehaviour
{
    [Header("Câmeras")]
    [Tooltip("Câmera usada durante a introdução.")]
    [SerializeField] private Camera cinematicCamera;

    [Tooltip("Câmera normal do gameplay, geralmente a Main Camera.")]
    [SerializeField] private Camera gameplayCamera;

    [Tooltip("Script que controla a câmera de gameplay. Ele fica pausado durante a troca final.")]
    [SerializeField] private DynamicFollowCamera gameplayCameraControlScript;

    [Header("Pontos da trajetória")]
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform endPoint;

    [Tooltip("Tempo total do movimento entre Start Point e End Point.")]
    [SerializeField, Min(0f)] private float movementDuration = 2.5f;

    [SerializeField] private AnimationCurve movementCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Look At Target")]
    [Tooltip("Objeto que a câmera deve acompanhar durante o movimento.")]
    [SerializeField] private Transform lookTarget;
    [SerializeField] private float lookAtHeight = 1.2f;
    [SerializeField, Min(0.01f)] private float lookRotationSpeed = 8f;
    [SerializeField] private bool lookAtTargetDuringMovement = true;

    [Header("Transição para a câmera de gameplay")]
    [Tooltip("Tempo do blend da posição cinematográfica até a posição normal da câmera.")]
    [SerializeField, Min(0f)] private float gameplayTransitionDuration = 0.5f;

    [Tooltip("Curva usada na transição final para a câmera de gameplay.")]
    [SerializeField] private AnimationCurve gameplayTransitionCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private bool isPlaying;
    private Coroutine cameraCoroutine;
    private Vector3 fallbackPosition;
    private Quaternion fallbackRotation;
    private bool previousGameplayControlEnabled;
    private bool gameplayControlStateSaved;

    public bool IsPlaying => isPlaying;
    public bool IsTransitioning => cameraCoroutine != null;

    private void Awake()
    {
        if (cinematicCamera == null)
            cinematicCamera = GetComponentInChildren<Camera>(true);

        if (gameplayCamera == null)
            gameplayCamera = Camera.main;

        if (gameplayCamera == null)
        {
            Camera[] cameras = FindObjectsByType<Camera>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            foreach (Camera foundCamera in cameras)
            {
                if (foundCamera != cinematicCamera)
                {
                    gameplayCamera = foundCamera;
                    break;
                }
            }
        }

        if (gameplayCameraControlScript == null)
            gameplayCameraControlScript = FindFirstObjectByType<DynamicFollowCamera>();

        if (lookTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                lookTarget = player.transform;
        }

        if (cinematicCamera != null)
        {
            fallbackPosition = cinematicCamera.transform.position;
            fallbackRotation = cinematicCamera.transform.rotation;
            cinematicCamera.enabled = false;
        }
        else
        {
            Debug.LogError(
                "ReadyGoCinematicCamera: Cinematic Camera não foi configurada."
            );
        }

        if (cinematicCamera != null && cinematicCamera == gameplayCamera)
        {
            Debug.LogError(
                "ReadyGoCinematicCamera: Cinematic Camera e Gameplay Camera " +
                "não podem ser a mesma câmera."
            );
        }
    }

    public void Play()
    {
        if (cinematicCamera == null || isPlaying)
            return;

        if (cameraCoroutine != null)
            StopCoroutine(cameraCoroutine);

        cameraCoroutine = StartCoroutine(PlayRoutine());
    }

    /// <summary>
    /// Encerra a introdução e inicia o blend até a câmera normal.
    /// </summary>
    public void Stop()
    {
        if (cameraCoroutine != null)
            StopCoroutine(cameraCoroutine);

        cameraCoroutine = StartCoroutine(SwitchToGameplayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        isPlaying = true;

        cinematicCamera.enabled = true;

        if (gameplayCamera != null && gameplayCamera != cinematicCamera)
            gameplayCamera.enabled = false;

        Vector3 fromPosition = startPoint != null
            ? startPoint.position
            : fallbackPosition;
        Quaternion fromRotation = startPoint != null
            ? startPoint.rotation
            : fallbackRotation;

        Vector3 toPosition = endPoint != null
            ? endPoint.position
            : fromPosition;
        Quaternion toRotation = endPoint != null
            ? endPoint.rotation
            : fromRotation;

        cinematicCamera.transform.SetPositionAndRotation(
            fromPosition,
            fromRotation
        );

        if (movementDuration > 0f)
        {
            float elapsed = 0f;

            while (elapsed < movementDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / movementDuration);
                float curvedProgress = movementCurve.Evaluate(progress);

                cinematicCamera.transform.position = Vector3.LerpUnclamped(
                    fromPosition,
                    toPosition,
                    curvedProgress
                );

                if (lookAtTargetDuringMovement && lookTarget != null)
                    LookAtTarget();
                else
                    cinematicCamera.transform.rotation = Quaternion.Slerp(
                        fromRotation,
                        toRotation,
                        curvedProgress
                    );

                yield return null;
            }
        }

        cinematicCamera.transform.position = toPosition;

        if (lookAtTargetDuringMovement && lookTarget != null)
            LookAtTarget();
        else
            cinematicCamera.transform.rotation = toRotation;

        // A câmera permanece cinematográfica até o READY GO chamar Stop().
        while (isPlaying)
            yield return null;
    }

    private IEnumerator SwitchToGameplayRoutine()
    {
        isPlaying = false;

        if (cinematicCamera == null || gameplayCamera == null ||
            cinematicCamera == gameplayCamera)
        {
            if (gameplayCamera == null)
            {
                // Nunca desligue a câmera cinematográfica se não existe uma
                // câmera de destino válida para assumir a renderização.
                if (cinematicCamera != null)
                    cinematicCamera.enabled = true;

                Debug.LogError(
                    "ReadyGoCinematicCamera: Gameplay Camera não foi encontrada. " +
                    "A câmera cinematográfica permanecerá ativa."
                );
            }
            else
            {
                gameplayCamera.gameObject.SetActive(true);
                gameplayCamera.enabled = true;

                if (cinematicCamera != null && cinematicCamera != gameplayCamera)
                    cinematicCamera.enabled = false;
            }

            cameraCoroutine = null;
            yield break;
        }

        // Salva a posição normal que a câmera de gameplay calculou antes da troca.
        Vector3 gameplayTargetPosition = gameplayCamera.transform.position;
        Quaternion gameplayTargetRotation = gameplayCamera.transform.rotation;
        float gameplayTargetFov = gameplayCamera.fieldOfView;

        // Guarda a posição da câmera cinematográfica. A Main Camera assumirá
        // essa posição antes de a câmera cinematográfica ser desligada.
        Vector3 cinematicPosition = cinematicCamera.transform.position;
        Quaternion cinematicRotation = cinematicCamera.transform.rotation;
        float cinematicFov = cinematicCamera.fieldOfView;

        if (gameplayCameraControlScript != null)
        {
            previousGameplayControlEnabled = gameplayCameraControlScript.enabled;
            gameplayControlStateSaved = true;
            gameplayCameraControlScript.enabled = false;
        }

        // A Main Camera assume a imagem já na posição da cinematográfica.
        // O DynamicFollowCamera continua desligado durante todo o blend.
        gameplayCamera.gameObject.SetActive(true);
        gameplayCamera.transform.SetPositionAndRotation(
            cinematicPosition,
            cinematicRotation
        );
        gameplayCamera.fieldOfView = cinematicFov;
        gameplayCamera.enabled = true;

        if (!IsCameraRenderReady(gameplayCamera))
        {
            cinematicCamera.enabled = true;
            Debug.LogError(
                "ReadyGoCinematicCamera: não foi possível ativar a Main Camera. " +
                "A câmera cinematográfica permanecerá renderizando."
            );
            cameraCoroutine = null;
            yield break;
        }

        // Só agora a cinematográfica deixa de renderizar.
        cinematicCamera.enabled = false;

        // Segunda verificação no frame da troca. Se outro script desligar a
        // Main Camera, a cinematográfica volta imediatamente como segurança.
        if (!IsCameraRenderReady(gameplayCamera))
        {
            cinematicCamera.enabled = true;
            gameplayCamera.enabled = true;
            Debug.LogError(
                "ReadyGoCinematicCamera: outro script desativou a Gameplay Camera " +
                "durante a troca. A câmera cinematográfica foi mantida ativa."
            );
            cameraCoroutine = null;
            yield break;
        }

        if (gameplayTransitionDuration > 0f)
        {
            float elapsed = 0f;

            while (elapsed < gameplayTransitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(
                    elapsed / gameplayTransitionDuration
                );
                float curvedProgress = gameplayTransitionCurve.Evaluate(progress);

                gameplayCamera.transform.position = Vector3.LerpUnclamped(
                    cinematicPosition,
                    gameplayTargetPosition,
                    curvedProgress
                );

                gameplayCamera.transform.rotation = Quaternion.Slerp(
                    cinematicRotation,
                    gameplayTargetRotation,
                    curvedProgress
                );

                gameplayCamera.fieldOfView = Mathf.Lerp(
                    cinematicFov,
                    gameplayTargetFov,
                    curvedProgress
                );

                yield return null;
            }
        }

        gameplayCamera.transform.SetPositionAndRotation(
            gameplayTargetPosition,
            gameplayTargetRotation
        );
        gameplayCamera.fieldOfView = gameplayTargetFov;

        if (gameplayCameraControlScript is DynamicFollowCamera dynamicCamera)
            dynamicCamera.SyncRotationToCurrentTransform();

        if (gameplayCameraControlScript != null && gameplayControlStateSaved)
            gameplayCameraControlScript.enabled = previousGameplayControlEnabled;

        gameplayControlStateSaved = false;
        cameraCoroutine = null;
    }

    private bool IsCameraRenderReady(Camera camera)
    {
        return camera != null &&
               camera.gameObject.activeInHierarchy &&
               camera.enabled &&
               camera.isActiveAndEnabled;
    }

    private void LookAtTarget()
    {
        if (cinematicCamera == null || lookTarget == null)
            return;

        Vector3 targetPosition = lookTarget.position +
                                 Vector3.up * lookAtHeight;
        Vector3 direction = targetPosition - cinematicCamera.transform.position;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion desiredRotation =
            Quaternion.LookRotation(direction.normalized, Vector3.up);

        float rotationT = 1f - Mathf.Exp(
            -lookRotationSpeed * Time.unscaledDeltaTime
        );

        cinematicCamera.transform.rotation = Quaternion.Slerp(
            cinematicCamera.transform.rotation,
            desiredRotation,
            rotationT
        );
    }
}
