using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Marcador 3D que aparece acima de uma alavanca durante o modo remoto.
/// Pode usar um SpriteRenderer automático ou um visual manual.
/// </summary>
[ExecuteAlways]
public class RemoteLeverTargetIcon : MonoBehaviour
{
    private static readonly List<RemoteLeverTargetIcon> allIcons =
        new List<RemoteLeverTargetIcon>();

    [Header("Referências")]
    [SerializeField] private LeverSwitch lever;
    [SerializeField] private Sprite iconSprite;
    [SerializeField] private Transform iconAnchor;
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private Collider targetCollider;
    [SerializeField] private RemoteLeverOutline objectOutline;
    [SerializeField] private Transform outlineSourceRoot;

    [Header("Ícone automático")]
    [SerializeField] private bool createWorldIconAutomatically = true;
    [Tooltip("Escala horizontal do ícone no mundo. Pode ser alterada durante o Play Mode.")]
    [SerializeField, Min(0.01f)] private float iconWidth = 0.7f;
    [Tooltip("Escala vertical do ícone no mundo. Use o mesmo valor para manter a proporção.")]
    [SerializeField, Min(0.01f)] private float iconHeight = 0.7f;
    [Tooltip("Permite ajustar a profundidade do sprite, normalmente deixe em 1.")]
    [SerializeField, Min(0.01f)] private float iconDepth = 1f;
    [SerializeField] private Vector3 iconLocalOffset = new Vector3(0f, 2f, 0f);
    [SerializeField] private bool billboardToCamera = true;
    [SerializeField] private int iconSortingOrder = 50;

    [Header("Área de mira")]
    [SerializeField, Min(0.05f)] private float targetRadius = 0.55f;

    private GameObject generatedIconObject;
    private SpriteRenderer generatedSpriteRenderer;
    private Graphic[] childGraphics;
    private Renderer[] childRenderers;
    private Camera cachedCamera;

    public LeverSwitch Lever => lever;

    public Vector3 AimWorldPosition
    {
        get
        {
            if (generatedIconObject != null)
                return generatedIconObject.transform.position;

            if (targetCollider != null)
                return targetCollider.bounds.center;

            return transform.position + iconLocalOffset;
        }
    }

    public static RemoteLeverTargetIcon[] GetActiveIcons()
    {
        return FindObjectsByType<RemoteLeverTargetIcon>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );
    }

    private void Awake()
    {
        if (lever == null)
            lever = GetComponentInParent<LeverSwitch>();

        if (objectOutline == null)
            objectOutline = GetComponent<RemoteLeverOutline>();

        if (objectOutline == null)
            objectOutline = gameObject.AddComponent<RemoteLeverOutline>();

        if (outlineSourceRoot == null)
            outlineSourceRoot = lever != null ? lever.transform : transform.parent;

        if (Application.isPlaying && objectOutline != null)
            objectOutline.Initialize(outlineSourceRoot);

        cachedCamera = Camera.main;

        if (createWorldIconAutomatically && iconSprite != null)
            CreateAutomaticWorldIcon();

        if (targetCollider == null)
            targetCollider = GetComponent<Collider>();

        if (targetCollider == null)
            CreateTargetCollider();
        else
            targetCollider.isTrigger = true;

        childGraphics = GetComponentsInChildren<Graphic>(true);
        childRenderers = GetComponentsInChildren<Renderer>(true);
    }

    private void OnEnable()
    {
        if (!allIcons.Contains(this))
            allIcons.Add(this);

        EnsureObjectOutlineInitialized();

        if (createWorldIconAutomatically && iconSprite != null && generatedIconObject == null)
            CreateAutomaticWorldIcon();

        UpdateIconScale();
    }

    private void OnValidate()
    {
        iconWidth = Mathf.Max(0.01f, iconWidth);
        iconHeight = Mathf.Max(0.01f, iconHeight);
        iconDepth = Mathf.Max(0.01f, iconDepth);

        if (!Application.isPlaying && isActiveAndEnabled &&
            createWorldIconAutomatically && iconSprite != null && generatedIconObject == null)
        {
            CreateAutomaticWorldIcon();
        }

        UpdateIconScale();
    }

    private void OnDisable()
    {
        allIcons.Remove(this);
    }

    private void LateUpdate()
    {
        // Mantém o sistema pronto mesmo quando o objeto, a alavanca ou o
        // modelo são ativados/desativados durante a partida.
        EnsureObjectOutlineInitialized();
        UpdateIconScale();

        if (!billboardToCamera || generatedIconObject == null)
            return;

        if (cachedCamera == null)
            cachedCamera = Camera.main;

        if (cachedCamera != null)
            generatedIconObject.transform.forward = cachedCamera.transform.forward;
    }

    private void EnsureObjectOutlineInitialized()
    {
        if (!Application.isPlaying)
            return;

        if (lever == null)
            lever = GetComponentInParent<LeverSwitch>();

        if (objectOutline == null)
            objectOutline = GetComponent<RemoteLeverOutline>();

        if (objectOutline == null)
            objectOutline = gameObject.AddComponent<RemoteLeverOutline>();

        if (outlineSourceRoot == null)
            outlineSourceRoot = lever != null ? lever.transform : transform.parent;

        if (objectOutline != null && outlineSourceRoot != null)
            objectOutline.Initialize(outlineSourceRoot);
    }

    private void UpdateIconScale()
    {
        if (generatedIconObject == null)
            return;

        generatedIconObject.transform.localScale = new Vector3(
            Mathf.Max(0.01f, iconWidth),
            Mathf.Max(0.01f, iconHeight),
            Mathf.Max(0.01f, iconDepth)
        );
    }

    private void CreateAutomaticWorldIcon()
    {
        Transform visualParent = iconAnchor != null ? iconAnchor : transform;
        Transform existingVisual = FindAndRemoveDuplicateChildren(
            visualParent,
            "RemoteTargetIconVisual_Auto"
        );

        if (existingVisual != null)
        {
            generatedIconObject = existingVisual.gameObject;
            generatedSpriteRenderer =
                generatedIconObject.GetComponent<SpriteRenderer>();

            if (generatedSpriteRenderer == null)
                generatedSpriteRenderer =
                    generatedIconObject.AddComponent<SpriteRenderer>();

            generatedSpriteRenderer.sprite = iconSprite;
            generatedSpriteRenderer.sortingOrder = iconSortingOrder;
            UpdateIconScale();
            return;
        }

        generatedIconObject = new GameObject("RemoteTargetIconVisual_Auto");
        generatedIconObject.transform.SetParent(visualParent, false);
        generatedIconObject.transform.localPosition =
            iconAnchor != null ? Vector3.zero : iconLocalOffset;
        generatedIconObject.transform.localRotation = Quaternion.identity;
        UpdateIconScale();

        generatedSpriteRenderer =
            generatedIconObject.AddComponent<SpriteRenderer>();
        generatedSpriteRenderer.sprite = iconSprite;
        generatedSpriteRenderer.color = Color.white;
        generatedSpriteRenderer.sortingOrder = iconSortingOrder;
    }

    private void CreateTargetCollider()
    {
        Transform existingCollider = FindAndRemoveDuplicateChildren(
            transform,
            "RemoteTargetIconCollider"
        );
        GameObject colliderObject;

        if (existingCollider != null)
        {
            colliderObject = existingCollider.gameObject;
        }
        else
        {
            colliderObject = new GameObject("RemoteTargetIconCollider");
            colliderObject.transform.SetParent(transform, false);
        }

        colliderObject.transform.localPosition =
            iconAnchor != null ? iconAnchor.localPosition : iconLocalOffset;
        colliderObject.layer = gameObject.layer;

        SphereCollider sphere = colliderObject.GetComponent<SphereCollider>();
        if (sphere == null)
            sphere = colliderObject.AddComponent<SphereCollider>();

        sphere.isTrigger = true;
        sphere.radius = targetRadius;
        targetCollider = sphere;
    }

    private Transform FindAndRemoveDuplicateChildren(
        Transform parent,
        string childName)
    {
        Transform first = null;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (child.name != childName)
                continue;

            if (first == null)
            {
                first = child;
                continue;
            }

            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }

        return first;
    }

    public void SetVisible(bool visible)
    {
        if (generatedSpriteRenderer != null)
            generatedSpriteRenderer.enabled = visible;

        if (visualRoot != null && visualRoot != gameObject)
            visualRoot.SetActive(visible);

        if (childGraphics != null)
        {
            foreach (Graphic graphic in childGraphics)
            {
                if (graphic != null)
                    graphic.enabled = visible;
            }
        }
    }

    /// <summary>
    /// Ativa ou desativa o contorno do objeto associado, sem alterar o ícone.
    /// </summary>
    public void SetObjectOutlineVisible(bool visible)
    {
        if (objectOutline != null)
            objectOutline.SetVisible(visible);
    }

    public static void SetAllVisible(bool visible)
    {
        RemoteLeverTargetIcon[] sceneIcons = FindObjectsByType<RemoteLeverTargetIcon>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        foreach (RemoteLeverTargetIcon icon in sceneIcons)
        {
            if (icon != null && !allIcons.Contains(icon))
                allIcons.Add(icon);
        }

        for (int i = allIcons.Count - 1; i >= 0; i--)
        {
            if (allIcons[i] == null)
            {
                allIcons.RemoveAt(i);
                continue;
            }

            allIcons[i].SetVisible(visible);
        }
    }
}
