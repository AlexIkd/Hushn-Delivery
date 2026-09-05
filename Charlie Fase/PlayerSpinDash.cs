using UnityEngine;

/// <summary>
/// PlayerSpinDash — Habilidade de Spin Dash inspirada no Sonic.
/// O jogador segura LeftShift enquanto está no chão para carregar um impulso.
/// Existem 3 níveis de carregamento:
///   Nível 1 (Impulso Curto)  → Air Dash-like, pequeno burst de velocidade.
///   Nível 2 (Alcance Médio)  → Velocidade aumentada com duração média.
///   Nível 3 (Longo Alcance)  → Velocidade máxima com duração longa.
///
/// INTEGRAÇÃO: Funciona SEM precisar alterar o PlayerMovement_FrontiersStyle.
/// Durante o carregamento, usa LateUpdate para zerar a velocidade após o script de movimento.
/// Durante o dash ativo, usa LateUpdate para manter a velocidade forçada.
///
/// ROTAÇÃO: Durante o Spin Dash (charge e dash ativo), o jogador pode rotacionar
/// usando WASD, relativo à câmera — exatamente como o movimento normal.
///
/// RESTRIÇÃO: O Spin Dash NÃO pode ser ativado enquanto o jogador estiver no Slope Slide.
/// Se o jogador iniciar um Slope Slide durante o charge, o carregamento é cancelado.
/// Se o Spin Dash estiver ativo e o jogador entrar no Slope Slide, o dash é cancelado.
///
/// RESTRIÇÃO ADICIONAL: O Spin Dash NÃO pode ser ativado enquanto o jogador estiver
/// grinding (em um rail). Se o jogador estiver no rail, o carregamento e o dash são cancelados.
/// </summary>
public class PlayerSpinDash : MonoBehaviour
{
    // ======================================================
    // REFERÊNCIAS
    // ======================================================

    [Header("Referências")]
    [Tooltip("Script principal de movimento do jogador")]
    [SerializeField] private PlayerMovement_FrontiersStyle playerMovement;

    [Tooltip("Script de câmera para o efeito de congelamento")]
    [SerializeField] private DynamicFollowCamera cameraScript;

    [Tooltip("Animator do jogador (opcional, para triggers de animação)")]
    [SerializeField] private Animator animator;

    [Tooltip("Sistema de Slope Slide do jogador")]
    [SerializeField] private SlopeSlideSystem slopeSlideSystem;

    [Tooltip("Script de rail ride do jogador (opcional, para bloquear Spin Dash no rail)")]
    [SerializeField] private PlayerRailRide_SonicStyle_Spline railRide;

    [Tooltip("Sistema de Parkour do jogador")]
    [SerializeField] private ParkourSystem parkourSystem;

    [Tooltip("Sistema de vida usado para bloquear o Spin Dash durante a morte")]
    [SerializeField] private PlayerHealthSystem healthSystem;

    // ======================================================
    // TECLA DE ATIVAÇÃO
    // ======================================================

    [Header("Input")]
    [SerializeField] private KeyCode spinDashKey = KeyCode.LeftShift;

    // ======================================================
    // ROTAÇÃO DURANTE O SPIN DASH (Charge + Dash Ativo)
    // ======================================================

    [Header("Rotação Durante Spin Dash")]
    [Tooltip("Se verdadeiro, o jogador pode rotacionar durante o carregamento e o dash")]
    [SerializeField] private bool allowRotationDuringDash = true;

    [Tooltip("Velocidade de rotação durante o carregamento (charge) em graus por segundo")]
    [SerializeField] private float chargeRotationSpeed = 540f;

    [Tooltip("Velocidade de rotação durante o dash ativo em graus por segundo")]
    [SerializeField] private float dashRotationSpeed = 360f;

    // ======================================================
    // ZONAS DE CARREGAMENTO (em segundos segurando a tecla)
    // ======================================================

    [Header("Zonas de Carga")]
    [Tooltip("Tempo mínimo para ativar o Nível 1 (Impulso Curto)")]
    [SerializeField] private float chargeThresholdLevel1 = 0.15f;

    [Tooltip("Tempo mínimo para ativar o Nível 2 (Alcance Médio)")]
    [SerializeField] private float chargeThresholdLevel2 = 0.5f;

    [Tooltip("Tempo mínimo para ativar o Nível 3 (Longo Alcance)")]
    [SerializeField] private float chargeThresholdLevel3 = 0.85f;

    [Header("Quick Tap (Tap Rápido)")]
    [Tooltip("Tempo máximo para considerar um 'tap rápido' (apertar e soltar rápido). Se o jogador soltar a tecla antes deste tempo, o Spin Dash é ignorado completamente: sem animação de carga, sem travar movimento. 0 = desativado.")]
    [SerializeField] private float quickTapMaxTime = 0.08f;

    // ======================================================
    // CONFIGURAÇÕES DE VELOCIDADE POR NÍVEL
    // ======================================================

    [Header("Configurações de Velocidade — Nível 1 (Impulso Curto)")]
    [SerializeField] private float level1Speed = 20f;
    [SerializeField] private float level1Duration = 0.25f;

    [Header("Configurações de Velocidade — Nível 2 (Alcance Médio)")]
    [SerializeField] private float level2Speed = 28f;
    [SerializeField] private float level2Duration = 0.5f;

    [Header("Configurações de Velocidade — Nível 3 (Longo Alcance)")]
    [SerializeField] private float level3Speed = 35f;
    [SerializeField] private float level3Duration = 0.8f;

    // ======================================================
    // COOLDOWN E BLOQUEIOS
    // ======================================================

    [Header("Cooldown e Bloqueios")]
    [SerializeField] private float cooldownAfterDash = 0.4f;
    [SerializeField] private float maxChargeTime = 1.2f;

    // ======================================================
    // EFEITOS VISUAIS (PARTÍCULAS)
    // ======================================================

    [Header("Efeitos Visuais")]
    [Tooltip("Partículas exibidas durante o carregamento (girando)")]
    [SerializeField] private ParticleSystem chargeParticles;

    [Tooltip("Partículas exibidas ao liberar o Nível 1")]
    [SerializeField] private ParticleSystem level1Particles;

    [Tooltip("Partículas exibidas ao liberar o Nível 2")]
    [SerializeField] private ParticleSystem level2Particles;

    [Tooltip("Partículas exibidas ao liberar o Nível 3")]
    [SerializeField] private ParticleSystem level3Particles;

    [Tooltip("Trail Renderer para o efeito visual durante o Spin Dash")]
    [SerializeField] private TrailRenderer spinDashTrail;

    // ======================================================
    // ÁUDIO (OPCIONAL)
    // ======================================================

    [Header("Áudio (Opcional)")]
    [SerializeField] private AudioClip chargeSound;
    [SerializeField] private AudioClip level1Sound;
    [SerializeField] private AudioClip level2Sound;
    [SerializeField] private AudioClip level3Sound;
    [SerializeField] private AudioSource audioSource;

    // ======================================================
    // ESTADO INTERNO
    // ======================================================

    private bool isCharging = false;
    private bool isSpinning = false;
    private float chargeTimer = 0f;
    private float dashTimer = 0f;
    private float cooldownTimer = 0f;

    private SpinDashLevel currentLevel = SpinDashLevel.None;
    private Vector3 spinDashDirection = Vector3.forward;

    // Estado salvo para restaurar após o dash
    private float savedMaxSpeed = 0f;

    // ✅ QUICK TAP — Estado de tap rápido (apertar e soltar rapidamente)
    private bool isQuickTapping = false;
    private bool quickTapActivatedAnimation = false;

    // ======================================================
    // PROPRIEDADE PÚBLICA (útil se quiser consultar de outros scripts)
    // ======================================================

    public bool IsSpinDashActive => (isCharging && !isQuickTapping) || isSpinning;
    public int GetChargeLevel() => (int)currentLevel;
    public bool IsChargingSpinDash() => isCharging && !isQuickTapping;
    public bool IsSpinDashing() => isSpinning;
    public bool IsQuickTapping => isQuickTapping;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    // ======================================================
    // ENUM DE NÍVEIS
    // ======================================================

    private enum SpinDashLevel
    {
        None,
        Level1,
        Level2,
        Level3
    }

    // ======================================================
    // INICIALIZAÇÃO
    // ======================================================

    private void Awake()
    {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement_FrontiersStyle>();

        if (cameraScript == null)
            cameraScript = FindObjectOfType<DynamicFollowCamera>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (slopeSlideSystem == null)
            slopeSlideSystem = GetComponent<SlopeSlideSystem>();

        // Tenta encontrar o PlayerRailRide_SonicStyle_Spline no mesmo GameObject
        if (railRide == null)
            railRide = GetComponent<PlayerRailRide_SonicStyle_Spline>();

        if (parkourSystem == null)
            parkourSystem = GetComponent<ParkourSystem>();

        if (healthSystem == null)
            healthSystem = GetComponent<PlayerHealthSystem>();

        StopAllParticleEffects();
    }

    // ======================================================
    // UPDATE — LÓGICA DE INPUT E ESTADO
    // ======================================================

    private void Update()
    {
        // Enquanto o jogador estiver morto ou em Game Over, cancela qualquer
        // carga/dash ativo e impede uma nova ativação do Spin Dash.
        if (healthSystem != null &&
            (healthSystem.IsDying || healthSystem.IsGameOver))
        {
            if (isCharging || isSpinning)
                ForceCancelSpinDash();

            return;
        }

        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
            return;
        }

        // =============================================
        // BLOQUEIO DURANTE A REAÇÃO DE DANO
        // =============================================
        if (playerMovement != null && playerMovement.IsDamageMovementLocked)
        {
            if (isCharging)
            {
                CancelCharge();
                if (showDebugInfo)
                    Debug.Log("🚫 Spin Dash: Cancelado — reação de dano ativa.");
            }

            if (isSpinning)
            {
                StopActiveSpinDash();
                if (showDebugInfo)
                    Debug.Log("🚫 Spin Dash: Parado — reação de dano ativa.");
            }

            return;
        }

        // =============================================
        // ✅ NOVO: SE O JOGADOR ESTIVER EM DIÁLOGO COM UM NPC, CANCELA
        // TUDO (charge e dash ativo) — igual aos outros bloqueios
        // =============================================
        if (IsInDialogue())
        {
            if (isCharging)
            {
                CancelCharge();
                if (showDebugInfo)
                    Debug.Log("🚫 Spin Dash: Cancelado — Jogador está em diálogo.");
            }
            if (isSpinning)
            {
                StopActiveSpinDash();
                if (showDebugInfo)
                    Debug.Log("🚫 Spin Dash: Parado — Jogador está em diálogo.");
            }
            return;
        }

        // =============================================
        // SE O JOGADOR ESTIVER INTERAGINDO (SENTADO), CANCELA
        // TUDO (charge e dash ativo)
        // =============================================
        if (playerMovement != null && playerMovement.IsSitting)
        {
            if (isCharging) CancelCharge();
            if (isSpinning) StopActiveSpinDash();
            return;
        }

        // =============================================
        // SE O JOGADOR ENTRAR NO SLOPE SLIDE, CANCELA
        // TUDO (charge e dash ativo)
        // =============================================
        if (IsInSlopeSlide())
        {
            if (isCharging)
            {
                CancelCharge();
                if (showDebugInfo)
                    Debug.Log("🚫 Spin Dash: Cancelado — Slope Slide ativado.");
            }
            if (isSpinning)
            {
                StopActiveSpinDash();
                if (showDebugInfo)
                    Debug.Log("🚫 Spin Dash: Parado — Slope Slide ativado.");
            }
            return;
        }

        // =============================================
        // SE O JOGADOR ESTIVER NO RAIL (GRINDING), CANCELA
        // TUDO (charge e dash ativo)
        // =============================================
        if (IsInRail())
        {
            if (isCharging)
            {
                CancelCharge();
                if (showDebugInfo)
                    Debug.Log("🚫 Spin Dash: Cancelado — Jogador está no rail.");
            }
            if (isSpinning)
            {
                StopActiveSpinDash();
                if (showDebugInfo)
                    Debug.Log("🚫 Spin Dash: Parado — Jogador está no rail.");
            }
            return;
        }

        // ✅ NOVO: Impede INICIAR o carregamento do Spin Dash durante o diálogo
        if (IsInDialogue())
            return;

        // =============================================
        // SE O JOGADOR ESTIVER NO PARKOUR, CANCELA
        // TUDO (charge e dash ativo)
        // =============================================
        if (IsInParkour())
        {
            if (isCharging)
            {
                CancelCharge();
                if (showDebugInfo)
                    Debug.Log("🚫 Spin Dash: Cancelado — Parkour ativado.");
            }
            if (isSpinning)
            {
                StopActiveSpinDash();
                if (showDebugInfo)
                    Debug.Log("🚫 Spin Dash: Parado — Parkour ativado.");
            }
            return;
        }

        // =============================================
        // SE O JOGADOR EXECUTAR UM SKID, CANCELA
        // TUDO (charge e dash ativo)
        // =============================================
        if (playerMovement != null && playerMovement.IsSkidding)
        {
            if (isCharging)
            {
                CancelCharge();
                if (showDebugInfo)
                    Debug.Log("🚫 Spin Dash: Cancelado — Skid ativado.");
            }
            if (isSpinning)
            {
                StopActiveSpinDash();
                if (showDebugInfo)
                    Debug.Log("🚫 Spin Dash: Parado — Skid ativado.");
            }
            return;
        }

        if (isSpinning)
        {
            // Rotação durante dash ativo (relativa à câmera, como movimento normal)
            if (allowRotationDuringDash)
            {
                HandleRotationWithCamera();
            }

            UpdateActiveSpinDash();
            return;
        }

        if (isCharging)
        {
            // Rotação durante o charge (relativa à câmera, como movimento normal)
            if (allowRotationDuringDash)
            {
                HandleRotationWithCamera();
            }

            UpdateCharge();
            return;
        }

        HandleChargeStart();
    }

    // ======================================================
    // LATEUPDATE — AJUSTE DE VELOCIDADE
    // ======================================================

    private void LateUpdate()
    {
        if (cooldownTimer > 0) return;

        // Se estiver no Slope Slide, não interfere na velocidade (o SlopeSlideSystem cuida disso)
        if (IsInSlopeSlide()) return;

        // Se estiver no rail, não interfere na velocidade (o RailRide cuida disso)
        if (IsInRail()) return;

        // ✅ QUICK TAP: Durante o quick tap (tap rápido), NÃO zera a velocidade.
        // O jogador pode continuar se movendo normalmente.
        if (isCharging && quickTapActivatedAnimation && playerMovement != null)
        {
            // Durante o carregamento: ZERA a velocidade para o jogador ficar parado
            playerMovement.moveDirection.x = 0f;
            playerMovement.moveDirection.z = 0f;
            playerMovement.currentSpeed = 0f;
        }

        if (isSpinning && playerMovement != null)
        {
            // Durante o dash ativo: FORÇA a velocidade do Spin Dash na direção atual
            float targetSpeed = 0f;
            switch (currentLevel)
            {
                case SpinDashLevel.Level1: targetSpeed = level1Speed; break;
                case SpinDashLevel.Level2: targetSpeed = level2Speed; break;
                case SpinDashLevel.Level3: targetSpeed = level3Speed; break;
            }

            Vector3 dir = spinDashDirection.normalized;
            playerMovement.moveDirection.x = dir.x * targetSpeed;
            playerMovement.moveDirection.z = dir.z * targetSpeed;
            playerMovement.currentSpeed = targetSpeed;
        }
    }

    // ======================================================
    // INÍCIO DO CARREGAMENTO
    // ======================================================

    private void HandleChargeStart()
    {
        // Bloqueia o Spin Dash se o jogador estiver no Slope Slide
        if (IsInSlopeSlide()) return;

        // Bloqueia o Spin Dash se o jogador estiver no rail
        if (IsInRail()) return;

        if (!IsGrounded()) return;
        if (IsInBlockedState()) return;

        if (Input.GetKeyDown(spinDashKey))
        {
            StartCharging();
        }
    }

    private void StartCharging()
    {
        isCharging = true;
        isQuickTapping = false;
        quickTapActivatedAnimation = false;
        chargeTimer = 0f;
        currentLevel = SpinDashLevel.None;
        spinDashDirection = GetSpinDashDirection();

        // ✅ QUICK TAP: Não ativa animação/efeitos imediatamente.
        // Eles serão ativados apenas se o jogador segurar por mais tempo que quickTapMaxTime.

        if (showDebugInfo)
            Debug.Log("🔵 Spin Dash: Carregamento iniciado!");
    }

    // ======================================================
    // ATUALIZAÇÃO DO CARREGAMENTO
    // ======================================================

    private void UpdateCharge()
    {
        // Se soltar a tecla, libera o Spin Dash (ou cancela se nível < 1)
        if (Input.GetKeyUp(spinDashKey))
        {
            ReleaseSpinDash();
            return;
        }

        // Cancela se sair do chão
        if (!IsGrounded())
        {
            CancelCharge();
            return;
        }

        // Cancela se entrar em estado bloqueado
        if (IsInBlockedState())
        {
            CancelCharge();
            return;
        }

        // Se o jogador pular, cancela o carregamento
        if (Input.GetButtonDown("Jump"))
        {
            CancelCharge();
            return;
        }

        // Cancela se entrar no Slope Slide
        if (IsInSlopeSlide())
        {
            CancelCharge();
            return;
        }

        // Cancela se entrar no rail
        if (IsInRail())
        {
            CancelCharge();
            return;
        }

        // Atualiza o timer de carga
        chargeTimer += Time.deltaTime;

        // ✅ QUICK TAP: Se o jogador segurou mais que o tempo máximo de tap,
        // agora ativa a animação e os efeitos (é um charge real).
        if (!isQuickTapping && !quickTapActivatedAnimation && quickTapMaxTime > 0f)
        {
            if (chargeTimer >= quickTapMaxTime)
            {
                ActivateChargeAnimationAndEffects();
            }
        }

        SpinDashLevel previousLevel = currentLevel;
        currentLevel = GetCurrentLevel();

        if (currentLevel != previousLevel && currentLevel != SpinDashLevel.None)
        {
            OnChargeLevelUp(currentLevel);
        }

        if (chargeTimer >= maxChargeTime)
        {
            chargeTimer = maxChargeTime;
            currentLevel = SpinDashLevel.Level3;
        }

        if (showDebugInfo)
            Debug.Log($"🟡 Spin Dash: Carregando... {chargeTimer:F2}s | Nível: {currentLevel}");
    }

    /// <summary>
    /// ✅ QUICK TAP: Ativa a animação, partículas e som do carregamento.
    /// Chamada quando o jogador segura a tecla por mais tempo que quickTapMaxTime.
    /// </summary>
    private void ActivateChargeAnimationAndEffects()
    {
        quickTapActivatedAnimation = true;

        if (animator != null)
            animator.SetBool("IsSpinDashCharging", true);

        PlayParticle(chargeParticles);
        PlaySound(chargeSound);

        if (showDebugInfo)
            Debug.Log("🟡 Spin Dash: Animação e efeitos de carga ativados (segurou tempo suficiente).");
    }

    private SpinDashLevel GetCurrentLevel()
    {
        if (chargeTimer >= chargeThresholdLevel3) return SpinDashLevel.Level3;
        if (chargeTimer >= chargeThresholdLevel2) return SpinDashLevel.Level2;
        if (chargeTimer >= chargeThresholdLevel1) return SpinDashLevel.Level1;
        return SpinDashLevel.None;
    }

    private void OnChargeLevelUp(SpinDashLevel level)
    {
        if (chargeParticles != null)
        {
            chargeParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            chargeParticles.Play();
        }

        if (showDebugInfo)
            Debug.Log($"⚡ Spin Dash: Subiu para Nível {(int)level}!");
    }

    private void CancelCharge()
    {
        isCharging = false;
        isQuickTapping = false;
        quickTapActivatedAnimation = false;
        chargeTimer = 0f;
        currentLevel = SpinDashLevel.None;

        // Só para as partículas se a animação foi ativada (evita efeitos fantasma no quick tap)
        if (quickTapActivatedAnimation)
        {
            StopParticle(chargeParticles);
        }

        if (animator != null)
            animator.SetBool("IsSpinDashCharging", false);

        if (showDebugInfo)
            Debug.Log("🚫 Spin Dash: Carregamento cancelado.");
    }

    // ======================================================
    // LIBERAÇÃO DO SPIN DASH
    // ======================================================

    private void ReleaseSpinDash()
    {
        isCharging = false;

        if (currentLevel == SpinDashLevel.None)
        {
            // ✅ QUICK TAP: Se soltou rápido e nunca ativou a animação, é um tap rápido.
            // Cancela silenciosamente sem animação, sem travar movimento.
            if (quickTapMaxTime > 0f && chargeTimer < quickTapMaxTime && !quickTapActivatedAnimation)
            {
                CancelQuickTap();
                return;
            }
            CancelCharge();
            return;
        }

        // Ativa o congelamento da câmera apenas para Nível 2 e 3
        if (cameraScript != null && (currentLevel == SpinDashLevel.Level2 || currentLevel == SpinDashLevel.Level3))
        {
            cameraScript.TriggerSpinDashFreeze();
        }

        isSpinning = true;
        spinDashDirection = transform.forward;
        spinDashDirection.y = 0f;
        spinDashDirection = spinDashDirection.normalized;

        switch (currentLevel)
        {
            case SpinDashLevel.Level1: ApplyLevel1Dash(); break;
            case SpinDashLevel.Level2: ApplyLevel2Dash(); break;
            case SpinDashLevel.Level3: ApplyLevel3Dash(); break;
        }
    }

    /// <summary>
    /// ✅ QUICK TAP: Cancela o carregamento silenciosamente.
    /// Não ativa animação, não toca som, não trava movimento.
    /// O jogador pode continuar se movendo normalmente.
    /// </summary>
    private void CancelQuickTap()
    {
        isCharging = false;
        isQuickTapping = false;
        quickTapActivatedAnimation = false;
        chargeTimer = 0f;
        currentLevel = SpinDashLevel.None;

        // NÃO ativa/desativa animação — mantém o estado do Animator como está.
        // NÃO para partículas de charge (elas nunca foram iniciadas).
        // NÃO toca som de cancelamento.

        if (showDebugInfo)
            Debug.Log("👆 Spin Dash: Quick tap ignorado — jogador pode continuar se movendo.");
    }

    private void ApplyLevel1Dash()
    {
        dashTimer = level1Duration;
        savedMaxSpeed = playerMovement != null ? playerMovement.maxSpeed : 0f;

        if (playerMovement != null)
            playerMovement.ResetVerticalVelocity();

        if (playerMovement != null)
        {
            Vector3 dir = spinDashDirection.normalized;
            playerMovement.moveDirection.x = dir.x * level1Speed;
            playerMovement.moveDirection.z = dir.z * level1Speed;
            playerMovement.currentSpeed = level1Speed;
        }

        PlayParticle(level1Particles);
        PlaySound(level1Sound);
        StopParticle(chargeParticles);
        EnableTrail();

        if (animator != null)
        {
            animator.SetBool("IsSpinDashCharging", false);
            animator.SetTrigger("SpinDashLevel1");
        }

        if (showDebugInfo)
            Debug.Log($"💨 Spin Dash Nível 1: {level1Speed} por {level1Duration}s");
    }

    private void ApplyLevel2Dash()
    {
        dashTimer = level2Duration;
        savedMaxSpeed = playerMovement != null ? playerMovement.maxSpeed : 0f;

        if (playerMovement != null)
            playerMovement.ResetVerticalVelocity();

        if (playerMovement != null)
        {
            Vector3 dir = spinDashDirection.normalized;
            playerMovement.moveDirection.x = dir.x * level2Speed;
            playerMovement.moveDirection.z = dir.z * level2Speed;
            playerMovement.currentSpeed = level2Speed;
        }

        PlayParticle(level2Particles);
        PlaySound(level2Sound);
        StopParticle(chargeParticles);
        EnableTrail();

        if (animator != null)
        {
            animator.SetBool("IsSpinDashCharging", false);
            animator.SetTrigger("SpinDashLevel2");
        }

        if (showDebugInfo)
            Debug.Log($"💨 Spin Dash Nível 2: {level2Speed} por {level2Duration}s");
    }

    private void ApplyLevel3Dash()
    {
        dashTimer = level3Duration;
        savedMaxSpeed = playerMovement != null ? playerMovement.maxSpeed : 0f;

        if (playerMovement != null)
            playerMovement.ResetVerticalVelocity();

        if (playerMovement != null)
        {
            Vector3 dir = spinDashDirection.normalized;
            playerMovement.moveDirection.x = dir.x * level3Speed;
            playerMovement.moveDirection.z = dir.z * level3Speed;
            playerMovement.currentSpeed = level3Speed;
        }

        PlayParticle(level3Particles);
        PlaySound(level3Sound);
        StopParticle(chargeParticles);
        EnableTrail();

        if (animator != null)
        {
            animator.SetBool("IsSpinDashCharging", false);
            animator.SetTrigger("SpinDashLevel3");
        }

        if (showDebugInfo)
            Debug.Log($"🔥 Spin Dash Nível 3: {level3Speed} por {level3Duration}s");
    }

    // ======================================================
    // ROTAÇÃO COM WASD RELATIVO À CÂMERA
    // ======================================================

    private void HandleRotationWithCamera()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        if (Mathf.Abs(h) < 0.01f && Mathf.Abs(v) < 0.01f)
            return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 cameraForward = cam.transform.forward;
        Vector3 cameraRight = cam.transform.right;
        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 worldDirection = (cameraForward * v + cameraRight * h);

        if (worldDirection.sqrMagnitude < 0.01f)
            worldDirection = transform.forward;

        worldDirection.y = 0f;
        worldDirection.Normalize();

        float targetAngle = Mathf.Atan2(worldDirection.x, worldDirection.z) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);

        float speed = isSpinning ? dashRotationSpeed : chargeRotationSpeed;

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            speed * Time.deltaTime
        );

        if (isSpinning)
        {
            spinDashDirection = transform.forward;
            spinDashDirection.y = 0f;
            spinDashDirection = spinDashDirection.normalized;
        }
    }

    // ======================================================
    // ATUALIZAÇÃO DO SPIN DASH ATIVO
    // ======================================================

    private void UpdateActiveSpinDash()
    {
        dashTimer -= Time.deltaTime;

        if (spinDashTrail != null)
            spinDashTrail.emitting = true;

        if (dashTimer <= 0)
        {
            StopActiveSpinDash();
        }
    }

    private void StopActiveSpinDash()
    {
        isSpinning = false;
        dashTimer = 0f;
        currentLevel = SpinDashLevel.None;

        DisableTrail();

        if (playerMovement != null)
        {
            playerMovement.maxSpeed = savedMaxSpeed;
        }

        cooldownTimer = cooldownAfterDash;

        if (showDebugInfo)
            Debug.Log("✅ Spin Dash: Finalizado. Cooldown iniciado.");
    }

    // ======================================================
    // VERIFICAÇÃO DO SLOPE SLIDE
    // ======================================================

    private bool IsInSlopeSlide()
    {
        if (slopeSlideSystem == null) return false;
        return slopeSlideSystem.IsSliding();
    }

    // ======================================================
    // VERIFICAÇÃO DO RAIL (GRINDING)
    // ======================================================

    private bool IsInRail()
    {
        if (railRide == null) return false;
        return railRide.isGrinding;
    }

    private bool IsInParkour()
    {
        if (parkourSystem == null) return false;
        return parkourSystem.IsParkourActive;
    }

    // ======================================================
    // UTILITÁRIOS
    // ======================================================

    private Vector3 GetSpinDashDirection()
    {
        if (playerMovement != null)
        {
            Vector3 horizontalMove = playerMovement.moveDirection;
            horizontalMove.y = 0f;

            if (horizontalMove.sqrMagnitude > 0.01f)
                return horizontalMove.normalized;
        }

        return transform.forward;
    }

    private bool IsGrounded()
    {
        // Se o SlopeSlideSystem estiver ativo, assumimos que o jogador está no chão (pois ele desliza na rampa)
        if (IsInSlopeSlide()) return true;

        if (playerMovement != null) return playerMovement.IsGrounded;
        return Physics.CheckSphere(transform.position + Vector3.up * 0.1f, 0.2f);
    }

    private bool IsInBlockedState()
    {
        // Se estiver no Slope Slide, o estado está bloqueado para Spin Dash
        if (IsInSlopeSlide()) return true;

        if (playerMovement == null) return false;

        // Nota: Durante o Slope Slide, o playerMovement.enabled pode ser false,
        // mas as variáveis booleanas dele ainda podem ser consultadas se não dependerem do Update.
        return playerMovement.IsGroundSliding ||
               playerMovement.IsStomping ||
               playerMovement.IsRotationLocked ||
               playerMovement.isSwinging ||
               playerMovement.IsInNarrowPassage ||
               playerMovement.IsGrabbingBar ||
               IsInParkour();
    }

    private void EnableTrail()
    {
        if (spinDashTrail != null)
            spinDashTrail.emitting = true;
    }

    private void DisableTrail()
    {
        if (spinDashTrail != null)
            spinDashTrail.emitting = false;
    }

    private void PlayParticle(ParticleSystem ps)
    {
        if (ps == null) return;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.Play();
    }

    private void StopParticle(ParticleSystem ps)
    {
        if (ps == null) return;
        ps.Stop();
    }

    private void StopAllParticleEffects()
    {
        StopParticle(chargeParticles);
        StopParticle(level1Particles);
        StopParticle(level2Particles);
        StopParticle(level3Particles);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip);
    }

    // ======================================================
    // MÉTODOS PÚBLICOS
    // ======================================================

    public void ForceCancelSpinDash()
    {
        bool hadSpinDashState = isCharging || isSpinning || isQuickTapping || currentLevel != SpinDashLevel.None;

        // Não usar else-if: limpa todos os estados mesmo se a carga estiver
        // mudando para o dash no mesmo frame em que o dano foi recebido.
        if (isCharging)
            CancelCharge();

        if (isSpinning)
            StopActiveSpinDash();

        isCharging = false;
        isSpinning = false;
        isQuickTapping = false;
        quickTapActivatedAnimation = false;
        chargeTimer = 0f;
        dashTimer = 0f;
        currentLevel = SpinDashLevel.None;
        spinDashDirection = transform.forward;

        DisableTrail();
        StopAllParticleEffects();

        if (hadSpinDashState && showDebugInfo)
            Debug.Log("🚫 Spin Dash: cancelado imediatamente por dano.");
    }

    /// <summary>
    /// ✅ NOVO: Verifica se o jogador está em diálogo com um NPC
    /// (usa o NPCDialogueManager, igual aos bloqueios do skid e do air dash)
    /// </summary>
    private bool IsInDialogue()
    {
        return NPCDialogueManager.Instance != null && NPCDialogueManager.Instance.IsDialogueActive;
    }

    // ======================================================
    // DEBUG GUI
    // ======================================================

    private void OnGUI()
    {
        if (!showDebugInfo) return;

        float yPos = 10f;
        float lineHeight = 20f;

        GUI.Label(new Rect(10, yPos, 350, 20), $"Spin Dash: {(isCharging ? "Carregando" : isSpinning ? "Ativo" : "Idle")}");
        yPos += lineHeight;

        GUI.Label(new Rect(10, yPos, 350, 20), $"Slope Slide: {IsInSlopeSlide()}");
        yPos += lineHeight;

        GUI.Label(new Rect(10, yPos, 350, 20), $"No Rail: {IsInRail()}");
        yPos += lineHeight;

        GUI.Label(new Rect(10, yPos, 350, 20), $"No Parkour: {IsInParkour()}");
        yPos += lineHeight;

        GUI.Label(new Rect(10, yPos, 350, 20), $"Em Diálogo: {IsInDialogue()}");
        yPos += lineHeight;

        if (isCharging)
        {
            GUI.Label(new Rect(10, yPos, 350, 20), $"Carga: {chargeTimer:F2}s | Nível: {currentLevel}");
            yPos += lineHeight;

            float barWidth = 200f;
            float progress = Mathf.Clamp01(chargeTimer / maxChargeTime);
            Color barColor = currentLevel == SpinDashLevel.Level3 ? Color.red :
                             currentLevel == SpinDashLevel.Level2 ? Color.yellow :
                             currentLevel == SpinDashLevel.Level1 ? Color.cyan : Color.gray;
            GUI.color = barColor;
            GUI.DrawTexture(new Rect(10, yPos, barWidth * progress, 12), Texture2D.whiteTexture);
            GUI.color = Color.white;
            yPos += 20f;

            GUI.Label(new Rect(10, yPos, 350, 20), $"T1: {chargeThresholdLevel1:F2}s | T2: {chargeThresholdLevel2:F2}s | T3: {chargeThresholdLevel3:F2}s");
            yPos += lineHeight;
            GUI.Label(new Rect(10, yPos, 350, 20), $"Rotação: {allowRotationDuringDash} | Charge: {chargeRotationSpeed}°/s");
        }

        if (isSpinning)
        {
            GUI.Label(new Rect(10, yPos, 350, 20), $"Dash Ativo: {dashTimer:F2}s restantes | Nível: {currentLevel}");
            yPos += lineHeight;
            GUI.Label(new Rect(10, yPos, 350, 20), $"Rotação: {allowRotationDuringDash} | Dash: {dashRotationSpeed}°/s");
        }

        if (cooldownTimer > 0)
        {
            GUI.Label(new Rect(10, yPos, 350, 20), $"Cooldown: {cooldownTimer:F2}s");
        }

        GUI.Label(new Rect(10, yPos, 350, 20), $"Bloqueado: {IsInBlockedState()}");
        yPos += lineHeight;

        if (isQuickTapping || quickTapActivatedAnimation)
        {
            GUI.Label(new Rect(10, yPos, 350, 20), $"Quick Tap: {isQuickTapping} | Anim Ativa: {quickTapActivatedAnimation}");
        }
    }
}
