using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Alavanca que implementa IInteractable e aciona uma ou mais plataformas.
/// </summary>
public class LeverSwitch : MonoBehaviour, IInteractable
{
    public enum LeverMode
    {
        OneShot,
        Toggle,
        Continuous
    }

    [Header("Plataformas")]
    [Tooltip("Todas as plataformas que esta alavanca deve mover.")]
    [SerializeField] private LeverPlatformMover[] platforms;

    [Header("Comportamento")]
    [SerializeField] private LeverMode mode = LeverMode.OneShot;
    [SerializeField] private bool startActivated = false;
    [SerializeField] private bool blockInteractionWhileMoving = true;

    [Header("Visual da alavanca")]
    [SerializeField] private Transform leverHandle;
    [SerializeField] private Vector3 inactiveLocalEulerAngles = new Vector3(0f, 0f, -35f);
    [SerializeField] private Vector3 activeLocalEulerAngles = new Vector3(0f, 0f, 35f);
    [SerializeField, Min(0.01f)] private float handleRotationDuration = 0.25f;
    [SerializeField] private AnimationCurve handleRotationCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Eventos")]
    [SerializeField] private UnityEvent onActivated;
    [SerializeField] private UnityEvent onDeactivated;

    private bool isActivated;
    private bool isAnimatingHandle;
    private Quaternion inactiveHandleRotation;
    private Quaternion activeHandleRotation;

    public bool IsActivated => isActivated;

    private void Awake()
    {
        isActivated = startActivated;

        if (leverHandle != null)
        {
            inactiveHandleRotation = Quaternion.Euler(inactiveLocalEulerAngles);
            activeHandleRotation = Quaternion.Euler(activeLocalEulerAngles);
            leverHandle.localRotation = isActivated
                ? activeHandleRotation
                : inactiveHandleRotation;
        }
    }

    public void Interact(GameObject player)
    {
        if (blockInteractionWhileMoving &&
            mode != LeverMode.Continuous &&
            ArePlatformsMoving())
            return;

        if (mode == LeverMode.OneShot && isActivated)
            return;

        if (mode == LeverMode.Toggle || mode == LeverMode.Continuous)
        {
            if (isActivated)
                Deactivate();
            else
                Activate();
        }
        else
        {
            Activate();
        }
    }

    public string GetInteractText()
    {
        if (mode == LeverMode.Toggle || mode == LeverMode.Continuous)
            return isActivated ? "Desativar alavanca" : "Acionar alavanca";

        return isActivated ? "Alavanca acionada" : "Acionar alavanca";
    }

    public void Activate()
    {
        if (isActivated && mode == LeverMode.OneShot)
            return;

        isActivated = true;
        if (mode == LeverMode.Continuous)
            StartPlatformsPingPong();
        else
            SetPlatformsToEnd();
        AnimateHandle(true);
        onActivated?.Invoke();
    }

    public void Deactivate()
    {
        if (!isActivated)
            return;

        isActivated = false;
        if (mode == LeverMode.Continuous)
            StopPlatformsPingPongAndReturnToStart();
        else
            SetPlatformsToStart();
        AnimateHandle(false);
        onDeactivated?.Invoke();
    }

    private void SetPlatformsToEnd()
    {
        if (platforms == null)
            return;

        foreach (LeverPlatformMover platform in platforms)
        {
            if (platform != null)
                platform.MoveToEnd();
        }
    }

    private void SetPlatformsToStart()
    {
        if (platforms == null)
            return;

        foreach (LeverPlatformMover platform in platforms)
        {
            if (platform != null)
                platform.MoveToStart();
        }
    }

    private void StartPlatformsPingPong()
    {
        if (platforms == null)
            return;

        foreach (LeverPlatformMover platform in platforms)
        {
            if (platform != null)
                platform.StartPingPong();
        }
    }

    private void StopPlatformsPingPongAndReturnToStart()
    {
        if (platforms == null)
            return;

        foreach (LeverPlatformMover platform in platforms)
        {
            if (platform != null)
                platform.StopPingPongAndReturnToStart();
        }
    }

    private bool ArePlatformsMoving()
    {
        if (platforms == null)
            return false;

        foreach (LeverPlatformMover platform in platforms)
        {
            if (platform != null && platform.IsMoving)
                return true;
        }

        return false;
    }

    private void AnimateHandle(bool activated)
    {
        if (leverHandle == null)
            return;

        if (isAnimatingHandle)
            StopAllCoroutines();

        StartCoroutine(AnimateHandleRoutine(activated));
    }

    private System.Collections.IEnumerator AnimateHandleRoutine(bool activated)
    {
        isAnimatingHandle = true;

        Quaternion from = leverHandle.localRotation;
        Quaternion to = activated ? activeHandleRotation : inactiveHandleRotation;
        float elapsed = 0f;

        while (elapsed < handleRotationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / handleRotationDuration);
            float curvedProgress = handleRotationCurve.Evaluate(progress);
            leverHandle.localRotation = Quaternion.Slerp(from, to, curvedProgress);
            yield return null;
        }

        leverHandle.localRotation = to;
        isAnimatingHandle = false;
    }
}
