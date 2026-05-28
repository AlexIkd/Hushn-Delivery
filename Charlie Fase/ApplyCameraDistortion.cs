using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ApplyCameraDistortion : MonoBehaviour
{
    [Header("Referências")]
    public DynamicFollowCamera camScript;
    public Volume postProcessVolume;

    private LensDistortion lensDist;
    private ChromaticAberration chromatic;
    private MotionBlur motionBlur;
    private Vignette vignette;
    
    private bool initialized = false;

    void Start()
    {
        Initialize();
    }

    void Initialize()
    {
        if (postProcessVolume == null) postProcessVolume = GetComponent<Volume>();
        
        if (postProcessVolume != null && postProcessVolume.profile != null)
        {
            var profile = postProcessVolume.profile;
            
            // Tenta obter todos os efeitos
            profile.TryGet(out lensDist);
            profile.TryGet(out chromatic);
            profile.TryGet(out motionBlur);
            profile.TryGet(out vignette);

            if (lensDist == null) Debug.LogWarning("[ApplyCameraDistortion] Lens Distortion não encontrado no Volume Profile.");
            if (chromatic == null) Debug.LogWarning("[ApplyCameraDistortion] Chromatic Aberration não encontrado no Volume Profile.");
            if (motionBlur == null) Debug.LogWarning("[ApplyCameraDistortion] Motion Blur não encontrado no Volume Profile.");
            if (vignette == null) Debug.LogWarning("[ApplyCameraDistortion] Vignette não encontrado no Volume Profile.");

            initialized = true;
        }
        else
        {
            Debug.LogError("[ApplyCameraDistortion] Volume ou Perfil de Post-Processing não atribuído!");
        }

        if (camScript == null) camScript = Camera.main.GetComponent<DynamicFollowCamera>();
    }

    void Update()
    {
        if (!initialized || camScript == null) return;

        // Aplica Lens Distortion
        if (lensDist != null)
        {
            lensDist.intensity.overrideState = true;
            lensDist.intensity.value = camScript.CurrentLensDistortion;
        }

        // Aplica Chromatic Aberration
        if (chromatic != null)
        {
            chromatic.intensity.overrideState = true;
            chromatic.intensity.value = camScript.CurrentChromaticAberration;
        }

        // Aplica Motion Blur
        if (motionBlur != null)
        {
            motionBlur.intensity.overrideState = true;
            motionBlur.intensity.value = camScript.CurrentMotionBlur;
        }

        // Aplica Vignette
        if (vignette != null)
        {
            vignette.intensity.overrideState = true;
            vignette.intensity.value = camScript.CurrentVignette;
        }
    }
}
