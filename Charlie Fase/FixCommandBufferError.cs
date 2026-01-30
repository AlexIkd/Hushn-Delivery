using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Script que resolve o erro: "CommandBuffer: built-in render texture type 3 not found while executing (SetRenderTarget depth buffer)"
/// 
/// COM TOGGLE DE DEBUG - Pressione F3 para ativar/desativar logs
/// 
/// Adicione este script à câmera principal do seu jogo.
/// 
/// ATALHOS:
/// - F3: Ativa/desativa debug do FixCommandBufferError
/// - F4: Mostra/oculta painel de status
/// </summary>
public class FixCommandBufferError_ComToggle : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool showDebugLogs = false;
    [SerializeField] private KeyCode toggleDebugKey = KeyCode.F3;
    [SerializeField] private KeyCode toggleStatusPanelKey = KeyCode.F4;

    private Camera targetCamera;
    private bool isFixed = false;
    private bool showStatusPanel = false;
    private float lastToggleTime = 0f;
    private float toggleCooldown = 0.2f;

    private void OnEnable()
    {
        if (isFixed) return;

        targetCamera = GetComponent<Camera>();

        if (targetCamera == null)
        {
            LogError("Camera não encontrada no GameObject!");
            return;
        }

        // ✅ SOLUÇÃO 1: Habilitar Depth Texture no Built-in Pipeline
        EnableDepthTextureBuiltIn();

        // ✅ SOLUÇÃO 2: Habilitar Depth Texture no URP (se aplicável)
        EnableDepthTextureURP();

        // ✅ SOLUÇÃO 3: Habilitar Opaque Texture (se necessário)
        EnableOpaqueTextureURP();

        isFixed = true;

        Log("✅ CommandBuffer Error Fix aplicado com sucesso!");
    }

    private void Update()
    {
        // ✅ Toggle de debug com cooldown
        if (Input.GetKeyDown(toggleDebugKey) && Time.realtimeSinceStartup - lastToggleTime > toggleCooldown)
        {
            showDebugLogs = !showDebugLogs;
            lastToggleTime = Time.realtimeSinceStartup;
            Debug.Log($"🔧 FixCommandBufferError Debug: {(showDebugLogs ? "✅ ATIVADO" : "❌ DESATIVADO")}");
        }

        // ✅ Toggle de painel de status
        if (Input.GetKeyDown(toggleStatusPanelKey) && Time.realtimeSinceStartup - lastToggleTime > toggleCooldown)
        {
            showStatusPanel = !showStatusPanel;
            lastToggleTime = Time.realtimeSinceStartup;
        }
    }

    private void OnGUI()
    {
        if (!showStatusPanel) return;

        // ✅ Painel de Status
        GUILayout.BeginArea(new Rect(10, 10, 400, 200), GUI.skin.box);

        GUILayout.Label("🔧 FixCommandBufferError Status", new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold
        });

        GUILayout.Space(10);

        // Status
        string statusText = isFixed ? "✅ FIX APLICADO" : "❌ NÃO APLICADO";
        GUI.color = isFixed ? Color.green : Color.red;
        GUILayout.Label(statusText, new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold
        });
        GUI.color = Color.white;

        GUILayout.Space(5);

        // Debug Status
        string debugText = showDebugLogs ? "✅ DEBUG ATIVADO" : "❌ DEBUG DESATIVADO";
        GUI.color = showDebugLogs ? Color.green : Color.red;
        GUILayout.Label(debugText, new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold
        });
        GUI.color = Color.white;

        GUILayout.Space(10);

        // Informações
        if (targetCamera != null)
        {
            GUILayout.Label($"Camera: {targetCamera.name}");
            GUILayout.Label($"Depth Texture Mode: {targetCamera.depthTextureMode}");
        }

        GUILayout.Space(10);

        // Botões
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Toggle Debug (F3)", GUILayout.Height(25)))
        {
            showDebugLogs = !showDebugLogs;
            Debug.Log($"🔧 Debug: {(showDebugLogs ? "✅ ON" : "❌ OFF")}");
        }

        if (GUILayout.Button("Fechar (F4)", GUILayout.Height(25)))
        {
            showStatusPanel = false;
        }

        GUILayout.EndHorizontal();

        GUILayout.EndArea();

        // ✅ Atalhos na tela
        GUILayout.BeginArea(new Rect(10, Screen.height - 40, 400, 35));
        GUILayout.Label("F3: Toggle Debug | F4: Toggle Panel", new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            fontStyle = FontStyle.Bold
        });
        GUILayout.EndArea();
    }

    /// <summary>
    /// Habilita Depth Texture para Built-in Render Pipeline
    /// </summary>
    private void EnableDepthTextureBuiltIn()
    {
        try
        {
            targetCamera.depthTextureMode |= DepthTextureMode.Depth;

            if (targetCamera.depthTextureMode != DepthTextureMode.None)
            {
                Log("✅ Depth Texture habilitada no Built-in Pipeline");
            }
        }
        catch (System.Exception e)
        {
            LogWarning($"Erro ao habilitar Depth Texture Built-in: {e.Message}");
        }
    }

    /// <summary>
    /// Habilita Depth Texture para URP (Universal Render Pipeline)
    /// </summary>
    private void EnableDepthTextureURP()
    {
        try
        {
            var uniData = targetCamera.GetComponent("UniversalAdditionalCameraData");

            if (uniData != null)
            {
                var dataType = uniData.GetType();

                // ✅ Habilita requiresDepthOption
                var requiresDepthProperty = dataType.GetProperty("requiresDepthOption");
                if (requiresDepthProperty != null)
                {
                    requiresDepthProperty.SetValue(uniData, 1);
                    Log("✅ Depth Texture habilitada no URP (requiresDepthOption = On)");
                }

                // ✅ Habilita requiresColorOption
                var requiresColorProperty = dataType.GetProperty("requiresColorOption");
                if (requiresColorProperty != null)
                {
                    requiresColorProperty.SetValue(uniData, 0);
                    Log("✅ Color Texture habilitada no URP (requiresColorOption = UsePipelineSettings)");
                }
            }
            else
            {
                Log("ℹ️ UniversalAdditionalCameraData não encontrado (não está usando URP)");
            }
        }
        catch (System.Exception e)
        {
            LogWarning($"Erro ao habilitar Depth Texture URP: {e.Message}");
        }
    }

    /// <summary>
    /// Habilita Opaque Texture para URP (necessário para alguns efeitos)
    /// </summary>
    private void EnableOpaqueTextureURP()
    {
        try
        {
            var uniData = targetCamera.GetComponent("UniversalAdditionalCameraData");

            if (uniData != null)
            {
                var dataType = uniData.GetType();

                // ✅ Habilita requiresOpaqueOption
                var requiresOpaqueProperty = dataType.GetProperty("requiresOpaqueOption");
                if (requiresOpaqueProperty != null)
                {
                    requiresOpaqueProperty.SetValue(uniData, 1);
                    Log("✅ Opaque Texture habilitada no URP");
                }
            }
        }
        catch (System.Exception e)
        {
            LogWarning($"Erro ao habilitar Opaque Texture URP: {e.Message}");
        }
    }

    /// <summary>
    /// Log com verificação de debug
    /// </summary>
    private void Log(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[FixCommandBufferError] {message}");
        }
    }

    /// <summary>
    /// Warning com verificação de debug
    /// </summary>
    private void LogWarning(string message)
    {
        if (showDebugLogs)
        {
            Debug.LogWarning($"[FixCommandBufferError] {message}");
        }
    }

    /// <summary>
    /// Error (sempre mostra)
    /// </summary>
    private void LogError(string message)
    {
        Debug.LogError($"[FixCommandBufferError] {message}");
    }
}
