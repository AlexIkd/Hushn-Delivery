using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Contorno simples por "inverted hull": duplica os meshes do objeto,
/// aumenta levemente a escala e renderiza uma casca branca por trás.
/// </summary>
public class RemoteLeverOutline : MonoBehaviour
{
    [SerializeField, Min(1.001f)] private float outlineScale = 1.035f;
    [SerializeField] private Color outlineColor = Color.white;

    private readonly List<GameObject> outlineObjects = new List<GameObject>();
    private Material outlineMaterial;
    private bool initialized;

    public void Initialize(Transform sourceRoot)
    {
        if (initialized || sourceRoot == null)
            return;

        initialized = true;
        outlineMaterial = CreateOutlineMaterial();
        if (outlineMaterial == null)
            return;

        MeshFilter[] meshFilters = sourceRoot.GetComponentsInChildren<MeshFilter>(true);
        foreach (MeshFilter sourceFilter in meshFilters)
        {
            if (sourceFilter == null || sourceFilter.sharedMesh == null)
                continue;

            MeshRenderer sourceRenderer = sourceFilter.GetComponent<MeshRenderer>();
            if (sourceRenderer == null)
                continue;

            GameObject outlineObject = new GameObject("RemoteLeverOutline_Mesh");
            // O outline precisa ser filho do mesmo Transform que anima o mesh.
            // Assim ele acompanha a alavanca quando ela gira, se move ou muda
            // de escala durante a interação.
            outlineObject.transform.SetParent(sourceFilter.transform, false);
            outlineObject.transform.localPosition = Vector3.zero;
            outlineObject.transform.localRotation = Quaternion.identity;
            outlineObject.transform.localScale = Vector3.one * outlineScale;
            outlineObject.layer = sourceFilter.gameObject.layer;

            MeshFilter outlineFilter = outlineObject.AddComponent<MeshFilter>();
            outlineFilter.sharedMesh = sourceFilter.sharedMesh;

            MeshRenderer outlineRenderer = outlineObject.AddComponent<MeshRenderer>();
            outlineRenderer.sharedMaterial = outlineMaterial;
            outlineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            outlineRenderer.receiveShadows = false;
            outlineRenderer.enabled = true;

            outlineObjects.Add(outlineObject);
        }

        SkinnedMeshRenderer[] skinnedRenderers =
            sourceRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);

        foreach (SkinnedMeshRenderer sourceRenderer in skinnedRenderers)
        {
            if (sourceRenderer == null || sourceRenderer.sharedMesh == null)
                continue;

            GameObject outlineObject = new GameObject("RemoteLeverOutline_SkinnedMesh");
            outlineObject.transform.SetParent(sourceRenderer.transform, false);
            outlineObject.transform.localPosition = Vector3.zero;
            outlineObject.transform.localRotation = Quaternion.identity;
            outlineObject.transform.localScale = Vector3.one * outlineScale;
            outlineObject.layer = sourceRenderer.gameObject.layer;

            SkinnedMeshRenderer outlineRenderer = outlineObject.AddComponent<SkinnedMeshRenderer>();
            outlineRenderer.sharedMesh = sourceRenderer.sharedMesh;
            outlineRenderer.rootBone = sourceRenderer.rootBone;
            outlineRenderer.bones = sourceRenderer.bones;
            outlineRenderer.sharedMaterial = outlineMaterial;
            outlineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            outlineRenderer.receiveShadows = false;
            outlineRenderer.enabled = true;

            outlineObjects.Add(outlineObject);
        }
    }

    public void SetVisible(bool visible)
    {
        foreach (GameObject outlineObject in outlineObjects)
        {
            if (outlineObject != null)
                outlineObject.SetActive(visible);
        }
    }

    private void LateUpdate()
    {
        if (outlineMaterial == null)
            return;

        // Permite alterar cor e parâmetros do outline durante o Play Mode
        // sem precisar recriar os objetos gerados.
        outlineMaterial.color = outlineColor;
        if (outlineMaterial.HasProperty("_BaseColor"))
            outlineMaterial.SetColor("_BaseColor", outlineColor);
    }

    private Material CreateOutlineMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        if (shader == null)
        {
            Debug.LogWarning("RemoteLeverOutline: não foi possível encontrar um shader Unlit.");
            return null;
        }

        Material material = new Material(shader)
        {
            name = "RemoteLeverOutlineMaterial_Runtime",
            color = outlineColor,
            renderQueue = 3000
        };

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", outlineColor);

        if (material.HasProperty("_Cull"))
            material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Front);

        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 0f);

        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 1f);

        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", Texture2D.whiteTexture);

        return material;
    }

    private void OnDestroy()
    {
        foreach (GameObject outlineObject in outlineObjects)
        {
            if (outlineObject == null)
                continue;

            if (Application.isPlaying)
                Destroy(outlineObject);
            else
                DestroyImmediate(outlineObject);
        }

        if (outlineMaterial != null)
        {
            if (Application.isPlaying)
                Destroy(outlineMaterial);
            else
                DestroyImmediate(outlineMaterial);
        }
    }
}
