using UnityEngine;
using System.Collections;

public class CollectibleItem : MonoBehaviour, IInteractable
{
    [Header("Dados do Item")]
    public ItemData itemData;
    
    [Header("Configurações de Animação")]
    public string collectTrigger = "PickUp"; // Nome do Trigger no Animator do Jogador
    public float animationDuration = 1.5f;   // Tempo que o jogador fica travado na animação
    public float collectDelay = 0.5f;        // Tempo para o item sumir (quando a mão toca o item)

    [Header("Feedback")]
    public ParticleSystem collectEffect;
    public AudioClip collectSFX;

    private bool isCollected = false;

    public string GetInteractText()
    {
        return itemData != null ? "Coletar " + itemData.itemName : "Coletar Item";
    }

    public void Interact(GameObject player)
    {
        if (itemData == null || isCollected) return;
        
        StartCoroutine(CollectRoutine(player));
    }

    private IEnumerator CollectRoutine(GameObject player)
    {
        isCollected = true;

        // 1. Tenta pegar o Animator e o script de movimento
        Animator anim = player.GetComponentInChildren<Animator>();
        var movement = player.GetComponent<PlayerMovement_FrontiersStyle>();

        // 2. Trava o movimento do jogador
        if (movement != null)
        {
            movement.animatorBusy = true;
            // Opcional: Zerar a velocidade para ele não deslizar enquanto pega
            movement.currentSpeed = 0;
            movement.moveDirection = Vector3.zero;
        }

        // 3. Toca a animação
        if (anim != null)
        {
            anim.SetTrigger(collectTrigger);
            // Rotaciona o jogador para olhar para o item (opcional, mas fica melhor)
            Vector3 lookPos = transform.position;
            lookPos.y = player.transform.position.y;
            player.transform.LookAt(lookPos);
        }

        // 4. Espera o tempo do "toque" no item
        yield return new WaitForSeconds(collectDelay);

        // 5. Adiciona ao inventário (gera o pop-up)
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItem(itemData);
        }

        // 6. Feedback visual e sonoro
        if (collectEffect) Instantiate(collectEffect, transform.position, Quaternion.identity);
        if (collectSFX) AudioSource.PlayClipAtPoint(collectSFX, transform.position);

        // 7. Esconde o objeto visualmente (não destrói ainda para não cancelar a Coroutine)
        foreach (var renderer in GetComponentsInChildren<Renderer>()) renderer.enabled = false;
        foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = false;

        // 8. Espera o resto da animação terminar
        yield return new WaitForSeconds(animationDuration - collectDelay);

        // 9. Destrava o movimento
        if (movement != null)
        {
            movement.animatorBusy = false;
        }

        Debug.Log("Item coletado com animação: " + itemData.itemName);

        // 10. Agora sim, destrói o objeto
        Destroy(gameObject);
    }
}
