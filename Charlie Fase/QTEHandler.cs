using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class QTEHandler : MonoBehaviour
{
    public static QTEHandler Instance;

    [Header("Configurações de UI")]
    public GameObject qtePanel;
    public GameObject buttonIconPrefab; // Prefab de uma Image para os botões
    public Transform iconContainer;     // Um objeto com Horizontal Layout Group
    public Image timerBar;
    public Text progressText;
    
    [Header("Estilo Visual")]
    public Color normalColor = Color.white;
    public Color completedColor = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Cinza transparente
    public Color activeColor = Color.yellow; // Destaque para o botão atual
    public float activeScale = 1.2f;
    
    [Header("Sprites de Botões")]
    public Sprite buttonASprite;
    public Sprite buttonBSprite;
    public Sprite buttonXSprite;
    public Sprite buttonYSprite;

    [Header("Teclas do Teclado (Customizáveis)")]
    public KeyCode keyForA = KeyCode.Space;
    public KeyCode keyForB = KeyCode.E;
    public KeyCode keyForX = KeyCode.Q;
    public KeyCode keyForY = KeyCode.R;

    private bool isQTEActive = false;
    private int[] sequenceActions;
    private System.Collections.Generic.List<Image> spawnedIcons = new System.Collections.Generic.List<Image>();
    private int currentSequenceIndex = 0;
    private KeyCode currentTargetKey;
    private float qteTimer;
    private float qteDuration;
    private System.Action<bool> onComplete;
    private DynamicFollowCamera qteCamera;

    void Awake()
    {
        Instance = this;
        if (qtePanel) qtePanel.SetActive(false);
    }

    public void StartQTE(float duration, int sequenceLength, System.Action<bool> callback)
    {
        if (isQTEActive) return;
        
        Debug.Log("QTE: Iniciando sequência de " + sequenceLength + " botões.");

        isQTEActive = true;
        qteDuration = duration;
        qteTimer = duration;
        onComplete = callback;
        currentSequenceIndex = 0;

        // Limpa ícones anteriores
        foreach (var icon in spawnedIcons) if(icon != null) Destroy(icon.gameObject);
        spawnedIcons.Clear();

        if (buttonIconPrefab == null) Debug.LogError("QTE: Button Icon Prefab não está atribuído no Inspetor!");
        if (iconContainer == null) Debug.LogError("QTE: Icon Container não está atribuído no Inspetor!");

        // Gera uma sequência aleatória e cria os ícones
        sequenceActions = new int[sequenceLength];
        for (int i = 0; i < sequenceLength; i++)
        {
            sequenceActions[i] = Random.Range(0, 4);
            CreateIcon(sequenceActions[i]);
        }

        UpdateSequenceUI();

        if (qtePanel) 
        {
            qtePanel.SetActive(true);
            Debug.Log("QTE: Painel ativado.");
        }
        else 
        {
            Debug.LogError("QTE: QTE Panel não está atribuído no Inspetor!");
        }
        
        // Efeito de Slow Motion
        Time.timeScale = 0.3f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        // ✅ MELHORADO: Trava a câmera, foca no jogador e ajusta o FOV
        if (qteCamera == null) qteCamera = FindObjectOfType<DynamicFollowCamera>();
        if (qteCamera != null)
        {
            qteCamera.EnterQTENow(); // Usa o novo método que força FOV e trava completa
        }
        else
        {
            Debug.LogWarning("QTE: DynamicFollowCamera não encontrada na cena!");
        }
    }

    private void CreateIcon(int action)
    {
        if (buttonIconPrefab == null || iconContainer == null) return;

        GameObject newIconObj = Instantiate(buttonIconPrefab, iconContainer);
        
        // Garante que o objeto está ativo e com escala correta
        newIconObj.SetActive(true);
        RectTransform rect = newIconObj.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.localScale = Vector3.one;
            rect.localPosition = new Vector3(rect.localPosition.x, rect.localPosition.y, 0); // Garante Z = 0
        }
        
        Image img = newIconObj.GetComponent<Image>();
        if (img == null)
        {
            Debug.LogError("QTE: O Prefab do ícone não possui um componente Image!");
            return;
        }
        
        // Garante que o alpha está em 1
        Color c = img.color;
        c.a = 1f;
        img.color = c;
        
        switch (action)
        {
            case 0: img.sprite = buttonASprite; break;
            case 1: img.sprite = buttonBSprite; break;
            case 2: img.sprite = buttonXSprite; break;
            case 3: img.sprite = buttonYSprite; break;
        }

        spawnedIcons.Add(img);
        Debug.Log("QTE: Ícone criado em " + (rect != null ? rect.anchoredPosition.ToString() : "N/A") + " com tamanho " + (rect != null ? rect.sizeDelta.ToString() : "N/A"));
    }

    private void UpdateSequenceUI()
    {
        if (currentSequenceIndex >= sequenceActions.Length) return;

        if (progressText != null)
        {
            progressText.text = (currentSequenceIndex + 1) + " / " + sequenceActions.Length;
        }

        int action = sequenceActions[currentSequenceIndex];
        bool isUsingJoystick = Input.GetJoystickNames().Length > 0;

        // Define a tecla alvo
        switch (action)
        {
            case 0: currentTargetKey = isUsingJoystick ? KeyCode.JoystickButton0 : keyForA; break;
            case 1: currentTargetKey = isUsingJoystick ? KeyCode.JoystickButton1 : keyForB; break;
            case 2: currentTargetKey = isUsingJoystick ? KeyCode.JoystickButton2 : keyForX; break;
            case 3: currentTargetKey = isUsingJoystick ? KeyCode.JoystickButton3 : keyForY; break;
        }

        // Atualiza o visual de todos os ícones
        for (int i = 0; i < spawnedIcons.Count; i++)
        {
            if (i < currentSequenceIndex) // Já completados
            {
                spawnedIcons[i].color = completedColor;
                spawnedIcons[i].transform.localScale = Vector3.one;
            }
            else if (i == currentSequenceIndex) // Botão atual
            {
                spawnedIcons[i].color = activeColor;
                spawnedIcons[i].transform.localScale = Vector3.one * activeScale;
            }
            else // Futuros
            {
                spawnedIcons[i].color = normalColor;
                spawnedIcons[i].transform.localScale = Vector3.one;
            }
        }
    }

    void Update()
    {
        if (!isQTEActive) return;

        qteTimer -= Time.unscaledDeltaTime;
        if (timerBar) timerBar.fillAmount = qteTimer / qteDuration;

        if (Input.anyKeyDown)
        {
            if (Input.GetKeyDown(currentTargetKey))
            {
                // Acertou o botão atual
                currentSequenceIndex++;

                if (currentSequenceIndex >= sequenceActions.Length)
                {
                    // Completou a sequência com sucesso
                    EndQTE(true);
                }
                else
                {
                    // Vai para o próximo botão
                    UpdateSequenceUI();
                    // Opcional: Dar um pequeno bônus de tempo ao acertar
                    qteTimer = Mathf.Min(qteTimer + (qteDuration * 0.2f), qteDuration);
                }
            }
            else
            {
                // Errou o botão (ignora se for mouse scroll ou botões que não são KeyCode)
                if (!Input.GetMouseButtonDown(0) && !Input.GetMouseButtonDown(1))
                {
                    EndQTE(false);
                }
            }
        }
        else if (qteTimer <= 0)
        {
            EndQTE(false);
        }
    }

    void EndQTE(bool success)
    {
        isQTEActive = false;
        if (qtePanel) qtePanel.SetActive(false);
        
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f;

        // ✅ MELHORADO: Libera a câmera e restaura FOV normal
        if (qteCamera != null)
        {
            qteCamera.ExitQTENow();
        }

        onComplete?.Invoke(success);
    }
}
