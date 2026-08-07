using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class InventoryMenu : MonoBehaviour
{
    public static InventoryMenu Instance;

    [Header("Configuração de UI")]
    public GameObject menuPanel;
    public Transform contentGrid; 
    public GameObject itemButtonPrefab; 

    [Header("Teclas")]
    public KeyCode toggleKey = KeyCode.Tab; 

    private bool isMenuOpen = false;
    private List<GameObject> spawnedButtons = new List<GameObject>();

    private void Awake()
    {
        Instance = this;
        if (menuPanel) menuPanel.SetActive(false);
    }

    private void Update()
    {
        // Se o visualizador 3D estiver aberto, o InventoryMenu não processa teclas
        if (RE7Inspector.Instance != null && RE7Inspector.Instance.IsInspecting) return;

        // Abre/Fecha com Tab
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleMenu();
        }
        
        // Fecha com Esc se o menu estiver aberto
        if (isMenuOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseMenu();
        }
    }

    public void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;
        
        if (isMenuOpen) OpenMenu();
        else CloseMenu();
    }

    private void OpenMenu()
    {
        menuPanel.SetActive(true);
        RefreshList();

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseMenu()
    {
        isMenuOpen = false;
        if (menuPanel) menuPanel.SetActive(false);

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void RefreshList()
    {
        foreach (GameObject btn in spawnedButtons) Destroy(btn);
        spawnedButtons.Clear();

        if (InventoryManager.Instance == null) return;

        foreach (ItemData item in InventoryManager.Instance.collectedItems)
        {
            GameObject newBtn = Instantiate(itemButtonPrefab, contentGrid);
            spawnedButtons.Add(newBtn);

            var buttonScript = newBtn.GetComponent<InventoryButton>();
            if (buttonScript != null) buttonScript.Setup(item);
            
            Button btnComp = newBtn.GetComponent<Button>();
            btnComp.onClick.AddListener(() => {
                OnItemClicked(item);
            });
        }
    }

    private void OnItemClicked(ItemData item)
    {
        // Fecha o menu de lista
        isMenuOpen = false;
        if (menuPanel) menuPanel.SetActive(false);
        
        // Abre a inspeção 3D diretamente
        if (RE7Inspector.Instance != null)
        {
            RE7Inspector.Instance.OpenInspector(item);
        }
    }
}
