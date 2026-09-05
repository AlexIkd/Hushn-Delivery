using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI da personagem com contador numérico opcional de vidas e uma imagem composta
/// para representar os três hits restantes.
/// Também controla a animação de entrada e saída da HUD quando a personagem recebe dano.
/// </summary>
public class PlayerLivesUI : MonoBehaviour
{
    [Header("Referência")]
    [SerializeField] private PlayerHealthSystem healthSystem;

    [Header("Vidas Numéricas - Opcional")]
    [Tooltip("Texto opcional para mostrar a quantidade de vidas, por exemplo: x3.")]
    [SerializeField] private TMP_Text livesText;
    [SerializeField] private string livesPrefix = "x";

    [Header("Imagem dos Hits")]
    [Tooltip("Um único objeto UI > Image que exibirá a imagem composta dos três hits.")]
    [SerializeField] private Image hitsImage;

    [Header("Sprites Compostos")]
    [Tooltip("Imagem com três exclamações ativas: 3 On.")]
    [SerializeField] private Sprite hits3Sprite;

    [Tooltip("Imagem com duas exclamações ativas e uma Off: 2 On + 1 Off.")]
    [SerializeField] private Sprite hits2Sprite;

    [Tooltip("Imagem com uma exclamação ativa e duas Off: 1 On + 2 Off.")]
    [SerializeField] private Sprite hits1Sprite;

    [Tooltip("Imagem com as três exclamações Off: 0 On.")]
    [SerializeField] private Sprite hits0Sprite;

    [Header("Animação da HUD")]
    [Tooltip("RectTransform do painel que contém a vida e os hits. Exemplo: LivesHUD.")]
    [SerializeField] private RectTransform hudRoot;

    [Tooltip("CanvasGroup do painel da HUD. Se ficar vazio, será criado automaticamente no hudRoot.")]
    [SerializeField] private CanvasGroup hudCanvasGroup;

    [Tooltip("Quanto a HUD sobe acima da posição normal durante a entrada.")]
    [SerializeField] private float riseDistance = 70f;

    [Tooltip("Quanto a HUD desce abaixo da posição normal durante a acomodação.")]
    [SerializeField] private float dipDistance = 18f;

    [Tooltip("Distância abaixo da posição normal onde a HUD começa a entrada e termina a saída.")]
    [SerializeField] private float hiddenDistance = 90f;

    [Tooltip("Duração do movimento de subida na entrada.")]
    [SerializeField] private float riseDuration = 0.18f;

    [Tooltip("Duração do movimento para baixo, passando um pouco da posição normal.")]
    [SerializeField] private float dipDuration = 0.12f;

    [Tooltip("Duração do retorno à posição normal.")]
    [SerializeField] private float settleDuration = 0.16f;

    [Tooltip("Tempo que a HUD permanece parada na posição normal antes de desaparecer.")]
    [SerializeField] private float visibleDuration = 2.5f;

    [Tooltip("Duração de cada trecho do caminho inverso durante a saída.")]
    [SerializeField] private float exitStepDuration = 0.14f;

    [Tooltip("Duração do desaparecimento gradual da HUD.")]
    [SerializeField] private float fadeDuration = 0.18f;

    private Vector2 normalPosition;
    private Coroutine hudAnimation;
    private int lastHitsRemaining = -1;
    private bool ignoreHitAnimation;

    private void Awake()
    {
        if (healthSystem == null)
            healthSystem = FindFirstObjectByType<PlayerHealthSystem>();

        if (hudRoot == null)
            hudRoot = GetComponent<RectTransform>();

        if (hudRoot != null)
        {
            normalPosition = hudRoot.anchoredPosition;

            if (hudCanvasGroup == null)
                hudCanvasGroup = hudRoot.GetComponent<CanvasGroup>();

            if (hudCanvasGroup == null)
                hudCanvasGroup = hudRoot.gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void Start()
    {
        HideHudImmediately();
    }

    private void OnEnable()
    {
        if (healthSystem == null)
            return;

        healthSystem.onLivesChanged.AddListener(UpdateLives);
        healthSystem.onHitsChanged.AddListener(UpdateHits);
        healthSystem.onHitsRestored.AddListener(HandleHitsRestored);
        RefreshFromSystem();
    }

    private void OnDisable()
    {
        if (healthSystem == null)
            return;

        healthSystem.onLivesChanged.RemoveListener(UpdateLives);
        healthSystem.onHitsChanged.RemoveListener(UpdateHits);
        healthSystem.onHitsRestored.RemoveListener(HandleHitsRestored);
    }

    private void RefreshFromSystem()
    {
        ignoreHitAnimation = true;
        UpdateLives(healthSystem.LivesRemaining);
        UpdateHits(healthSystem.HitsRemaining);
        ignoreHitAnimation = false;
    }

    public void UpdateLives(int lives)
    {
        if (livesText != null)
            livesText.text = livesPrefix + Mathf.Max(0, lives);
    }

    private void HandleHitsRestored()
    {
        ShowHudAfterHit();
    }

    /// <summary>
    /// Troca a imagem completa conforme a quantidade de hits restantes.
    /// Inicia a animação quando os hits diminuem ou quando são restaurados.
    /// </summary>
    public void UpdateHits(int hitsRemaining)
    {
        int remaining = Mathf.Clamp(hitsRemaining, 0, 3);

        Sprite stateSprite = remaining switch
        {
            3 => hits3Sprite,
            2 => hits2Sprite,
            1 => hits1Sprite,
            _ => hits0Sprite
        };

        if (hitsImage != null && stateSprite != null)
            hitsImage.sprite = stateSprite;

        bool receivedHit = lastHitsRemaining >= 0 && remaining < lastHitsRemaining;
        lastHitsRemaining = remaining;

        if (!ignoreHitAnimation && receivedHit)
            ShowHudAfterHit();
    }

    public void ShowHudAfterHit()
    {
        if (hudRoot == null || hudCanvasGroup == null)
            return;

        if (hudAnimation != null)
            StopCoroutine(hudAnimation);

        hudAnimation = StartCoroutine(HudVisibilitySequence());
    }

    private IEnumerator HudVisibilitySequence()
    {
        Vector2 hiddenPosition = normalPosition + Vector2.down * hiddenDistance;
        Vector2 peakPosition = normalPosition + Vector2.up * riseDistance;
        Vector2 dipPosition = normalPosition + Vector2.down * dipDistance;

        hudRoot.gameObject.SetActive(true);
        hudRoot.anchoredPosition = hiddenPosition;
        hudCanvasGroup.alpha = 0f;

        yield return MoveHud(hiddenPosition, peakPosition, riseDuration, 0f, 1f);
        yield return MoveHud(peakPosition, dipPosition, dipDuration, 1f, 1f);
        yield return MoveHud(dipPosition, normalPosition, settleDuration, 1f, 1f);

        yield return new WaitForSeconds(visibleDuration);

        // Caminho inverso: posição normal -> abaixo -> acima -> posição escondida.
        yield return MoveHud(normalPosition, dipPosition, exitStepDuration, 1f, 1f);
        yield return MoveHud(dipPosition, peakPosition, exitStepDuration, 1f, 1f);
        yield return MoveHud(peakPosition, hiddenPosition, exitStepDuration, 1f, 0f);

        hudCanvasGroup.alpha = 0f;
        hudRoot.anchoredPosition = normalPosition;
        hudAnimation = null;
    }

    private IEnumerator MoveHud(
        Vector2 from,
        Vector2 to,
        float duration,
        float startAlpha,
        float endAlpha)
    {
        if (duration <= 0f)
        {
            hudRoot.anchoredPosition = to;
            hudCanvasGroup.alpha = endAlpha;
            yield break;
        }

        float elapsed = 0f;
        hudRoot.anchoredPosition = from;
        hudCanvasGroup.alpha = startAlpha;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

            hudRoot.anchoredPosition = Vector2.LerpUnclamped(from, to, easedProgress);
            hudCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, easedProgress);
            yield return null;
        }

        hudRoot.anchoredPosition = to;
        hudCanvasGroup.alpha = endAlpha;
    }

    private void HideHudImmediately()
    {
        if (hudRoot == null || hudCanvasGroup == null)
            return;

        if (hudAnimation != null)
        {
            StopCoroutine(hudAnimation);
            hudAnimation = null;
        }

        hudRoot.anchoredPosition = normalPosition;
        hudCanvasGroup.alpha = 0f;
    }
}
