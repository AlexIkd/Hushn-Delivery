using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Controla a entrada e o reinício interno da tela de Game Over.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("Painel completo do Game Over. Deve começar desativado.")]
    [SerializeField] private GameObject gameOverPanel;

    [Tooltip("Imagem fixa exibida como fundo do Game Over.")]
    [SerializeField] private Image backgroundImage;

    [Tooltip("Botão que recebe a seleção quando o menu aparece.")]
    [SerializeField] private Button firstSelectedButton;

    [SerializeField] private EventSystem eventSystem;
    [SerializeField] private MainMenuManager mainMenuManager;
    [SerializeField] private PlayerHealthSystem playerHealthSystem;
    [SerializeField] private CinematicBarsOpener cinematicBars;

    [Header("Sequência de Morte")]
    [Tooltip("Tempo entre as barras fecharem e o painel aparecer.")]
    [SerializeField, Min(0f)] private float menuEntryDelay = 0.35f;

    [Tooltip("Garante que as barras sejam fechadas ao iniciar a sequência.")]
    [SerializeField] private bool closeBarsAtSequenceStart = true;

    [Header("Pausa")]
    [SerializeField] private bool pauseGameWhenShown = true;
    [SerializeField] private bool pauseBackgroundAudio = true;

    [Header("Opções de Cena")]
    [SerializeField] private string lobbySceneName = "LobbyScene";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private bool isVisible;
    private bool sequenceStarted;
    private bool restartInProgress;

    private bool selectionLocked;
    private Button lockedSelectedButton;

    private float previousTimeScale = 1f;
    private bool previousAudioListenerPause;

    public bool IsVisible => isVisible;

    private void Awake()
    {
        if (gameOverPanel == null)
            gameOverPanel = gameObject;

        if (eventSystem == null)
            eventSystem = FindFirstObjectByType<EventSystem>();

        if (mainMenuManager == null)
            mainMenuManager = FindFirstObjectByType<MainMenuManager>();

        if (playerHealthSystem == null)
            playerHealthSystem = FindFirstObjectByType<PlayerHealthSystem>();

        if (cinematicBars == null)
            cinematicBars = FindFirstObjectByType<CinematicBarsOpener>();

        if (backgroundImage != null)
            backgroundImage.raycastTarget = false;

        gameOverPanel.SetActive(false);
        isVisible = false;
        LimparSelecaoFixa();
    }

    private void Update()
    {
        // Mantém o botão acionado selecionado durante toda a transição,
        // inclusive enquanto as barras fecham e o jogo está pausado.
        if (selectionLocked &&
            lockedSelectedButton != null &&
            gameOverPanel != null &&
            gameOverPanel.activeSelf)
        {
            ManterSelecaoFixa();
        }

        if (!isVisible || sequenceStarted || restartInProgress)
            return;

        if (Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter) ||
            Input.GetKeyDown(KeyCode.Space))
        {
            SubmitSelectedButtonDirectly();
        }
    }

    private void SubmitSelectedButtonDirectly()
    {
        GameObject selectedObject = eventSystem != null
            ? eventSystem.currentSelectedGameObject
            : null;

        Button selectedButton = selectedObject != null
            ? selectedObject.GetComponent<Button>()
            : null;

        if (selectedButton != null && selectedButton.interactable)
        {
            selectedButton.onClick.Invoke();
            return;
        }

        if (firstSelectedButton != null && firstSelectedButton.interactable)
        {
            FixarSelecao(firstSelectedButton);
            RestartCurrentLevel();
        }
    }

    public void ShowGameOver()
    {
        if (gameOverPanel == null ||
            isVisible ||
            restartInProgress ||
            sequenceStarted)
        {
            return;
        }

        LimparSelecaoFixa();
        sequenceStarted = true;
        StartCoroutine(GameOverSequence());
    }

    private IEnumerator GameOverSequence()
    {
        PauseGame();

        if (cinematicBars != null &&
            closeBarsAtSequenceStart &&
            !cinematicBars.BarrasEstaoFechadas)
        {
            cinematicBars.FecharBarras();

            while (!cinematicBars.BarrasEstaoFechadas)
                yield return null;
        }

        if (menuEntryDelay > 0f)
            yield return new WaitForSecondsRealtime(menuEntryDelay);

        gameOverPanel.SetActive(true);
        isVisible = true;

        if (mainMenuManager != null)
            mainMenuManager.ActivateGameOverNavigation();

        SelectFirstButton();
        StartCoroutine(ReselectFirstButtonNextFrame());

        if (cinematicBars != null)
            cinematicBars.AbrirBarrasSemDeslocamentoExtraImediato();

        sequenceStarted = false;
    }

    private IEnumerator ReselectFirstButtonNextFrame()
    {
        yield return null;

        if (!selectionLocked && gameOverPanel != null && gameOverPanel.activeSelf)
            SelectFirstButton();
    }

    private void PauseGame()
    {
        if (pauseGameWhenShown)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        if (pauseBackgroundAudio)
        {
            previousAudioListenerPause = AudioListener.pause;
            AudioListener.pause = true;
        }
    }

    private void ResumeGame()
    {
        if (pauseBackgroundAudio)
            AudioListener.pause = previousAudioListenerPause;

        if (pauseGameWhenShown)
            Time.timeScale = previousTimeScale;
    }

    private void SelectFirstButton()
    {
        if (eventSystem == null)
            eventSystem = FindFirstObjectByType<EventSystem>();

        if (eventSystem != null && firstSelectedButton != null)
        {
            eventSystem.SetSelectedGameObject(null);
            eventSystem.SetSelectedGameObject(firstSelectedButton.gameObject);
        }
    }

    /// <summary>
    /// Fixa a seleção no botão acionado até o GameOverPanel desaparecer.
    /// Deve ser chamado antes de executar a ação do botão.
    /// </summary>
    public void FixarSelecao(Button button)
    {
        if (button == null || eventSystem == null)
            return;

        lockedSelectedButton = button;
        selectionLocked = true;

        if (mainMenuManager != null)
            mainMenuManager.FixarSelectionBar(button);

        ManterSelecaoFixa();
    }

    private void ManterSelecaoFixa()
    {
        if (eventSystem == null)
            eventSystem = FindFirstObjectByType<EventSystem>();

        if (eventSystem == null || lockedSelectedButton == null)
            return;

        if (eventSystem.currentSelectedGameObject != lockedSelectedButton.gameObject)
        {
            eventSystem.SetSelectedGameObject(null);
            eventSystem.SetSelectedGameObject(lockedSelectedButton.gameObject);
        }
    }

    private void LimparSelecaoFixa()
    {
        selectionLocked = false;
        lockedSelectedButton = null;

        if (mainMenuManager != null)
            mainMenuManager.LiberarSelectionBar();
    }

    /// <summary>
    /// Fecha as barras, aguarda o fechamento completo e recarrega
    /// a cena atual desde o início.
    /// </summary>
    public void RestartCurrentLevel()
    {
        if (restartInProgress)
            return;

        restartInProgress = true;
        sequenceStarted = true;
        isVisible = false;

        StartCoroutine(RestartInternalSequence());
    }

    private IEnumerator RestartInternalSequence()
    {
        if (cinematicBars != null)
        {
            cinematicBars.FecharBarrasDoRestart();

            while (!cinematicBars.BarrasEstaoFechadas)
                yield return null;
        }
        else
        {
            Debug.LogError("GameOverUI: CinematicBarsOpener não foi encontrado.");
        }

        // O painel desaparece somente depois do fechamento completo.
        // A cena será recriada logo abaixo, mas limpamos o estado antes
        // para evitar qualquer interação durante a troca.
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        LimparSelecaoFixa();
        isVisible = false;

        ResumeGame();

        // Recarregar a cena restaura todos os objetos e estados iniciais:
        // jogador, vidas, itens, timer, pontuação, inimigos e checkpoints.
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToLobby()
    {
        if (restartInProgress)
            return;

        ResumeGame();

        if (mainMenuManager != null)
            mainMenuManager.LoadSceneWithTransition(lobbySceneName);
        else
            Debug.LogError("GameOverUI: MainMenuManager não foi encontrado.");
    }

    public void GoToMainMenu()
    {
        if (restartInProgress)
            return;

        ResumeGame();

        if (mainMenuManager != null)
            mainMenuManager.LoadSceneWithTransition(mainMenuSceneName);
        else
            Debug.LogError("GameOverUI: MainMenuManager não foi encontrado.");
    }
}
