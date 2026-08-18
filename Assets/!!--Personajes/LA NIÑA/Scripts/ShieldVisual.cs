using System.Collections;
using UnityEngine;

// ============================================================
//  VISUAL DEL ESCUDO
//  Va en un GameObject HIJO del player (por ejemplo "Escudo"), con su propio
//  SpriteRenderer. NO usa el Animator del player ni el sistema de State Anims:
//  se dibuja SOBRE la niña y corre en paralelo, asi nunca interrumpe ni pisa
//  las animaciones de correr, saltar, atacar, etc.
//
//  Las dos animaciones (aparicion y rotura) se reproducen POR CODIGO, pasando
//  los sprites del spritesheet uno atras del otro. Por eso NO hace falta crear
//  clips .anim ni un AnimatorController: se arrastran los sprites a las listas
//  de abajo y se ajustan los FPS desde el Inspector.
//
//  Toda la LOGICA (cuantos golpes aguanta, cooldown, etc) esta en PlayerShield.
//  Este script solo se ocupa de que se vea bien.
// ============================================================
[RequireComponent(typeof(SpriteRenderer))]
public class ShieldVisual : MonoBehaviour
{
    // Las tres etapas por las que pasa el escudo a medida que lo golpean.
    public enum Etapa { Limpio, Roto1, Roto2 }

    [Header("Sprites de cada etapa")]
    [Tooltip("Escudo entero, recien invocado.")]
    public Sprite spriteLimpio;
    [Tooltip("Escudo con el primer nivel de daño.")]
    public Sprite spriteRoto1;
    [Tooltip("Escudo con el segundo nivel de daño, a punto de romperse.")]
    public Sprite spriteRoto2;

    [Header("Animacion de APARICION")]
    [Tooltip("Frames del spritesheet EscudoAparece, en orden. Se pueden arrastrar todos juntos.")]
    public Sprite[] framesAparece;
    [Tooltip("Velocidad de la animacion de aparicion, en cuadros por segundo.")]
    public float fpsAparece = 15f;

    [Header("Animacion de ROTURA")]
    [Tooltip("Frames del spritesheet EscudoBreaking, en orden. Se pueden arrastrar todos juntos.")]
    public Sprite[] framesRompe;
    [Tooltip("Velocidad de la animacion de rotura, en cuadros por segundo.")]
    public float fpsRompe = 15f;

    [Header("Posicion segun hacia donde mira la niña")]
    // Los dos offsets se escriben en coordenadas de PANTALLA: X positivo SIEMPRE es hacia la
    // derecha de la pantalla, mire hacia donde mire la niña. Hacen falta dos porque el sprite
    // de la niña no esta centrado en su pivot (la postura encorvada la corre para un lado), y
    // al darse vuelta ese corrimiento se espeja: lo que corregis de un lado se duplica del otro.
    [Tooltip("Corrimiento del escudo cuando la niña MIRA A LA DERECHA. X positivo = hacia la derecha de la pantalla.")]
    public Vector2 offsetMirandoDerecha = Vector2.zero;
    [Tooltip("Corrimiento del escudo cuando la niña MIRA A LA IZQUIERDA. X positivo = hacia la derecha de la pantalla (igual que arriba, no se invierte solo).")]
    public Vector2 offsetMirandoIzquierda = Vector2.zero;

    [Header("Tamaño")]
    [Tooltip("Tamaño del escudo. 1 = el tamaño original del sprite.")]
    public float escala = 1f;
    [Tooltip("Si esta activo, el escudo NO se da vuelta cuando la niña cambia de direccion. Dejalo activo si el sprite del escudo es simetrico.")]
    public bool ignorarFlipDelPlayer = true;

    [Header("Orden de dibujado")]
    [Tooltip("Sorting Layer donde se dibuja el escudo. Tiene que ser una de las capas del proyecto.")]
    public string sortingLayer = "Player";
    [Tooltip("Numero dentro de la capa. Mas ALTO = se dibuja mas adelante. Poné un valor mayor al del sprite de la niña para que el escudo quede por encima.")]
    public int ordenEnCapa = 10;

    [Header("Transparencia")]
    [Range(0f, 1f)]
    [Tooltip("Opacidad normal del escudo. Bajalo si tapa mucho a la niña.")]
    public float opacidad = 1f;

    [Header("Sacudon del escudo al recibir un golpe")]
    [Tooltip("El sprite del escudo vibra cuando frena un golpe. Es independiente del sacudon de camara.")]
    public bool sacudirAlRecibirGolpe = true;
    [Tooltip("Que tan lejos se corre el escudo al vibrar, en unidades del mundo.")]
    public float shakeFuerza = 0.12f;
    [Tooltip("Cuanto dura la vibracion, en segundos.")]
    public float shakeDuracion = 0.15f;
    [Tooltip("Que tan rapido vibra. Mas alto = mas nervioso.")]
    public float shakeFrecuencia = 45f;

    [Header("Flash al recibir un golpe")]
    [Tooltip("El escudo se pinta de un color por un instante cuando frena un golpe.")]
    public bool flashAlRecibirGolpe = true;
    public Color colorFlash = Color.white;
    [Tooltip("Cuanto dura el destello, en segundos.")]
    public float flashDuracion = 0.08f;

    [Header("Parpadeo de aviso")]
    [Tooltip("El escudo parpadea cuando esta por caerse (por tiempo o porque le queda un golpe).")]
    public bool parpadearAlFinal = true;
    [Tooltip("Cuantas veces por segundo parpadea.")]
    public float parpadeoVelocidad = 6f;
    [Range(0f, 1f)]
    [Tooltip("Hasta que opacidad baja en cada parpadeo.")]
    public float parpadeoOpacidadMinima = 0.55f;

    [Header("Desvanecido al agotarse el tiempo")]
    [Tooltip("Segundos que tarda en apagarse cuando se le acaba el tiempo. 0 = desaparece de golpe.")]
    public float fadeSalidaDuracion = 0.4f;

    private SpriteRenderer sr;
    private Color colorBase;
    private Coroutine rutinaAnimacion;
    private Coroutine rutinaShake;
    private Coroutine rutinaFlash;
    private Coroutine rutinaFade;
    private bool parpadeando;

    // Getter por si la logica quiere esperar a que termine una animacion.
    public bool AnimacionEnCurso => rutinaAnimacion != null;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        colorBase = sr.color;
        AplicarTransform();
        AplicarOrden();
        sr.enabled = false; // arranca invisible: lo prende PlayerShield al activarse
    }

    // Si tocamos los valores en el Inspector mientras el juego corre, se ven al toque.
    private void OnValidate()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;

        AplicarTransform();
        AplicarOrden();
    }

    private void LateUpdate()
    {
        // Se reacomoda en LateUpdate para quedar SIEMPRE pegado al player, aunque el
        // player se haya movido o dado vuelta en este mismo frame.
        if (rutinaShake == null) AplicarTransform();
        else AplicarEscalaYFlip(); // durante el sacudon la posicion la maneja el shake
    }

    // ---------- API que usa PlayerShield ----------

    // Aparece: reproduce la animacion de invocacion y queda en el sprite limpio.
    public void Mostrar()
    {
        DetenerRutinas();

        sr.enabled = true;
        parpadeando = false;
        colorBase = new Color(colorBase.r, colorBase.g, colorBase.b, opacidad);
        sr.color = colorBase;

        AplicarTransform();
        AplicarOrden();

        if (framesAparece != null && framesAparece.Length > 0)
        {
            rutinaAnimacion = StartCoroutine(ReproducirAnimacion(framesAparece, fpsAparece, false));
        }
        else
        {
            CambiarEtapa(Etapa.Limpio); // sin frames de aparicion, aparece directo
        }
    }

    // Cambia el sprite segun cuantos golpes lleva aguantados.
    public void CambiarEtapa(Etapa etapa)
    {
        // Si esta corriendo la animacion de aparicion, no la cortamos: el sprite
        // correcto queda seteado igual cuando termine.
        Sprite objetivo = SpriteDeEtapa(etapa);
        etapaActual = etapa;

        if (rutinaAnimacion != null) return;
        if (objetivo != null) sr.sprite = objetivo;
    }

    // Frena un golpe: vibra y destella, pero el escudo sigue en pie.
    public void Golpe()
    {
        if (sacudirAlRecibirGolpe)
        {
            if (rutinaShake != null) StopCoroutine(rutinaShake);
            rutinaShake = StartCoroutine(ShakeRutina());
        }

        if (flashAlRecibirGolpe)
        {
            if (rutinaFlash != null) StopCoroutine(rutinaFlash);
            rutinaFlash = StartCoroutine(FlashRutina());
        }
    }

    // Se rompe: reproduce la animacion de rotura y despues se apaga solo.
    public void Romper()
    {
        DetenerRutinas();
        parpadeando = false;
        sr.color = new Color(colorBase.r, colorBase.g, colorBase.b, opacidad);

        if (framesRompe != null && framesRompe.Length > 0)
        {
            sr.enabled = true;
            rutinaAnimacion = StartCoroutine(ReproducirAnimacion(framesRompe, fpsRompe, true));
        }
        else
        {
            Ocultar(); // sin frames de rotura, desaparece de una
        }
    }

    // Se apaga sin animacion (por ejemplo cuando se le acaba el tiempo).
    public void Ocultar()
    {
        DetenerRutinas();
        parpadeando = false;
        sr.enabled = false;
        sr.color = new Color(colorBase.r, colorBase.g, colorBase.b, opacidad);
        transform.localPosition = OffsetActual();
    }

    // Se apaga con un fade suave. Es lo que corre cuando al escudo se le acaba el tiempo:
    // arranca desde la opacidad que tenga en ese momento, asi enlaza con el parpadeo
    // de aviso sin pegar un salto.
    public void Desvanecer()
    {
        if (!sr.enabled || fadeSalidaDuracion <= 0f)
        {
            Ocultar();
            return;
        }

        DetenerRutinas();
        parpadeando = false;
        rutinaFade = StartCoroutine(FadeSalidaRutina());
    }

    private IEnumerator FadeSalidaRutina()
    {
        float alphaInicial = sr.color.a;
        float t = 0f;

        while (t < fadeSalidaDuracion)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(alphaInicial, 0f, t / fadeSalidaDuracion);
            sr.color = new Color(colorBase.r, colorBase.g, colorBase.b, alpha);
            yield return null;
        }

        rutinaFade = null;
        Ocultar();
    }

    // Prende o apaga el parpadeo de aviso ("se me esta por caer").
    public void SetParpadeo(bool activo)
    {
        if (!parpadearAlFinal) activo = false;
        if (parpadeando == activo) return;

        parpadeando = activo;

        if (!activo && sr.enabled && rutinaFlash == null)
        {
            sr.color = new Color(colorBase.r, colorBase.g, colorBase.b, opacidad);
        }
    }

    private void Update()
    {
        // El parpadeo se calcula todos los frames, asi se puede prender y apagar
        // sin cortar ninguna corutina.
        if (!parpadeando || !sr.enabled || rutinaFlash != null || rutinaFade != null) return;

        float t = (Mathf.Sin(Time.time * parpadeoVelocidad * Mathf.PI * 2f) + 1f) * 0.5f;
        float alpha = Mathf.Lerp(parpadeoOpacidadMinima, opacidad, t);
        sr.color = new Color(colorBase.r, colorBase.g, colorBase.b, alpha);
    }

    // ---------- Interno ----------

    private Etapa etapaActual = Etapa.Limpio;

    private Sprite SpriteDeEtapa(Etapa etapa)
    {
        switch (etapa)
        {
            case Etapa.Roto1: return spriteRoto1 != null ? spriteRoto1 : spriteLimpio;
            case Etapa.Roto2: return spriteRoto2 != null ? spriteRoto2 : spriteLimpio;
            default: return spriteLimpio;
        }
    }

    // Pasa los sprites de una lista uno atras del otro. Si apagarAlFinal es true,
    // el escudo desaparece cuando termina (se usa para la rotura).
    private IEnumerator ReproducirAnimacion(Sprite[] frames, float fps, bool apagarAlFinal)
    {
        float espera = fps > 0f ? 1f / fps : 0.06f;

        for (int i = 0; i < frames.Length; i++)
        {
            if (frames[i] != null) sr.sprite = frames[i];
            yield return new WaitForSeconds(espera);
        }

        rutinaAnimacion = null;

        if (apagarAlFinal)
        {
            Ocultar();
        }
        else
        {
            // Termino la aparicion: dejamos el sprite de la etapa que corresponda.
            Sprite objetivo = SpriteDeEtapa(etapaActual);
            if (objetivo != null) sr.sprite = objetivo;
        }
    }

    private IEnumerator ShakeRutina()
    {
        float t = 0f;

        while (t < shakeDuracion)
        {
            t += Time.deltaTime;

            // La vibracion arranca fuerte y se va apagando.
            float caida = 1f - (t / shakeDuracion);
            float desplazamiento = Mathf.Sin(t * shakeFrecuencia) * shakeFuerza * caida;

            // El offset se recalcula todos los frames: si la niña se da vuelta en pleno
            // sacudon, el escudo se reacomoda al lado nuevo sin saltar.
            transform.localPosition = OffsetActual() + new Vector2(desplazamiento, desplazamiento * 0.5f);
            yield return null;
        }

        transform.localPosition = OffsetActual();
        rutinaShake = null;
    }

    private IEnumerator FlashRutina()
    {
        Color original = new Color(colorBase.r, colorBase.g, colorBase.b, opacidad);
        sr.color = colorFlash;

        yield return new WaitForSeconds(flashDuracion);

        sr.color = original;
        rutinaFlash = null;
    }

    private void DetenerRutinas()
    {
        if (rutinaAnimacion != null) { StopCoroutine(rutinaAnimacion); rutinaAnimacion = null; }
        if (rutinaShake != null)     { StopCoroutine(rutinaShake);     rutinaShake = null; }
        if (rutinaFlash != null)     { StopCoroutine(rutinaFlash);     rutinaFlash = null; }
        if (rutinaFade != null)      { StopCoroutine(rutinaFade);      rutinaFade = null; }

        transform.localPosition = OffsetActual();
    }

    // +1 si la niña mira a la derecha, -1 si mira a la izquierda.
    // El player se da vuelta poniendo su localScale.x en negativo, asi que el signo
    // de la escala del padre nos dice para donde esta mirando. Usamos lossyScale para
    // que funcione igual si el escudo cuelga mas abajo en la jerarquia.
    private float SignoDelPadre()
    {
        if (transform.parent == null) return 1f;
        return transform.parent.lossyScale.x < 0f ? -1f : 1f;
    }

    // Elige el offset del lado que corresponde y lo pasa de coordenadas de PANTALLA
    // a coordenadas locales. Como el padre esta espejado cuando mira a la izquierda,
    // hay que des-espejar la X, sino el corrimiento se iria para el lado contrario.
    private Vector2 OffsetActual()
    {
        float signo = SignoDelPadre();
        Vector2 elegido = signo < 0f ? offsetMirandoIzquierda : offsetMirandoDerecha;

        return new Vector2(elegido.x * signo, elegido.y);
    }

    private void AplicarTransform()
    {
        transform.localPosition = OffsetActual();
        AplicarEscalaYFlip();
    }

    private void AplicarEscalaYFlip()
    {
        // Si no queremos que el escudo se de vuelta con la niña, contrarrestamos su espejado.
        float signo = ignorarFlipDelPlayer ? SignoDelPadre() : 1f;

        transform.localScale = new Vector3(escala * signo, escala, 1f);
    }

    // Atajo del Inspector (engranaje del componente): copia el offset de la derecha al de
    // la izquierda espejado. Sirve como punto de partida si el desfasaje es simetrico.
    [ContextMenu("Copiar offset derecho al izquierdo (espejado)")]
    private void CopiarOffsetEspejado()
    {
        offsetMirandoIzquierda = new Vector2(-offsetMirandoDerecha.x, offsetMirandoDerecha.y);
    }

    private void AplicarOrden()
    {
        if (!string.IsNullOrEmpty(sortingLayer)) sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = ordenEnCapa;
    }
}
