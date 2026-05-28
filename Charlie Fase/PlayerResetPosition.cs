using UnityEngine;

public class PlayerResetPosition : MonoBehaviour
{
    private Vector3 startPosition;
    private Quaternion startRotation;
    private CharacterController controller;

    [Header("Configurações")]
    [Tooltip("Tecla para resetar a posição.")]
    public KeyCode resetKey = KeyCode.R;

    void Start()
    {
        // Salva a posição e rotação inicial assim que o jogo começa
        startPosition = transform.position;
        startRotation = transform.rotation;
        
        // Pega a referência do CharacterController
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        // Verifica se a tecla de reset foi pressionada
        if (Input.GetKeyDown(resetKey))
        {
            ResetPlayer();
        }
    }

    public void ResetPlayer()
    {
        // IMPORTANTE: Para teleportar um CharacterController, 
        // precisamos desativá-lo brevemente, mudar a posição e reativar.
        if (controller != null)
        {
            controller.enabled = false;
        }

        transform.position = startPosition;
        transform.rotation = startRotation;

        if (controller != null)
        {
            controller.enabled = true;
        }

        Debug.Log("Jogador resetado para a posição inicial!");
    }
}
