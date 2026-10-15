using System.Collections;
using UnityEngine;

// ============================================================
//  CAMBIAR LA MUSICA (desde un evento)
//
//  Va en un objeto vacio de la escena. Sirve para cambiar el tema que esta sonando
//  desde cualquier UnityEvent del Inspector, sin tocar codigo. Por ejemplo, en el
//  evento "Al Terminar" de la cinematica final del boss: ahi se cambia a la musica
//  tranquila del cierre.
//
//  Metodos que se pueden elegir en el evento:
//    - Cambiar()          -> pone la musica nueva, con fundido.
//    - VolverALaDeLaZona() -> vuelve a la musica normal de la zona.
// ============================================================
public class CambiarMusica : MonoBehaviour
{
    [Header("La musica nueva")]
    [Tooltip("El tema que empieza a sonar. Vacio = silencio.")]
    public AudioClip musicaNueva;

    [Header("Fundidos (segundos)")]
    [Tooltip("Cuanto tarda en apagarse el tema anterior.")]
    [Min(0f)] public float fadeOut = 2f;
    [Tooltip("Cuanto tarda en entrar el tema nuevo.")]
    [Min(0f)] public float fadeIn = 3f;

    [Header("Arranque automatico")]
    [Tooltip("Activo: cambia la musica apenas carga la escena, sin esperar ningun evento.")]
    public bool cambiarAlEmpezar = false;

    private Coroutine rutina;

    private void Start()
    {
        if (cambiarAlEmpezar) Cambiar();
    }

    // La que se arrastra al evento del Inspector.
    public void Cambiar()
    {
        Lanzar(musicaNueva, false);
    }

    public void VolverALaDeLaZona()
    {
        Lanzar(null, true);
    }

    private void Lanzar(AudioClip clip, bool volverALaDeLaZona)
    {
        if (AudioManager.Instance == null) return;

        if (rutina != null) StopCoroutine(rutina);
        rutina = StartCoroutine(Rutina(clip, volverALaDeLaZona));
    }

    private IEnumerator Rutina(AudioClip clip, bool volverALaDeLaZona)
    {
        AudioSource fuente = AudioManager.Instance.musicSource;
        if (fuente == null) yield break;

        float volumenNormal = fuente.volume;

        // 1) Se va el tema anterior.
        yield return Fundir(fuente, fuente.volume, 0f, fadeOut);

        // 2) Entra el nuevo (o vuelve el de la zona), desde el silencio.
        fuente.volume = 0f;
        if (volverALaDeLaZona) AudioManager.Instance.VolverALaMusicaDeLaZona();
        else AudioManager.Instance.ReproducirMusicaEspecial(clip);

        yield return Fundir(fuente, 0f, volumenNormal, fadeIn);

        rutina = null;
    }

    private IEnumerator Fundir(AudioSource fuente, float desde, float hasta, float duracion)
    {
        if (duracion <= 0f)
        {
            fuente.volume = hasta;
            yield break;
        }

        float t = 0f;
        fuente.volume = desde;

        while (t < duracion && fuente != null)
        {
            t += Time.unscaledDeltaTime;
            fuente.volume = Mathf.Lerp(desde, hasta, Mathf.Clamp01(t / duracion));
            yield return null;
        }

        if (fuente != null) fuente.volume = hasta;
    }
}
