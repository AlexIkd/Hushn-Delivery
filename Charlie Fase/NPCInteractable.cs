using UnityEngine;
using TMPro;

/// <summary>
/// Adicione este script ao objeto do NPC.
/// Ele implementa a interface IInteractable para funcionar com o PlayerInteractor
/// que você já tem (detecção por área/OverlapSphere).
/// </summary>
public class NPCInteractable : MonoBehaviour, IInteractable
{
    [Header("Dados")]
    public NPCDialogueData dialogue;
    public string interactText = "Falar";

    [Header("Rotação Durante Diálogo")]
    [Tooltip("Se true, o NPC gira suavemente em direção ao jogador durante o diálogo")]
    public bool rotateTowardPlayer = true;
    [Tooltip("Velocidade da rotação (quanto maior, mais rápido o NPC gira para o jogador)")]
    public float rotationSpeed = 8f;
    [Tooltip("Se true, gira apenas no eixo Y (horizontal), mantendo a postura vertical")]
    public bool rotateOnlyHorizontal = true;

    [Header("Visual (Opcional)")]
    public GameObject interactionPrompt; // Objeto UI que mostra o prompt de interação
    public Transform lookAtTarget;       // Se null, usa o próprio NPC

    [Header("Bloqueio Após Diálogo")]
    [Tooltip("Tempo (em segundos) em que este NPC fica bloqueado para nova interação após o diálogo terminar. Impede que o E usado para fechar o diálogo reabra a conversa.")]
    [SerializeField] private float postDialogueCooldown = 0.5f;
    private float cooldownTimer = 0f;

    public Transform InteractTransform => lookAtTarget != null ? lookAtTarget : transform;

    /// <summary>
    /// Chamado pelo PlayerInteractor quando o jogador está perto do NPC
    /// </summary>
    public string GetInteractText()
    {
        // ✅ NOVO: Durante o diálogo, o prompt deste NPC fica escondido
        // (evita que o E usado para fechar o diálogo seja capturado de novo)
        if (NPCDialogueManager.Instance != null && NPCDialogueManager.Instance.IsDialogueActive)
            return "";

        // Só mostra o prompt se existir um diálogo configurado
        return dialogue != null ? interactText : "";
    }

    /// <summary>
    /// Chamado pelo PlayerInteractor quando o jogador aperta o botão de interação
    /// </summary>
    public void Interact(GameObject player)
    {
        if (dialogue == null) return;

        // ✅ NOVO: Se este NPC estiver no cooldown pós-diálogo, ignora a interação
        // (o E usado para FECHAR o diálogo não pode reiniciar a conversa)
        if (cooldownTimer > 0f) return;

        // ✅ Aciona a animação de conversa automaticamente
        TriggerTalkAnimation();

        if (NPCDialogueManager.Instance != null)
        {
            NPCDialogueManager.Instance.StartDialogue(dialogue);

            // ✅ NOVO: Faz o jogador olhar para este NPC durante o diálogo
            if (player != null)
            {
                PlayerMovement_FrontiersStyle playerMovement = player.GetComponent<PlayerMovement_FrontiersStyle>();
                if (playerMovement != null)
                    playerMovement.dialogueTargetNPC = InteractTransform;
            }
        }
        else
            Debug.LogError("[NPCInteractable] NPCDialogueManager não encontrado na cena!");
    }

    /// <summary>
    /// Chamado quando o diálogo termina para o jogador voltar a olhar livremente
    /// </summary>
    public void OnDialogueEnded(GameObject player)
    {
        if (player != null)
        {
            PlayerMovement_FrontiersStyle playerMovement = player.GetComponent<PlayerMovement_FrontiersStyle>();
            if (playerMovement != null)
                playerMovement.dialogueTargetNPC = null;
        }

        // ✅ NOVO: Inicia o cooldown pós-diálogo deste NPC
        // (bloqueia re-interação imediata pelo mesmo E que fechou a conversa)
        cooldownTimer = postDialogueCooldown;
    }

    private void Update()
    {
        // ✅ NOVO: Cronômetro do cooldown pós-diálogo
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;
    }

    public void OnFocusEnter()
    {
        if (interactionPrompt != null)
            interactionPrompt.SetActive(true);
    }

    public void OnFocusExit()
    {
        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
    }

    private bool isTalkingNow = false;

    /// <summary>
    /// Aciona a animação de fala no Animator do NPC (somente enquanto estiver falando)
    /// </summary>
    public void TriggerTalkAnimation()
    {
        if (isTalkingNow) return; // já está falando, não aciona de novo

        isTalkingNow = true;
        Animator npcAnimator = GetComponent<Animator>();
        if (npcAnimator != null)
        {
            // Estratégia: bool "Talk" em loop durante o diálogo
            // O Animator deve ter: Idle → Talk (quando Talk = true, speed 1) e Talk → Idle (quando Talk = false)
            npcAnimator.SetBool("Talk", true);
        }
    }

    /// <summary>
    /// ✅ NOVO: Para a animação de fala e volta o NPC ao Idle
    /// Chamado pelo NPCDialogueManager ao final do diálogo
    /// </summary>
    public void StopTalking()
    {
        isTalkingNow = false;
        Animator npcAnimator = GetComponent<Animator>();
        if (npcAnimator != null)
        {
            npcAnimator.SetBool("Talk", false);
        }
    }

    // ✅ NOVO: Faz o NPC girar suavemente em direção ao jogador durante o diálogo
    private void LateUpdate()
    {
        if (!rotateTowardPlayer) return;

        // Só gira enquanto o diálogo deste NPC estiver ativo
        bool isTalking = NPCDialogueManager.Instance != null
                         && NPCDialogueManager.Instance.IsDialogueActive
                         && NPCDialogueManager.Instance.CurrentSpeakingNPC == this;

        if (!isTalking) return;

        GameObject playerObject = GetPlayerObject();
        if (playerObject == null) return;

        // Direção do NPC até o jogador
        Vector3 direction = playerObject.transform.position - InteractTransform.position;

        if (rotateOnlyHorizontal)
            direction.y = 0f; // Remove o componente vertical para girar só no eixo Y

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            InteractTransform.rotation = Quaternion.Slerp(InteractTransform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }

    /// <summary>
    /// Encontra o GameObject do jogador (usa o PlayerInteractor como referência)
    /// </summary>
    private GameObject GetPlayerObject()
    {
        PlayerInteractor interactor = FindObjectOfType<PlayerInteractor>();
        if (interactor != null && interactor.gameObject != null)
            return interactor.gameObject;
        return GameObject.FindGameObjectWithTag("Player");
    }
}
