using UnityEngine;

// ============================================================
//  AJUSTES DE LA TRANSICION ENTRE ZONAS
//
//  ---------------------------------------------------------------
//  NOTA DEL EQUIPO (pedido de Val, agosto 2026):
//  TODO tiene que quedar AJUSTABLE DESDE EL INSPECTOR mientras se prueba.
//  Nada de numeros escondidos en el codigo.
//  Cuando los valores queden como gustan, se pueden "guardar permanentemente"
//  copiandolos aca como valores por defecto, pero mientras tanto se tocan
//  siempre desde el asset, en vivo, con el juego corriendo.
//  ---------------------------------------------------------------
//
//  Esto es un ScriptableObject: UN asset con los ajustes GLOBALES de todas las
//  transiciones. Vive en Assets/Resources/AjustesTransicion.asset y lo crea solo
//  el comando "Not Home > Puertas > Configurar desde los nombres".
//  Se selecciona en el Project y se edita igual que un componente.
//
//  Los ajustes de CADA puerta (hacia donde camina, cuanto camina, donde aparece)
//  estan en el componente PuertaZona de esa puerta, no aca.
// ============================================================
[CreateAssetMenu(fileName = "AjustesTransicion", menuName = "Not Home/Ajustes de Transicion")]
public class AjustesTransicion : ScriptableObject
{
    [Header("Fundidos")]
    [Tooltip("Segundos que tarda la pantalla en ponerse negra al salir de una zona.")]
    public float fadeANegro = 0.5f;
    [Tooltip("Segundos de pantalla negra entre una zona y la otra (mientras carga).")]
    public float esperaEnNegro = 0.2f;
    [Tooltip("Segundos que tarda la zona nueva en aparecer desde el negro.")]
    public float fadeDesdeNegro = 0.5f;

    [Header("Caminata automatica")]
    [Tooltip("Velocidad con la que camina sola la niña. En 0 usa la misma velocidad que tiene el player normalmente.")]
    public float velocidadCaminata = 0f;
    [Tooltip("Envion hacia arriba para las puertas por las que se SUBE.")]
    public float impulsoAlSubir = 14f;
    [Tooltip("Empujon hacia abajo para las puertas por las que se CAE.")]
    public float empujeAlCaer = 4f;

    [Header("Al llegar a la zona nueva")]
    [Tooltip("Busca el piso debajo de la puerta y apoya ahi a la niña. Sin esto aparece flotando a la altura " +
             "del centro del marcador y se cae. NO se aplica a las puertas verticales (por esas tiene que caer).")]
    public bool pegarAlPisoAlLlegar = true;
    [Tooltip("Hasta cuantos metros hacia abajo busca el piso.")]
    public float distanciaMaximaAlPiso = 30f;

    [Header("Animaciones (valores del parametro stateAnim del Animator)")]
    [Tooltip("1 = idle. Ver las clases de State Anims de la niña.")]
    public int animIdle = 1;
    [Tooltip("2 = correr.")]
    public int animCorrer = 2;
    [Tooltip("3 = inicio de salto.")]
    public int animSaltar = 3;
    [Tooltip("4 = caer.")]
    public int animCaer = 4;

    [Header("Oscurecimiento al acercarse a una puerta")]
    [Tooltip("Llave general: si lo destildas, ninguna puerta oscurece nada.")]
    public bool oscurecerAlAcercarse = true;
    [Tooltip("Color de la sombra. Negro normalmente, pero podes usar un azul o un violeta oscuro.")]
    public Color colorDelOscurecimiento = Color.black;
    [Tooltip("LARGO del negro: que parte de la pantalla ocupa, de 0 a 1. 0.45 = casi la mitad. " +
             "Cada puerta puede pisar este valor con su propio 'Largo Del Negro'.")]
    [Range(0.05f, 1f)]
    public float anchoDelDegradado = 0.45f;
    [Tooltip("DUREZA del desvanecido. 1 = parejo. Mas alto (2, 3) = el negro se concentra pegado al borde " +
             "y se aclara rapido. Mas bajo (0.5) = el negro se estira hacia el centro de la pantalla.")]
    [Range(0.2f, 5f)]
    public float durezaDelDegradado = 1f;
    [Tooltip("Que tan rapido acompaña la sombra al movimiento. Mas bajo = mas suave y perezoso.")]
    public float suavizadoDelOscurecimiento = 8f;
    [Tooltip("Opcional: tu propia imagen de degradado. Si lo dejas vacio, se genera una por codigo " +
             "(negro pegado al borde que se desvanece hacia el centro).")]
    public Sprite spriteDelDegradado;

    [Header("Diagnostico (mientras se prueba)")]
    [Tooltip("Escribe en la Console cada paso de la transicion: por que puerta sale, donde aparece, hacia donde camina. " +
             "Destildalo cuando ya funcione todo.")]
    public bool mostrarDiagnosticoEnConsola = true;
    [Tooltip("Avisa si la niña camino contra una pared y no se movio (casi siempre = Direccion Salida al reves).")]
    public bool avisarSiNoSeMueve = true;

    [Header("Seguridad")]
    [Tooltip("Mientras dura la transicion la niña no puede recibir daño (no se muere cruzando una puerta).")]
    public bool invulnerableDuranteLaTransicion = true;
    [Tooltip("Segundos despues de una transicion en los que NINGUNA puerta se activa. Evita rebotes entre dos puertas.")]
    public float graciaEntrePuertas = 0.5f;
}
