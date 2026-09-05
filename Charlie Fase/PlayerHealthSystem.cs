using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Sistema de vida em duas camadas:
/// 1) A personagem pode receber uma quantidade definida de hits por vida.
/// 2) Ao perder todos os hits, perde uma vida numérica e respawna, se ainda houver vidas.
/// </summary>
public class PlayerHealthSystem : MonoBehaviour
{
    [Serializable]
    public class IntUnityEvent : UnityEvent<int> { }

    [Header("Configuração de Hits")]
    [Tooltip("Quantidade de hits que a personagem suporta antes de perder uma vida.")]
    [SerializeField, Min(1)] private int hitsPerLife = 3;

    [Tooltip("Quantidade de vidas no início da partida.")]
    [SerializeField, Min(1)] private int startingLives = 3;

    [Tooltip("Tempo de invulnerabilidade após cada hit.")]
    [SerializeField, Min(0f)] private float hitInvulnerabilityDuration = 1f;

    [Header("Morte e Respawn")]
    [SerializeField, Min(0f)] private float respawnDelay = 1.25f;
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private bool disableMovementWhileDead = true;
    [SerializeField] private bool resetVelocityOnRespawn = true;

    [Header("Reação ao Dano")]
    [Tooltip("Para a velocidade horizontal ao receber um hit.")]
    [SerializeField] private bool stopMovementOnHit = true;

    [Tooltip("Tempo que o input fica bloqueado durante a animação de dano no chão.")]
    [SerializeField, Min(0f)] private float hitMovementLockDuration = 0.3f;

    [Tooltip("Tempo que o input fica bloqueado durante a animação de dano no ar.")]
    [SerializeField, Min(0f)] private float airHitMovementLockDuration = 0.6f;

    [Tooltip("Ativa o afastamento da personagem na direção oposta à origem do dano.")]
    [SerializeField] private bool applyDamageKnockback = true;

    [Tooltip("Força horizontal do afastamento causado pelo dano.")]
    [SerializeField, Min(0f)] private float damageKnockbackForce = 2.5f;

    [Tooltip("Impulso vertical opcional do knockback. Deixe 0 para não levantar a personagem.")]
    [SerializeField, Min(0f)] private float damageKnockbackUpwardForce = 0f;

    [Header("Levantar após Dano no Chão")]
    [Tooltip("Permite disparar GetUp por um Animation Event colocado no final do clip HitReaction.")]
    [SerializeField] private bool playGetUpAfterGroundHit = true;

    [Header("Referências")]
    [SerializeField] private PlayerMovement_FrontiersStyle playerMovement;
    [Tooltip("Arraste aqui o componente do sistema de swing. Ele será desativado durante a morte.")]
    [SerializeField] private Behaviour swingSystem;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Animator animator;
    [SerializeField] private DynamicFollowCamera dynamicFollowCamera;
    [Tooltip("Barras cinematográficas que fecham antes do respawn e abrem depois dele.")]
    [SerializeField] private CinematicBarsOpener cinematicBars;
    [Tooltip("Tela exibida quando todas as vidas acabam.")]
    [SerializeField] private GameOverUI gameOverUI;
    [Tooltip("Sistema de Spin Dash, cancelado imediatamente ao receber dano.")]
    [SerializeField] private PlayerSpinDash playerSpinDash;

    [Header("Parâmetros do Animator - Opcionais")]
    [SerializeField] private string hitTrigger = "Hit";
    [SerializeField] private string airHitTrigger = "AirHit";
    [SerializeField] private string getUpTrigger = "GetUp";
    [SerializeField] private string deathTrigger = "Death";
    [SerializeField] private string deathStateName = "DeathAnimation";
    [SerializeField, Min(0f)] private float deathCrossFadeDuration = 0.05f;
    [Tooltip("Duração do clip de morte antes de iniciar o respawn.")]
    [SerializeField, Min(0f)] private float deathAnimationDuration = 0.8f;
    [SerializeField] private string respawnTrigger = "Respawn";

    [Header("Eventos")]
    [Tooltip("Envia os hits restantes nesta vida.")]
    public IntUnityEvent onHitsChanged = new IntUnityEvent();

    [Tooltip("Envia a quantidade de vidas restantes.")]
    public IntUnityEvent onLivesChanged = new IntUnityEvent();

    public UnityEvent onHit = new UnityEvent();
    public UnityEvent onLifeLost = new UnityEvent();
    public UnityEvent onDeath = new UnityEvent();
    public UnityEvent onRespawn = new UnityEvent();
    public UnityEvent onHitsRestored = new UnityEvent();
    public UnityEvent onGameOver = new UnityEvent();

    private int currentHitsTaken;
    private int currentLives;
    private float nextDamageAllowedTime;
    private bool isDying;
    private bool gameOver;
    private Coroutine respawnCoroutine;

    private Vector3 defaultRespawnPosition;
    private Quaternion defaultRespawnRotation;
    private bool hasDefaultRespawnTransform;

    private bool movementWasEnabled;
    private bool swingWasEnabled;

    public int HitsPerLife => hitsPerLife;
    public int HitsTaken => currentHitsTaken;
    public int HitsRemaining => Mathf.Max(0, hitsPerLife - currentHitsTaken);
    public int LivesRemaining => currentLives;
    public bool IsInvulnerable => Time.time < nextDamageAllowedTime;
    public bool IsDying => isDying;
    public bool IsGameOver => gameOver;

    private void Awake()
    {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement_FrontiersStyle>();

        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (dynamicFollowCamera == null)
            dynamicFollowCamera = FindFirstObjectByType<DynamicFollowCamera>();

        if (cinematicBars == null)
            cinematicBars = FindFirstObjectByType<CinematicBarsOpener>();

        if (gameOverUI == null)
            gameOverUI = FindFirstObjectByType<GameOverUI>();

        if (playerSpinDash == null)
            playerSpinDash = GetComponent<PlayerSpinDash>();

        if (respawnPoint == null)
        {
            defaultRespawnPosition = transform.position;
            defaultRespawnRotation = transform.rotation;
            hasDefaultRespawnTransform = true;
        }

        hitsPerLife = Mathf.Max(1, hitsPerLife);
        startingLives = Mathf.Max(1, startingLives);
        currentLives = startingLives;
        currentHitsTaken = 0;
    }

    private void Start()
    {
        NotifyStateChanged();
    }

    /// <summary>
    /// Detecta contato físico entre o CharacterController da personagem e um
    /// objeto que possua PlayerDamageOnContact.
    /// </summary>
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (isDying || gameOver)
            return;

        PlayerDamageOnContact damageSource =
            hit.collider.GetComponentInParent<PlayerDamageOnContact>();

        if (damageSource != null && damageSource.DamageOnCollision)
            TakeDamage(damageSource.DamageAmount, hit.collider.transform.position);
    }

    /// <summary>
    /// Aplica um hit. O parâmetro padrão representa um dano de um hit.
    /// </summary>
    public bool TakeHit(int amount = 1)
    {
        return TakeDamage(amount);
    }

    /// <summary>
    /// Aplica dano sem uma origem informada, usando a direção frontal da personagem
    /// como fallback para o afastamento.
    /// </summary>
    public bool TakeDamage(int amount = 1)
    {
        return TakeDamage(amount, transform.position - transform.forward);
    }

    /// <summary>
    /// Aplica dano e recebe a posição da origem do golpe.
    /// </summary>
    public bool TakeDamage(int amount, Vector3 damageOrigin)
    {
        if (amount <= 0 || isDying || gameOver || IsInvulnerable)
            return false;

        currentHitsTaken = Mathf.Clamp(currentHitsTaken + amount, 0, hitsPerLife);
        nextDamageAllowedTime = Time.time + hitInvulnerabilityDuration;

        // Captura o estado antes de limpar os estados de movimento do impacto.
        bool wasAirborne = IsPlayerAirborne();
        float movementLockDuration = wasAirborne
            ? airHitMovementLockDuration
            : hitMovementLockDuration;

        // Cancela imediatamente a carga ou o dash ativo antes da reação de dano.
        if (playerSpinDash != null)
            playerSpinDash.ForceCancelSpinDash();

        if (stopMovementOnHit && playerMovement != null)
            playerMovement.StopMovementOnHit(movementLockDuration);

        if (applyDamageKnockback && playerMovement != null)
        {
            Vector3 knockbackDirection = transform.position - damageOrigin;
            playerMovement.ApplyDamageKnockback(
                knockbackDirection,
                damageKnockbackForce,
                damageKnockbackUpwardForce
            );
        }

        if (dynamicFollowCamera != null)
            dynamicFollowCamera.TriggerDamageShake(wasAirborne);

        onHit.Invoke();
        onHitsChanged.Invoke(HitsRemaining);

        bool isLethalHit = currentHitsTaken >= hitsPerLife;

        // O último hit prioriza a animação de morte. Os demais usam a reação
        // de chão ou a reação aérea conforme o estado no momento do dano.
        if (animator != null && !isLethalHit)
        {
            string reactionTrigger = wasAirborne ? airHitTrigger : hitTrigger;

            if (!string.IsNullOrWhiteSpace(reactionTrigger))
                animator.SetTrigger(reactionTrigger);


        }

        if (isLethalHit)
        {
            LoseLife();
        }

        return true;
    }

    /// <summary>
    /// Chamado por um Animation Event no final do clip HitReaction.
    /// Assim o GetUp respeita o tempo real da animação, sem atraso fixo.
    /// </summary>
    public void PlayGetUpAnimation()
    {
        if (!playGetUpAfterGroundHit || isDying || gameOver || animator == null)
            return;

        if (!string.IsNullOrWhiteSpace(getUpTrigger))
            animator.SetTrigger(getUpTrigger);
    }

    private bool IsPlayerAirborne()
    {
        if (playerMovement != null)
        {
            if (playerMovement.isJumping || playerMovement.isFalling)
                return true;

            return !playerMovement.isGrounded;
        }

        return characterController != null && !characterController.isGrounded;
    }

    private void ResetAnimatorTrigger(string triggerName)
    {
        if (animator != null && !string.IsNullOrWhiteSpace(triggerName))
            animator.ResetTrigger(triggerName);
    }

    private void LoseLife()
    {
        if (isDying || gameOver)
            return;

        isDying = true;
        currentHitsTaken = hitsPerLife;

        currentLives = Mathf.Max(0, currentLives - 1);
        onLifeLost.Invoke();
        onLivesChanged.Invoke(currentLives);
        onDeath.Invoke();

        // Assume a câmera automaticamente durante toda a sequência de morte.
        if (dynamicFollowCamera != null)
            dynamicFollowCamera.EnterDeathCamera();

        // Fecha as barras imediatamente ao iniciar a morte.
        // Assim elas retornam do deslocamento extra antes de chegar ao centro.
        if (cinematicBars != null)
            cinematicBars.FecharBarras();

        if (animator != null)
        {
            // Remove estados pendentes. A morte será iniciada no próximo frame,
            // depois que o Animator concluir o estado atual.
            ResetAnimatorTrigger(hitTrigger);
            ResetAnimatorTrigger(airHitTrigger);
            ResetAnimatorTrigger(getUpTrigger);
            ResetAnimatorTrigger(respawnTrigger);
            ResetAnimatorTrigger(deathTrigger);
        }

        StopPlayerSystems();

        // Bloqueio permanente da morte, independente das durações de Hit/AirHit.
        if (playerMovement != null)
            playerMovement.SetDeathMovementLock(true);

        if (currentLives <= 0)
        {
            gameOver = true;
            onGameOver.Invoke();

            // Exibe a tela final somente quando todas as vidas acabam.
            if (gameOverUI != null)
                gameOverUI.ShowGameOver();
        }

        if (respawnCoroutine != null)
            StopCoroutine(respawnCoroutine);

        // A mesma sequência toca a morte primeiro. Se não houver vidas,
        // ela apenas mantém a personagem no estado de Game Over.
        respawnCoroutine = StartCoroutine(DeathThenRespawn());
    }

    private void ForceDeathAnimation()
    {
        if (animator == null || string.IsNullOrWhiteSpace(deathStateName))
            return;

        int deathStateHash = Animator.StringToHash(deathStateName);

        if (!animator.HasState(0, deathStateHash))
        {
            Debug.LogError($"PlayerHealthSystem: o estado '{deathStateName}' não foi encontrado na Layer 0 do Animator.");
            return;
        }

        animator.enabled = true;
        animator.speed = 1f;
        animator.Play(deathStateHash, 0, 0f);
        animator.Update(0f);
    }

    private IEnumerator DeathThenRespawn()
    {
        // Aguarda um frame para não disputar a troca com a animação que estava
        // tocando no momento do último hit.
        yield return null;
        ForceDeathAnimation();

        // A morte toca primeiro. Após o clip, o Animator continua livre;
        // apenas o movimento do jogador permanece bloqueado até o respawn.
        if (deathAnimationDuration > 0f)
            yield return new WaitForSeconds(deathAnimationDuration);

        if (currentLives <= 0)
        {
            respawnCoroutine = null;
            yield break;
        }

        if (respawnDelay > 0f)
            yield return new WaitForSeconds(respawnDelay);

        // O fechamento já foi iniciado no começo da morte.
        // Aguarda o estado real de barras fechadas, respeitando as durações
        // configuradas no CinematicBarsOpener.
        if (cinematicBars != null)
        {
            while (!cinematicBars.BarrasEstaoFechadas)
                yield return null;
        }

        ResetPlayerTransform();
        currentHitsTaken = 0;
        nextDamageAllowedTime = Time.time + hitInvulnerabilityDuration;
        isDying = false;

        RestorePlayerSystems();

        // O controle só volta depois do reposicionamento e da reativação.
        if (playerMovement != null)
            playerMovement.SetDeathMovementLock(false);

        // Devolve o controle manual somente depois do respawn estar concluído.
        if (dynamicFollowCamera != null)
            dynamicFollowCamera.ExitDeathCamera();

        // Só abre depois que o jogador já foi reposicionado e reativado.
        if (cinematicBars != null)
            cinematicBars.AbrirBarras();

        if (animator != null && !string.IsNullOrWhiteSpace(respawnTrigger))
            animator.SetTrigger(respawnTrigger);

        NotifyStateChanged();
        onRespawn.Invoke();
        respawnCoroutine = null;
    }

    private void StopPlayerSystems()
    {
        if (playerMovement != null)
        {
            playerMovement.CancelWallRun();
            playerMovement.CancelGlide();
            playerMovement.CancelAirDash();
            playerMovement.CancelStomp();
            playerMovement.CancelGroundSlideImmediate();
            playerMovement.moveDirection = Vector3.zero;
            playerMovement.currentSpeed = 0f;
            playerMovement.isSwinging = false;

            movementWasEnabled = playerMovement.enabled;
            if (disableMovementWhileDead)
                playerMovement.enabled = false;
        }

        if (swingSystem != null)
        {
            // Se o PlayerSwingSystem tiver ForceStopSwing(), ele será chamado sem
            // criar uma dependência obrigatória com esta classe.
            swingSystem.SendMessage("ForceStopSwing", SendMessageOptions.DontRequireReceiver);
            swingWasEnabled = swingSystem.enabled;
            swingSystem.enabled = false;
        }

        if (characterController != null)
            characterController.enabled = false;
    }

    private void RestorePlayerSystems()
    {
        if (characterController != null)
            characterController.enabled = true;

        if (playerMovement != null && disableMovementWhileDead)
            playerMovement.enabled = movementWasEnabled;

        if (swingSystem != null)
            swingSystem.enabled = swingWasEnabled;

        if (playerMovement != null)
        {
            playerMovement.moveDirection = Vector3.zero;
            playerMovement.currentSpeed = 0f;
            playerMovement.isJumping = false;
            playerMovement.isFalling = false;
            playerMovement.isGrounded = false;
            playerMovement.isSwinging = false;
        }
    }

    private void ResetPlayerTransform()
    {
        Vector3 targetPosition;
        Quaternion targetRotation;

        if (respawnPoint != null)
        {
            targetPosition = respawnPoint.position;
            targetRotation = respawnPoint.rotation;
        }
        else if (hasDefaultRespawnTransform)
        {
            targetPosition = defaultRespawnPosition;
            targetRotation = defaultRespawnRotation;
        }
        else
        {
            targetPosition = transform.position;
            targetRotation = transform.rotation;
        }

        if (characterController != null)
            characterController.enabled = false;

        transform.SetPositionAndRotation(targetPosition, targetRotation);

        if (characterController != null)
            characterController.enabled = true;
    }

    private void NotifyStateChanged()
    {
        onHitsChanged.Invoke(HitsRemaining);
        onLivesChanged.Invoke(currentLives);
    }

    /// <summary>
    /// Atualiza o checkpoint usado no próximo respawn.
    /// </summary>
    public void SetRespawnPoint(Transform newRespawnPoint)
    {
        respawnPoint = newRespawnPoint;
    }

    /// <summary>
    /// Adiciona vidas extras, útil para coletáveis no estilo Sonic.
    /// </summary>
    public void AddLife(int amount = 1)
    {
        if (amount <= 0 || gameOver)
            return;

        currentLives += amount;
        onLivesChanged.Invoke(currentLives);
    }

    /// <summary>
    /// Restaura todos os hits da vida atual e avisa a HUD.
    /// Pode ser usado por coletáveis de vida ou itens de recuperação.
    /// </summary>
    public void RestoreHits()
    {
        if (isDying || gameOver)
            return;

        currentHitsTaken = 0;
        nextDamageAllowedTime = 0f;
        onHitsChanged.Invoke(HitsRemaining);
        onHitsRestored.Invoke();
    }

    /// <summary>
    /// Define diretamente a quantidade de vidas restantes.
    /// </summary>
    public void SetLives(int amount)
    {
        currentLives = Mathf.Max(0, amount);
        gameOver = currentLives <= 0;
        onLivesChanged.Invoke(currentLives);
    }

        /// <summary>
    /// Reinicia a fase a partir da tela de Game Over, sem recarregar a cena.
    /// </summary>
    public void RestartFromGameOver()
    {
        if (respawnCoroutine != null)
        {
            StopCoroutine(respawnCoroutine);
            respawnCoroutine = null;
        }

        if (playerSpinDash != null)
            playerSpinDash.ForceCancelSpinDash();

        gameOver = false;
        isDying = false;
        currentLives = startingLives;
        currentHitsTaken = 0;
        nextDamageAllowedTime = 0f;

        ResetPlayerTransform();
        RestorePlayerSystems();

        if (playerMovement != null)
        {
            playerMovement.SetDeathMovementLock(false);
            playerMovement.moveDirection = Vector3.zero;
            playerMovement.currentSpeed = 0f;
        }

        if (dynamicFollowCamera != null)
            dynamicFollowCamera.ExitDeathCamera();

        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }

        NotifyStateChanged();
        onRespawn.Invoke();
    }

    /// <summary>
    /// Restaura completamente as vidas, útil para reiniciar uma fase.
    /// </summary>
    public void ResetLives()

    {
        if (respawnCoroutine != null)
        {
            StopCoroutine(respawnCoroutine);
            respawnCoroutine = null;
        }

        gameOver = false;
        isDying = false;
        currentLives = startingLives;
        currentHitsTaken = 0;
        nextDamageAllowedTime = 0f;
        RestorePlayerSystems();
        NotifyStateChanged();
    }
}
