using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Sequência cinematográfica de início da fase:
/// animação do jogador + câmera orbitável + READY! -> GO!.
/// </summary>
public class LevelStartReadyGo : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private CanvasGroup sequenceCanvasGroup;
    [SerializeField] private TMP_Text sequenceText;
    [SerializeField] private RectTransform textTransform;

    [Header("Jogador")]
    [Tooltip("Animator do jogador que executará a animação READY GO.")]
    [SerializeField] private Animator playerAnimator;
    [Tooltip("Trigger configurado na Animator Controller do jogador.")]
    [SerializeField] private string readyGoTrigger = "ReadyGo";
    [Tooltip("Opcional. Nome exato do estado da animação na Layer 0. Se preenchido, inicia diretamente esse estado.")]
    [SerializeField] private string readyGoStateName = "";
    [Tooltip("Se ativado, a animação continua mesmo com Time.timeScale = 0.")]
    [SerializeField] private bool playAnimationWithUnscaledTime = true;

    [Header("Duração da sequência")]
    [SerializeField, Min(0f)] private float initialDelay = 0.25f;
    [SerializeField, Min(0.01f)] private float readyDuration = 0.9f;
    [SerializeField, Min(0.01f)] private float transitionDuration = 0.18f;
    [SerializeField, Min(0.01f)] private float goDuration = 0.65f;
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.2f;
    [SerializeField] private Vector3 hiddenScale = new Vector3(0.65f, 0.65f, 0.65f);
    [SerializeField] private Vector3 visibleScale = Vector3.one;

    [Header("Controles bloqueados durante a sequência")]
    [Tooltip("Arraste PlayerMovement, PlayerSpinDash e outros scripts de controle.")]
    [SerializeField] private Behaviour[] controlsToDisable;

    [Header("Câmera cinematográfica")]
    [Tooltip("Controlador separado da câmera da introdução.")]
    [SerializeField] private ReadyGoCinematicCamera cinematicCameraController;

    [Header("Evento")]
    [SerializeField] private UnityEvent onSequenceFinished;

    private bool isRunning;
    private float previousTimeScale = 1f;
    private AnimatorUpdateMode previousAnimatorUpdateMode;
    private bool previousAnimatorEnabled;
    private bool animatorStateWasSaved;

    public bool IsRunning => isRunning;

    private void Awake()
    {
        if (sequenceCanvasGroup == null)
            sequenceCanvasGroup = GetComponentInChildren<CanvasGroup>(true);

        if (sequenceText == null)
            sequenceText = GetComponentInChildren<TMP_Text>(true);

        if (textTransform == null && sequenceText != null)
            textTransform = sequenceText.rectTransform;

        if (cinematicCameraController == null)
            cinematicCameraController = FindFirstObjectByType<ReadyGoCinematicCamera>();

        HideSequenceImmediately();
    }

    private void Start()
    {
        StartCoroutine(PlayReadyGoSequence());
    }

    public void PlaySequence()
    {
        if (!isRunning)
            StartCoroutine(PlayReadyGoSequence());
    }

    private IEnumerator PlayReadyGoSequence()
    {
        isRunning = true;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        SetControlsEnabled(false);
        PrepareAnimator();
        if (cinematicCameraController != null)
            cinematicCameraController.Play();

        if (sequenceCanvasGroup != null)
        {
            sequenceCanvasGroup.gameObject.SetActive(true);
            sequenceCanvasGroup.interactable = false;
            sequenceCanvasGroup.blocksRaycasts = false;
        }

        if (initialDelay > 0f)
            yield return WaitUnscaled(initialDelay);

        PlayPlayerAnimation();

        yield return ShowWord("READY!", readyDuration);
        yield return ShowWord("GO!", goDuration);
        yield return FadeOut();

        HideSequenceImmediately();
        if (cinematicCameraController != null)
        {
            cinematicCameraController.Stop();
            while (cinematicCameraController.IsTransitioning)
                yield return null;
        }
        RestoreAnimator();
        SetControlsEnabled(true);

        Time.timeScale = previousTimeScale;
        isRunning = false;
        onSequenceFinished?.Invoke();
    }

    private void PrepareAnimator()
    {
        if (playerAnimator == null)
            return;

        previousAnimatorUpdateMode = playerAnimator.updateMode;
        previousAnimatorEnabled = playerAnimator.enabled;
        animatorStateWasSaved = true;

        // O Animator precisa continuar ativo para tocar a cena mesmo que
        // Animator tenha sido colocado por engano na lista de controles.
        playerAnimator.enabled = true;

        if (playAnimationWithUnscaledTime)
            playerAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
    }

    private void RestoreAnimator()
    {
        if (playerAnimator != null && animatorStateWasSaved)
        {
            playerAnimator.updateMode = previousAnimatorUpdateMode;
            playerAnimator.enabled = previousAnimatorEnabled;
        }

        animatorStateWasSaved = false;
    }

    private void PlayPlayerAnimation()
    {
        if (playerAnimator == null)
        {
            Debug.LogError("LevelStartReadyGo: Player Animator não foi configurado.");
            return;
        }

        playerAnimator.enabled = true;

        if (!string.IsNullOrWhiteSpace(readyGoStateName))
        {
            int stateHash = Animator.StringToHash(readyGoStateName);

            if (playerAnimator.HasState(0, stateHash))
            {
                playerAnimator.CrossFadeInFixedTime(
                    stateHash,
                    0.05f,
                    0,
                    0f
                );
                return;
            }

            Debug.LogError(
                $"LevelStartReadyGo: o estado '{readyGoStateName}' " +
                "não foi encontrado na Layer 0 do Animator."
            );
        }

        if (string.IsNullOrWhiteSpace(readyGoTrigger))
        {
            Debug.LogError("LevelStartReadyGo: Ready Go Trigger não foi configurado.");
            return;
        }

        playerAnimator.ResetTrigger(readyGoTrigger);
        playerAnimator.SetTrigger(readyGoTrigger);
    }

    private IEnumerator ShowWord(string word, float duration)
    {
        if (sequenceText == null || sequenceCanvasGroup == null)
        {
            yield return WaitUnscaled(duration);
            yield break;
        }

        sequenceText.text = word;
        sequenceCanvasGroup.alpha = 0f;

        if (textTransform != null)
            textTransform.localScale = hiddenScale;

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / transitionDuration);
            float eased = Mathf.SmoothStep(0f, 1f, progress);

            sequenceCanvasGroup.alpha = eased;
            if (textTransform != null)
                textTransform.localScale = Vector3.LerpUnclamped(
                    hiddenScale,
                    visibleScale,
                    eased
                );

            yield return null;
        }

        sequenceCanvasGroup.alpha = 1f;
        if (textTransform != null)
            textTransform.localScale = visibleScale;

        yield return WaitUnscaled(Mathf.Max(0f, duration - transitionDuration));
    }

    private IEnumerator FadeOut()
    {
        if (sequenceCanvasGroup == null)
            yield break;

        float startAlpha = sequenceCanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / fadeDuration);
            sequenceCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, progress);
            yield return null;
        }

        sequenceCanvasGroup.alpha = 0f;
    }

    private IEnumerator WaitUnscaled(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void SetControlsEnabled(bool enabled)
    {
        if (controlsToDisable == null)
            return;

        foreach (Behaviour control in controlsToDisable)
        {
            if (control != null)
                control.enabled = enabled;
        }
    }

    private void HideSequenceImmediately()
    {
        if (sequenceCanvasGroup != null)
        {
            sequenceCanvasGroup.alpha = 0f;
            sequenceCanvasGroup.interactable = false;
            sequenceCanvasGroup.blocksRaycasts = false;
        }

        if (textTransform != null)
            textTransform.localScale = visibleScale;
    }
}
