#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// ============================================================
//  REVISAR LA LLEGADA DE LAS PUERTAS (solo editor)
//
//  Recorre las 6 zonas y, para CADA puerta, revisa que la niña pueda "entrar
//  caminando" cuando llega por ahi:
//     - que haya piso abajo de la puerta (si no, aparece en el aire y se cae)
//     - que no quede demasiado alto (si cae mucho, no se ve la caminata)
//     - que el camino hacia adentro no este tapado por una pared
//
//  Menu:  Not Home > Puertas > Revisar la llegada (que entre caminando)
// ============================================================
public static class MenuLlegadaPuertas
{
    private const float CAIDA_ACEPTABLE = 2f;    // unidades que puede caer sin que se note feo
    private const float LARGO_DEL_RAYO = 30f;
    private const float PASOS_HACIA_ADENTRO = 2f;

    [MenuItem("Not Home/Puertas/Revisar la llegada (que entre caminando)", false, 22)]
    private static void Revisar()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string escenaOriginal = EditorSceneManager.GetActiveScene().path;
        StringBuilder reporte = new StringBuilder();
        int problemas = 0;

        reporte.AppendLine("[Puertas] Como queda la niña al LLEGAR por cada puerta:");

        try
        {
            foreach (EditorBuildSettingsScene entrada in EditorBuildSettings.scenes)
            {
                if (!entrada.enabled) continue;

                string nombreEscena = System.IO.Path.GetFileNameWithoutExtension(entrada.path);
                if (!nombreEscena.StartsWith("Zona")) continue;

                Scene escena = EditorSceneManager.OpenScene(entrada.path, OpenSceneMode.Single);
                reporte.AppendLine("");
                reporte.AppendLine("   " + escena.name);

                LayerMask piso = MascaraDePiso();

                PuertaZona[] puertas = Object.FindObjectsByType<PuertaZona>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);

                if (puertas.Length == 0) reporte.AppendLine("      (sin puertas)");

                foreach (PuertaZona p in puertas)
                {
                    if (p == null) continue;
                    problemas += RevisarUna(p, piso, reporte);
                }
            }
        }
        finally
        {
            if (!string.IsNullOrEmpty(escenaOriginal))
            {
                EditorSceneManager.OpenScene(escenaOriginal, OpenSceneMode.Single);
            }
        }

        reporte.AppendLine("");
        reporte.AppendLine(problemas == 0
            ? "   Todo en orden: en todas las puertas se entra caminando."
            : "   Hay " + problemas + " puerta(s) para revisar (mira arriba).");

        Debug.Log(reporte.ToString());
    }

    private static int RevisarUna(PuertaZona p, LayerMask piso, StringBuilder reporte)
    {
        Vector2 origen = p.PosicionDeAparicion;
        Vector2 haciaAdentro = p.VectorEntrada;
        float largoDeLaCaminata = LargoDeLaCaminata(p);

        // Las puertas verticales son agujeros: ahi se entra cayendo o subiendo, no caminando.
        if (haciaAdentro.y != 0f)
        {
            reporte.AppendLine("      • " + p.name + ": vertical (se entra cayendo o subiendo), no aplica.");
            return 0;
        }

        // 1) ¿Hay piso abajo?
        float caida = -1f;
        Vector2 aterrizaje = origen;

        foreach (RaycastHit2D h in Physics2D.RaycastAll(origen + Vector2.up * 0.1f, Vector2.down, LARGO_DEL_RAYO, piso))
        {
            if (h.collider == null || h.collider.isTrigger) continue;
            if (h.normal.y < 0.5f) continue;

            caida = origen.y - h.point.y;
            aterrizaje = h.point;
            break;
        }

        if (caida < 0f)
        {
            reporte.AppendLine("      • " + p.name + ":  PROBLEMA: NO hay piso debajo. La niña va a aparecer " +
                               "en el aire y caerse al vacio.");
            return 1;
        }

        List<string> avisos = new List<string>();

        // El juego YA apoya sola a la niña en el piso al llegar ('Pegar Al Piso Al Llegar'
        // en AjustesTransicion), asi que una puerta flotando unas unidades no se nota: la
        // niña aparece abajo, parada. Solo es un problema si esta MAS LEJOS de lo que el
        // juego busca, porque ahi no encuentra piso y aparece en el aire de verdad.
        AjustesTransicion ajustes = Resources.Load<AjustesTransicion>("AjustesTransicion");
        bool seApoyaSola = ajustes != null && ajustes.pegarAlPisoAlLlegar;
        float alcance = ajustes != null ? ajustes.distanciaMaximaAlPiso : 0f;

        if (!seApoyaSola && caida > CAIDA_ACEPTABLE)
        {
            avisos.Add("cae " + caida.ToString("0.0") + " unidades antes de tocar el piso " +
                       "(no se ve la caminata de entrada)");
        }
        else if (seApoyaSola && caida > alcance)
        {
            avisos.Add("el piso esta a " + caida.ToString("0.0") + " unidades y el juego solo " +
                       "busca hasta " + alcance.ToString("0") + ": no lo encuentra y la niña " +
                       "aparece en el aire");
        }

        // 2) ¿Puede caminar hacia adentro, o choca contra algo?
        Vector2 desde = aterrizaje + Vector2.up * 1.2f;
        RaycastHit2D pared = Physics2D.Raycast(desde, haciaAdentro, PASOS_HACIA_ADENTRO, piso);

        if (pared.collider != null && !pared.collider.isTrigger)
        {
            avisos.Add("camina contra '" + pared.collider.name + "' a " +
                       pared.distance.ToString("0.0") + " unidades");
        }

        // 3) ¿Le alcanza el piso para toda la caminata de entrada, o se le termina
        //    y se cae en el medio? Esto es lo que mas se nota: la niña "aparece y cae"
        //    en vez de entrar caminando.
        float sinPiso = DondeSeAcabaElPiso(aterrizaje, haciaAdentro, largoDeLaCaminata, piso);
        if (sinPiso >= 0f)
        {
            avisos.Add("se le acaba el piso a " + sinPiso.ToString("0.0") + " u de la puerta, " +
                       "y la caminata de entrada dura " + largoDeLaCaminata.ToString("0.0") + " u " +
                       "(bajale 'Segundos Caminando Al Entrar')");
        }

        if (avisos.Count == 0)
        {
            string detalle = seApoyaSola && caida > CAIDA_ACEPTABLE
                ? " (la puerta esta " + caida.ToString("0.0") + " u sobre el piso, pero el juego la apoya sola)"
                : "";
            reporte.AppendLine("      • " + p.name + ": OK (entra caminando bien)" + detalle);
            return 0;
        }

        reporte.AppendLine("      • " + p.name + ":  REVISAR: " + string.Join(" + ", avisos));
        return 1;
    }

    // Cuantas unidades camina sola la niña al entrar por esta puerta.
    private static float LargoDeLaCaminata(PuertaZona p)
    {
        AjustesTransicion ajustes = Resources.Load<AjustesTransicion>("AjustesTransicion");
        PlayerController pc = Object.FindFirstObjectByType<PlayerController>();

        float velocidad = 0f;
        if (ajustes != null) velocidad = ajustes.velocidadCaminata;
        if (velocidad <= 0f && pc != null) velocidad = pc.speed;
        if (velocidad <= 0f) velocidad = 5f;

        return velocidad * p.segundosCaminandoAlEntrar;
    }

    // Camina de a pasitos desde donde aterriza y devuelve a que distancia se queda sin
    // piso. -1 si le alcanza para toda la caminata.
    private static float DondeSeAcabaElPiso(Vector2 aterrizaje, Vector2 haciaAdentro,
                                            float largo, LayerMask piso)
    {
        const float PASO = 0.5f;
        const float ALTO_DEL_PASO = 1.5f;   // cuanto puede bajar sin que parezca una caida

        for (float d = PASO; d <= largo; d += PASO)
        {
            Vector2 pie = aterrizaje + haciaAdentro * d + Vector2.up * 0.3f;
            RaycastHit2D abajo = Physics2D.Raycast(pie, Vector2.down, ALTO_DEL_PASO + 0.3f, piso);

            bool hayPiso = abajo.collider != null && !abajo.collider.isTrigger && abajo.normal.y >= 0.5f;
            if (!hayPiso) return d;
        }

        return -1f;
    }

    // El mismo "suelo" que usa la niña. Si no esta en la escena, probamos con la capa Ground.
    private static LayerMask MascaraDePiso()
    {
        PlayerController pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc != null && pc.jump != null) return pc.jump.groundMask;

        int ground = LayerMask.NameToLayer("Ground");
        return ground >= 0 ? (LayerMask)(1 << ground) : (LayerMask)Physics2D.DefaultRaycastLayers;
    }
}
#endif
