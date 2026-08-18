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
    [Header("Tecla")]
    [Tooltip("Tecla que desbloquea las habilidades de la zona actual.")]
    public Key tecla = Key.H;
    [Tooltip("Destildalo para apagar el atajo sin sacar el componente (por ejemplo antes de una build).")]
    public bool activo = true;

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
        if (Keyboard.current == null || tecla == Key.None) return;

        var control = Keyboard.current[tecla];
        if (control == null || !control.wasPressedThisFrame) return;

        DesbloquearHastaLaZonaActual();
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
        string nombre = SceneManager.GetActiveScene().name;
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
