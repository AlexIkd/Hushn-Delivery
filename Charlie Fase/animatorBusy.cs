using UnityEngine;

public class animatorBusy : StateMachineBehaviour
{
    private PlayerMovement_FrontiersStyle playerController;
    private bool rotationCompleted = false;
    
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (playerController == null)
            playerController = animator.GetComponent<PlayerMovement_FrontiersStyle>();
        
        if (playerController != null)
        {
            playerController.animatorBusy = true;
            rotationCompleted = false;
            Debug.Log("Quick Turn: Animação iniciada - Controle bloqueado, iniciando desaceleração parcial");
        }
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (playerController == null) return;
        
        // Aplica rotação progressiva durante a animação
        if (!rotationCompleted)
        {
            float rotationProgress = Mathf.Clamp01(stateInfo.normalizedTime / 0.8f);
            animator.transform.rotation = Quaternion.Slerp(
                animator.transform.rotation, 
                playerController.targetRotation, 
                rotationProgress
            );
            
            // Quando chegou perto do final, completa a rotação
            if (stateInfo.normalizedTime >= 0.8f && !rotationCompleted)
            {
                CompleteRotation(animator);
            }
        }
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (playerController != null)
        {
            // Garante que a rotação seja completada se não foi durante o update
            if (!rotationCompleted)
            {
                CompleteRotation(animator);
            }
            
            playerController.animatorBusy = false;
            Debug.Log("Quick Turn: Animação finalizada - Controle liberado");
        }
    }
    
    private void CompleteRotation(Animator animator)
    {
        rotationCompleted = true;
        playerController.CompleteQuickTurn();
        Debug.Log("Quick Turn: Rotação completada durante animação");
    }
}