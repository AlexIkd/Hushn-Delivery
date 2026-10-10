using TMPro;
using UnityEngine;

/// <summary>
/// Sistema de interação remota com alavancas.
/// Ativa um HUD, usa a mira da câmera e permite acionar uma alavanca à distância.
/// </summary>
public class RemoteLeverInteraction : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private DynamicFollowCamera dynamicFollowCamera;
    [SerializeField] private GameObject remoteHud;
    [SerializeField] private GameObject targetIndicator;
    [SerializeField] private TMP_Text targetText;
    [SerializeField] private bool requireRemoteTargetIcon = true;

    [Header("Teclas")]
    [SerializeField] private KeyCode toggleRemoteModeKey = KeyCode.R;
    [SerializeField] private KeyCode activateLeverKey = KeyCode.E;
    [SerializeField] private bool requireHoldToUse = false;

    [Header("Mira remota")]
    [SerializeField, Min(0.1f)] private float maxRemoteDistance = 25f;
    [SerializeField] private LayerMask raycastLayers = ~0;
    [SerializeField] private QueryTriggerInteraction triggerInteraction =
        QueryTriggerInteraction.Collide;
    [SerializeField] private bool onlyAllowLeverSwitch = true;

    [Header("Assistência de mira")]
    [Tooltip("Raio de encaixe em pixels ao redor do centro da tela.")]
    [SerializeField, Min(1f)] private float targetSnapRadiusPixels = 90f;
    [Tooltip("Se ativado, o ícone também precisa estar visível sem uma parede na frente.")]
    [SerializeField] private bool requireTargetLineOfSight = true;

    [Header("Tempo em câmera lenta")]
    [Tooltip("0.2 = 20% da velocidade normal durante o modo remoto.")]
    [SerializeField, Range(0.05f, 1f)] private float remoteTimeScale = 0.2f;
    [SerializeField] private bool slowTimeWhileRemote = true;

    private LeverSwitch currentTarget;
    private RemoteLeverTargetIcon currentTargetIcon;
    private bool remoteModeActive;
    private bool remoteTimeScaleApplied;
    private float timeScaleBeforeRemote = 1f;
    private int collectedItemCount = -1;

    public bool IsRemoteModeActive => remoteModeActive;
    public LeverSwitch CurrentTarget => currentTarget;

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (dynamicFollowCamera == null)
            dynamicFollowCamera = FindFirstObjectByType<DynamicFollowCamera>();

        if (InventoryManager.Instance != null)
            collectedItemCount = InventoryManager.Instance.collectedItems.Count;

        SetRemoteMode(false);
    }

    private void Update()
    {
        if (NPCDialogueManager.Instance != null &&
            NPCDialogueManager.Instance.IsDialogueActive)
        {
            if (remoteModeActive)
                SetRemoteMode(false);

            return;
        }

        if (!remoteModeActive)
        {
            UpdateCollectedItemBaseline();

            // Não permite nem iniciar o relógio durante slide, warp, dano,
            // morte, QTE, narrow passage ou swing.
            if (dynamicFollowCamera != null &&
                dynamicFollowCamera.IsRemoteInteractionBlocked)
            {
                return;
            }
        }
        else if (IsRemoteCancellationRequested())
        {
            SetRemoteMode(false);
            return;
        }

        if (Input.GetKeyDown(toggleRemoteModeKey))
            SetRemoteMode(!remoteModeActive);

        if (!remoteModeActive)
            return;

        // Mantém todos os ícones de interação visíveis durante o modo relógio.
        // O alvo atualmente selecionado será escondido novamente logo depois
        // que o Raycast confirmar que a retícula está sobre ele.
        RemoteLeverTargetIcon.SetAllVisible(true);
        FindTargetUnderCrosshair();

        bool activatePressed = requireHoldToUse
            ? Input.GetKeyDown(activateLeverKey)
            : Input.GetKeyDown(activateLeverKey);

        if (activatePressed && currentTarget != null)
            ActivateCurrentTarget();

        if (Input.GetKeyDown(KeyCode.Escape))
            SetRemoteMode(false);
    }

    public void SetRemoteMode(bool active)
    {
        remoteModeActive = active;

        if (dynamicFollowCamera != null)
            dynamicFollowCamera.SetRemoteCameraMode(active);

        UpdateRemoteTimeScale(active);

        if (currentTargetIcon != null)
            currentTargetIcon.SetObjectOutlineVisible(false);

        currentTarget = null;
        currentTargetIcon = null;

        if (dynamicFollowCamera != null)
            dynamicFollowCamera.ClearRemoteAimTarget();

        RemoteLeverTargetIcon.SetAllVisible(active);

        if (remoteHud != null)
            remoteHud.SetActive(active);

        if (targetIndicator != null)
            targetIndicator.SetActive(false);

        if (targetText != null)
            targetText.text = string.Empty;
    }

    private void UpdateRemoteTimeScale(bool active)
    {
        if (!slowTimeWhileRemote)
            return;

        if (active && !remoteTimeScaleApplied)
        {
            timeScaleBeforeRemote = Time.timeScale;
            Time.timeScale = Mathf.Clamp(remoteTimeScale, 0.05f, 1f);
            remoteTimeScaleApplied = true;
        }
        else if (!active && remoteTimeScaleApplied)
        {
            Time.timeScale = timeScaleBeforeRemote;
            remoteTimeScaleApplied = false;
        }
    }

    private bool IsRemoteCancellationRequested()
    {
        if (dynamicFollowCamera != null && dynamicFollowCamera.IsRemoteInteractionBlocked)
            return true;

        if (InventoryManager.Instance != null)
        {
            int currentCount = InventoryManager.Instance.collectedItems.Count;
            if (collectedItemCount >= 0 && currentCount > collectedItemCount)
            {
                collectedItemCount = currentCount;
                return true;
            }

            collectedItemCount = currentCount;
        }

        return false;
    }

    private void UpdateCollectedItemBaseline()
    {
        if (InventoryManager.Instance != null)
            collectedItemCount = InventoryManager.Instance.collectedItems.Count;
    }

    /// <summary>
    /// Método público para o sistema de vida ou de coleta fechar o relógio
    /// imediatamente no mesmo frame do evento.
    /// </summary>
    public void DisableRemoteMode()
    {
        if (remoteModeActive)
            SetRemoteMode(false);
    }

    private void OnDisable()
    {
        if (dynamicFollowCamera != null)
        {
            dynamicFollowCamera.SetRemoteCameraMode(false);
            dynamicFollowCamera.ClearRemoteAimTarget();
        }

        if (remoteTimeScaleApplied)
        {
            Time.timeScale = timeScaleBeforeRemote;
            remoteTimeScaleApplied = false;
        }
    }

    public void ActivateCurrentTarget()
    {
        if (currentTarget == null)
            return;

        currentTarget.Interact(gameObject);
        FindTargetUnderCrosshair();
    }

    private void FindTargetUnderCrosshair()
    {
        if (currentTargetIcon != null)
        {
            currentTargetIcon.SetVisible(true);
            currentTargetIcon.SetObjectOutlineVisible(false);
        }

        currentTarget = null;
        currentTargetIcon = null;

        if (dynamicFollowCamera != null)
            dynamicFollowCamera.ClearRemoteAimTarget();

        if (playerCamera == null)
        {
            SetTargetFeedback(false, string.Empty);
            return;
        }

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            maxRemoteDistance,
            raycastLayers,
            triggerInteraction))
        {
            RemoteLeverTargetIcon raycastIcon =
                hit.collider.GetComponentInParent<RemoteLeverTargetIcon>();

            LeverSwitch raycastLever = raycastIcon != null
                ? raycastIcon.Lever
                : hit.collider.GetComponentInParent<LeverSwitch>();

            if (IsValidTarget(raycastIcon, raycastLever))
            {
                SelectTarget(raycastIcon, raycastLever);
                return;
            }
        }

        FindClosestIconToReticle();
    }

    private void FindClosestIconToReticle()
    {
        RemoteLeverTargetIcon bestIcon = null;
        LeverSwitch bestLever = null;
        float bestScreenDistance = targetSnapRadiusPixels;

        Vector2 screenCenter = new Vector2(
            Screen.width * 0.5f,
            Screen.height * 0.5f
        );

        foreach (RemoteLeverTargetIcon icon in RemoteLeverTargetIcon.GetActiveIcons())
        {
            if (icon == null || icon.Lever == null)
                continue;

            LeverSwitch lever = icon.Lever;
            if (!IsValidTarget(icon, lever))
                continue;

            Vector3 screenPoint = playerCamera.WorldToScreenPoint(icon.AimWorldPosition);
            if (screenPoint.z <= 0f)
                continue;

            float screenDistance = Vector2.Distance(
                screenCenter,
                new Vector2(screenPoint.x, screenPoint.y)
            );

            if (screenDistance > bestScreenDistance)
                continue;

            if (requireTargetLineOfSight && !HasLineOfSight(icon.AimWorldPosition, icon))
                continue;

            bestScreenDistance = screenDistance;
            bestIcon = icon;
            bestLever = lever;
        }

        if (bestIcon != null)
            SelectTarget(bestIcon, bestLever);
        else
            SetTargetFeedback(false, string.Empty);
    }

    private bool IsValidTarget(RemoteLeverTargetIcon icon, LeverSwitch lever)
    {
        if (requireRemoteTargetIcon && icon == null)
            return false;

        if (onlyAllowLeverSwitch && lever == null)
            return false;

        return lever != null;
    }

    private bool HasLineOfSight(Vector3 targetPosition, RemoteLeverTargetIcon expectedIcon)
    {
        Vector3 origin = playerCamera.transform.position;
        Vector3 direction = targetPosition - origin;
        float distance = direction.magnitude;

        if (distance > maxRemoteDistance)
            return false;

        if (!Physics.Raycast(origin, direction.normalized, out RaycastHit hit,
            distance + 0.05f, raycastLayers, triggerInteraction))
        {
            return true;
        }

        return hit.collider.GetComponentInParent<RemoteLeverTargetIcon>() == expectedIcon;
    }

    private void SelectTarget(RemoteLeverTargetIcon targetIcon, LeverSwitch lever)
    {
        currentTarget = lever;
        currentTargetIcon = targetIcon;

        if (currentTargetIcon != null)
        {
            currentTargetIcon.SetVisible(false);
            currentTargetIcon.SetObjectOutlineVisible(true);
        }

        if (dynamicFollowCamera != null)
            dynamicFollowCamera.SetRemoteAimTarget(currentTargetIcon != null
                ? currentTargetIcon.AimWorldPosition
                : lever.transform.position);

        SetTargetFeedback(true, lever.GetInteractText());
    }

    private void SetTargetFeedback(bool valid, string message)
    {
        if (targetIndicator != null)
            targetIndicator.SetActive(remoteModeActive && valid);

        if ((!valid || !remoteModeActive) && dynamicFollowCamera != null)
            dynamicFollowCamera.ClearRemoteAimTarget();

        if (targetText != null)
            targetText.text = remoteModeActive && valid
                ? message + " [" + activateLeverKey + "]"
                : string.Empty;
    }

    private void OnDrawGizmosSelected()
    {
        if (playerCamera == null)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(
            playerCamera.transform.position,
            playerCamera.transform.forward * maxRemoteDistance
        );
    }
}
