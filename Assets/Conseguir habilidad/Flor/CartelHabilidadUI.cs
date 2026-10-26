using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Como ENTRA un texto letra por letra: cada una aparece con fade y creciendo desde
// chiquita, y como cada una arranca un poco despues que la anterior, el conjunto se
// lee como un barrido de izquierda a derecha.
[System.Serializable]
public class EntradaDeTexto
{
    [Tooltip("Apagalo y el texto aparece entero de una, sin animacion.")]
    public bool activa = true;

    [Tooltip("Segundos entre el arranque de una letra y el de la siguiente. Esto es lo que hace " +
             "el BARRIDO.\n\n" +
             "IMPORTANTE: para que el barrido se NOTE, este valor tiene que ser grande en relacion " +
             "a 'Duracion Por Letra'. Si cada letra tarda mucho mas de lo que tarda en arrancar la " +
             "siguiente, entran todas casi juntas y parece que no hay barrido.")]
    public float retrasoEntreLetras = 0.07f;

    [Tooltip("Segundos que tarda CADA letra en terminar de entrar (aparecer y crecer a su tamaño).")]
    public float duracionPorLetra = 0.25f;

    [Tooltip("De que tamaño arranca cada letra.\n\n" +
             "MENOR a 1 = arranca chiquita y CRECE (0.4 = empieza al 40%).\n" +
             "MAYOR a 1 = arranca grande y se achica.\n" +
             "1 = sin zoom, solo fade.")]
    public float zoomInicial = 0.4f;

    [Tooltip("Cuanto SUBE cada letra mientras entra, en pixeles. 0 = entra sin moverse.")]
    public float desplazamientoInicial = 0f;

    // Copia los valores de otro, sin compartir el objeto.
    public void CopiarDesde(EntradaDeTexto otro)
    {
        if (otro == null) return;

        activa = otro.activa;
        retrasoEntreLetras = otro.retrasoEntreLetras;
        duracionPorLetra = otro.duracionPorLetra;
        zoomInicial = otro.zoomInicial;
        desplazamientoInicial = otro.desplazamientoInicial;
    }
}

[System.Serializable]
public class EstiloCartelHabilidad
{
    [Header("Panel")]
    public Vector2 posicionPanel = new Vector2(0f, 150f);
    public Vector2 tamanoPanel = new Vector2(1120f, 490f);
    public Color colorFondo = new Color(0.035f, 0.04f, 0.065f, 0.97f);
    public Color colorBorde = new Color(0f, 0f, 0f, 0.9f);
    public Vector2 grosorBorde = new Vector2(4f, -4f);

    [Header("Imagen")]
    public Vector2 posicionImagen = new Vector2(0f, 90f);
    public Vector2 tamanoImagen = new Vector2(220f, 220f);

    [Header("Titulo")]
    public TMP_FontAsset tipografiaTitulo;
    public float tamanoTitulo = 60f;
    public Color colorTitulo = Color.white;
    public Vector2 posicionTitulo = new Vector2(70f, 45f);
    public Vector2 cajaTitulo = new Vector2(800f, 100f);
    public FontStyles estiloTitulo = FontStyles.Bold;

    [Header("Descripcion")]
    public TMP_FontAsset tipografiaDescripcion;
    public float tamanoDescripcion = 24f;
    public Color colorDescripcion = Color.white;
    public Vector2 posicionDescripcion = new Vector2(100f, -25f);
    public Vector2 cajaDescripcion = new Vector2(720f, 90f);
    public FontStyles estiloDescripcion = FontStyles.Normal;

    [Header("Entrada del TITULO (letra por letra, fade + zoom)")]
    public EntradaDeTexto entradaTitulo = new EntradaDeTexto();

    [Header("Entrada del TEXTO PARA CERRAR")]
    public EntradaDeTexto entradaAyuda = new EntradaDeTexto
    {
        retrasoEntreLetras = 0.03f,
        duracionPorLetra = 0.2f,
        zoomInicial = 0.55f
    };

    [Header("Animacion de tipeo (descripcion)")]
    [Tooltip("El texto aparece letra por letra, como si alguien lo estuviera escribiendo.")]
    public bool tipearTexto = true;
    [Tooltip("Velocidad del tipeo, en letras por segundo. 45 es un ritmo comodo de leer.")]
    public float letrasPorSegundo = 45f;
    [Tooltip("Tipear tambien el TITULO. Apagado, el titulo aparece entero de una y solo se tipea la descripcion.")]
    public bool tipearTitulo = false;
    [Tooltip("Segundos de espera antes de empezar a escribir, para que el cartel se lea primero.")]
    public float esperaAntesDeTipear = 0.2f;
    [Tooltip("El texto de abajo ('presiona ESPACIO...') aparece recien cuando termina el tipeo.")]
    public bool mostrarAyudaAlTerminar = true;

    [Header("Cuando se puede cerrar el cartel")]
    [Tooltip("Segundos que el cartel espera DESPUES de escribir la descripcion y antes de mostrar " +
             "el texto de cerrar.\n\n" +
             "Mientras tanto el cartel NO se puede cerrar: recien cuando aparece el 'presiona " +
             "ESPACIO...' el jugador puede sacarlo. Sirve para que no lo cierren de un manotazo " +
             "sin haberlo leido.")]
    public float esperaAntesDelTextoDeCerrar = 3f;

    [Header("Tipeo: sonido (opcional)")]
    [Tooltip("Clics cortos que suenan mientras escribe. Podés poner varios (por ejemplo 3) y en cada " +
             "letra se elige uno AL AZAR, asi el tipeo no suena repetitivo. Dejalo vacio si no querés sonido.")]
    public AudioClip[] sonidosTipeo;
    [Tooltip("Sonido viejo, de cuando habia uno solo. Si cargaste la lista de arriba, este se ignora.")]
    public AudioClip sonidoTipeo;
    [Range(0f, 1f)] public float volumenTipeo = 0.5f;
    [Range(0f, 0.5f)]
    [Tooltip("Le cambia un poco el tono a cada clic, al azar. 0 = todos iguales. 0.1 ya disimula bastante la repeticion.")]
    public float variacionDeTono = 0.08f;
    [Tooltip("Cada cuantas letras suena. 1 = en todas (puede marear), 2 o 3 queda mejor.")]
    public int sonarCadaCuantasLetras = 2;

    [Header("Texto para cerrar")]
    public TMP_FontAsset tipografiaAyuda;
    public float tamanoAyuda = 21f;
    public Color colorAyuda = Color.white;
    public Vector2 posicionAyuda = new Vector2(80f, -105f);
    public Vector2 cajaAyuda = new Vector2(760f, 45f);
    public FontStyles estiloAyuda = FontStyles.Italic;

    // Copia TODOS los valores de otro estilo, campo por campo.
    //
    // Es una copia de verdad, no una referencia compartida: despues de llamarla, este
    // estilo se puede editar sin tocar el original. Se usa para arrancar un cartel nuevo
    // con el mismo diseño que los de habilidad y despues retocarlo aparte.
    //
    // Se hace a mano y no con JsonUtility porque las tipografias y el sonido son
    // referencias a assets de Unity, y JsonUtility no las copia bien.
    public void CopiarDesde(EstiloCartelHabilidad otro)
    {
        if (otro == null) return;

        // Panel
        posicionPanel = otro.posicionPanel;
        tamanoPanel = otro.tamanoPanel;
        colorFondo = otro.colorFondo;
        colorBorde = otro.colorBorde;
        grosorBorde = otro.grosorBorde;

        // Imagen
        posicionImagen = otro.posicionImagen;
        tamanoImagen = otro.tamanoImagen;

        // Titulo
        tipografiaTitulo = otro.tipografiaTitulo;
        tamanoTitulo = otro.tamanoTitulo;
        colorTitulo = otro.colorTitulo;
        posicionTitulo = otro.posicionTitulo;
        cajaTitulo = otro.cajaTitulo;
        estiloTitulo = otro.estiloTitulo;

        // Descripcion
        tipografiaDescripcion = otro.tipografiaDescripcion;
        tamanoDescripcion = otro.tamanoDescripcion;
        colorDescripcion = otro.colorDescripcion;
        posicionDescripcion = otro.posicionDescripcion;
        cajaDescripcion = otro.cajaDescripcion;
        estiloDescripcion = otro.estiloDescripcion;

        // Entradas letra por letra (tambien copiadas, no compartidas)
        if (entradaTitulo == null) entradaTitulo = new EntradaDeTexto();
        if (entradaAyuda == null) entradaAyuda = new EntradaDeTexto();
        entradaTitulo.CopiarDesde(otro.entradaTitulo);
        entradaAyuda.CopiarDesde(otro.entradaAyuda);

        // Tipeo
        tipearTexto = otro.tipearTexto;
        letrasPorSegundo = otro.letrasPorSegundo;
        tipearTitulo = otro.tipearTitulo;
        esperaAntesDeTipear = otro.esperaAntesDeTipear;
        mostrarAyudaAlTerminar = otro.mostrarAyudaAlTerminar;
        esperaAntesDelTextoDeCerrar = otro.esperaAntesDelTextoDeCerrar;

        // Sonido del tipeo
        sonidosTipeo = otro.sonidosTipeo;
        sonidoTipeo = otro.sonidoTipeo;
        volumenTipeo = otro.volumenTipeo;
        variacionDeTono = otro.variacionDeTono;
        sonarCadaCuantasLetras = otro.sonarCadaCuantasLetras;

        // Texto para cerrar
        tipografiaAyuda = otro.tipografiaAyuda;
        tamanoAyuda = otro.tamanoAyuda;
        colorAyuda = otro.colorAyuda;
        posicionAyuda = otro.posicionAyuda;
        cajaAyuda = otro.cajaAyuda;
        estiloAyuda = otro.estiloAyuda;
    }
}

public class CartelHabilidadUI : MonoBehaviour
{
    private static CartelHabilidadUI instance;

    private GameObject panel;
    private Image icono;
    private TMP_Text titulo;
    private TMP_Text descripcion;
    private TMP_Text ayudaCerrar;
    private Image fondo;
    private Image marco;
    private Outline borde;
    private bool abierto;
    private bool pausarJuego;
    private float escalaAnterior = 1f;
    private float abiertoDesde;
    private EstiloCartelHabilidad estiloActivo;

    // Tipeo y entradas
    private Coroutine tipeoCo;
    private bool tipeando;            // hay una animacion de texto corriendo
    private bool saltearAnimacion;    // el jugador apreto: terminar las animaciones ya
    private bool sePuedeCerrar;       // recien cuando el texto de cerrar termino de aparecer
    private AudioSource audioTipeo;
    private AudioClip ultimoClipDeTipeo;

    // ============================================================
    //  COOLDOWN: un cartel no se repite enseguida
    //
    //  Los carteles saltan cuando la niña ENTRA a un trigger. Si se pincha justo al
    //  lado (por ejemplo, los pinchos que hay pegados a una flor), el knockback la
    //  saca y la vuelve a meter, y el mismo cartel aparece una y otra vez: queda en
    //  loop y no hay forma de irse de ahi.
    //
    //  Con esto, despues de CERRAR un cartel pasan unos segundos antes de que ese
    //  mismo cartel pueda volver a aparecer, asi da tiempo de salir del lugar.
    //
    //  Vale para TODOS los carteles de TODAS las zonas: cada uno se reconoce por su
    //  clave y, si quien lo muestra no pasa ninguna, se usa el titulo del cartel.
    //  Para cambiar la espera de todo el juego alcanza con tocar este numero.
    // ============================================================
    public const float COOLDOWN_POR_DEFECTO = 8f;

    // Igual que la constante, pero se puede cambiar desde codigo en caliente.
    public static float SegundosDeCooldown = COOLDOWN_POR_DEFECTO;

    // Cuando se cerro por ultima vez cada cartel (en tiempo real).
    private static readonly Dictionary<string, float> ultimoCierre = new Dictionary<string, float>();

    // Que cartel es el que esta abierto ahora, para anotarlo al cerrarlo.
    private string claveActual;

    // El Reload Domain esta apagado, asi que los static sobreviven de una sesion de
    // Play a la siguiente. Sin esto, un cartel cerrado en la corrida anterior seguiria
    // en cooldown al volver a dar Play y no aparecería.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarEstaticas()
    {
        ultimoCierre.Clear();
        instance = null;
    }

    // Devuelve false si el cartel NO se mostro porque todavia esta en cooldown.
    // 'clave' identifica al cartel (si va vacia se usa el titulo) y 'cooldown'
    // permite una espera distinta para ese cartel (dejalo en -1 para la de siempre).
    public static bool Mostrar(Sprite imagen, Sprite imagenMarco, string textoTitulo, string textoDescripcion,
                               string textoAyuda, bool pausa, EstiloCartelHabilidad estilo,
                               string clave = null, float cooldown = -1f)
    {
        string laClave = string.IsNullOrEmpty(clave) ? textoTitulo : clave;
        if (EnCooldown(laClave, cooldown)) return false;

        if (instance == null) CrearInterfaz();
        instance.preguntando = false;
        instance.alAceptar = null;
        instance.alCancelar = null;
        instance.MostrarInterno(imagen, imagenMarco, textoTitulo, textoDescripcion, textoAyuda, pausa, estilo, laClave);
        return true;
    }

    // Igual que Mostrar, pero el cartel es una PREGUNTA: ESPACIO o ENTER aceptan,
    // ESC cancela. Lo usa la flor para preguntar si querés canjear tus puntos.
    public static bool Preguntar(Sprite imagen, Sprite imagenMarco, string textoTitulo, string textoDescripcion,
                                 string textoAyuda, bool pausa, EstiloCartelHabilidad estilo,
                                 System.Action alAceptar, System.Action alCancelar,
                                 string clave = null, float cooldown = -1f)
    {
        string laClave = string.IsNullOrEmpty(clave) ? textoTitulo : clave;
        if (EnCooldown(laClave, cooldown)) return false;

        if (instance == null) CrearInterfaz();

        instance.preguntando = true;
        instance.alAceptar = alAceptar;
        instance.alCancelar = alCancelar;
        instance.MostrarInterno(imagen, imagenMarco, textoTitulo, textoDescripcion, textoAyuda, pausa, estilo, laClave);
        return true;
    }

    // ¿Este cartel se cerro hace muy poco?
    private static bool EnCooldown(string clave, float cooldown)
    {
        if (string.IsNullOrEmpty(clave)) return false;

        float espera = cooldown >= 0f ? cooldown : SegundosDeCooldown;
        if (espera <= 0f) return false;

        float cuando;
        if (!ultimoCierre.TryGetValue(clave, out cuando)) return false;

        return Time.unscaledTime - cuando < espera;
    }

    // Borra la espera de un cartel para que pueda volver a salir ya mismo. Sirve
    // cuando algo cambio de verdad (por ejemplo, ya juntaste los puntos que faltaban)
    // y no queremos hacer esperar al jugador al pedo.
    public static void OlvidarCooldown(string clave)
    {
        if (!string.IsNullOrEmpty(clave)) ultimoCierre.Remove(clave);
    }

    // ¿Hay un cartel abierto ahora mismo? (para no abrir dos a la vez)
    public static bool HayUnoAbierto => instance != null && instance.abierto;

    private bool preguntando;
    private System.Action alAceptar;
    private System.Action alCancelar;

    private static void CrearInterfaz()
    {
        GameObject root = new GameObject("CartelHabilidadUI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CartelHabilidadUI));
        DontDestroyOnLoad(root);

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        instance = root.GetComponent<CartelHabilidadUI>();
        instance.ConstruirPanel(root.transform);
    }

    private void ConstruirPanel(Transform canvasRoot)
    {
        panel = CrearObjetoUI("PanelCartelHabilidad", canvasRoot);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(900f, 520f);

        fondo = panel.AddComponent<Image>();
        fondo.color = Color.clear;

        borde = panel.AddComponent<Outline>();
        borde.effectColor = new Color(0.55f, 0.9f, 1f, 0.9f);
        borde.effectDistance = new Vector2(4f, -4f);

        marco = CrearImagen("MarcoCartel", panel.transform, Vector2.zero, panelRect.sizeDelta);
        marco.preserveAspect = false;

        icono = CrearImagen("ImagenHabilidad", panel.transform, new Vector2(0f, 90f), new Vector2(220f, 220f));
        titulo = CrearTexto("TituloHabilidad", panel.transform, new Vector2(0f, -65f), new Vector2(760f, 70f), 46f, FontStyles.Bold);
        descripcion = CrearTexto("DescripcionHabilidad", panel.transform, new Vector2(0f, -155f), new Vector2(760f, 105f), 27f, FontStyles.Normal);
        ayudaCerrar = CrearTexto("AyudaCerrar", panel.transform, new Vector2(0f, -225f), new Vector2(760f, 45f), 21f, FontStyles.Italic);
        ayudaCerrar.text = "Presiona ESPACIO o ENTER para continuar";

        panel.SetActive(false);
    }

    private void MostrarInterno(Sprite imagen, Sprite imagenMarco, string textoTitulo, string textoDescripcion, string textoAyuda, bool pausa, EstiloCartelHabilidad estilo, string clave)
    {
        claveActual = clave;
        estiloActivo = estilo ?? new EstiloCartelHabilidad();
        marco.sprite = imagenMarco;
        marco.gameObject.SetActive(imagenMarco != null);
        AplicarEstilo(estiloActivo);
        icono.sprite = imagen;
        icono.gameObject.SetActive(imagen != null);
        titulo.text = string.IsNullOrWhiteSpace(textoTitulo) ? "NUEVA HABILIDAD" : textoTitulo;
        descripcion.text = textoDescripcion ?? string.Empty;
        ayudaCerrar.text = string.IsNullOrWhiteSpace(textoAyuda)
            ? "Presiona ESPACIO o ENTER para continuar"
            : textoAyuda;

        pausarJuego = pausa;
        escalaAnterior = Time.timeScale;
        if (pausarJuego) Time.timeScale = 0f;

        abierto = true;
        abiertoDesde = Time.unscaledTime;
        panel.SetActive(true);

        // Cada cartel arranca de cero: nada salteado y todavia no se puede cerrar.
        saltearAnimacion = false;
        sePuedeCerrar = false;

        // Se arranca DESPUES de SetActive: una corrutina no corre en un objeto apagado.
        // La rutina se lanza SIEMPRE, aunque no haya tipeo, porque es la que respeta la
        // espera y la que habilita el cierre al final.
        if (tipeoCo != null) StopCoroutine(tipeoCo);
        tipeoCo = StartCoroutine(RutinaTipeo());
    }

    // ---------- Tipeo ----------

    // Revela el texto letra por letra.
    //
    // Se hace con maxVisibleCharacters y NO cortando el string: si fueramos agregando
    // letras al texto, TMP recalcularia los saltos de linea en cada paso y la ultima
    // palabra saltaria de renglon sola mientras se escribe. Con maxVisibleCharacters el
    // texto ya esta maquetado desde el principio y solo se va destapando.
    //
    // Todo en tiempo REAL (unscaled): el cartel casi siempre aparece con el juego pausado.
    private IEnumerator RutinaTipeo()
    {
        tipeando = true;

        // Interruptor general de las animaciones de texto. Aunque este apagado, la rutina
        // corre igual: es la que hace la espera y la que habilita el cierre al final.
        bool animar = estiloActivo.tipearTexto;

        bool tituloAnimado = animar && estiloActivo.entradaTitulo != null && estiloActivo.entradaTitulo.activa;
        bool ayudaAnimada = animar && estiloActivo.entradaAyuda != null && estiloActivo.entradaAyuda.activa;

        // Arranca todo tapado.
        PrepararParaTipear(titulo, animar && (estiloActivo.tipearTitulo || tituloAnimado));
        PrepararParaTipear(descripcion, animar);
        if (animar && (estiloActivo.mostrarAyudaAlTerminar || ayudaAnimada)) ayudaCerrar.maxVisibleCharacters = 0;

        if (estiloActivo.esperaAntesDeTipear > 0f)
        {
            yield return new WaitForSecondsRealtime(estiloActivo.esperaAntesDeTipear);
        }

        // 1) El TITULO entra letra por letra: cada una con fade y creciendo.
        if (tituloAnimado)
        {
            titulo.maxVisibleCharacters = int.MaxValue;
            yield return AnimacionDeTexto.EntrarPorLetra(titulo, estiloActivo.entradaTitulo, () => saltearAnimacion);
        }
        else if (estiloActivo.tipearTitulo)
        {
            yield return TipearUno(titulo);
        }
        else
        {
            titulo.maxVisibleCharacters = int.MaxValue;
        }

        // 2) La DESCRIPCION se escribe como maquina de escribir.
        if (animar) yield return TipearUno(descripcion);
        else descripcion.maxVisibleCharacters = int.MaxValue;

        // 3) Espera antes de habilitar el cierre. Aunque el jugador haya apretado para
        //    apurar los textos, esta espera se respeta igual: es el tiempo minimo que el
        //    cartel se queda en pantalla para que alcance a leerlo.
        if (estiloActivo.esperaAntesDelTextoDeCerrar > 0f)
        {
            yield return new WaitForSecondsRealtime(estiloActivo.esperaAntesDelTextoDeCerrar);
        }

        // 4) Recien ahora aparece el texto de cerrar, que es la señal de "ya podes salir".
        if (ayudaAnimada)
        {
            ayudaCerrar.maxVisibleCharacters = int.MaxValue;
            yield return AnimacionDeTexto.EntrarPorLetra(ayudaCerrar, estiloActivo.entradaAyuda, () => saltearAnimacion);
        }

        MostrarTodoElTexto();

        tipeando = false;
        sePuedeCerrar = true;
        tipeoCo = null;
    }

    // ---------- Entrada letra por letra (fade + zoom) ----------

    // Anima CADA letra por separado tocando la malla del texto: cada una aparece con
    // fade y va creciendo desde el tamaño inicial hasta el suyo. Como cada letra empieza
    // un poco despues que la anterior, el conjunto se lee como un barrido de izquierda
    // a derecha.
    //
    // Los espacios no tienen malla, asi que no se animan, pero SI ocupan su lugar en la
    // cuenta: por eso el barrido hace una pausita natural entre palabras.
    private IEnumerator TipearUno(TMP_Text texto)
    {
        // ForceMeshUpdate para que characterCount ya tenga el valor real de este texto.
        texto.ForceMeshUpdate();
        int total = texto.textInfo.characterCount;
        if (total <= 0) yield break;

        float porSegundo = Mathf.Max(1f, estiloActivo.letrasPorSegundo);
        float esperaPorLetra = 1f / porSegundo;
        int cadaCuantas = Mathf.Max(1, estiloActivo.sonarCadaCuantasLetras);

        for (int i = 1; i <= total; i++)
        {
            texto.maxVisibleCharacters = i;

            if (i % cadaCuantas == 0)
            {
                SonarTipeo();
            }

            yield return new WaitForSecondsRealtime(esperaPorLetra);

            // Apretaron para apurar: mostramos el resto de una.
            if (saltearAnimacion)
            {
                texto.maxVisibleCharacters = int.MaxValue;
                yield break;
            }
        }
    }

    private void PrepararParaTipear(TMP_Text texto, bool seTipea)
    {
        texto.ForceMeshUpdate();
        texto.maxVisibleCharacters = seTipea ? 0 : int.MaxValue;
    }

    // Destapa todo de una y deja los textos como si nunca los hubieramos animado:
    // sin letras deformadas, opacas y nitidas. La usa el final del tipeo y la tecla
    // de saltear. Sin esto, saltear a mitad de la entrada dejaria letras a medio
    // achicar o el texto borroso para siempre.
    private void MostrarTodoElTexto()
    {
        titulo.maxVisibleCharacters = int.MaxValue;
        descripcion.maxVisibleCharacters = int.MaxValue;
        ayudaCerrar.maxVisibleCharacters = int.MaxValue;

        titulo.ForceMeshUpdate();
        descripcion.ForceMeshUpdate();
        ayudaCerrar.ForceMeshUpdate();
    }

    private void SonarTipeo()
    {
        AudioClip clip = ClipDeTipeo();
        if (clip == null) return;

        if (audioTipeo == null)
        {
            audioTipeo = gameObject.AddComponent<AudioSource>();
            audioTipeo.playOnAwake = false;
        }

        // PlayOneShot deja que un clic se superponga con el anterior, asi que con UNA sola
        // AudioSource alcanza: no hacen falta tres.
        audioTipeo.pitch = 1f + Random.Range(-estiloActivo.variacionDeTono, estiloActivo.variacionDeTono);
        audioTipeo.PlayOneShot(clip, estiloActivo.volumenTipeo);
    }

    // Elige al azar uno de los sonidos de tipeo, evitando repetir el de la letra anterior
    // (con dos o mas clips, dos iguales seguidos se notan enseguida).
    private AudioClip ClipDeTipeo()
    {
        AudioClip[] lista = estiloActivo.sonidosTipeo;

        if (lista == null || lista.Length == 0) return estiloActivo.sonidoTipeo;

        if (lista.Length == 1) return lista[0];

        AudioClip elegido = null;
        for (int intento = 0; intento < 6; intento++)
        {
            AudioClip candidato = lista[Random.Range(0, lista.Length)];
            if (candidato == null) continue;

            elegido = candidato;
            if (candidato != ultimoClipDeTipeo) break;
        }

        ultimoClipDeTipeo = elegido;
        return elegido;
    }

    private void AplicarEstilo(EstiloCartelHabilidad estilo)
    {
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchoredPosition = estilo.posicionPanel;
        panelRect.sizeDelta = estilo.tamanoPanel;
        AplicarRect(marco.rectTransform, Vector2.zero, estilo.tamanoPanel);
        fondo.color = marco.sprite == null ? estilo.colorFondo : Color.clear;
        borde.effectColor = marco.sprite == null ? estilo.colorBorde : Color.clear;
        borde.effectDistance = estilo.grosorBorde;

        AplicarRect(icono.rectTransform, estilo.posicionImagen, estilo.tamanoImagen);
        AplicarTexto(titulo, estilo.tipografiaTitulo, estilo.tamanoTitulo, estilo.colorTitulo,
            estilo.posicionTitulo, estilo.cajaTitulo, estilo.estiloTitulo);

        // Los avisos como "PUNTOS INSUFICIENTES" son bastante mas largos que los
        // nombres de habilidades. Los mantenemos en una sola linea y dejamos que TMP
        // reduzca la fuente si hiciera falta, para que nunca invadan la ilustracion ni
        // bajen sobre la descripcion.
        titulo.textWrappingMode = TextWrappingModes.NoWrap;
        titulo.enableAutoSizing = true;
        titulo.fontSizeMin = Mathf.Min(28f, estilo.tamanoTitulo);
        titulo.fontSizeMax = estilo.tamanoTitulo;

        AplicarTexto(descripcion, estilo.tipografiaDescripcion, estilo.tamanoDescripcion, estilo.colorDescripcion,
            estilo.posicionDescripcion, estilo.cajaDescripcion, estilo.estiloDescripcion);
        AplicarTexto(ayudaCerrar, estilo.tipografiaAyuda, estilo.tamanoAyuda, estilo.colorAyuda,
            estilo.posicionAyuda, estilo.cajaAyuda, estilo.estiloAyuda);

    }

    private static void AplicarRect(RectTransform rect, Vector2 posicion, Vector2 tamano)
    {
        rect.anchoredPosition = posicion;
        rect.sizeDelta = tamano;
    }

    private static void AplicarTexto(TMP_Text texto, TMP_FontAsset fuente, float tamano, Color color,
        Vector2 posicion, Vector2 caja, FontStyles estilo)
    {
        if (fuente != null) texto.font = fuente;
        texto.fontSize = tamano;
        texto.color = color;
        texto.fontStyle = estilo;
        AplicarRect(texto.rectTransform, posicion, caja);
    }

    private void Update()
    {
        if (!abierto) return;

        // Permite editar posiciones, tamanos, tipografias y colores desde el Inspector
        // y ver el cambio inmediatamente mientras el cartel esta abierto.
        if (estiloActivo != null) AplicarEstilo(estiloActivo);

        if (Keyboard.current == null || Time.unscaledTime - abiertoDesde < 0.2f) return;

        bool acepta = Keyboard.current.spaceKey.wasPressedThisFrame ||
                      Keyboard.current.enterKey.wasPressedThisFrame ||
                      Keyboard.current.numpadEnterKey.wasPressedThisFrame;
        bool cancela = Keyboard.current.escapeKey.wasPressedThisFrame;

        bool apreto = acepta || cancela;
        if (!apreto) return;

        // Si es una PREGUNTA, la tecla decide: ESPACIO/ENTER acepta, ESC cancela.
        if (preguntando && sePuedeCerrar)
        {
            System.Action respuesta = acepta ? alAceptar : alCancelar;

            preguntando = false;
            alAceptar = null;
            alCancelar = null;

            Cerrar();
            respuesta?.Invoke();
            return;
        }

        // El cartel solo se puede cerrar cuando ya aparecio el texto de cerrar. Hasta
        // entonces la tecla APURA las animaciones, pero no saca el cartel: asi nadie lo
        // cierra de un manotazo sin haberlo leido.
        if (!sePuedeCerrar)
        {
            CompletarTipeo();
            return;
        }

        Cerrar();
    }

    // Termina el tipeo de golpe y muestra todo el texto.
    // Apura las animaciones de texto que esten corriendo.
    //
    // OJO: NO corta la corrutina. Si la cortaramos, la espera previa al texto de cerrar
    // nunca terminaria, sePuedeCerrar se quedaria en false y el cartel quedaria trabado
    // en pantalla para siempre. Lo unico que hacemos es levantar una bandera: las
    // animaciones la miran y terminan solas, y la corrutina sigue su curso normal.
    private void CompletarTipeo()
    {
        saltearAnimacion = true;
        MostrarTodoElTexto();
    }

    public void Cerrar()
    {
        if (!abierto) return;

        // La cuenta del cooldown arranca al CERRAR, no al abrir: lo que nos importa es
        // darle tiempo al jugador de irse del lugar despues de leerlo.
        if (!string.IsNullOrEmpty(claveActual)) ultimoCierre[claveActual] = Time.unscaledTime;

        if (tipeoCo != null)
        {
            StopCoroutine(tipeoCo);
            tipeoCo = null;
        }

        tipeando = false;
        saltearAnimacion = false;
        sePuedeCerrar = false;

        abierto = false;
        panel.SetActive(false);
        if (pausarJuego) Time.timeScale = escalaAnterior;
    }

    private static GameObject CrearObjetoUI(string nombre, Transform padre)
    {
        GameObject objeto = new GameObject(nombre, typeof(RectTransform));
        objeto.transform.SetParent(padre, false);
        return objeto;
    }

    private static Image CrearImagen(string nombre, Transform padre, Vector2 posicion, Vector2 tamano)
    {
        GameObject objeto = CrearObjetoUI(nombre, padre);
        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicion;
        rect.sizeDelta = tamano;
        Image imagen = objeto.AddComponent<Image>();
        imagen.preserveAspect = true;
        imagen.raycastTarget = false;
        return imagen;
    }

    private static TMP_Text CrearTexto(string nombre, Transform padre, Vector2 posicion, Vector2 tamano, float tamanoFuente, FontStyles estilo)
    {
        GameObject objeto = CrearObjetoUI(nombre, padre);
        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicion;
        rect.sizeDelta = tamano;

        TextMeshProUGUI texto = objeto.AddComponent<TextMeshProUGUI>();
        texto.alignment = TextAlignmentOptions.Center;
        texto.fontSize = tamanoFuente;
        texto.fontStyle = estilo;
        texto.color = Color.white;
        texto.textWrappingMode = TextWrappingModes.Normal;
        texto.raycastTarget = false;
        return texto;
    }
}
