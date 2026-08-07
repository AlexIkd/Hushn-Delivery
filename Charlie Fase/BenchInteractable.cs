using UnityEngine;
using System.Collections;

public class BenchInteractable : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    [SerializeField] private string interactText = "Sentar (E)";
    [SerializeField] private string exitText = "Levantar (E)";
    [SerializeField] private Transform sitPoint; // Onde o jogador vai ficar
    [SerializeField] private string sitAnimationName = "SitIdle";
    [SerializeField] private float transitionSpeed = 5f;
    
    [Header("Position Adjustment (Real-time)")]
    [SerializeField] private Vector3 sitPositionOffset = Vector3.zero; // Ajuste manual de posição (X, Y, Z)
    [SerializeField] private float exitForwardDistance = 1.0f; // Distância à frente do banco ao sair

    private bool isPlayerSitting = false;
    private GameObject sittingPlayer;
    private PlayerMovement_FrontiersStyle playerMovement;
    private CharacterController sittingController;
    
    // Cache dos valores originais da hitbox
    private float originalHeight;
    private Vector3 originalCenter;

    void Update()
    {
        // Ajuste em tempo real se o jogador estiver sentado
        if (isPlayerSitting && sittingPlayer != null)
        {
            Vector3 basePos = sitPoint != null ? sitPoint.position : transform.position;
            Quaternion baseRot = sitPoint != null ? sitPoint.rotation : transform.rotation;

            // Calcula a posição final aplicando o offset relativo à rotação do banco/sitPoint
            // Isso garante que "Z" seja sempre frente/trás do banco, independente da rotação
            Vector3 finalPos = basePos + (baseRot * sitPositionOffset);
            
            // Aplica a posição instantaneamente para permitir o ajuste no Inspector
            sittingPlayer.transform.position = finalPos;
        }
    }

    public string GetInteractText()
    {
        return isPlayerSitting ? exitText : interactText;
    }

    public void Interact(GameObject player)
    {
        Debug.Log($"<color=green>Banco:</color> Interação iniciada por {player.name}. Sentado: {isPlayerSitting}");
        if (!isPlayerSitting)
        {
            StartCoroutine(SitDown(player));
        }
        else
        {
            StartCoroutine(StandUp());
        }
    }

    private IEnumerator SitDown(GameObject player)
    {
        sittingPlayer = player;
        playerMovement = player.GetComponent<PlayerMovement_FrontiersStyle>();
        sittingController = player.GetComponent<CharacterController>();
        
        if (playerMovement != null)
        {
            playerMovement.SetSittingState(true);
        }

        // ✅ HITBOX NORMAL: Mantém o tamanho original, apenas desativa o componente
        // para que ele não tente processar física enquanto o jogador está sentado.
        if (sittingController != null)
        {
            sittingController.enabled = false;
        }

        isPlayerSitting = true;

        // Posiciona o jogador suavemente
        Vector3 basePos = sitPoint != null ? sitPoint.position : transform.position;
        Quaternion targetRot = sitPoint != null ? sitPoint.rotation : transform.rotation;
        Vector3 targetPos = basePos + (targetRot * sitPositionOffset);

        float t = 0;
        Vector3 startPos = player.transform.position;
        Quaternion startRot = player.transform.rotation;

        while (t < 1f)
        {
            t += Time.deltaTime * transitionSpeed;
            // Durante a transição, ainda usamos Lerp
            player.transform.position = Vector3.Lerp(startPos, targetPos, t);
            player.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        // Ativa animação
        Animator anim = player.GetComponent<Animator>();
        if (anim != null)
        {
            anim.SetBool("IsSitting", true);
            anim.CrossFadeInFixedTime(sitAnimationName, 0.2f);
        }
    }

    private IEnumerator StandUp()
    {
        if (sittingPlayer == null) yield break;

        Animator anim = sittingPlayer.GetComponent<Animator>();
        if (anim != null)
        {
            anim.SetBool("IsSitting", false);
        }

        // 1. Reativamos a hitbox original
        if (sittingController != null)
        {
            sittingController.enabled = true;
        }

        // 2. Restauramos o estado de movimento
        if (playerMovement != null)
        {
            playerMovement.SetSittingState(false);
        }

        // 3. Posiciona o jogador à frente do banco para sair com segurança
        // Usamos a direção frontal do banco (transform.forward) para garantir que ele saia para frente do objeto
        Vector3 exitPos = transform.position + transform.forward * exitForwardDistance;
        
        // Mantém a altura atual do jogador para evitar que ele "caia" ou "suba" bruscamente
        exitPos.y = sittingPlayer.transform.position.y;

        // Tenta encontrar o chão na posição de saída para evitar que ele fique flutuando
        RaycastHit hit;
        if (Physics.Raycast(exitPos + Vector3.up, Vector3.down, out hit, 2f))
        {
            exitPos.y = hit.point.y;
        }

        sittingPlayer.transform.position = exitPos;

        isPlayerSitting = false;
        sittingPlayer = null;
        sittingController = null;
        
        yield return null;
    }
}
