using UnityEngine;
using UnityEngine.Rendering.Universal; // Necessário para URP
using TMPro;

public class RE7Inspector : MonoBehaviour
{
    public static RE7Inspector Instance;

    [Header("UI do Inspetor")]
    public GameObject inspectorUI; 
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public GameObject[] hudsToHide; // Outras HUDs (mapa, vida, etc) para esconder

    [Header("Configurações 3D")]
    public Vector3 inspectionOffset = new Vector3(0, 0, 0.5f); // X, Y e Z (Z é a distância)
    public float rotationSpeed = 150f; 
    public float rotationSmoothing = 10f; 
    public float zoomSpeed = 5f;
    public Vector2 zoomLimits = new Vector2(0.5f, 2.5f);
    public string inspectionLayer = "UI"; 

    private GameObject currentModel;
    private float currentZoom = 1f;
    public bool IsInspecting { get; private set; }

    // Variáveis para rotação estável
    private float rotationX = 0f;
    private float rotationY = 0f;
    private float targetRotationX = 0f;
    private float targetRotationY = 0f;
    
    private Camera mainCam;
    private Camera overlayCam;

    private void Awake()
    {
        Instance = this;
        mainCam = Camera.main;
        if (inspectorUI) 
        {
            inspectorUI.SetActive(false);
        }

        CreateOverlayCamera();
    }

    private void CreateOverlayCamera()
    {
        GameObject camObj = new GameObject("InspectionOverlayCamera");
        overlayCam = camObj.AddComponent<Camera>();
        camObj.transform.SetParent(mainCam.transform);
        camObj.transform.localPosition = Vector3.zero;
        camObj.transform.localRotation = Quaternion.identity;

        var cameraData = overlayCam.GetUniversalAdditionalCameraData();
        cameraData.renderType = CameraRenderType.Overlay; 
        cameraData.renderPostProcessing = false;
        
        overlayCam.cullingMask = 1 << LayerMask.NameToLayer(inspectionLayer); 
        overlayCam.fieldOfView = 60;
        overlayCam.enabled = false;
        overlayCam.clearFlags = CameraClearFlags.Depth;

        if (mainCam != null)
        {
            var mainCameraData = mainCam.GetUniversalAdditionalCameraData();
            if (mainCameraData.cameraStack.Contains(overlayCam))
            {
                mainCameraData.cameraStack.Remove(overlayCam);
            }
            mainCameraData.cameraStack.Add(overlayCam);
        }
    }

    public void OpenInspector(ItemData data)
    {
        if (data == null || data.modelPrefab == null) return;

        IsInspecting = true;
        
        // Se a tecla 'I' não estiver pressionada agora (aberto via mouse), já pode fechar
        canClose = !Input.GetKey(KeyCode.I); 

        if (inspectorUI != null) inspectorUI.SetActive(true);
        if (overlayCam) overlayCam.enabled = true;

        foreach (GameObject hud in hudsToHide)
        {
            if (hud != null) hud.SetActive(false);
        }

        if (InventoryManager.Instance != null && InventoryManager.Instance.popUpPanel != null)
        {
            InventoryManager.Instance.popUpPanel.SetActive(false);
        }
        
        if (nameText) nameText.text = data.itemName;
        if (descriptionText) descriptionText.text = data.description;

        currentModel = Instantiate(data.modelPrefab, overlayCam.transform);
        currentModel.transform.localPosition = inspectionOffset;
        
        rotationX = 0;
        rotationY = 0;
        targetRotationX = 0;
        targetRotationY = 0;
        currentModel.transform.localRotation = Quaternion.identity;
        
        currentZoom = 1f;
        SetLayerRecursive(currentModel, LayerMask.NameToLayer(inspectionLayer));

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void SetLayerRecursive(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursive(child.gameObject, newLayer);
        }
    }

    public void CloseInspector()
    {
        IsInspecting = false;
        if (inspectorUI) inspectorUI.SetActive(false);
        if (overlayCam) overlayCam.enabled = false;
        if (currentModel) Destroy(currentModel);

        foreach (GameObject hud in hudsToHide)
        {
            if (hud != null) hud.SetActive(true);
        }

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private bool canClose = false;

    void Update()
    {
        if (!IsInspecting) return;

        // Rotação e Zoom devem funcionar SEMPRE que estiver inspecionando
        HandleInput();

        // Lógica de fechamento com trava de segurança
        if (!canClose)
        {
            // Libera o fechamento assim que o jogador soltar a tecla 'I'
            if (!Input.GetKey(KeyCode.I)) canClose = true;
            return; 
        }

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.I))
        {
            CloseInspector();
        }
    }

    private void HandleInput()
    {
        if (currentModel == null) return;

        currentModel.transform.localPosition = inspectionOffset;
        currentModel.transform.localScale = Vector3.one * currentZoom;

        if (Input.GetMouseButton(0))
        {
            targetRotationX -= Input.GetAxis("Mouse X") * rotationSpeed;
            targetRotationY += Input.GetAxis("Mouse Y") * rotationSpeed;
        }

        rotationX = Mathf.Lerp(rotationX, targetRotationX, Time.unscaledDeltaTime * rotationSmoothing);
        rotationY = Mathf.Lerp(rotationY, targetRotationY, Time.unscaledDeltaTime * rotationSmoothing);

        currentModel.transform.localRotation = Quaternion.Euler(rotationY, rotationX, 0);

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            currentZoom = Mathf.Clamp(currentZoom + scroll * zoomSpeed, zoomLimits.x, zoomLimits.y);
        }
    }
}
