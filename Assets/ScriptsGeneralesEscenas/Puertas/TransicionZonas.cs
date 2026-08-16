using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// ============================================================
//  TRANSICION ENTRE ZONAS (estilo Hollow Knight)
//
//  La secuencia, cuando la niña toca el trigger de una PuertaZona:
//    1. Se le cortan los inputs, pero SIGUE CAMINANDO sola hacia afuera.
//    2. Mientras camina, la pantalla se funde a negro.
//    3. Se carga la otra zona.
//    4. Se la ubica en la puerta de llegada (todavia en negro) mirando hacia adentro.
//    5. Camina sola hacia adentro mientras la pantalla se aclara.
//    6. Recupera el control.
//  Nunca queda quieta ni hay un corte seco.
//
//  NO hay que poner nada en las escenas: este objeto se crea SOLO al arrancar el
//  juego y sobrevive a los cambios de escena (igual que el AudioManager). El
//  rectangulo negro tambien se arma solo por codigo: tiene que sobrevivir al cambio
//  de escena, si viviera adentro de una escena se destruiria justo en el medio.
//
//  Los tiempos y velocidades se ajustan en el asset AjustesTransicion
//  (Assets/Resources/AjustesTransicion.asset). Ver la nota del equipo en ese archivo.
// ============================================================
public class TransicionZonas : MonoBehaviour
{
    public static TransicionZonas Instancia { get; private set; }

    // Lo mira la PuertaZona (y el Damageable, para que la niña no muera cruzando).
    public static bool EnCurso { get; private set; }

    // Momento a partir del cual una puerta puede volver a activarse.
    private static float bloqueadoHasta = 0f;

    // ¿Se puede cruzar una puerta ahora mismo?
    public static bool PuedeCruzar
    {
        get { return !EnCurso && Time.time >= bloqueadoHasta; }
    }

    // Lo mira el Damageable de la niña: si esta en true, los golpes no le entran.
    // Depende del check "invulnerableDuranteLaTransicion" del asset de ajustes.
    public static bool InvulnerablePorTransicion
    {
        get
        {
            if (!EnCurso) return false;
            if (Instancia == null || Instancia.ajustes == null) return true;
            return Instancia.ajustes.invulnerableDuranteLaTransicion;
        }
    }

    private AjustesTransicion ajustes;
    private Image negro;

    // Oscurecimiento por cercania a una puerta.
    private Image degradado;
    private float alphaDegradado = 0f;
    private LadoOscuro ladoDelDegradado = LadoOscuro.Izquierda;
    private Sprite spriteGenerado;
    private PuertaZona ultimaGanadora;

    // Lo que QUEREMOS dibujar vs lo que ya esta dibujado: cuando no coinciden,
    // se regenera la imagen del degradado.
    private float anchoDeseado = 0.45f;
    private float anchoUsado = -1f;
    private float durezaUsada = -1f;
    private LadoOscuro ladoUsado = LadoOscuro.Automatico; // "todavia no dibuje ninguno"

    // Cache de la escena actual (para no andar buscando las puertas todos los frames).
    private PuertaZona[] puertasDeLaEscena;
    private PlayerController jugador;
    private string escenaCacheada = "";

    // Al cargar una escena la sombra se pone de una, sin transicion.
    private bool ponerLaSombraDeGolpe = true;

    // Datos que viajan de una escena a la otra.
    private string idDeLlegada = "";
    private PuertaZona puertaDeLlegada;
    private PlayerController playerEnLaZonaNueva;
    private bool escenaLista = false;

    // Con Reload Domain desactivado los static se arrastran entre sesiones de Play.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarStatics()
    {
        Instancia = null;
        EnCurso = false;
        bloqueadoHasta = 0f;
    }

    // Se crea solo al arrancar el juego, en cualquier escena. No hay que ponerlo a mano.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCrear()
    {
        if (Instancia != null) return;

        GameObject go = new GameObject("TransicionZonas");
        go.AddComponent<TransicionZonas>();
    }

    private void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        Instancia = this;
        DontDestroyOnLoad(gameObject);

        CargarAjustes();
        CrearPantallaNegra();
    }

    private void CargarAjustes()
    {
        ajustes = Resources.Load<AjustesTransicion>("AjustesTransicion");

        if (ajustes == null)
        {
            // Sin el asset igual funciona, con los valores por defecto.
            ajustes = ScriptableObject.CreateInstance<AjustesTransicion>();
            Debug.Log("TransicionZonas: no encontre Assets/Resources/AjustesTransicion.asset, " +
                      "uso los valores por defecto. Para poder ajustarlos, corre " +
                      "'Not Home > Puertas > Configurar desde los nombres'.");
        }
    }

    // El rectangulo negro que tapa la pantalla, armado por codigo.
    private void CrearPantallaNegra()
    {
        GameObject canvasGo = new GameObject("CanvasTransicion");
        canvasGo.transform.SetParent(transform, false);

        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9000; // por encima de todo: HUD, pausa, cinematicas

        // El DEGRADADO va primero: se dibuja DEBAJO del negro de la transicion,
        // asi cuando arranca el fundido lo tapa por completo.
        GameObject degradadoGo = new GameObject("DegradadoCercania");
        degradadoGo.transform.SetParent(canvasGo.transform, false);

        degradado = degradadoGo.AddComponent<Image>();
        degradado.color = new Color(0f, 0f, 0f, 0f);
        degradado.raycastTarget = false;
        EstirarATodaLaPantalla(degradado.rectTransform);

        GameObject negroGo = new GameObject("Negro");
        negroGo.transform.SetParent(canvasGo.transform, false);

        negro = negroGo.AddComponent<Image>();
        negro.color = new Color(0f, 0f, 0f, 0f); // arranca transparente
        negro.raycastTarget = false;             // que no tape los clicks de la UI

        EstirarATodaLaPantalla(negro.rectTransform);
    }

    private void EstirarATodaLaPantalla(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // ---------- Oscurecimiento por cercania ----------

    private void Update()
    {
        ActualizarOscurecimiento();
    }

    private void ActualizarOscurecimiento()
    {
        if (degradado == null || ajustes == null) return;

        float objetivo = 0f;
        PuertaZona ganadora = null;

        // La deteccion corre SIEMPRE, tambien durante la transicion. Gracias a eso, al
        // llegar a una zona nueva la niña ya aparece parada en la zona de degrade y la
        // sombra esta puesta desde el primer frame: se va disolviendo mientras camina
        // hacia adentro, en vez de aparecer de la nada cuando volves a acercarte.
        if (ajustes.oscurecerAlAcercarse)
        {
            RefrescarCacheSiCambioLaEscena();

            if (jugador != null && puertasDeLaEscena != null)
            {
                Vector2 posJugador = jugador.transform.position;

                // Gana la puerta que mas oscurezca (o sea, la mas cerca).
                foreach (PuertaZona p in puertasDeLaEscena)
                {
                    if (p == null) continue;

                    float valor = p.OscuridadSegunDistancia(posJugador);
                    if (valor > objetivo)
                    {
                        objetivo = valor;
                        ganadora = p;
                    }
                }
            }
        }

        // Que puerta manda la FORMA de la sombra (lado y largo).
        //
        // Si es la MISMA de antes, los cambios se aplican al toque: asi podes mover los
        // valores en el Inspector con el juego corriendo y ver el resultado en vivo.
        // Si es OTRA puerta, esperamos a que la sombra este apagada para cambiar, si no
        // se veria saltar de un borde de la pantalla al otro.
        if (ganadora != null && (ganadora == ultimaGanadora || alphaDegradado <= 0.02f))
        {
            ultimaGanadora = ganadora;
            ladoDelDegradado = ganadora.LadoResuelto;
            anchoDeseado = ganadora.largoDelNegro > 0f ? ganadora.largoDelNegro : ajustes.anchoDelDegradado;
        }

        PrepararSpriteDelDegradado();

        if (ponerLaSombraDeGolpe)
        {
            // Recien cargo una escena: la sombra tiene que estar YA puesta, sin subir de a
            // poco. Si no, al aparecer se veria "encenderse" en vez de estar desde el arranque.
            alphaDegradado = objetivo;
            ponerLaSombraDeGolpe = false;
        }
        else
        {
            // Acompaña suave en vez de pegar saltos.
            float velocidad = Mathf.Max(0.01f, ajustes.suavizadoDelOscurecimiento);
            alphaDegradado = Mathf.MoveTowards(alphaDegradado, objetivo, velocidad * Time.unscaledDeltaTime);
        }

        Color c = ajustes.colorDelOscurecimiento;
        c.a = alphaDegradado;
        degradado.color = c;
    }

    private void RefrescarCacheSiCambioLaEscena()
    {
        string actual = SceneManager.GetActiveScene().name;
        if (actual == escenaCacheada && jugador != null && puertasDeLaEscena != null) return;

        escenaCacheada = actual;
        puertasDeLaEscena = Object.FindObjectsByType<PuertaZona>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        jugador = BuscarPlayer();
        ponerLaSombraDeGolpe = true;
    }

    // Arma la imagen del degradado. Si el equipo puso su propia imagen en los ajustes, usamos esa;
    // si no, la generamos por codigo: una tirita de pixeles que va de opaco (pegado al borde)
    // a transparente. Al estirarse a toda la pantalla queda un degradado perfecto y no depende
    // de ningun asset ni de post-procesado (importante para que ande en itch.io).
    private void PrepararSpriteDelDegradado()
    {
        if (ajustes.spriteDelDegradado != null)
        {
            if (degradado.sprite != ajustes.spriteDelDegradado) degradado.sprite = ajustes.spriteDelDegradado;
            return;
        }

        // Solo regeneramos la imagen si cambio el lado, el largo o la dureza.
        if (spriteGenerado != null &&
            ladoUsado == ladoDelDegradado &&
            anchoUsado == anchoDeseado &&
            durezaUsada == ajustes.durezaDelDegradado) return;

        ladoUsado = ladoDelDegradado;
        anchoUsado = anchoDeseado;
        durezaUsada = ajustes.durezaDelDegradado;

        spriteGenerado = GenerarDegradado(ladoDelDegradado, anchoUsado, durezaUsada);
        degradado.sprite = spriteGenerado;
    }

    private Sprite GenerarDegradado(LadoOscuro lado, float ancho, float dureza)
    {
        const int PASOS = 128;

        if (lado == LadoOscuro.PantallaEntera)
        {
            Texture2D plano = new Texture2D(1, 1);
            plano.SetPixel(0, 0, Color.white);
            plano.Apply();
            return Sprite.Create(plano, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        }

        bool horizontal = (lado == LadoOscuro.Izquierda || lado == LadoOscuro.Derecha);
        bool desdeElPrincipio = (lado == LadoOscuro.Izquierda || lado == LadoOscuro.Abajo);

        Texture2D tex = horizontal ? new Texture2D(PASOS, 1) : new Texture2D(1, PASOS);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        for (int i = 0; i < PASOS; i++)
        {
            float f = (i + 0.5f) / PASOS;                         // 0 = borde izquierdo / abajo
            float distanciaAlBorde = desdeElPrincipio ? f : 1f - f;

            // 1 pegado al borde, 0 al terminar el "largo del negro".
            float t = 1f - Mathf.Clamp01(distanciaAlBorde / Mathf.Max(ancho, 0.001f));

            // La dureza curva ese desvanecido: 1 = recto, mas alto = el negro se
            // concentra pegado al borde, mas bajo = se estira hacia el centro.
            float alpha = Mathf.Pow(t, Mathf.Max(0.05f, dureza));

            Color pixel = new Color(1f, 1f, 1f, alpha);
            if (horizontal) tex.SetPixel(i, 0, pixel);
            else            tex.SetPixel(0, i, pixel);
        }

        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
    }

    // ---------- La transicion ----------

    // La llama la PuertaZona cuando la niña la toca.
    public void Cruzar(PuertaZona salida)
    {
        if (!PuedeCruzar || salida == null) return;
        StartCoroutine(Rutina(salida));
    }

    private IEnumerator Rutina(PuertaZona salida)
    {
        EnCurso = true;

        PlayerController pc = BuscarPlayer();
        if (pc == null)
        {
            Debug.LogWarning("TransicionZonas: no encontre al player (tag 'Player').");
            EnCurso = false;
            yield break;
        }

        idDeLlegada = salida.idPuertaDestino;
        escenaLista = false;
        puertaDeLlegada = null;
        playerEnLaZonaNueva = null;

        TomarElControl(pc);

        Diagnostico("SALE por '" + salida.id + "' hacia " + salida.escenaDestino +
                    ", va a llegar a '" + salida.idPuertaDestino + "'");

        // 1) Camina hacia afuera MIENTRAS la pantalla se va a negro.
        StartCoroutine(Fundir(1f, ajustes.fadeANegro));
        yield return Caminar(pc, salida.VectorSalida, salida.segundosCaminandoAlSalir, "SALIDA por '" + salida.id + "'");

        // Por si la caminata fue mas corta que el fundido: nos aseguramos el negro total.
        yield return Fundir(1f, 0.1f);
        yield return new WaitForSeconds(ajustes.esperaEnNegro);

        // 2) Cargar la zona destino. La ubicacion de la niña se hace en AlCargarEscena,
        //    que corre ANTES del primer frame de la escena nueva: asi la camara de
        //    Cinemachine ya la encuentra en su lugar y no hace ningun paneo raro.
        SceneManager.sceneLoaded += AlCargarEscena;
        SceneManager.LoadScene(salida.escenaDestino);

        // Esperamos a que la escena este cargada y la niña ubicada (con un limite,
        // para no quedarnos colgados en negro si algo salio mal).
        for (int i = 0; i < 120 && !escenaLista; i++)
        {
            yield return null;
        }

        if (!escenaLista)
        {
            SceneManager.sceneLoaded -= AlCargarEscena;
            Debug.LogError("[Puertas] No se pudo completar la llegada a '" + salida.escenaDestino +
                           "' (puerta '" + salida.idPuertaDestino + "'). La niña queda donde estaba puesta en la escena.");
        }

        // 3) Camina hacia adentro mientras la pantalla se aclara.
        PlayerController pcNuevo = playerEnLaZonaNueva != null ? playerEnLaZonaNueva : BuscarPlayer();

        StartCoroutine(Fundir(0f, ajustes.fadeDesdeNegro));

        if (puertaDeLlegada != null && pcNuevo != null)
        {
            yield return Caminar(pcNuevo, puertaDeLlegada.VectorEntrada, puertaDeLlegada.segundosCaminandoAlEntrar,
                                 "LLEGADA a '" + puertaDeLlegada.id + "'");
        }
        else
        {
            Debug.LogWarning("[Puertas] No camino al llegar" +
                             (puertaDeLlegada == null ? ": no encontre la puerta de llegada." : ": no encontre al player."));
            yield return new WaitForSeconds(ajustes.fadeDesdeNegro);
        }

        // 4) Devolver el control.
        DevolverElControl(pcNuevo);

        bloqueadoHasta = Time.time + ajustes.graciaEntrePuertas;
        EnCurso = false;
    }

    // Corre apenas termina de cargar la escena nueva, antes de su primer frame.
    private void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        SceneManager.sceneLoaded -= AlCargarEscena;

        PuertaZona llegada = null;
        PlayerController pc = null;

        // Todo esto va dentro de un try: si algo explota aca (que corre "por afuera" de la
        // corrutina), sin esto la transicion quedaria colgada en negro para siempre.
        try
        {
            llegada = PuertaZona.Buscar(idDeLlegada);
            pc = BuscarPlayer();

            if (llegada == null)
            {
                Debug.LogError("[Puertas] En la escena '" + escena.name + "' no existe ninguna puerta con el id '" +
                               idDeLlegada + "'. Corre 'Not Home > Puertas > Revisar puertas'.");
            }
            if (pc == null)
            {
                Debug.LogError("[Puertas] En la escena '" + escena.name + "' no encontre al player (tag 'Player').");
            }

            if (llegada != null && pc != null)
            {
                TomarElControl(pc);

                Vector3 destino = UbicacionDeLlegada(llegada, pc);
                pc.transform.position = destino;

                if (pc.rb != null)
                {
                    pc.rb.linearVelocity = Vector2.zero;
                    pc.rb.angularVelocity = 0f;
                }

                // Que mire hacia adentro del mapa.
                Vector2 haciaAdentro = llegada.VectorEntrada;
                if (haciaAdentro.x != 0f && pc.movement != null)
                {
                    pc.movement.SetFacing(haciaAdentro.x);
                }

                Diagnostico("LLEGA a '" + llegada.id + "' en " + escena.name +
                            ", aparece en " + destino + " y va a caminar hacia " + NombreDeDireccion(haciaAdentro));
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Puertas] Exploto al llegar a '" + escena.name + "': " + e);
        }

        puertaDeLlegada = llegada;
        playerEnLaZonaNueva = pc;

        // Refrescamos el cache de la escena nueva de una, asi la sombra de cercania ya
        // esta calculada (y puesta de golpe) en el primer frame de la zona nueva.
        escenaCacheada = escena.name;
        puertasDeLaEscena = Object.FindObjectsByType<PuertaZona>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        jugador = pc;
        ponerLaSombraDeGolpe = true;

        escenaLista = true;
    }

    // ---------- Piezas ----------

    // Donde plantamos a la niña al llegar.
    //
    // El marcador de la puerta es una barra vertical: su CENTRO esta flotando a media
    // altura del hueco. Si la dejamos ahi, aparece en el aire y se cae (y encima se le
    // va el tiempo de caminata cayendo). Asi que tiramos un rayo hacia abajo, buscamos
    // el piso y la apoyamos ahi.
    //
    // Excepcion: las puertas VERTICALES (por las que se cae o se sube). En esas la niña
    // TIENE que entrar por el aire, asi que se respeta la altura del marcador.
    private Vector3 UbicacionDeLlegada(PuertaZona llegada, PlayerController pc)
    {
        Vector3 origen = llegada.PosicionDeAparicion;

        if (!ajustes.pegarAlPisoAlLlegar) return origen;
        if (llegada.VectorEntrada.y != 0f) return origen; // puerta vertical: entra cayendo o subiendo

        // Usamos el mismo layer de piso que usa la niña para saber si esta parada.
        LayerMask mascaraPiso = pc.jump != null ? pc.jump.groundMask : ~0;

        RaycastHit2D golpe = Physics2D.Raycast(origen + Vector3.up * 0.1f, Vector2.down,
                                               ajustes.distanciaMaximaAlPiso, mascaraPiso);
        if (golpe.collider == null)
        {
            Debug.LogWarning("TransicionZonas: no encontre piso debajo de la puerta '" + llegada.id +
                             "'. La niña va a aparecer flotando. Podes usar el campo 'Punto De Aparicion' " +
                             "de esa puerta para elegir el lugar a mano.", llegada);
            return origen;
        }

        // La apoyamos justo arriba del piso, segun el alto de su propio collider.
        float mitadDelAlto = 0.5f;
        Collider2D colPlayer = pc.GetComponent<Collider2D>();
        if (colPlayer != null) mitadDelAlto = colPlayer.bounds.extents.y;

        return new Vector3(origen.x, golpe.point.y + mitadDelAlto + 0.02f, origen.z);
    }

    // Mueve a la niña sola, sin inputs, con la animacion que corresponda.
    private IEnumerator Caminar(PlayerController pc, Vector2 direccion, float segundos, string paraElLog)
    {
        if (pc == null || segundos <= 0f) yield break;

        float velocidad = ajustes.velocidadCaminata > 0f ? ajustes.velocidadCaminata : pc.speed;
        Vector3 posicionInicial = pc.transform.position;

        Diagnostico(paraElLog + ": camina hacia " + NombreDeDireccion(direccion) +
                    " durante " + segundos + "s a velocidad " + velocidad);

        // Que mire hacia donde camina.
        if (direccion.x != 0f && pc.movement != null)
        {
            pc.movement.SetFacing(direccion.x);
        }

        // Envion inicial para las puertas por las que se sube.
        if (direccion.y > 0f && pc.rb != null)
        {
            pc.rb.linearVelocity = new Vector2(0f, ajustes.impulsoAlSubir);
        }

        float t = 0f;
        while (t < segundos)
        {
            t += Time.deltaTime;

            if (pc.rb != null)
            {
                if (direccion.x != 0f)
                {
                    // Horizontal: mantiene la velocidad de caminata y deja que la gravedad haga lo suyo.
                    pc.rb.linearVelocity = new Vector2(direccion.x * velocidad, pc.rb.linearVelocity.y);
                    PonerAnim(pc, ajustes.animCorrer);
                }
                else if (direccion.y < 0f)
                {
                    // Cae por el agujero.
                    pc.rb.linearVelocity = new Vector2(0f, Mathf.Min(pc.rb.linearVelocity.y, -ajustes.empujeAlCaer));
                    PonerAnim(pc, ajustes.animCaer);
                }
                else
                {
                    // Sube: mientras va para arriba, animacion de salto; cuando frena, de caida.
                    PonerAnim(pc, pc.rb.linearVelocity.y > 0f ? ajustes.animSaltar : ajustes.animCaer);
                }
            }

            yield return null;
        }

        // Si camino contra una pared, no se movio de lugar. Casi siempre significa que la
        // Direccion Salida de esa puerta esta al reves.
        if (ajustes.avisarSiNoSeMueve && direccion.x != 0f)
        {
            float recorrido = Mathf.Abs(pc.transform.position.x - posicionInicial.x);
            if (recorrido < 0.1f)
            {
                Debug.LogWarning("[Puertas] " + paraElLog + ": la niña NO se movio (camino contra algo). " +
                                 "Revisa la 'Direccion Salida' de esa puerta, lo mas probable es que este al reves. " +
                                 "Tambien puede ser que este apareciendo pegada a una pared.");
            }
        }
    }

    private string NombreDeDireccion(Vector2 d)
    {
        if (d.x > 0f) return "DERECHA";
        if (d.x < 0f) return "IZQUIERDA";
        if (d.y > 0f) return "ARRIBA";
        return "ABAJO";
    }

    private void Diagnostico(string mensaje)
    {
        if (ajustes != null && ajustes.mostrarDiagnosticoEnConsola)
        {
            Debug.Log("[Puertas] " + mensaje);
        }
    }

    // Apaga los controles y frena en seco (el PlayerController desactivado corta los inputs).
    private void TomarElControl(PlayerController pc)
    {
        if (pc == null) return;

        // La invulnerabilidad la aplica el Damageable de la niña mirando
        // TransicionZonas.InvulnerablePorTransicion (no hace falta apagar nada aca).
        pc.enabled = false;
    }

    private void DevolverElControl(PlayerController pc)
    {
        if (pc == null) return;

        // Que no salga disparada con la velocidad de la caminata automatica.
        if (pc.rb != null)
        {
            pc.rb.linearVelocity = new Vector2(0f, pc.rb.linearVelocity.y);
        }

        PonerAnim(pc, ajustes.animIdle);
        pc.enabled = true;
    }

    private void PonerAnim(PlayerController pc, int valor)
    {
        if (pc != null && pc.animPlayer != null)
        {
            pc.animPlayer.SetInteger("stateAnim", valor);
        }
    }

    private IEnumerator Fundir(float alphaObjetivo, float duracion)
    {
        if (negro == null) yield break;

        float inicial = negro.color.a;

        if (duracion <= 0f)
        {
            PonerAlpha(alphaObjetivo);
            yield break;
        }

        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            PonerAlpha(Mathf.Lerp(inicial, alphaObjetivo, t / duracion));
            yield return null;
        }

        PonerAlpha(alphaObjetivo);
    }

    private void PonerAlpha(float a)
    {
        Color c = negro.color;
        c.a = a;
        negro.color = c;
    }

    // Buscamos a la niña por su COMPONENTE, no por el tag.
    //
    // Buscar por tag es fragil: si en una escena quedo otro objeto con el tag "Player"
    // por error (nos paso con un cuadrado del piso en Zona 4), Unity puede devolver ese
    // y todo el sistema se rompe sin motivo aparente. El PlayerController, en cambio,
    // lo tiene una sola cosa en el juego: la niña.
    private PlayerController BuscarPlayer()
    {
        PlayerController pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc != null) return pc;

        // Plan B: por tag, por si algun dia el player estuviera desactivado al cargar.
        GameObject go = GameObject.FindGameObjectWithTag("Player");
        if (go == null) return null;

        pc = go.GetComponent<PlayerController>();
        if (pc == null) pc = go.GetComponentInParent<PlayerController>();
        if (pc == null) pc = go.GetComponentInChildren<PlayerController>();

        return pc;
    }
}
