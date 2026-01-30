using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class SimpleHeightHUD : MonoBehaviour
{
    [Header("Configurações do HUD")]
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.7f);
    [Range(10, 40)] [SerializeField] private int fontSize = 20;
    [SerializeField] private float verticalOffset = 0.5f;

    [Header("Informações (Leitura)")]
    public float objectHeight;

    private void Update()
    {
        // Calcula a altura baseada no Renderer ou Collider do objeto
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            objectHeight = renderer.bounds.size.y;
        }
        else
        {
            Collider collider = GetComponent<Collider>();
            if (collider != null)
            {
                objectHeight = collider.bounds.size.y;
            }
            else
            {
                // Se não tiver nada, usa a escala local Y como fallback
                objectHeight = transform.localScale.y;
            }
        }
    }

    #if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // 1. Configura o estilo do HUD
        GUIStyle style = new GUIStyle();
        style.normal.textColor = textColor;
        style.fontSize = fontSize;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;

        // Criar um fundo sólido
        Texture2D bgTex = new Texture2D(1, 1);
        bgTex.SetPixel(0, 0, backgroundColor);
        bgTex.Apply();
        style.normal.background = bgTex;

        // 2. Define a posição do HUD (Topo do objeto)
        Vector3 topPosition = transform.position;
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            topPosition = new Vector3(transform.position.x, renderer.bounds.max.y, transform.position.z);
        }

        // 3. Desenha a etiqueta na Scene View
        string text = $" ALTURA: {objectHeight:F2}m ";
        Handles.Label(topPosition + Vector3.up * verticalOffset, text, style);

        // 4. Desenha uma linha vertical de auxílio
        Handles.color = backgroundColor;
        Vector3 basePosition = new Vector3(topPosition.x, topPosition.y - objectHeight, topPosition.z);
        Handles.DrawLine(basePosition, topPosition);
        
        // Força a atualização visual
        if (!Application.isPlaying)
        {
            SceneView.RepaintAll();
        }
    }
    #endif
}
