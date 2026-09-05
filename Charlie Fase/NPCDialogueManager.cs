using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Gerencia a janela de diálogo dos NPCs.
/// Adicione este script a um objeto vazio na cena (ex: _GAME_MANAGERS).
/// Requer: painel de diálogo desativado por padrão, com NameText e LineText (TMP).
/// </summary>
public class NPCDialogueManager : MonoBehaviour
{
    public static NPCDialogueManager Instance;

    [Header("UI de Diálogo")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI lineText;
    public GameObject continueIndicator; // Seta/texto "▼" que aparece no fim da fala

    [Header("Efeito Typewriter")]
    public float typeSpeed = 0.03f; // Segundos por caractere

    [Header("Som (Opcional)")]
    public AudioSource sfxSource;
    public AudioClip defaultTypeSound;

    [Header("Controles")]
    public KeyCode advanceKey = KeyCode.E;
    public KeyCode skipKey = KeyCode.Mouse0;

    private NPCDialogueData currentDialogue;
    private int currentLineIndex = 0;
    private bool isTyping = false;
    private string fullText = "";
    private Coroutine typeRoutine;
    private bool isDialogueActive = false;

    // ✅ NOVO: Impede que o diálogo reabra imediatamente após ser fechado
    // (evita que o E usado para fechar seja capturado de novo e reinicie a conversa)
    private bool recentlyEnded = false;
    private float reopenCooldown = 0.25f; // segundos de bloqueio após fechar
    private float endCooldownTimer = 0f;

    // ✅ NOVO: Referência ao NPC que está falando atualmente (para liberar a rotação do jogador ao fim)
    private NPCInteractable currentSpeakingNPC;

    private void Awake()
    {
        Instance = this;
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    private void Update()
    {
        if (!isDialogueActive)
        {
            // ✅ NOVO: Cronômetro que bloqueia reabertura imediata após fechar o diálogo
            if (recentlyEnded)
            {
                endCooldownTimer -= Time.deltaTime;
                if (endCooldownTimer <= 0f)
                    recentlyEnded = false;
            }
            return;
        }

        if (Input.GetKeyDown(advanceKey) || Input.GetKeyDown(skipKey))
        {
            if (isTyping)
            {
                // Pula o efeito typewriter e mostra o texto completo na hora
                StopTyping();
            }
            else if (currentLineIndex < currentDialogue.lines.Length - 1)
            {
                // AINDA HÁ mais falas: avança normalmente
                AdvanceLine();
            }
            else
            {
                // ✅ NOVO: ÚLTIMA FALA — aperta E para FECHAR o diálogo (não avança para a primeira)
                EndDialogue();
            }
        }
    }

    /// <summary>
    /// Inicia um diálogo completo do NPC
    /// </summary>
    public void StartDialogue(NPCDialogueData data)
    {
        if (data == null || data.lines == null || data.lines.Length == 0)
        {
            Debug.LogError("[NPCDialogueManager] Diálogo sem linhas!");
            return;
        }

        // ✅ NOVO: Se o diálogo acabou há pouco tempo, ignora a reabertura imediata
        // (o E que fechou o diálogo não pode reiniciá-lo no mesmo instante)
        if (recentlyEnded) return;

        // Se já há um diálogo ativo deste mesmo NPC, não reinicia
        if (isDialogueActive && currentDialogue == data)
            return;

        currentDialogue = data;
        currentLineIndex = 0;
        isDialogueActive = true;

        // ✅ NOVO: Encontra o NPC dono deste diálogo para a rotação do jogador
        currentSpeakingNPC = null;
        NPCInteractable[] npcs = FindObjectsOfType<NPCInteractable>();
        foreach (var npc in npcs)
        {
            if (npc != null && npc.dialogue == data)
            {
                currentSpeakingNPC = npc;
                break;
            }
        }

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        ShowLine();
    }

    private void ShowLine()
    {
        if (currentLineIndex >= currentDialogue.lines.Length)
        {
            EndDialogue();
            return;
        }

        NPCLineData line = currentDialogue.lines[currentLineIndex];
        fullText = line.text;
        lineText.text = "";

        if (speakerNameText != null)
            speakerNameText.text = line.speakerName;

        if (continueIndicator != null)
            continueIndicator.SetActive(false);

        // Inicia o efeito typewriter
        if (typeRoutine != null) StopCoroutine(typeRoutine);
        typeRoutine = StartCoroutine(TypeWriterRoutine(line));
    }

    private IEnumerator TypeWriterRoutine(NPCLineData line)
    {
        isTyping = true;
        AudioClip sound = line.typeSound != null ? line.typeSound : defaultTypeSound;

        for (int i = 0; i < fullText.Length; i++)
        {
            lineText.text = fullText.Substring(0, i + 1);

            // Toca o som de digitação
            if (sound != null && sfxSource != null && i > 0)
            {
                sfxSource.pitch = Random.Range(0.9f, 1.1f);
                sfxSource.PlayOneShot(sound);
            }

            yield return new WaitForSecondsRealtime(typeSpeed);
        }

        isTyping = false;

        // Mostra o indicador de "avançar"
        if (continueIndicator != null)
            continueIndicator.SetActive(true);
    }

    private void StopTyping()
    {
        if (typeRoutine != null)
            StopCoroutine(typeRoutine);
        typeRoutine = null;

        lineText.text = fullText;
        isTyping = false;

        if (continueIndicator != null)
            continueIndicator.SetActive(true);
    }

    private void AdvanceLine()
    {
        currentLineIndex++;
        ShowLine();
    }

    /// <summary>
    /// Termina o diálogo e dispara o evento Unity (se configurado)
    /// </summary>
    private void EndDialogue()
    {
        isDialogueActive = false;
        isTyping = false;

        // ✅ NOVO: Bloqueia reabertura imediata do diálogo
        recentlyEnded = true;
        endCooldownTimer = reopenCooldown;
        if (typeRoutine != null) StopCoroutine(typeRoutine);
        typeRoutine = null;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        // ✅ NOVO: Ao final do diálogo, o NPC PARA de falar e volta ao Idle
        // (NÃO aciona mais o trigger Talk — isso fazia o NPC "falar" após o fim da conversa)
        if (currentSpeakingNPC != null)
            currentSpeakingNPC.StopTalking();

        // ✅ NOVO: Libera a rotação do jogador ao fim do diálogo
        if (currentSpeakingNPC != null)
            currentSpeakingNPC.OnDialogueEnded(GetPlayerObject());

        // Dispara o evento de fim de diálogo (ex: abrir loja, dar item, etc.)
        if (currentDialogue != null)
            currentDialogue.onDialogueFinished?.Invoke();

        currentDialogue = null;
        currentSpeakingNPC = null;
    }

    /// <summary>
    /// Retorna o GameObject do jogador para liberar a rotação (usando o PlayerInteractor como referência)
    /// </summary>
    private GameObject GetPlayerObject()
    {
        PlayerInteractor interactor = FindObjectOfType<PlayerInteractor>();
        if (interactor != null && interactor.gameObject != null)
            return interactor.gameObject;
        return GameObject.FindGameObjectWithTag("Player");
    }

    /// <summary>
    /// ✅ NOVO: Retorna o NPC que está falando atualmente (para o NPC girar em direção ao jogador)
    /// </summary>
    public NPCInteractable CurrentSpeakingNPC => currentSpeakingNPC;

    /// <summary>
    /// Força o fechamento do diálogo (útil se o jogador andar para longe)
    /// </summary>
    public void ForceClose()
    {
        if (isDialogueActive)
            EndDialogue();
    }

    /// <summary>
    /// Retorna true se um diálogo está ativo (útil para bloquear movimento/interações)
    /// </summary>
    public bool IsDialogueActive => isDialogueActive;
}
