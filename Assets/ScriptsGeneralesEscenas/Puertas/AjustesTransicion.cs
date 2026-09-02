using UnityEngine;
using TMPro;

// El nombre "lindo" de una zona: el que ve el jugador, no el de la escena.
[System.Serializable]
public class NombreDeZona
{
    [Tooltip("El nombre EXACTO de la escena. Ej: 'Zona 3'.")]
    public string escena;
    [Tooltip("Lo que se muestra en pantalla. Ej: 'el jardin de las agujas'. Si lo dejas vacio se muestra el nombre de la escena.")]
    public string textoQueSeMuestra;
}

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

    [Header("Cartel con el nombre de la zona")]
    public bool mostrarCartelDeZona = true;
    [Tooltip("LA tipografia del juego. Arrastrar aca el Font Asset de TextMeshPro. " +
             "Si lo dejas vacio usa la fuente por defecto de TMP (fea, pero sirve para probar).")]
    public TMP_FontAsset fuenteDelCartel;
    [Tooltip("Los nombres lindos de cada zona. La escena tiene que escribirse EXACTO ('Zona 3'). " +
             "Las zonas que no esten en esta lista muestran el nombre de su escena.")]
    public NombreDeZona[] nombresDeLasZonas;
    [Tooltip("Fuerza todo a minuscula, sin importar como este escrito arriba.")]
    public bool pasarAMinuscula = true;

    [Header("Cartel: como se ve")]
    public float tamanioDeLetra = 64f;
    public Color colorDelCartel = Color.white;
    [Tooltip("Donde se ubica en la pantalla respecto del centro. Y negativo = mas abajo.")]
    public Vector2 posicionDelCartel = new Vector2(0f, -220f);

    [Header("Cartel: tiempos (todo en segundos)")]
    [Tooltip("Cuanto espera desde que empieza a verse la zona nueva hasta que aparece el cartel.")]
    public float retrasoDelCartel = 0.3f;
    [Tooltip("Cuanto tarda en aparecer.")]
    public float entradaDelCartel = 1.2f;
    [Tooltip("Cuanto se queda quieto y bien visible.")]
    public float sostenerElCartel = 2f;
    [Tooltip("Cuanto tarda en desvanecerse. Alto = lento y tranquilo.")]
    public float salidaDelCartel = 1.8f;

    [Header("Cartel: la animacion de entrada")]
    [Tooltip("Cuantos pixeles SUBE mientras aparece. 0 = queda quieto.")]
    public float subidaDelCartel = 30f;
    [Tooltip("Las letras arrancan separadas y se van juntando: da esa sensacion de 'aura'. " +
             "0 = sin efecto. 20-30 esta bueno.")]
    public float separacionInicialDeLetras = 25f;
    [Tooltip("Si esta activo, el cartel de cada zona se ve UNA sola vez por partida " +
             "(se borra con 'Not Home > Borrar progreso guardado').")]
    public bool mostrarSoloLaPrimeraVez = false;

    [Header("Cartel: fondo difuminado detras del texto")]
    [Tooltip("Una mancha oscura y difuminada detras del nombre de la zona, para que se lea " +
             "sobre fondos claros. Cuando esten las visuales definitivas se puede destildar.")]
    public bool fondoDetrasDelCartel = true;
    [Tooltip("Color de la mancha. Negro normalmente.")]
    public Color colorDelFondoDelCartel = Color.black;
    [Range(0f, 1f)]
    [Tooltip("Cuanto se llega a ver la mancha. 0.6 tapa bastante sin taparlo todo.")]
    public float opacidadDelFondoDelCartel = 0.6f;
    [Tooltip("Tamaño de la mancha en pixeles (ancho, alto). Que sea bastante mas grande que el texto.")]
    public Vector2 tamanioDelFondoDelCartel = new Vector2(1100f, 300f);
    [Range(0.05f, 1f)]
    [Tooltip("Que tan difuminados son los bordes. 1 = se desvanece desde el centro mismo (muy suave). " +
             "0.1 = casi un ovalo solido con el borde apenas suavizado.")]
    public float suavidadDelFondoDelCartel = 0.6f;

    [Header("Cartel: opacidad")]
    [Range(0f, 1f)]
    [Tooltip("Con cuanta opacidad ARRANCA la entrada. 0 = invisible del todo.")]
    public float opacidadInicialDelCartel = 0f;
    [Range(0f, 1f)]
    [Tooltip("A cuanta opacidad LLEGA. 1 = solido. Bajalo a 0.8 si lo queres un poco fantasmal.")]
    public float opacidadFinalDelCartel = 1f;

    [Header("Cartel: desenfoque (blur)")]
    [Tooltip("El texto entra DESENFOCADO y se va enfocando. Usa el desenfoque propio de " +
             "TextMeshPro (shader SDF): no necesita post-procesado, asi que anda en itch.io.")]
    public bool desenfocarAlEntrar = true;
    [Range(0f, 1f)]
    [Tooltip("Cuanto desenfoque tiene al aparecer. 0 = nitido. 0.4-0.6 se ve lindo.")]
    public float desenfoqueInicial = 0.45f;
    [Range(0f, 0.5f)]
    [Tooltip("Cuanto se ENGROSAN las letras mientras estan desenfocadas. Ayuda a que el borroso " +
             "no se vea flaquito y desaparecido. 0 = sin engrosar.")]
    public float engrosadoInicial = 0.1f;
    [Tooltip("Si esta activo, al desvanecerse tambien se va desenfocando (se disuelve en el aire).")]
    public bool desenfocarAlSalir = true;

    [Header("Pruebas (saltear partes que quitan el control)")]
    [Tooltip("SALTEA LA INTRO DE ZONA: la niña arranca pudiendo moverse, sin la caida inicial, " +
             "sin el nombre de la zona y sin el cartel del tutorial. Sirve para probar un nivel sin " +
             "esperar la apertura cada vez.\n\n" +
             "Es un ajuste normal: si haces la build con esto tildado, la build sale con la intro salteada.\n\n" +
             "Mientras esta tildado NO se anota la intro como vista, asi al destildarlo vuelve a aparecer.")]
    public bool saltearIntroDeZona = false;

    [Tooltip("SALTEA EL CARTEL con el nombre de la zona al cruzar una puerta. No toca la caminata " +
             "automatica ni el fundido: solo evita que aparezca el titulo.")]
    public bool saltearCartelDeZona = false;

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
