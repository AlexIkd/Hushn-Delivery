using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("Dados")]
    public List<ItemData> collectedItems = new List<ItemData>();
    private ItemData lastCollectedItem;
    private bool isPopUpActive = false;

    [Header("Pop-up de Coleta")]
    public GameObject popUpPanel;
    public TextMeshProUGUI popUpNameText;
    public Image popUpIcon;
    public float popUpDuration = 3f;

    [Header("Teclas")]
    public KeyCode inspectKey = KeyCode.I;

    private void Awake()
    {
        Instance = this;
        if (popUpPanel) popUpPanel.SetActive(false);
    }

    private void Update()
    {
        // Só permite abrir o inspetor se o pop-up estiver visível na tela
        if (isPopUpActive && Input.GetKeyDown(inspectKey))
        {
            Debug.Log("[InventoryManager] Tecla I pressionada com Pop-up ATIVO.");

            if (RE7Inspector.Instance != null)
            {
                Debug.Log("[InventoryManager] Chamando RE7Inspector.Instance.OpenInspector...");
                RE7Inspector.Instance.OpenInspector(lastCollectedItem);
                
                // Força o fechamento do pop-up e desativa a flag ao abrir o inspetor
                ClosePopUp();
            }
            else
            {
                Debug.LogError("[InventoryManager] ERRO: RE7Inspector.Instance não foi encontrado na cena!");
            }
        }
    }

    public void AddItem(ItemData item)
    {
        if (item == null) return;
        
        Debug.Log("[InventoryManager] Item coletado: " + item.itemName);
        collectedItems.Add(item);
        lastCollectedItem = item;

        StopAllCoroutines();
        StartCoroutine(ShowPopUpRoutine(item));
    }

    private IEnumerator ShowPopUpRoutine(ItemData item)
    {
        if (popUpPanel == null) yield break;

        popUpNameText.text = item.itemName;
        popUpIcon.sprite = item.icon;
        
        popUpPanel.SetActive(true);
        isPopUpActive = true;
        Debug.Log("[InventoryManager] Pop-up ativado. Janela de inspeção disponível.");

        yield return new WaitForSeconds(popUpDuration);
        
        // Se o jogador não abriu a inspeção durante o tempo, fecha o pop-up
        if (isPopUpActive)
        {
            Debug.Log("[InventoryManager] Tempo esgotado. Pop-up fechado.");
            ClosePopUp();
        }
    }

    private void ClosePopUp()
    {
        if (popUpPanel != null) popUpPanel.SetActive(false);
        isPopUpActive = false;
    }
}
