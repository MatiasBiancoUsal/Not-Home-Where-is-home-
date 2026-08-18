using UnityEngine;

// Que borde de la pantalla se va poniendo oscuro al acercarse a una puerta.
public enum LadoOscuro
{
    Automatico, // el lado OPUESTO a la puerta (si la puerta esta a la izquierda, oscurece la derecha)
    Izquierda,
    Derecha,
    Arriba,
    Abajo,
    PantallaEntera
}

// Hacia donde CAMINA la niña cuando SALE por esta puerta.
// Al LLEGAR por esta misma puerta, camina justo al reves (entra al mapa).
public enum DireccionPuerta
{
    Derecha,
    Izquierda,
    Arriba,
    Abajo
}

// ============================================================
//  UNA PUERTA ENTRE ZONAS
//  Va EN el marcador de la puerta (el objeto que se llama "PUERTA Z2 ARRIBA --> Z3").
//
//  Los tres campos de IDENTIDAD los completa solo el comando
//  "Not Home > Puertas > Configurar desde los nombres", leyendo el nombre del objeto.
//  No hace falta escribirlos a mano (pero se pueden corregir si hiciera falta).
//
//  Lo que SI se ajusta a mano es todo lo de abajo: hacia donde camina, cuanto,
//  y donde aparece exactamente la niña al llegar.
//
//  REGLA DE ORO: por cada puerta A --> B tiene que existir la puerta B --> A
//  en la otra escena. El comando "Revisar puertas" lo verifica.
// ============================================================
// OJO: NO usar [RequireComponent(typeof(Collider2D))]. Collider2D es una clase abstracta,
// asi que Unity no puede agregarlo solo y AddComponent<PuertaZona>() FALLA en cualquier
// objeto que no tenga collider. El comando de Puertas se encarga de ponerle el BoxCollider2D.
public class PuertaZona : MonoBehaviour
{
    [Header("Identidad (la completa sola el comando de Puertas)")]
    [Tooltip("Como se llama ESTA puerta. Tiene que ser unica dentro de la zona. Ej: 'Z2 ARRIBA'.")]
    public string id;
    [Tooltip("A que escena lleva. Ej: 'Zona 3'.")]
    public string escenaDestino;
    [Tooltip("El id de la puerta EXACTA de la otra zona donde aparece la niña. Ej: 'Z3'.")]
    public string idPuertaDestino;

    [Header("Ajustes de esta puerta")]
    [Tooltip("Hacia donde camina la niña al SALIR por aca. Al LLEGAR camina al reves (hacia adentro del mapa).")]
    public DireccionPuerta direccionSalida = DireccionPuerta.Derecha;
    [Tooltip("Segundos que camina sola hacia afuera antes de cambiar de zona.")]
    public float segundosCaminandoAlSalir = 0.7f;
    [Tooltip("Segundos que camina sola hacia adentro al llegar a la zona nueva.")]
    public float segundosCaminandoAlEntrar = 0.7f;

    [Header("Donde aparece la niña al llegar")]
    [Tooltip("Opcional. Si lo dejas vacio, aparece en la posicion de este mismo objeto. " +
             "Poner un objeto vacio como hijo si queres correrla un poco (ej: mas adentro del mapa).")]
    public Transform puntoDeAparicion;

    [Header("Oscurecimiento al acercarse")]
    [Tooltip("Al acercarse a esta puerta, un borde de la pantalla se va poniendo oscuro (estilo Hollow Knight).")]
    public bool oscurecerAlAcercarse = true;
    [Tooltip("Que borde se oscurece. 'Automatico' oscurece el lado OPUESTO a la puerta. " +
             "Si te queda al reves de lo que imaginabas, elegi el lado a mano.")]
    public LadoOscuro ladoQueSeOscurece = LadoOscuro.Automatico;
    [Tooltip("Distancia a la que EMPIEZA a oscurecerse (circulo grande del gizmo).")]
    public float distanciaEmpieza = 14f;
    [Tooltip("Distancia a la que llega al MAXIMO de oscuridad (circulo chico del gizmo).")]
    public float distanciaMaximo = 2f;
    [Range(0f, 1f)]
    [Tooltip("Cuanto se llega a oscurecer como maximo. 1 = negro total.")]
    public float opacidadMaxima = 0.85f;
    [Range(0f, 1f)]
    [Tooltip("LARGO del negro SOLO para esta puerta: cuanta pantalla ocupa, de 0 a 1. " +
             "Dejalo en 0 para usar el valor global del asset AjustesTransicion.")]
    public float largoDelNegro = 0f;

    [Header("Otros")]
    [Tooltip("Destildar para BLOQUEAR esta puerta (no se puede salir por aca, pero si llegar).")]
    public bool activa = true;
    [Tooltip("Esconde el sprite del marcador cuando corre el juego. En el editor lo seguis viendo.")]
    public bool ocultarSpriteEnElJuego = true;
    [Tooltip("Largo de la flecha celeste que se dibuja en la ventana Scene. Subilo si te queda muy chica para verla.")]
    public float largoFlecha = 3f;

    private void Awake()
    {
        if (ocultarSpriteEnElJuego)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = false;
        }

        // Sin un collider en Is Trigger, la puerta nunca se entera de que la niña paso.
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            Debug.LogWarning("PuertaZona: '" + name + "' no tiene ningun Collider2D, no va a funcionar. " +
                             "Corre 'Not Home > Puertas > Configurar desde los nombres'.", this);
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning("PuertaZona: al collider de '" + name + "' le falta tildar Is Trigger. " +
                             "Asi la niña choca contra la puerta en vez de cruzarla.", this);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!activa) return;
        if (!other.CompareTag("Player")) return;

        // No dispararse durante una transicion (al llegar, la niña aparece DENTRO
        // del trigger de la puerta de llegada: sin esto rebotaria de vuelta).
        if (!TransicionZonas.PuedeCruzar) return;

        if (string.IsNullOrEmpty(escenaDestino) || string.IsNullOrEmpty(idPuertaDestino))
        {
            Debug.LogWarning("PuertaZona: '" + name + "' no esta configurada. " +
                             "Corre 'Not Home > Puertas > Configurar desde los nombres'.", this);
            return;
        }

        if (TransicionZonas.Instancia != null)
        {
            TransicionZonas.Instancia.Cruzar(this);
        }
    }

    // Donde aparece la niña cuando LLEGA por esta puerta.
    public Vector3 PosicionDeAparicion
    {
        get { return puntoDeAparicion != null ? puntoDeAparicion.position : transform.position; }
    }

    // Hacia donde camina al SALIR.
    public Vector2 VectorSalida
    {
        get
        {
            switch (direccionSalida)
            {
                case DireccionPuerta.Izquierda: return Vector2.left;
                case DireccionPuerta.Arriba:    return Vector2.up;
                case DireccionPuerta.Abajo:     return Vector2.down;
                default:                        return Vector2.right;
            }
        }
    }

    // Hacia donde camina al LLEGAR: al reves que la salida (entra al mapa).
    public Vector2 VectorEntrada
    {
        get { return -VectorSalida; }
    }

    // Que borde se oscurece de verdad, resolviendo el "Automatico".
    public LadoOscuro LadoResuelto
    {
        get
        {
            if (ladoQueSeOscurece != LadoOscuro.Automatico) return ladoQueSeOscurece;

            // Automatico: el lado CONTRARIO a la puerta.
            switch (direccionSalida)
            {
                case DireccionPuerta.Izquierda: return LadoOscuro.Derecha;
                case DireccionPuerta.Derecha:   return LadoOscuro.Izquierda;
                case DireccionPuerta.Arriba:    return LadoOscuro.Abajo;
                default:                        return LadoOscuro.Arriba;
            }
        }
    }

    // Cuanta oscuridad corresponde segun lo cerca que este la niña (0 = nada, 1 = el maximo).
    public float OscuridadSegunDistancia(Vector2 posicionDelJugador)
    {
        if (!oscurecerAlAcercarse || !activa) return 0f;

        float distancia = Vector2.Distance(posicionDelJugador, PosicionDeAparicion);
        float t = Mathf.InverseLerp(distanciaEmpieza, distanciaMaximo, distancia);

        return t * opacidadMaxima;
    }

    // Busca en la escena YA CARGADA la puerta con ese id.
    public static PuertaZona Buscar(string idBuscado)
    {
        if (string.IsNullOrEmpty(idBuscado)) return null;

        PuertaZona[] todas = Object.FindObjectsByType<PuertaZona>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (PuertaZona p in todas)
        {
            if (p.id == idBuscado) return p;
        }
        return null;
    }

    // FLECHA DE AYUDA en la ventana Scene: sale desde donde aparece la niña y apunta
    // hacia donde CAMINA AL SALIR. La bolita llena es la punta.
    // Si la flecha apunta hacia AFUERA del mapa, la direccion esta bien puesta.
    private void OnDrawGizmos()
    {
        Vector3 desde = PosicionDeAparicion;
        Vector3 punta = desde + (Vector3)VectorSalida * largoFlecha;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(desde, 0.3f); // aca aparece la niña cuando LLEGA
        Gizmos.DrawLine(desde, punta);
        Gizmos.DrawSphere(punta, 0.35f);    // hacia aca camina cuando SE VA

        // En las puertas horizontales la niña no aparece exactamente en el circulo:
        // cae hasta el primer piso que haya JUSTO DEBAJO. Esta linea marca por donde baja.
        if (VectorEntrada.y == 0f)
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
            Gizmos.DrawLine(desde, desde + Vector3.down * 30f);
        }
    }

    // Los circulos del oscurecimiento SOLO se dibujan con la puerta seleccionada,
    // asi no queda la escena tapada de circulos.
    private void OnDrawGizmosSelected()
    {
        if (!oscurecerAlAcercarse) return;

        Vector3 centro = PosicionDeAparicion;

        // Donde EMPIEZA a oscurecerse.
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.6f);
        Gizmos.DrawWireSphere(centro, distanciaEmpieza);

        // Donde llega al MAXIMO.
        Gizmos.color = new Color(1f, 0f, 0f, 0.8f);
        Gizmos.DrawWireSphere(centro, distanciaMaximo);
    }
}
