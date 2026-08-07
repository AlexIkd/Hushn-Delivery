using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

public class WarpSystem : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private RectTransform warpIcon;
    [SerializeField] private Animator animator;
    private CharacterController characterController;
    private MonoBehaviour movementScript;
    private PlayerRailRide_SonicStyle_Spline grindScript;

    [Header("Configurações de Detecção")]
    [SerializeField] private float maxWarpDistance = 50f;
    [SerializeField] private float detectionRadius = 100f;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Configurações de Warp")]
    [SerializeField] private float warpCooldown = 1.5f;
    [SerializeField] private float preparationTime = 0.3f;
    [SerializeField] private float warpLerpSpeed = 20f; 
    [SerializeField] private float warpVerticalOffset = 0.1f;

    [Header("Visual & Espada")]
    public Transform sword;
    public Transform swordHand;
    public GameObject hitParticle;
    private Vector3 swordOrigRot;
    private Vector3 swordOrigPos;
    private MeshRenderer swordMesh;
    private SkinnedMeshRenderer[] playerRenderers;

    [Header("Efeito de Rastro (Anime Speed Lines)")]
    [SerializeField] private PlayerAnimeSpeedLines animeSpeedLines;

    [Header("Partículas")]
    public ParticleSystem blueTrail;
    public ParticleSystem whiteTrail;
    public ParticleSystem swordParticle;

    [Header("Câmera")]
    [SerializeField] private bool followWarpEffect = true;
    private Transform cameraTarget;

    [Header("Input")]
    [SerializeField] private KeyCode warpKey = KeyCode.F;

    // Estado Interno
    private float lastWarpTime = -10f;
    private WarpPoint currentTarget;
    private bool isWarping = false;
    private bool isHanging = false;
    private bool isPreparing = false;
    private Vector3 lockedPosition;

    // PROPRIEDADES PÚBLICAS
    public bool IsPreparing => isPreparing;
    public bool IsHanging => isHanging;
    public bool IsWarping => isWarping;

    private static readonly int HashWarpStart = Animator.StringToHash("WarpStart");
    private static readonly int HashIsHanging = Animator.StringToHash("isHanging");

    private static readonly List<WarpPoint> allWarpPoints = new List<WarpPoint>();
    public static void RegisterPoint(WarpPoint point) => allWarpPoints.Add(point);
    public static void UnregisterPoint(WarpPoint point) => allWarpPoints.Remove(point);

    private void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (playerTransform == null) playerTransform = transform;
        
        characterController = playerTransform.GetComponent<CharacterController>();
        movementScript = playerTransform.GetComponent("PlayerMovement_FrontiersStyle") as MonoBehaviour;
        grindScript = playerTransform.GetComponent<PlayerRailRide_SonicStyle_Spline>();
        if (animator == null) animator = playerTransform.GetComponent<Animator>();

        playerRenderers = playerTransform.GetComponentsInChildren<SkinnedMeshRenderer>();

        if (sword != null)
        {
            swordOrigRot = sword.localEulerAngles;
            swordOrigPos = sword.localPosition;
            swordMesh = sword.GetComponentInChildren<MeshRenderer>();
            if (swordMesh != null) swordMesh.enabled = false;
        }

        GameObject targetObj = new GameObject("WarpCameraTarget");
        cameraTarget = targetObj.transform;

        if (animeSpeedLines == null)
        {
            animeSpeedLines = GetComponent<PlayerAnimeSpeedLines>();
        }
    }

    private void Update()
    {
        if (isPreparing || isHanging || isWarping)
        {
            // REFORÇO: Desativa a física, o movimento e o grind durante QUALQUER estado de warp
            if (characterController != null && characterController.enabled) characterController.enabled = false;
            if (movementScript != null && movementScript.enabled) movementScript.enabled = false;
            if (grindScript != null && grindScript.enabled) grindScript.enabled = false;

            // Trava a posição explicitamente para evitar drift ou queda
            if (isPreparing)
            {
                playerTransform.position = lockedPosition;
            }
            // CORREÇÃO: Agora usa GetWarpPosition() para respeitar o offset
            else if (isHanging && currentTarget != null)
            {
                playerTransform.position = currentTarget.GetWarpPosition() + Vector3.up * warpVerticalOffset;
            }

            if (warpIcon != null && warpIcon.gameObject.activeSelf) warpIcon.gameObject.SetActive(false);
            if (isHanging || isWarping) CheckForRelease();
            
            return;
        }

        // Reativa apenas quando não estiver em nenhum estado de warp
        if (characterController != null && !characterController.enabled) characterController.enabled = true;
        if (movementScript != null && !movementScript.enabled) movementScript.enabled = true;
        if (grindScript != null && !grindScript.enabled) grindScript.enabled = true;

        FindBestWarpPoint();
        HandleInput();
        UpdateUI();
    }

    private void LateUpdate()
    {
        if (isPreparing && currentTarget != null)
        {
            Vector3 dir = currentTarget.transform.position - playerTransform.position;
            dir.y = 0;
            if (dir.sqrMagnitude > 0.001f) playerTransform.rotation = Quaternion.LookRotation(dir);
            
            // Garante a posição travada também no LateUpdate para evitar jitter
            playerTransform.position = lockedPosition;
        }

        if (isWarping && followWarpEffect && sword != null)
        {
            cameraTarget.position = sword.position;
        }
    }

    private void ResetMovementVelocity()
    {
        if (movementScript == null) return;

        try 
        {
            FieldInfo moveDirField = movementScript.GetType().GetField("moveDirection", BindingFlags.NonPublic | BindingFlags.Instance);
            if (moveDirField != null)
            {
                moveDirField.SetValue(movementScript, Vector3.zero);
            }

            FieldInfo extVelField = movementScript.GetType().GetField("externalVelocity", BindingFlags.NonPublic | BindingFlags.Instance);
            if (extVelField != null)
            {
                extVelField.SetValue(movementScript, Vector3.zero);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Não foi possível zerar moveDirection via Reflection: " + e.Message);
        }
    }

    private void FindBestWarpPoint()
    {
        WarpPoint bestPoint = null;
        float closestToCenterSqr = detectionRadius * detectionRadius;
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        for (int i = 0; i < allWarpPoints.Count; i++)
        {
            WarpPoint point = allWarpPoints[i];
            if (point == null || !point.isAvailable) continue;
            if ((playerTransform.position - point.transform.position).sqrMagnitude > maxWarpDistance * maxWarpDistance) continue;

            Vector3 screenPos = mainCamera.WorldToScreenPoint(point.transform.position);
            if (screenPos.z <= 0) continue;

            Vector3 origin = mainCamera.transform.position;
            Vector3 direction = point.transform.position - origin;
            if (Physics.Raycast(origin, direction, out RaycastHit hit, direction.magnitude, obstacleLayer))
            {
                if (hit.transform != point.transform && !hit.transform.IsChildOf(point.transform)) continue;
            }

            float distFromCenterSqr = (new Vector2(screenPos.x, screenPos.y) - screenCenter).sqrMagnitude;
            if (distFromCenterSqr < closestToCenterSqr) { closestToCenterSqr = distFromCenterSqr; bestPoint = point; }
        }
        currentTarget = bestPoint;
    }

    private void UpdateUI()
    {
        if (warpIcon == null) return;
        
        // Bloqueia o warp se estiver fazendo wallrun
        bool isWallRunning = IsPlayerWallRunning();
        
        bool shouldShow = currentTarget != null && Time.time >= lastWarpTime + warpCooldown && !isWallRunning;
        warpIcon.gameObject.SetActive(shouldShow);
        if (shouldShow) warpIcon.position = mainCamera.WorldToScreenPoint(currentTarget.transform.position);
    }

    private void HandleInput()
    {
        // Não permite iniciar o warp se estiver fazendo wallrun
        if (IsPlayerWallRunning()) return;

        if ((Input.GetKeyDown(warpKey) || Input.GetMouseButtonDown(0)) && currentTarget != null && Time.time >= lastWarpTime + warpCooldown)
        {
            StartCoroutine(WarpSequence());
        }
    }

    private bool IsPlayerWallRunning()
    {
        if (movementScript == null) return false;
        
        try 
        {
            // Tenta obter a propriedade IsWallRunning do script de movimento via Reflection
            // para manter a consistência com o resto do código que usa Reflection.
            PropertyInfo wallRunProp = movementScript.GetType().GetProperty("IsWallRunning");
            if (wallRunProp != null)
            {
                return (bool)wallRunProp.GetValue(movementScript);
            }
        }
        catch 
        {
            // Caso falhe, assume que não está em wallrun
        }
        return false;
    }

    private IEnumerator WarpSequence()
    {
        isPreparing = true;
        lockedPosition = playerTransform.position;
        lastWarpTime = Time.time;
        
        // Desativa imediatamente para evitar queda no primeiro frame
        if (characterController != null) characterController.enabled = false;
        if (movementScript != null) movementScript.enabled = false;

        // Garante a saída do estado de grind se o jogador estiver nele
        if (grindScript != null)
        {
            if (grindScript.isGrinding)
            {
                // CORREÇÃO: Usando Reflection para chamar ExitRail que está inacessível
                try
                {
                    MethodInfo exitRailMethod = grindScript.GetType().GetMethod("ExitRail", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (exitRailMethod != null)
                    {
                        exitRailMethod.Invoke(grindScript, new object[] { false });
                    }
                    else
                    {
                        Debug.LogWarning("Método ExitRail não encontrado em PlayerRailRide_SonicStyle_Spline");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("Erro ao chamar ExitRail via Reflection: " + e.Message);
                }
            }
            grindScript.enabled = false; // Desativa o script para evitar reentrada automática
        }

        ResetMovementVelocity();

        if (swordParticle != null) swordParticle.Play();
        if (swordMesh != null) swordMesh.enabled = true;
        if (animator != null) animator.SetTrigger(HashWarpStart);

        if (animeSpeedLines != null && currentTarget != null)
        {
            Vector3 direction = (currentTarget.transform.position - playerTransform.position).normalized;
            animeSpeedLines.EnableEffect(direction);
        }

        yield return new WaitForSeconds(preparationTime);

        isPreparing = false;
        isWarping = true;
        
        // NOVO: Torna o jogador invisível no início da transição
        SetPlayerVisibility(false);
        
        if (blueTrail != null) blueTrail.Play();
        if (whiteTrail != null) whiteTrail.Play();

        Vector3 targetPos = currentTarget.GetWarpPosition() + Vector3.up * warpVerticalOffset;
        Quaternion targetRot = currentTarget.transform.rotation;

        if (sword != null) StartCoroutine(MoveSwordRoutine(targetPos));

        float t = 0;
        Vector3 startPos = playerTransform.position;

        while (t < 1.0f && isWarping)
        {
            t += Time.deltaTime * warpLerpSpeed;
            playerTransform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        if (isWarping) 
        {
            isWarping = false;
            
            if (animeSpeedLines != null)
            {
                animeSpeedLines.DisableEffect();
            }

            isHanging = true;
            playerTransform.SetPositionAndRotation(targetPos, targetRot);
            
            // NOVO: Torna o jogador visível ao chegar no destino
            SetPlayerVisibility(true);

            if (animator != null) animator.SetBool(HashIsHanging, true);
            if (hitParticle != null) Instantiate(hitParticle, targetPos, Quaternion.identity);
        }
    }

    private IEnumerator MoveSwordRoutine(Vector3 targetPos)
    {
        sword.parent = null;
        float elapsed = 0;
        float duration = 1f / warpLerpSpeed;
        Vector3 startPos = sword.position;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            sword.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
            sword.LookAt(targetPos);
            yield return null;
        }
        sword.position = targetPos;
        
        while (isWarping) yield return null;
        
        sword.parent = swordHand;
        sword.localPosition = swordOrigPos;
        sword.localEulerAngles = swordOrigRot;
    }

    private void SetPlayerVisibility(bool visible)
    {
        if (playerRenderers == null) return;
        foreach (var renderer in playerRenderers)
        {
            renderer.enabled = visible;
        }
    }

    private void ReloadAirAbilities()
    {
        if (movementScript == null) return;

        try
        {
            // Recarrega o air dash
            FieldInfo airDashChargesField = movementScript.GetType().GetField("airDashCharges", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo maxAirDashChargesField = movementScript.GetType().GetField("maxAirDashCharges", BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (maxAirDashChargesField != null)
            {
                int maxCharges = (int)maxAirDashChargesField.GetValue(movementScript);
                if (airDashChargesField != null)
                {
                    airDashChargesField.SetValue(movementScript, maxCharges);
                    Debug.Log($"✅ Air Dash recarregado! Cargas: {maxCharges}");
                }
            }

            // Recarrega o pulo duplo
            FieldInfo doubleJumpChargesField = movementScript.GetType().GetField("doubleJumpCharges", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo maxDoubleJumpChargesField = movementScript.GetType().GetField("maxDoubleJumpCharges", BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (maxDoubleJumpChargesField != null)
            {
                int maxCharges = (int)maxDoubleJumpChargesField.GetValue(movementScript);
                if (doubleJumpChargesField != null)
                {
                    doubleJumpChargesField.SetValue(movementScript, maxCharges);
                    Debug.Log($"✅ Pulo Duplo recarregado! Cargas: {maxCharges}");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Não foi possível recarregar as habilidades aéreas via Reflection: " + e.Message);
        }
    }

    private void CheckForRelease()
    {
        if (Input.GetButtonDown("Jump") || Input.GetKeyDown(warpKey) || (isHanging && Input.GetMouseButtonDown(0)))
        {
            ReleasePlayer();
        }
    }

    public void ReleasePlayer()
    {
        isPreparing = false;
        isWarping = false;
        isHanging = false;
        
        if (animeSpeedLines != null)
        {
            animeSpeedLines.DisableEffect();
        }
        
        // NOVO: Garante que o jogador fica visível ao ser liberado
        SetPlayerVisibility(true);
        ResetMovementVelocity();
        
        // NOVO: Recarrega air dash e pulo duplo quando o warp termina
        ReloadAirAbilities();

        if (characterController != null) characterController.enabled = true;
        if (movementScript != null) movementScript.enabled = true;

        if (blueTrail != null) blueTrail.Stop();
        if (whiteTrail != null) whiteTrail.Stop();
        if (swordMesh != null) swordMesh.enabled = false;
        if (animator != null) animator.SetBool(HashIsHanging, false);
    }
}