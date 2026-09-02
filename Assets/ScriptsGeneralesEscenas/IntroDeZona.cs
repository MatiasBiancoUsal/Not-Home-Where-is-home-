using System.Collections;
using UnityEngine;

// ============================================================
//  INTRO DE ZONA (la apertura del juego)
//
//  Pensado para el arranque de Zona 1: la niña aparece CAYENDO y no responde a los
//  controles. Cuando toca el piso:
//     1. Queda quieta en idle.
//     2. Aparece el nombre de la zona.
//     3. Aparece el cartel del tutorial.
//     4. Recien ahi recupera el control.
//
//  NO se dispara cuando llegas a la zona por una puerta (para eso ya esta la transicion
//  con su propio cartel), ni la segunda vez en la misma partida.
//
//  Va en un objeto vacio de la escena (por ejemplo uno que se llame "INTRO").
//  Como siempre: todo ajustable desde el Inspector.
// ============================================================
public class IntroDeZona : MonoBehaviour
{
    [Header("Quien participa")]
    [Tooltip("La niña. Si lo dejas vacio la busca sola en la escena.")]
    public PlayerController jugador;
    [Tooltip("El cartel del tutorial (el componente TutorialMovimiento, que esta EN la niña). " +
             "Si lo dejas vacio lo busca solo. Se mantiene apagado hasta que termina el nombre de la zona.")]
    public TutorialMovimiento cartelTutorial;

    [Header("Cuando corre")]
    [Tooltip("Si esta activo, la intro se ve UNA sola vez por partida. Se borra con " +
             "'Not Home > Borrar progreso guardado'.")]
    public bool soloUnaVezPorPartida = true;
    [Tooltip("Con que nombre se recuerda que ya se vio. Si haces otra intro, ponele una clave distinta.")]
    public string claveGuardado = "IntroZona1";
    [Tooltip("Si esta activo, NO corre cuando llegas a esta zona cruzando una puerta.")]
    public bool soloAlArrancarElJuego = true;

    [Header("Tiempos")]
    [Tooltip("Segundos que espera despues de tocar el piso, antes de mostrar el nombre de la zona.")]
    public float esperaAlTocarElPiso = 0.4f;
    [Tooltip("Si esta activo, el cartel del tutorial espera a que el nombre de la zona TERMINE de " +
             "desvanecerse. Asi nunca se superponen en pantalla. Si lo destildas, aparece al toque " +
             "y los dos conviven (ahi conviene separarlos de posicion).")]
    public bool esperarQueTermineElNombre = true;
    [Tooltip("Segundos entre el nombre de la zona y el cartel del tutorial. Si esta tildado lo de arriba, " +
             "se cuentan DESPUES de que el nombre se fue.")]
    public float esperaDespuesDelNombre = 0.6f;
    [Tooltip("Segundos entre que aparece el cartel del tutorial y que la niña recupera el control.")]
    public float esperaAntesDeDevolverElControl = 0.3f;

    [Header("Seguridad")]
    [Tooltip("Si por lo que sea nunca detecta el piso, a los tantos segundos le devuelve el control igual " +
             "para no dejar al jugador trabado.")]
    public float segundosMaximosCayendo = 15f;

    [Header("Animaciones (valores del parametro stateAnim del Animator)")]
    public int animIdle = 1;
    public int animCaer = 4;

    private bool corriendo = false;

    // ============================================================
    //  1. CICLO DE VIDA
    //  Decide si la intro tiene que correr en esta escena, y la arranca.
    // ============================================================

    private void Awake()
    {
        // Llegue por una puerta: de esto se encarga la transicion, no la intro.
        if (soloAlArrancarElJuego && TransicionZonas.EnCurso)
        {
            enabled = false;
            return;
        }

        // Ya la vi en esta partida.
        if (soloUnaVezPorPartida && ProgresoJuego.YaMostrado(claveGuardado))
        {
            enabled = false;
            return;
        }

        corriendo = true;

        // El cartel del tutorial arranca apagado: lo prendemos nosotros, despues del
        // nombre de la zona. Se hace en Awake porque asi su Start todavia no corrio.
        if (cartelTutorial == null) cartelTutorial = Object.FindFirstObjectByType<TutorialMovimiento>();
        if (cartelTutorial != null) cartelTutorial.enabled = false;
    }

    private void Start()
    {
        if (!corriendo) return;

        if (jugador == null) jugador = Object.FindFirstObjectByType<PlayerController>();

        if (jugador == null)
        {
            Debug.LogWarning("IntroDeZona: no encontre a la niña (PlayerController) en la escena.", this);
            SoltarTodo();
            return;
        }

        // Le sacamos los controles: cae sola, sin poder moverse.
        jugador.enabled = false;

        if (soloUnaVezPorPartida) ProgresoJuego.MarcarMostrado(claveGuardado);

        StartCoroutine(Rutina());
    }

    // ============================================================
    //  2. LA SECUENCIA DE LA INTRO
    //  Los pasos en orden: caer, tocar el piso, mostrar el nombre de la zona,
    //  mostrar el cartel del tutorial y recien ahi devolver el control.
    // ============================================================

    private IEnumerator Rutina()
    {
        // 1) Cae. Mientras cae le mantenemos la animacion de caida, porque con el
        //    PlayerController apagado nadie se la actualiza.
        float reloj = 0f;
        bool toco = false;

        while (!toco && reloj < segundosMaximosCayendo)
        {
            reloj += Time.deltaTime;

            PonerAnim(animCaer);

            // El chequeo de piso lo hace el PlayerJump, pero como el controller esta
            // apagado no lo llama nadie: lo llamamos nosotros.
            if (jugador.jump != null)
            {
                jugador.jump.CheckGround();
                toco = jugador.jump.IsGrounded;
            }
            else
            {
                toco = true; // sin PlayerJump no podemos saberlo: seguimos igual
            }

            yield return null;
        }

        if (!toco)
        {
            Debug.LogWarning("IntroDeZona: nunca detecte el piso, le devuelvo el control a la niña igual.", this);
            SoltarTodo();
            yield break;
        }

        // 2) Toco el piso: queda quieta.
        if (jugador.rb != null)
        {
            jugador.rb.linearVelocity = new Vector2(0f, jugador.rb.linearVelocity.y);
        }
        PonerAnim(animIdle);

        yield return new WaitForSeconds(esperaAlTocarElPiso);

        // 3) El nombre de la zona.
        if (TransicionZonas.Instancia != null)
        {
            TransicionZonas.Instancia.MostrarNombreDeLaZona();

            if (esperarQueTermineElNombre)
            {
                yield return null; // un frame para que la corrutina del cartel arranque

                // Esperamos a que se muestre y se desvanezca del todo, asi el cartel del
                // tutorial no se le encima.
                while (TransicionZonas.Instancia != null && TransicionZonas.Instancia.CartelDeZonaVisible)
                {
                    yield return null;
                }
            }
        }

        yield return new WaitForSeconds(esperaDespuesDelNombre);

        // 4) El cartel del tutorial.
        if (cartelTutorial != null) cartelTutorial.enabled = true;

        yield return new WaitForSeconds(esperaAntesDeDevolverElControl);

        // 5) Ya puede jugar.
        SoltarTodo();
    }

    // ============================================================
    //  3. CIERRE Y AYUDANTES
    //  Devolver el control pase lo que pase, incluso si la intro se corta por la
    //  mitad (por eso OnDisable tambien llama a SoltarTodo).
    // ============================================================

    // Devuelve el control y prende lo que haya quedado apagado. Se llama siempre,
    // tambien si algo salio mal: nunca hay que dejar al jugador sin poder moverse.
    private void SoltarTodo()
    {
        if (jugador != null)
        {
            PonerAnim(animIdle);
            jugador.enabled = true;
        }

        if (cartelTutorial != null) cartelTutorial.enabled = true;

        corriendo = false;
    }

    private void PonerAnim(int valor)
    {
        if (jugador != null && jugador.animPlayer != null)
        {
            jugador.animPlayer.SetInteger("stateAnim", valor);
        }
    }

    // Por si el objeto se apaga o se recarga la escena en el medio de la intro.
    private void OnDisable()
    {
        if (corriendo) SoltarTodo();
    }
}
