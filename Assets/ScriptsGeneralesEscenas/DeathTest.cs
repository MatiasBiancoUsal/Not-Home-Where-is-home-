using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

// ============================================================
//  Muerte del player. Se dispara cuando pierde TODAS las vidas
//  (escucha HealthHandler.OnDeath) o con la tecla T para testear.
//  Reproduce la animacion "death", funde a negro y reinicia la zona.
// ============================================================
public class DeathTest : MonoBehaviour
{
    [Header("Animacion de muerte")]
    public int deathStateAnim = 9;        // valor de stateAnim para la animacion death (engancharlo en el Animator)
    public float deathAnimDuration = 1f;  // cuanto se ve la animacion antes del fundido

    [Header("Fundido a negro")]
    public float fadeDuration = 0.6f;

    private Image fadeImage;
    private bool isDying = false;
    private HealthHandler playerHealth;

    private void Awake()
    {
        CreateFadeOverlay();
    }

    private void Start()
    {
        // Al cargar la escena arrancamos en negro y aclaramos (fundido de entrada).
        StartCoroutine(Fade(1f, 0f));

        // Enganchamos la vida del player: la muerte se dispara al perder TODAS las vidas.
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            playerHealth = player.GetComponent<HealthHandler>();
            if (playerHealth != null)
            {
                playerHealth.destroyOnDeath = false;       // el player NO se destruye: reaparece
                playerHealth.OnDeath += HandlePlayerDeath;
            }
        }
    }

    private void OnDestroy()
    {
        if (playerHealth != null) playerHealth.OnDeath -= HandlePlayerDeath;
    }

    private void HandlePlayerDeath()
    {
        if (!isDying) StartCoroutine(DeathSequence());
    }

    private void Update()
    {
        if (isDying) return;

        // Solo en el editor: en el juego final la T no mata (se apretaba sin querer).
        bool esPrueba = Application.isEditor || Debug.isDebugBuild;
        if (esPrueba && Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            StartCoroutine(DeathSequence());
        }
    }

    private IEnumerator DeathSequence()
    {
        isDying = true;

        // 1) Frenamos al player y reproducimos la animacion de muerte.
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            player.enabled = false;                  // corta el control (movimiento, salto, anims, etc.)
            player.rb.linearVelocity = Vector2.zero;
            player.rb.gravityScale = 0f;             // que no se caiga mientras "muere"
            player.animPlayer.SetInteger("stateAnim", deathStateAnim);
        }

        // 2) Esperamos a que se vea la animacion de muerte.
        yield return new WaitForSeconds(deathAnimDuration);

        // 3) Fundido a negro.
        yield return Fade(0f, 1f);

        // 4) Reiniciamos la zona actual (el player reaparece en el inicio).
        // Que reaparezca en el ultimo checkpoint (o donde entro a la zona), no al principio.
        PuntoDeReaparicion.ReaparecerAlRecargar();
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
        GameObject canvasGO = new GameObject("FadeCanvas (temp)");
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
