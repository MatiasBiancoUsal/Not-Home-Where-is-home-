using UnityEngine;
using UnityEngine.SceneManagement;

// ============================================================
//  PUNTO DE REAPARICION (checkpoints)
//
//  Recuerda donde tiene que reaparecer la niña si muere:
//    - el ultimo CHECKPOINT que toco, o
//    - si todavia no toco ninguno en esta zona, donde entro a la zona por una puerta.
//  Si no hay ninguno, reaparece al principio de la zona, como antes.
//
//  No va en ningun objeto. Ademas, al cargar cada zona le pone solo el script
//  Checkpoint a todos los objetos que se llamen "CHECKPOINT ..." (los bloques verdes),
//  asi no hay que agregarlo a mano en ninguna escena.
// ============================================================
public static class PuntoDeReaparicion
{
    private static bool hayPunto;
    private static string escenaDelPunto;
    private static Vector3 posicion;
    private static bool reaparecerAlCargar;

    // Con Reload Domain desactivado los static se arrastran entre sesiones de Play.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reiniciar()
    {
        hayPunto = false;
        escenaDelPunto = null;
        reaparecerAlCargar = false;

        SceneManager.sceneLoaded -= AlCargarEscena;
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    public static void Guardar(string escena, Vector3 donde)
    {
        hayPunto = true;
        escenaDelPunto = escena;
        posicion = donde;
    }

    // Guarda un punto "parada en el piso" debajo de 'desde'. Lo usan los checkpoints y las
    // flores de habilidad. Si no encuentra piso, usa donde esta la niña ahora.
    public static void GuardarEnElPiso(string escena, Vector2 desde, PlayerController pc, float distanciaAlPiso = 15f)
    {
        if (pc == null) return;
        Guardar(escena, EnElPiso(desde, pc, distanciaAlPiso));
    }

    public static Vector3 EnElPiso(Vector2 desde, PlayerController pc, float distanciaAlPiso = 15f)
    {
        LayerMask piso = pc.jump != null ? pc.jump.groundMask : (LayerMask)Physics2D.DefaultRaycastLayers;

        RaycastHit2D[] golpes = Physics2D.RaycastAll(desde, Vector2.down, distanciaAlPiso, piso);
        foreach (RaycastHit2D g in golpes)
        {
            if (g.collider == null || g.collider.isTrigger || g.distance <= 0f || g.normal.y < 0.5f) continue;

            Collider2D colNina = pc.GetComponent<Collider2D>();
            float alPiso = colNina != null ? pc.transform.position.y - colNina.bounds.min.y : 0.5f;
            return new Vector3(desde.x, g.point.y + alPiso + 0.05f, pc.transform.position.z);
        }

        return pc.transform.position;
    }

    // Partida nueva: nos olvidamos de todo.
    public static void Olvidar()
    {
        hayPunto = false;
        escenaDelPunto = null;
        reaparecerAlCargar = false;
    }

    // ¿Este punto es el que esta guardado ahora? (para no repetir el aviso del checkpoint)
    public static bool EsElActual(string escena, Vector3 donde)
    {
        return hayPunto && escena == escenaDelPunto && (posicion - donde).sqrMagnitude < 0.01f;
    }

    // La llama la muerte de la niña justo antes de recargar la zona.
    public static void ReaparecerAlRecargar()
    {
        reaparecerAlCargar = true;
    }

    // Corre apenas carga la escena, antes del primer frame: la camara ya encuentra a la
    // niña en su lugar y no hace ningun paneo raro.
    private static void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        PrepararCheckpoints(escena);

        if (!reaparecerAlCargar) return;
        reaparecerAlCargar = false;

        if (!hayPunto || escena.name != escenaDelPunto) return;

        PlayerController pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc == null) return;

        pc.transform.position = posicion;
        if (pc.rb != null) pc.rb.linearVelocity = Vector2.zero;
    }

    private static void PrepararCheckpoints(Scene escena)
    {
        foreach (GameObject raiz in escena.GetRootGameObjects())
        {
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (!t.name.StartsWith("CHECKPOINT", System.StringComparison.OrdinalIgnoreCase)) continue;

                // El objeto padre "checkpoints" (solo agrupa) no tiene dibujo ni collider.
                if (t.GetComponent<SpriteRenderer>() == null && t.GetComponent<Collider2D>() == null) continue;

                if (t.GetComponent<Checkpoint>() == null) t.gameObject.AddComponent<Checkpoint>();
            }
        }
    }
}
