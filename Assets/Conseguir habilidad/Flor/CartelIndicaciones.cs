using System.Collections;
using UnityEngine;

// ============================================================
//  CARTEL DE INDICACIONES
//
//  Muestra el MISMO cartel que usan las flores de habilidad, pero desde donde quieras:
//  al terminar una cinematica, al pisar un trigger, desde un boton, etc.
//
//  El caso para el que se hizo: cuando termina la cinematica del osito, avisarle al
//  jugador que ya puede atacar y con que tecla.
//
//  Va en cualquier objeto de la escena (uno vacio alcanza).
//
//  Como se dispara (cualquiera de las dos):
//    a) Arrastrar la cinematica al campo "Cinematica": se engancha solo al final.
//    b) Desde el evento "Al Terminar" de la CinematicaFrames (o cualquier UnityEvent),
//       arrastrar este objeto y elegir CartelIndicaciones > Mostrar().
// ============================================================
public class CartelIndicaciones : MonoBehaviour
{
    [Header("Cuando aparece")]
    [Tooltip("Si lo llenas, el cartel aparece solo cuando esa cinematica termina. " +
             "Dejalo vacio si preferis dispararlo a mano desde otro evento.")]
    public CinematicaFrames cinematica;
    [Tooltip("Segundos de espera antes de que aparezca. Le da aire despues de la cinematica, " +
             "en vez de saltar encima del ultimo fundido.")]
    public float esperaAntesDeAparecer = 0.5f;

    [Header("Contenido")]
    [Tooltip("Ilustracion o icono que va arriba del texto. Se puede dejar vacio.")]
    public Sprite imagenCartel;
    [Tooltip("La imagen que hace de BASE del cartel (el marco). Es la misma que usan las flores.")]
    public Sprite fondoCartel;
    public string tituloCartel = "COMBATE";
    [TextArea(2, 5)]
    public string descripcionCartel = "Presiona CLICK IZQUIERDO para atacar.";
    public string textoParaCerrar = "Presiona ESPACIO, ENTER o ESC para continuar";
    [Tooltip("Congela el juego mientras el cartel esta en pantalla.")]
    public bool pausarMientrasSeMuestra = true;

    [Header("Diseño del cartel")]
    [Tooltip("PUNTO DE PARTIDA: arrastra aca una Flor y despues usa el menu del engranaje de este " +
             "componente (arriba a la derecha) > 'Copiar el diseño de esa Flor'.\n\n" +
             "Eso copia los valores UNA SOLA VEZ al Estilo Cartel de abajo. Despues quedan " +
             "independientes: podes editar este cartel sin tocar el de las flores, y al reves.")]
    public ActivarFlor copiarDiseñoDeEstaFlor;

    [Tooltip("El diseño que usa ESTE cartel. Se edita libremente. Para arrancar con el mismo " +
             "look que los carteles de habilidad, usa el boton de copiado de arriba.")]
    public EstiloCartelHabilidad estiloCartel = new EstiloCartelHabilidad();

    // Copia los valores de la Flor de referencia a nuestro propio estilo, de una vez.
    // A partir de ahi los dos son independientes: es una copia, no un vinculo.
    [ContextMenu("Copiar el diseño de esa Flor")]
    private void CopiarDiseñoDeLaFlor()
    {
        if (copiarDiseñoDeEstaFlor == null)
        {
            Debug.LogWarning("CartelIndicaciones: primero arrastra una Flor al campo " +
                             "'Copiar Diseño De Esta Flor'.", this);
            return;
        }

        if (estiloCartel == null) estiloCartel = new EstiloCartelHabilidad();
        estiloCartel.CopiarDesde(copiarDiseñoDeEstaFlor.EstiloDelCartel);

#if UNITY_EDITOR
        // Sin esto Unity no se entera del cambio y no lo guarda en la escena.
        UnityEditor.EditorUtility.SetDirty(this);
#endif

        Debug.Log("CartelIndicaciones: diseño copiado de '" + copiarDiseñoDeEstaFlor.name +
                  "'. Desde ahora se edita aparte.", this);
    }

    [Header("Si ya se mostro")]
    [Tooltip("Activo: el cartel se ve UNA sola vez en toda la partida. No se repite al morir " +
             "ni al volver a la zona.")]
    public bool mostrarSoloUnaVez = true;
    [Tooltip("Con que nombre se recuerda. Dejalo VACIO y se arma solo con la escena y el nombre " +
             "del objeto.")]
    public string claveGuardado = "";

    private bool yaLoMostre;

    private void Awake()
    {
        if (cinematica != null && cinematica.alTerminar != null)
        {
            cinematica.alTerminar.AddListener(Mostrar);
        }
    }

    private void OnDestroy()
    {
        // Si no lo sacamos, el listener queda apuntando a un objeto destruido.
        if (cinematica != null && cinematica.alTerminar != null)
        {
            cinematica.alTerminar.RemoveListener(Mostrar);
        }
    }

    // La que se conecta al evento. Llamarla dos veces no muestra el cartel dos veces.
    public void Mostrar()
    {
        if (yaLoMostre) return;
        if (mostrarSoloUnaVez && ProgresoJuego.YaMostrado(Clave())) return;

        yaLoMostre = true;
        if (mostrarSoloUnaVez) ProgresoJuego.MarcarMostrado(Clave());

        StartCoroutine(MostrarConEspera());
    }

    // Para probar sin tener que ver la cinematica entera.
    [ContextMenu("Mostrar el cartel ahora")]
    private void MostrarAhora()
    {
        StartCoroutine(MostrarConEspera());
    }

    private IEnumerator MostrarConEspera()
    {
        if (esperaAntesDeAparecer > 0f)
        {
            // En tiempo REAL: si algo dejo el juego congelado, la espera igual corre.
            yield return new WaitForSecondsRealtime(esperaAntesDeAparecer);
        }

        CartelHabilidadUI.Mostrar(
            imagenCartel,
            fondoCartel,
            tituloCartel,
            descripcionCartel,
            textoParaCerrar,
            pausarMientrasSeMuestra,
            estiloCartel);
    }

    private string Clave()
    {
        if (!string.IsNullOrEmpty(claveGuardado)) return claveGuardado;

        return "Cartel@" + gameObject.scene.name + "@" + gameObject.name;
    }
}
