using UnityEngine;
using System.Collections;

/// <summary>
/// Gerencia a transição suave entre a câmera principal do jogador e as câmeras cinemáticas dos rails.
/// Esta versão refatorada utiliza a própria Main Camera para a transição,
/// garantindo que todos os efeitos de pós-processamento, luzes e sombras sejam preservados.
/// O controle do jogador sobre a Main Camera é desativado durante a transição.
/// </summary>
public class CameraRailManager : MonoBehaviour
{
    public static CameraRailManager Instance { get; private set; }

    [Header("Configuração da Câmera")]
    [Tooltip("Se a transição cinemática de câmera deve ser ativada.")]
    [SerializeField] private bool enableCinematicCamera = true;

    [Tooltip("A câmera principal do jogo (Camera.main).")]
    [SerializeField] private Camera mainCamera;

    [Header("Componente de Controle da Câmera Principal")]
    [Tooltip("O script que controla o movimento da câmera principal (ex: CameraController.cs). Deve ser desativado durante a transição.")]	
    [SerializeField] private MonoBehaviour mainCameraControlScript;
    
    [Tooltip("O tempo de transição padrão (em segundos) caso nenhum seja especificado.")]
    [SerializeField] private float defaultTransitionDuration = 0.5f;

    [Header("Curva de Transição")]
    [Tooltip("Curva para controlar a suavidade do blending entre as câmeras.")]
    [SerializeField] private AnimationCurve transitionCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));

    private Coroutine transitionCoroutine;
    private bool isTransitioning = false;
    
    private Transform targetTransform;
    private Camera currentActiveCamera; // A câmera que está ativa e renderizando (mainCamera ou railCam)
    private Camera railCameraToDeactivate; // A câmera do rail que será desativada no final da transição para a main

    private bool isReturningToMain;
    private float transitionDuration;
    private float elapsedTime = 0f;
    
    private Vector3 startPosition;
    private Quaternion startRotation;
    private float startFOV;
    private float targetFOV;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        
        if (mainCamera == null)
        {
            Debug.LogError("Nenhuma Câmera Principal encontrada. Certifique-se de que uma câmera tenha a tag 'MainCamera'.");
        }
    }

    /// <summary>
    /// Inicia a transição suave para a câmera do rail.
    /// </summary>
    public void StartTransitionToRail(GameObject railCameraObject, float duration = -1f)
    {
        if (!enableCinematicCamera || mainCamera == null || railCameraObject == null) return;

        Camera railCam = railCameraObject.GetComponent<Camera>();
        if (railCam == null)
        {
            Debug.LogError("O objeto de Rail não possui um componente Camera.");
            return;
        }

        if (isTransitioning) StopCoroutine(transitionCoroutine);

        float finalDuration = duration > 0 ? duration : defaultTransitionDuration;

        // Garante que a câmera do rail esteja ativa no GameObject, mas não renderizando ainda
        railCameraObject.SetActive(true);
        railCam.enabled = false; // Desativa temporariamente para a mainCamera renderizar a transição

        transitionCoroutine = StartCoroutine(TransitionRoutine(mainCamera, railCam, false, finalDuration));
        
        Debug.Log($"Iniciando transição para Rail: {railCameraObject.name} em {finalDuration}s");
    }

    /// <summary>
    /// Inicia a transição suave de volta para a câmera principal do jogador.
    /// </summary>
    public void StartTransitionToMain(Transform mainCameraTarget, GameObject currentRailCameraObject, float duration = -1f)
    {
        if (!enableCinematicCamera || mainCamera == null || currentRailCameraObject == null) return;

        Camera railCam = currentRailCameraObject.GetComponent<Camera>();
        if (railCam == null) return;

        if (isTransitioning) StopCoroutine(transitionCoroutine);

        float finalDuration = duration > 0 ? duration : defaultTransitionDuration;

        // A câmera principal já deve estar ativa no GameObject, mas não renderizando
        // O railCam é a câmera 'from' neste caso, e a mainCamera é a câmera 'to'
        transitionCoroutine = StartCoroutine(TransitionRoutine(railCam, mainCamera, true, finalDuration));
        
        Debug.Log($"Iniciando transição de volta para Principal em {finalDuration}s");
    }

    private IEnumerator TransitionRoutine(Camera fromCamera, Camera toCamera, bool returning, float duration)
    {
        isTransitioning = true;
        isReturningToMain = returning;
        transitionDuration = duration;
        elapsedTime = 0f;

        // A câmera que está ativa no momento (renderizando) é a 'fromCamera'
        currentActiveCamera = fromCamera;
        targetTransform = toCamera.transform;

        // Se estamos indo para um rail, a mainCamera é a 'fromCamera' e o railCam é a 'toCamera'
        // Se estamos voltando para a main, o railCam é a 'fromCamera' e a mainCamera é a 'toCamera'
        if (!isReturningToMain) // Indo para o rail
        {
            // Desativa o controle do jogador na Main Camera
            if (mainCameraControlScript != null) 
            {
                mainCameraControlScript.enabled = false;
            }
            // A mainCamera será a câmera que fará a transição visualmente
            // Copia a posição e rotação da 'fromCamera' (mainCamera) para a mainCamera
            startPosition = mainCamera.transform.position;
            startRotation = mainCamera.transform.rotation;
            startFOV = mainCamera.fieldOfView;

            // Garante que a mainCamera esteja habilitada para renderizar a transição
            mainCamera.enabled = true;
            // Desativa a câmera do rail temporariamente
            toCamera.enabled = false;
        }
        else // Voltando para a main
        {
            // A mainCamera já está desativada (pelo CompleteTransition anterior ou StartTransitionToRail)
            // A 'fromCamera' (railCam) está ativa e renderizando
            // A mainCamera fará a transição visualmente
            // Copia a posição e rotação da 'fromCamera' (railCam) para a mainCamera
            mainCamera.transform.position = fromCamera.transform.position;
            mainCamera.transform.rotation = fromCamera.transform.rotation;
            mainCamera.fieldOfView = fromCamera.fieldOfView;

            startPosition = mainCamera.transform.position;
            startRotation = mainCamera.transform.rotation;
            startFOV = mainCamera.fieldOfView;

            // Garante que a mainCamera esteja habilitada para renderizar a transição
            mainCamera.enabled = true;
            // Desativa a câmera do rail que está sendo deixada
            fromCamera.enabled = false;
            railCameraToDeactivate = fromCamera; // Guarda para desativar o GameObject no final
        }

        // Aguarda o LateUpdate processar a movimentação
        while (isTransitioning)
        {
            yield return null;
        }
    }

    private void LateUpdate()
    {
        if (!isTransitioning || targetTransform == null) return;

        elapsedTime += Time.deltaTime;
        float t = Mathf.Clamp01(elapsedTime / transitionDuration);
        
        // Usa a AnimationCurve para um blending mais suave e customizável
        float curveT = transitionCurve.Evaluate(t);

        // Atualiza o FOV alvo dinamicamente caso a câmera de destino mude de FOV
        targetFOV = targetTransform.GetComponent<Camera>().fieldOfView; // Pega o FOV da câmera de destino real

        // Move a Main Camera
        mainCamera.transform.position = Vector3.Lerp(startPosition, targetTransform.position, curveT);
        mainCamera.transform.rotation = Quaternion.Slerp(startRotation, targetTransform.rotation, curveT);
        mainCamera.fieldOfView = Mathf.Lerp(startFOV, targetFOV, curveT);

        if (t >= 1.0f)
        {
            CompleteTransition();
        }
    }

    private void CompleteTransition()
    {
        // A mainCamera já está ativa e na posição final
        // Se voltamos para a principal, reativamos o controle do jogador
        if (isReturningToMain)
        {
            if (mainCameraControlScript != null) 
            {
                mainCameraControlScript.enabled = true;
            }
            
            // Desativa completamente o GameObject da câmera do rail que foi deixada
            if (railCameraToDeactivate != null)
            {
                railCameraToDeactivate.gameObject.SetActive(false);
            }
        }
        else // Se fomos para o rail
        {
            // Desativa a mainCamera e ativa a câmera do rail
            mainCamera.enabled = false;
            targetTransform.GetComponent<Camera>().enabled = true;
        }

        isTransitioning = false;
        targetTransform = null;
        currentActiveCamera = null;
        railCameraToDeactivate = null;
        
        Debug.Log("Transição de câmera concluída com sucesso.");
    }

    public bool IsTransitioning()
    {
        return isTransitioning;
    }

    public bool IsThisCameraTransitioning(Transform camTransform)
    {
        // Agora, apenas a mainCamera estará envolvida na transição visualmente
        return isTransitioning && camTransform == mainCamera.transform;
    }

    public void ForceDeactivateRailCamera(GameObject railCameraObject)
    {
        if (isTransitioning)
        {
            StopCoroutine(transitionCoroutine);
            isTransitioning = false;
            // Garante que a mainCamera esteja ativa e renderizando
            if (mainCamera != null) mainCamera.enabled = true;
        }

        if (railCameraObject != null)
        {
            railCameraObject.SetActive(false);
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
