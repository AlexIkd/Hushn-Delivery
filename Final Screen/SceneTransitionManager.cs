using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance;

    [Header("Configurações de Fade")]
    [Tooltip("Referência ao componente Image do Canvas que será usado para o fade.")]
    public Image fadeImage;
    [Tooltip("Duração do fade em segundos.")]
    public float fadeDuration = 1.0f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Garante que o objeto persista entre as cenas
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Inicia com um Fade In (transição da cena anterior para a atual)
        FadeIn();
    }

    // Inicia o processo de transição para uma nova cena
    public void TransitionToScene(string sceneName)
    {
        StartCoroutine(FadeOutAndLoadScene(sceneName));
    }

    // Coroutine para o Fade Out e carregamento da cena
    private IEnumerator FadeOutAndLoadScene(string sceneName)
    {
        // 1. Fade Out (Escurecer a tela)
        yield return StartCoroutine(Fade(1f));

        // 2. Carregar a nova cena
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        
        // Opcional: Mostrar uma tela de carregamento enquanto operation.progress < 0.9f
        while (!operation.isDone)
        {
            // Aqui você pode atualizar uma barra de progresso, se houver
            yield return null;
        }

        // A nova cena foi carregada. O método Start() dela chamará FadeIn()
    }

    // Coroutine genérica para o efeito de Fade
    private IEnumerator Fade(float targetAlpha)
    {
        if (fadeImage == null)
        {
            Debug.LogError("Fade Image não está atribuída no SceneTransitionManager.");
            yield break;
        }

        float startAlpha = fadeImage.color.a;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, timer / fadeDuration);
            Color newColor = fadeImage.color;
            newColor.a = newAlpha;
            fadeImage.color = newColor;
            yield return null;
        }

        // Garante que o alpha final seja exatamente o alvo
        Color finalColor = fadeImage.color;
        finalColor.a = targetAlpha;
        fadeImage.color = finalColor;
    }

    // Método público para iniciar o Fade In (para ser chamado no Start() da cena)
    public void FadeIn()
    {
        // O Fade In é a transição de preto (alpha=1) para transparente (alpha=0)
        StartCoroutine(Fade(0f));
    }
}
