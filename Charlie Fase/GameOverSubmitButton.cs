using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Executa uma ação quando este botão recebe o evento Submit da Unity.
/// O evento Submit ocorre com Enter, Espaço ou o botão de confirmação do controle.
/// </summary>
public class GameOverSubmitButton : MonoBehaviour, ISubmitHandler
{
    public enum SubmitAction
    {
        Restart,
        GoToLobby,
        GoToMainMenu
    }

    [Header("Ação")]
    [SerializeField] private SubmitAction action = SubmitAction.Restart;

    [Header("Referências")]
    [SerializeField] private GameOverUI gameOverUI;

    private bool alreadySubmitted;

    private void Awake()
    {
        if (gameOverUI == null)
            gameOverUI = GetComponentInParent<GameOverUI>();

        if (gameOverUI == null)
            gameOverUI = FindFirstObjectByType<GameOverUI>();
    }

    private void OnEnable()
    {
        alreadySubmitted = false;
    }

    /// <summary>
    /// Chamado pelo EventSystem quando este botão recebe Submit.
    /// </summary>
    public void OnSubmit(BaseEventData eventData)
    {
        if (alreadySubmitted)
            return;

        alreadySubmitted = true;
        ExecuteAction();
    }

    /// <summary>
    /// Também pode ser usado no Button > On Click()
    /// para cliques do mouse.
    /// </summary>
    public void ExecuteAction()
    {
        if (gameOverUI == null)
        {
            Debug.LogError($"{name}: GameOverUI não foi encontrado.");
            alreadySubmitted = false;
            return;
        }

        // Mantém este botão selecionado até o GameOverPanel desaparecer.
        Button thisButton = GetComponent<Button>();
        if (thisButton != null)
            gameOverUI.FixarSelecao(thisButton);

        switch (action)
        {
            case SubmitAction.Restart:
                gameOverUI.RestartCurrentLevel();
                break;

            case SubmitAction.GoToLobby:
                gameOverUI.GoToLobby();
                break;

            case SubmitAction.GoToMainMenu:
                gameOverUI.GoToMainMenu();
                break;
        }
    }

    public void ResetSubmitLock()
    {
        alreadySubmitted = false;
    }
}
