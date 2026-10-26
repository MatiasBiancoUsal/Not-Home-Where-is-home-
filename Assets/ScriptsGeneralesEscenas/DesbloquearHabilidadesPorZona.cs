using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

// ============================================================
//  HERRAMIENTA DE TESTEO: desbloquear habilidades segun la zona
//
//  Apretando una tecla (por defecto H) se desbloquean de una todas las
//  habilidades que el jugador YA deberia tener al llegar a esa zona, como si
//  hubiera ido tocando las flores una por una. Sirve para probar un nivel sin
//  tener que jugar todas las zonas anteriores.
//
//  Ademas del set por zona, habilita el ATAQUE en todas (se configura mas abajo).
//
//  Es ACUMULATIVO: en la Zona N se desbloquean las primeras N habilidades de
//  la lista de abajo. Con el orden que viene puesto:
//    Zona 1 -> doble salto
//    Zona 2 -> + escalar
//    Zona 3 -> + dash
//    Zona 4 -> + pisoton
//    Zona 5 -> + super salto
//    Zona 6 -> + escudo
//  Para cambiar que da cada zona, alcanza con reordenar la lista en el Inspector.
//
//  Se puede poner EN el player (lo mas comodo: se agrega una sola vez al prefab
//  y ya esta en las 6 zonas) o en cualquier objeto de la escena: si no encuentra
//  el PlayerController al lado, lo busca en la escena.
// ============================================================
public class DesbloquearHabilidadesPorZona : MonoBehaviour
{
    // Que flor hay en cada zona. Lo usa tambien el atajo SHIFT + N (ZoneSkipByKey).
    public static readonly PlayerController.Habilidad[] ORDEN_POR_DEFECTO =
    {
        PlayerController.Habilidad.DobleSalto, // Zona 1
        PlayerController.Habilidad.Escalar,    // Zona 2
        PlayerController.Habilidad.Dash,       // Zona 3
        PlayerController.Habilidad.Pisoton,    // Zona 4
        PlayerController.Habilidad.SuperSalto, // Zona 5
        PlayerController.Habilidad.Escudo      // Zona 6
    };

    [Header("Tecla")]
    [Tooltip("Tecla que desbloquea las habilidades de la zona actual.")]
    public Key tecla = Key.H;
    [Tooltip("Destildalo para apagar el atajo sin sacar el componente (por ejemplo antes de una build).")]
    public bool activo = true;

    [Header("Tecla de PUNTOS (para probar el cartel de canje)")]
    [Tooltip("Te regala JUSTO los puntos que te faltan para comprar la flor de esta zona, " +
             "asi podés probar el cartel de '¿canjear puntos?' sin juntar monedas a mano. " +
             "Si ya te alcanza, no suma nada.")]
    public Key teclaPuntos = Key.P;
    [Tooltip("Destildalo para apagar el atajo de los puntos sin tocar el de las habilidades.")]
    public bool regalarPuntosDeLaFlor = true;

    [Header("Que habilidad suma cada zona")]
    [Tooltip("El ORDEN es lo que importa: en la Zona N se desbloquean las primeras N de esta lista. Reordenala para cambiar que da cada zona.")]
    public PlayerController.Habilidad[] ordenPorZona = new PlayerController.Habilidad[]
    {
        PlayerController.Habilidad.DobleSalto, // Zona 1
        PlayerController.Habilidad.Escalar,    // Zona 2
        PlayerController.Habilidad.Dash,       // Zona 3
        PlayerController.Habilidad.Pisoton,    // Zona 4
        PlayerController.Habilidad.SuperSalto, // Zona 5
        PlayerController.Habilidad.Escudo      // Zona 6
    };

    [Header("Ataque")]
    [Tooltip("El ataque se habilita SIEMPRE, en cualquier zona. Va aparte porque no es una Habilidad del PlayerController: es un desbloqueo propio de PlayerAttacks, el que normalmente da la cinematica del osito.")]
    public bool tambienDesbloquearAtaque = true;

    [Header("Progreso guardado")]
    [Tooltip("Apagado (recomendado para testear): desbloquea solo para esta partida, sin tocar el archivo de progreso. Si lo prendes, queda guardado igual que si hubieras agarrado las flores de verdad.")]
    public bool guardarProgreso = false;

    [Header("Aviso en consola")]
    [Tooltip("Escribe en la consola que habilidades se desbloquearon.")]
    public bool mostrarLogs = true;

    private void Update()
    {
        if (!activo) return;

        // Solo en el editor (o en una build de desarrollo). En el juego final la H no hace
        // nada: en las pruebas, los jugadores la apretaban sin querer, recibian habilidades
        // SIN guardar, y despues "desaparecian" al morir o al cambiar de zona.
        if (!Application.isEditor && !Debug.isDebugBuild) return;
        if (Keyboard.current == null) return;

        if (Apretada(tecla)) DesbloquearHastaLaZonaActual();

        if (regalarPuntosDeLaFlor && Apretada(teclaPuntos)) RegalarLosPuntosDeLaFlor();
    }

    private static bool Apretada(Key k)
    {
        if (k == Key.None) return false;

        var control = Keyboard.current[k];
        return control != null && control.wasPressedThisFrame;
    }

    // ============================================================
    //  TESTEO: regalar los puntos justos de la flor de esta zona
    //
    //  Busca la flor que todavia no se recogio, mira cuanto sale y suma la diferencia
    //  que falta para llegar a ese precio. Asi, al tocar la flor, aparece el cartel de
    //  "¿canjear puntos?" sin tener que juntar todas las monedas a mano.
    //
    //  Los puntos SI se guardan (van por el mismo camino que una moneda normal), asi
    //  que no se pierden al cambiar de zona ni al morir.
    // ============================================================
    [ContextMenu("Regalar los puntos de la flor de esta zona")]
    public void RegalarLosPuntosDeLaFlor()
    {
        ScoreManager puntos = ScoreManager.Instance;
        if (puntos == null)
        {
            Log("No hay ScoreManager en la escena, no se pueden sumar puntos.");
            return;
        }

        ActivarFlor flor = FlorSinRecoger();
        if (flor == null)
        {
            Log("En esta zona no queda ninguna flor sin recoger. No se sumo nada.");
            return;
        }

        int falta = flor.CostoEnPuntos - puntos.PuntajeGlobal;
        if (falta <= 0)
        {
            Log(flor.HabilidadQueEntrega + " sale " + flor.CostoEnPuntos + " y ya tenes " +
                puntos.PuntajeGlobal + ". No hace falta sumar nada.");
            flor.OlvidarEsperaDeCarteles();
            return;
        }

        puntos.AddPoints(falta);

        // Si recien vimos el cartel de "puntos insuficientes", su espera seguiria
        // corriendo y el cartel de canje no saldria hasta que se cumpla. Como ya
        // tenemos la plata, lo olvidamos para poder probar en el acto.
        flor.OlvidarEsperaDeCarteles();

        Log("+" + falta + " puntos. Ahora tenes " + puntos.PuntajeGlobal + ", justo lo que sale " +
            flor.HabilidadQueEntrega + ". Tocá la flor para ver el cartel de canje.");
    }

    // La flor de esta zona que todavia esta cerrada. Si hubiera mas de una, la primera.
    private static ActivarFlor FlorSinRecoger()
    {
        ActivarFlor[] flores = FindObjectsByType<ActivarFlor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (ActivarFlor f in flores)
        {
            if (f != null && !f.Recogida) return f;
        }
        return null;
    }

    // Tambien se puede disparar a mano desde el engranaje del componente.
    [ContextMenu("Desbloquear habilidades de esta zona")]
    public void DesbloquearHastaLaZonaActual()
    {
        int zona = NumeroDeZonaActual();

        if (zona <= 0)
        {
            Log("No estamos en una zona (la escena no se llama \"Zona ...\"). No se desbloqueo nada.");
            return;
        }

        PlayerController player = GetComponent<PlayerController>();
        if (player == null) player = FindFirstObjectByType<PlayerController>();

        if (player == null)
        {
            Log("No se encontro el player en la escena. No se desbloqueo nada.");
            return;
        }

        if (ordenPorZona == null || ordenPorZona.Length == 0)
        {
            Log("La lista de habilidades esta vacia. Cargala en el Inspector.");
            return;
        }

        // En la Zona N van las primeras N habilidades. Si la zona es mas alta que la
        // lista, se desbloquean todas (no se rompe nada).
        int cantidad = Mathf.Min(zona, ordenPorZona.Length);

        string desbloqueadas = "";
        for (int i = 0; i < cantidad; i++)
        {
            player.DesbloquearHabilidad(ordenPorZona[i], guardarProgreso);
            desbloqueadas += (i > 0 ? ", " : "") + ordenPorZona[i];
        }

        // El ataque va aparte: no es una Habilidad del PlayerController sino un
        // desbloqueo propio de PlayerAttacks (normalmente lo da la cinematica del osito).
        // Por eso no depende de la zona: se habilita siempre.
        if (tambienDesbloquearAtaque)
        {
            PlayerAttacks attacks = player.GetComponent<PlayerAttacks>();
            if (attacks != null)
            {
                // Con guardado usamos el mismo metodo que la cinematica; sin guardado
                // prendemos el flag a mano para no dejarlo anotado en el progreso.
                if (guardarProgreso) attacks.DesbloquearAtaque();
                else attacks.puedeAtacar = true;

                desbloqueadas += (desbloqueadas.Length > 0 ? ", " : "") + "Atacar";
            }
        }

        Log("Zona " + zona + " -> " + desbloqueadas
            + (guardarProgreso ? " (guardado en el progreso)" : " (solo para esta partida)"));
    }

    // Saca el numero del nombre de la escena: "Zona 3" -> 3.
    // Devuelve 0 si la escena no es una zona (menu, creditos, etc).
    private int NumeroDeZonaActual()
    {
        return NumeroDeZona(SceneManager.GetActiveScene().name);
    }

    public static int NumeroDeZona(string nombre)
    {
        if (string.IsNullOrEmpty(nombre)) return 0;
        if (!nombre.StartsWith("Zona")) return 0;

        string digitos = "";
        foreach (char c in nombre)
        {
            if (char.IsDigit(c)) digitos += c;
            else if (digitos.Length > 0) break; // ya agarramos el primer numero entero
        }

        int numero;
        return int.TryParse(digitos, out numero) ? numero : 0;
    }

    private void Log(string mensaje)
    {
        if (mostrarLogs) Debug.Log("[Testeo habilidades] " + mensaje);
    }
}
