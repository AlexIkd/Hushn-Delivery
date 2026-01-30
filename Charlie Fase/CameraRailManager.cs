using UnityEngine;
using System.Collections;

/// <summary>
/// Gerencia a transicao suave entre a camera principal do jogador e as cameras cinematicas dos rails.
/// Esta versao usa Coroutines e Lerp/Slerp para transicao suave, sem depender do Cinemachine.
/// </summary>
public class CameraRailManager : MonoBehaviour
{
    public static CameraRailManager Instance { get; private set; }

    [Header("Configuracao da Camera")]
    [Tooltip("Se a transicao cinematica de camera deve ser ativada.")]
    [SerializeField] private bool enableCinematicCamera = true;

    [Tooltip("A camera principal do jogo (Camera.main).")]
    [SerializeField] private Camera mainCamera;

    [Header("Componente de Controle da Camera Principal")]
    [Tooltip("O script que controla o movimento da camera principal (ex: CameraController.cs). Deve ser desativado durante a transicao.")]
    [SerializeField] private MonoBehaviour mainCameraControlScript;
    
    [Tooltip("O tempo de transicao padrao (em segundos) caso nenhum seja especificado.")]
    [SerializeField] private float defaultTransitionDuration = 0.5f;

    private Coroutine transitionCoroutine;
    private bool isTransitioning = false;
    private float originalFOV;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
            if (mainCamera == null)
            {
                Debug.LogError("Nenhuma Camera Principal (tag 'MainCamera') encontrada na cena.");
            }
            else
            {
                originalFOV = mainCamera.fieldOfView;
            }
        }
    }

    /// <summary>
    /// Inicia a transicao suave para a camera do rail.
    /// </summary>
    /// <param name="railCameraObject">O GameObject da camera do rail.</param>
    /// <param name="duration">Duracao da transicao (opcional).</param>
    public void StartTransitionToRail(GameObject railCameraObject, float duration = -1f)
    {
        if (!enableCinematicCamera) return;
        if (mainCamera == null || railCameraObject == null) return;

        if (isTransitioning)
        {
            StopCoroutine(transitionCoroutine);
        }

        float finalDuration = duration > 0 ? duration : defaultTransitionDuration;

        // 1. Ativa a camera do rail para que possamos obter sua Transform
        railCameraObject.SetActive(true);

        // 2. Inicia a transicao
        Transform targetTransform = railCameraObject.transform;
        transitionCoroutine = StartCoroutine(TransitionCamera(mainCamera.transform, targetTransform, railCameraObject, false, finalDuration));
        
        Debug.Log($"Iniciando transicao suave para camera do Rail: {railCameraObject.name} em {finalDuration}s");
    }

    /// <summary>
    /// Inicia a transicao suave de volta para a camera principal do jogador.
    /// </summary>
    /// <param name="mainCameraTarget">O Transform que define a posicao e rotacao desejada da camera principal.</param>
    /// <param name="currentRailCameraObject">O GameObject da camera do rail que esta sendo desativada.</param>
    /// <param name="duration">Duracao da transicao (opcional).</param>
    public void StartTransitionToMain(Transform mainCameraTarget, GameObject currentRailCameraObject, float duration = -1f)
    {
        if (!enableCinematicCamera) return;
        if (mainCamera == null || mainCameraTarget == null) return;

        if (isTransitioning)
        {
            StopCoroutine(transitionCoroutine);
        }

        float finalDuration = duration > 0 ? duration : defaultTransitionDuration;

        // 1. A camera principal deve estar ativa para receber a transicao
        mainCamera.gameObject.SetActive(true);

        // 2. Inicia a transicao
        transitionCoroutine = StartCoroutine(TransitionCamera(mainCamera.transform, mainCameraTarget, currentRailCameraObject, true, finalDuration));
        
        Debug.Log($"Iniciando transicao suave de volta para camera Principal em {finalDuration}s");
    }

    /// <summary>
    /// Corrotina que gerencia a transicao suave.
    /// </summary>
    private IEnumerator TransitionCamera(Transform cameraTransform, Transform targetTransform, GameObject cameraToDeactivate, bool isReturningToMain, float duration)
    {
        isTransitioning = true;
        float elapsedTime = 0f;

        Vector3 startPosition = cameraTransform.position;
        Quaternion startRotation = cameraTransform.rotation;
        
        // FOV
        float startFOV = mainCamera.fieldOfView;
        float targetFOV = originalFOV;
        
        if (!isReturningToMain)
        {
            Camera railCam = cameraToDeactivate.GetComponent<Camera>();
            if (railCam != null)
            {
                targetFOV = railCam.fieldOfView;
            }
        }
        
        if (mainCameraControlScript != null)
        {
            mainCameraControlScript.enabled = false;
        }

        mainCamera.enabled = true;

        if (!isReturningToMain)
        {
            if (cameraToDeactivate != null)
            {
                Camera railCam = cameraToDeactivate.GetComponent<Camera>();
                if (railCam != null) railCam.enabled = false;
            }
        }

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            // Suavizacao da curva de transicao (SmoothStep)
            t = t * t * (3f - 2f * t); 

            cameraTransform.position = Vector3.Lerp(startPosition, targetTransform.position, t);
            cameraTransform.rotation = Quaternion.Slerp(startRotation, targetTransform.rotation, t);
            mainCamera.fieldOfView = Mathf.Lerp(startFOV, targetFOV, t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        cameraTransform.position = targetTransform.position;
        cameraTransform.rotation = targetTransform.rotation;
        mainCamera.fieldOfView = targetFOV;

        if (isReturningToMain)
        {
            if (cameraToDeactivate != null)
            {
                cameraToDeactivate.SetActive(false);
            }
        }
        else
        {
            if (cameraToDeactivate != null)
            {
                Camera railCam = cameraToDeactivate.GetComponent<Camera>();
                if (railCam != null) railCam.enabled = true;
            }
            mainCamera.enabled = false;
        }

        isTransitioning = false;
        Debug.Log("Transicao de camera concluida.");
    }

    public bool IsTransitioning()
    {
        return isTransitioning;
    }

    public void ForceDeactivateRailCamera(GameObject railCameraObject)
    {
        if (isTransitioning)
        {
            StopCoroutine(transitionCoroutine);
            isTransitioning = false;
        }

        if (railCameraObject != null)
        {
            railCameraObject.SetActive(false);
        }

        if (mainCamera != null)
        {
            mainCamera.enabled = true;
        }

        if (mainCameraControlScript != null)
        {
            mainCameraControlScript.enabled = true;
        }
    }

    public void ActivateMainCameraOnly()
    {
        if (isTransitioning)
        {
            StopCoroutine(transitionCoroutine);
            isTransitioning = false;
        }

        if (mainCamera != null)
        {
            mainCamera.enabled = true;
        }

        if (mainCameraControlScript != null)
        {
            mainCameraControlScript.enabled = true;
        }
    }
}
