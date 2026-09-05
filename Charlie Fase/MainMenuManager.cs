using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class MainMenuManager : MonoBehaviour
{
    public List<Button> menuButtons;

    [Header("Botões do Game Over")]
    [Tooltip("Botões do Game Over. A mesma Selection Bar será usada para selecioná-los.")]
    public List<Button> gameOverButtons = new List<Button>();

    private int currentSelectedButtonIndex = 0;
    private List<Button> mainMenuButtonsBackup;

    public EventSystem eventSystem;

    [Header("Efeito de Seleção")]
    public RectTransform selectionBar;
    public CinematicBarsOpener cinematicBars;
    public float selectionSmoothSpeed = 10f;
    public Vector2 selectionOffset;

    private Vector2 targetBarPosition;
    private bool selectionBarLocked;
    private Button lockedSelectionButton;

    private void Start()
    {
        mainMenuButtonsBackup = menuButtons != null
            ? new List<Button>(menuButtons)
            : new List<Button>();

        if (eventSystem == null)
        {
            eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                Debug.LogError("EventSystem não encontrado na cena!");
                return;
            }
        }

        if (menuButtons != null && menuButtons.Count > 0)
        {
            SelectButton(0);

            if (selectionBar != null)
            {
                RectTransform btnRect = menuButtons[0].GetComponent<RectTransform>();
                UpdateSelectionBarTarget(btnRect);
                selectionBar.anchoredPosition = targetBarPosition;
            }
        }
    }

    private void Update()
    {
        if (!selectionBarLocked)
        {
            if (Input.GetKeyDown(KeyCode.S))
                NavigateDown();
            else if (Input.GetKeyDown(KeyCode.W))
                NavigateUp();

            if (Input.GetKeyDown(KeyCode.Return) ||
                Input.GetKeyDown(KeyCode.KeypadEnter) ||
                Input.GetKeyDown(KeyCode.Space))
            {
                SubmitCurrentButton();
            }
        }

        if (selectionBar != null)
        {
            selectionBar.anchoredPosition = Vector2.Lerp(
                selectionBar.anchoredPosition,
                targetBarPosition,
                Time.unscaledDeltaTime * selectionSmoothSpeed);
        }
    }

    private void SubmitCurrentButton()
    {
        GameObject selectedObject = eventSystem != null
            ? eventSystem.currentSelectedGameObject
            : null;

        if (selectedObject != null)
        {
            ExecuteEvents.Execute(
                selectedObject,
                new BaseEventData(eventSystem),
                ExecuteEvents.submitHandler);
            return;
        }

        if (menuButtons != null &&
            currentSelectedButtonIndex >= 0 &&
            currentSelectedButtonIndex < menuButtons.Count &&
            menuButtons[currentSelectedButtonIndex] != null &&
            menuButtons[currentSelectedButtonIndex].interactable)
        {
            menuButtons[currentSelectedButtonIndex].onClick.Invoke();
        }
    }

    private void NavigateDown()
    {
        if (selectionBarLocked) return;
        if (menuButtons == null || menuButtons.Count == 0) return;

        currentSelectedButtonIndex++;
        if (currentSelectedButtonIndex >= menuButtons.Count)
            currentSelectedButtonIndex = 0;

        SelectButton(currentSelectedButtonIndex);
    }

    private void NavigateUp()
    {
        if (selectionBarLocked) return;
        if (menuButtons == null || menuButtons.Count == 0) return;

        currentSelectedButtonIndex--;
        if (currentSelectedButtonIndex < 0)
            currentSelectedButtonIndex = menuButtons.Count - 1;

        SelectButton(currentSelectedButtonIndex);
    }

    private void SelectButton(int index)
    {
        if (selectionBarLocked)
            return;

        if (menuButtons == null ||
            index < 0 ||
            index >= menuButtons.Count ||
            menuButtons[index] == null)
        {
            return;
        }

        if (eventSystem == null)
            eventSystem = FindFirstObjectByType<EventSystem>();

        if (eventSystem != null)
        {
            eventSystem.SetSelectedGameObject(null);
            eventSystem.SetSelectedGameObject(menuButtons[index].gameObject);
        }

        UpdateSelectionBarTarget(menuButtons[index].GetComponent<RectTransform>());
    }

    private void UpdateSelectionBarTarget(RectTransform buttonRect)
    {
        if (selectionBar == null || buttonRect == null)
            return;

        if (selectionBar.parent == buttonRect.parent)
        {
            targetBarPosition = buttonRect.anchoredPosition + selectionOffset;
            return;
        }

        Vector3 worldPosition = buttonRect.TransformPoint(buttonRect.rect.center);
        Vector2 localPosition;
        RectTransform parentRect = selectionBar.parent as RectTransform;

        if (parentRect != null &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                RectTransformUtility.WorldToScreenPoint(null, worldPosition),
                null,
                out localPosition))
        {
            targetBarPosition = localPosition + selectionOffset;
        }
    }

    /// <summary>
    /// Fixa a Selection Bar no botão acionado durante a transição do Game Over.
    /// </summary>
    public void FixarSelectionBar(Button button)
    {
        if (button == null)
            return;

        selectionBarLocked = true;
        lockedSelectionButton = button;
        UpdateSelectionBarTarget(button.GetComponent<RectTransform>());
    }

    /// <summary>
    /// Libera a Selection Bar para o próximo ciclo de navegação.
    /// </summary>
    public void LiberarSelectionBar()
    {
        selectionBarLocked = false;
        lockedSelectionButton = null;
    }

    public void ActivateGameOverNavigation()
    {
        LiberarSelectionBar();

        if (gameOverButtons == null || gameOverButtons.Count == 0)
        {
            Debug.LogWarning("MainMenuManager: nenhum botão de Game Over foi configurado.");
            return;
        }

        menuButtons = gameOverButtons;
        currentSelectedButtonIndex = 0;
        SelectButton(0);
    }

    public void RestoreMainMenuNavigation()
    {
        LiberarSelectionBar();
        menuButtons = mainMenuButtonsBackup;
        currentSelectedButtonIndex = 0;

        if (menuButtons != null && menuButtons.Count > 0)
            SelectButton(0);
    }

    public void StartGame()
    {
        StartCoroutine(TransitionAndAction("GameScene"));
    }

    public void ExitGame()
    {
        StartCoroutine(TransitionAndAction("QUIT"));
    }

    public void RestartCurrentLevel()
    {
        StartCoroutine(TransitionAndAction(SceneManager.GetActiveScene().name));
    }

    public void LoadSceneWithTransition(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("MainMenuManager: o nome da cena está vazio.");
            return;
        }

        StartCoroutine(TransitionAndAction(sceneName));
    }

    private System.Collections.IEnumerator TransitionAndAction(string action)
    {
        if (cinematicBars != null)
            cinematicBars.FecharBarras();

        yield return new WaitForSecondsRealtime(1.0f);

        if (action == "QUIT")
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        else
        {
            SceneManager.LoadScene(action);
        }
    }
}
