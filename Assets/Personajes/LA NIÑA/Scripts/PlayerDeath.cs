using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

// ============================================================
//  Muerte del player. Va EN el player, asi funciona en TODAS las
//  escenas sin tener que poner un objeto suelto en cada una.
//  Se dispara al perder todas las vidas (HealthHandler.OnDeath)
//  o con la tecla T (test). Anima, funde a negro y reinicia la zona.
// ============================================================
[RequireComponent(typeof(HealthHandler))]
public class PlayerDeath : MonoBehaviour
{
    [Header("Animacion de muerte")]
    public int deathStateAnim = 9;        // valor de stateAnim para la animacion death
    public float deathAnimDuration = 1f;  // cuanto se ve la animacion antes del fundido

    [Header("Fundido a negro")]
    public float fadeDuration = 0.6f;

    private Image fadeImage;
    private bool isDying = false;
    private PlayerController playerController;
    private HealthHandler healthHandler;

    private void Awake()
    {
        CreateFadeOverlay();
        playerController = GetComponent<PlayerController>();
        healthHandler = GetComponent<HealthHandler>();
    }

    private void OnEnable()
    {
        if (healthHandler != null) healthHandler.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        if (healthHandler != null) healthHandler.OnDeath -= HandleDeath;
    }

    private void Start()
    {
        // Al cargar la escena arrancamos en negro y aclaramos (fundido de entrada).
        StartCoroutine(Fade(1f, 0f));

        // el player NO se destruye al morir: hace la secuencia y reaparece.
        if (healthHandler != null) healthHandler.destroyOnDeath = false;
    }

    private void Update()
    {
        if (isDying) return;

        // tecla T: testear la muerte sin perder vidas.
        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            StartCoroutine(DeathSequence());
        }
    }

    // se llama cuando la vida llega a 0 (lo dispara el HealthHandler).
    private void HandleDeath()
    {
        if (!isDying) StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        isDying = true;

        // 1) Frenamos al player y reproducimos la animacion de muerte.
        if (playerController != null)
        {
            playerController.enabled = false;            // corta el control (movimiento, salto, anims, etc.)
            playerController.rb.linearVelocity = Vector2.zero;
            playerController.rb.gravityScale = 0f;       // que no se caiga mientras "muere"
            playerController.animPlayer.SetInteger("stateAnim", deathStateAnim);
        }

        // 2) Esperamos a que se vea la animacion de muerte.
        yield return new WaitForSeconds(deathAnimDuration);

        // 3) Fundido a negro.
        yield return Fade(0f, 1f);

        // 4) Reiniciamos la zona actual (el player reaparece en el inicio).
        //    >> A FUTURO: aca va la logica de checkpoint / punto de entrada. <<
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private IEnumerator Fade(float from, float to)
    {
        SetFadeAlpha(from);

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            SetFadeAlpha(Mathf.Lerp(from, to, t / fadeDuration));
            yield return null;
        }

        SetFadeAlpha(to);
    }

    private void SetFadeAlpha(float a)
    {
        if (fadeImage == null) return;

        Color c = fadeImage.color;
        c.a = a;
        fadeImage.color = c;
    }

    // Crea por codigo un Canvas + una Image negra a pantalla completa (asi no hay que armar UI a mano).
    private void CreateFadeOverlay()
    {
        GameObject canvasGO = new GameObject("FadeCanvas (player)");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999; // por encima de todo

        GameObject imgGO = new GameObject("FadeImage");
        imgGO.transform.SetParent(canvasGO.transform, false);

        fadeImage = imgGO.AddComponent<Image>();
        fadeImage.color = new Color(0f, 0f, 0f, 1f); // negro, arranca opaco (para el fundido de entrada)
        fadeImage.raycastTarget = false;

        RectTransform rt = fadeImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
