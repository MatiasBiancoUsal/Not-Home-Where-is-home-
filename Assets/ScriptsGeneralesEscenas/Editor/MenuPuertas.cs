#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// ============================================================
//  MENU "Not Home > Puertas" (solo editor, no va al build)
//
//  Lee el NOMBRE de cada marcador de puerta y configura todo solo.
//  El nombre es la fuente de verdad:
//
//      PUERTA Z2 ARRIBA --> Z3
//             \______/      \/
//              mi id      a que puerta llego
//
//  Lo que va entre parentesis es una nota y se ignora:
//      PUERTA Z3 ABAJO 3 --> Z6 ARRIBA 3 (VERTICAL)
//
//  La escena destino sale sola del "Z<numero>" del id de llegada (Z6 -> "Zona 6").
// ============================================================
public static class MenuPuertas
{
    private const string PREFIJO = "PUERTA";

    // ---------------- Configurar ----------------

    [MenuItem("Not Home/Puertas/Configurar desde los nombres", false, 20)]
    private static void Configurar()
    {
        if (!EditorUtility.DisplayDialog(
                "Configurar puertas",
                "Voy a abrir las 6 zonas, ponerle el componente PuertaZona a cada marcador que empiece con 'PUERTA', " +
                "completarle los datos leyendo su nombre, marcar su collider como Is Trigger y sacarlo del layer Ground.\n\n" +
                "Se guardan las escenas. ¿Seguimos?",
                "Dale", "Cancelar"))
        {
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        CrearAjustesSiFaltan();

        string escenaOriginal = EditorSceneManager.GetActiveScene().path;
        int total = 0;
        int conProblemas = 0;

        try
        {
        foreach (string ruta in RutasDeZonas())
        {
            Scene escena = EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);
            int enEstaEscena = 0;

            foreach (Transform t in ObjetosDePuerta())
            {
                string id, idDestino;
                if (!ParsearNombre(t.name, out id, out idDestino))
                {
                    Debug.LogWarning("[Puertas] No pude leer el nombre '" + t.name + "' en " + escena.name +
                                     ". Formato esperado: PUERTA <mi id> --> <id destino>", t.gameObject);
                    conProblemas++;
                    continue;
                }

                string escenaDestino = EscenaDeId(idDestino);
                if (string.IsNullOrEmpty(escenaDestino))
                {
                    Debug.LogWarning("[Puertas] '" + t.name + "': no puedo deducir la zona destino de '" + idDestino +
                                     "'. El id de llegada tiene que empezar con Z y un numero (ej: Z3 ABAJO 1).", t.gameObject);
                    conProblemas++;
                    continue;
                }

                // PRIMERO el collider: si el objeto no tiene ninguno, AddComponent<PuertaZona>()
                // fallaria y devolveria null. Y ademas tiene que ser trigger, si no la niña
                // choca contra la puerta en vez de cruzarla.
                Collider2D col = t.GetComponent<Collider2D>();
                if (col == null)
                {
                    col = t.gameObject.AddComponent<BoxCollider2D>();
                    Debug.Log("[Puertas] '" + t.name + "' no tenia collider, le puse un BoxCollider2D.", t.gameObject);
                }
                col.isTrigger = true;

                PuertaZona puerta = t.GetComponent<PuertaZona>();
                if (puerta == null) puerta = t.gameObject.AddComponent<PuertaZona>();

                if (puerta == null)
                {
                    Debug.LogWarning("[Puertas] No pude ponerle el componente a '" + t.name + "'.", t.gameObject);
                    conProblemas++;
                    continue;
                }

                puerta.id = id;
                puerta.idPuertaDestino = idDestino;
                puerta.escenaDestino = escenaDestino;

                // Sacarla del layer Ground: el chequeo de piso de la niña usa CircleCast contra
                // ese layer y detecta triggers, asi que creeria que esta parada sobre la puerta.
                if (LayerMask.LayerToName(t.gameObject.layer) == "Ground")
                {
                    t.gameObject.layer = 0; // Default
                }

                EditorUtility.SetDirty(t.gameObject);
                enEstaEscena++;
                total++;
            }

            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Debug.Log("[Puertas] " + escena.name + ": " + enEstaEscena + " puertas configuradas.");
        }
        }
        finally
        {
            // Pase lo que pase, te devolvemos a la escena en la que estabas trabajando.
            if (!string.IsNullOrEmpty(escenaOriginal))
            {
                EditorSceneManager.OpenScene(escenaOriginal, OpenSceneMode.Single);
            }
        }

        Debug.Log("[Puertas] LISTO: " + total + " puertas configuradas" +
                  (conProblemas > 0 ? ", " + conProblemas + " con problemas (mira los avisos de arriba)." : ".") +
                  "\nAhora corre 'Not Home > Puertas > Revisar puertas'.");
    }

    // ---------------- Revisar ----------------

    private class Info
    {
        public string escena;
        public string objeto;
        public string id;
        public string idDestino;
        public string escenaDestino;
        public bool tieneComponente;
        public DireccionPuerta direccion;
    }

    [MenuItem("Not Home/Puertas/Revisar puertas", false, 21)]
    private static void Revisar()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string escenaOriginal = EditorSceneManager.GetActiveScene().path;
        List<Info> puertas = new List<Info>();
        List<string> errores = new List<string>();
        List<string> avisos = new List<string>();

        // 1) Juntar todas las puertas de las 6 zonas.
        foreach (string ruta in RutasDeZonas())
        {
            Scene escena = EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);

            // De paso revisamos los tags: TIENE que haber exactamente un objeto tagueado
            // como "Player" por escena. Si hay dos, Unity devuelve cualquiera de los dos
            // cuando alguien pregunta por el player, y se rompen cosas que no tienen nada
            // que ver entre si (los corazones, la transicion de zonas, etc).
            RevisarTagPlayer(escena, errores);

            foreach (Transform t in ObjetosDePuerta())
            {
                string id, idDestino;
                if (!ParsearNombre(t.name, out id, out idDestino))
                {
                    errores.Add(escena.name + ": no puedo leer el nombre '" + t.name + "'.");
                    continue;
                }

                PuertaZona comp = t.GetComponent<PuertaZona>();

                puertas.Add(new Info
                {
                    escena = escena.name,
                    objeto = t.name,
                    id = id,
                    idDestino = idDestino,
                    escenaDestino = EscenaDeId(idDestino),
                    tieneComponente = comp != null,
                    direccion = comp != null ? comp.direccionSalida : DireccionPuerta.Derecha
                });
            }
        }

        if (!string.IsNullOrEmpty(escenaOriginal))
        {
            EditorSceneManager.OpenScene(escenaOriginal, OpenSceneMode.Single);
        }

        // 2) Buscar problemas.
        foreach (Info p in puertas)
        {
            if (!p.tieneComponente)
            {
                avisos.Add(p.escena + " / " + p.objeto + ": todavia no tiene el componente. " +
                           "Corre 'Configurar desde los nombres'.");
            }

            // id repetido en la misma zona
            foreach (Info otra in puertas)
            {
                if (otra != p && otra.escena == p.escena && otra.id == p.id)
                {
                    errores.Add(p.escena + ": hay dos puertas con el mismo id '" + p.id + "' (" +
                                p.objeto + " y " + otra.objeto + "). Los ids tienen que ser unicos en la zona.");
                    break;
                }
            }

            // ¿existe la puerta de llegada?
            Info destino = puertas.Find(x => x.id == p.idDestino);
            if (destino == null)
            {
                errores.Add(p.escena + " / " + p.objeto + ": apunta a '" + p.idDestino + "', que no existe en ninguna zona.");
                continue;
            }

            if (destino.escena != p.escenaDestino)
            {
                errores.Add(p.escena + " / " + p.objeto + ": deberia llevar a '" + p.escenaDestino +
                            "', pero la puerta '" + p.idDestino + "' esta en " + destino.escena + ".");
            }

            // ¿vuelve a mi? (la regla de oro)
            if (destino.idDestino != p.id)
            {
                errores.Add("IDA Y VUELTA ROTA: '" + p.id + "' (" + p.escena + ") va a '" + p.idDestino +
                            "', pero '" + destino.id + "' vuelve a '" + destino.idDestino + "' y no a '" + p.id + "'.");
            }
        }

        // 3) Informe.
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("===== REVISION DE PUERTAS =====");
        sb.AppendLine(puertas.Count + " puertas encontradas (" + (puertas.Count / 2) + " conexiones).");
        sb.AppendLine();
        sb.AppendLine("--- Todas las puertas y hacia donde sale la niña ---");

        foreach (Info p in puertas)
        {
            sb.AppendLine("  [" + p.escena + "] " + p.id + "  -->  " + p.idDestino +
                          "   (sale hacia: " + p.direccion + ")");
        }

        Debug.Log(sb.ToString());

        foreach (string a in avisos) Debug.LogWarning("[Puertas] " + a);
        foreach (string e in errores) Debug.LogError("[Puertas] " + e);

        if (errores.Count == 0)
        {
            Debug.Log("[Puertas] ✔ Todas las conexiones cierran bien. " +
                      (avisos.Count > 0 ? "(Hay " + avisos.Count + " avisos arriba.)" : ""));
        }
        else
        {
            Debug.LogError("[Puertas] " + errores.Count + " problema(s) para revisar.");
        }
    }

    // Chequea que en la escena haya UNA sola cosa tagueada como "Player", y que sea la niña.
    private static void RevisarTagPlayer(Scene escena, List<string> errores)
    {
        List<string> tagueados = new List<string>();

        Transform[] todos = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Transform t in todos)
        {
            if (t.CompareTag("Player")) tagueados.Add(t.name);
        }

        if (tagueados.Count == 0)
        {
            errores.Add(escena.name + ": no hay NINGUN objeto con el tag 'Player'.");
            return;
        }

        if (tagueados.Count > 1)
        {
            errores.Add(escena.name + ": hay " + tagueados.Count + " objetos con el tag 'Player' (" +
                        string.Join(", ", tagueados.ToArray()) + "). Tiene que haber uno solo: la niña. " +
                        "A los otros ponelos en Untagged.");
        }

        // Y que el que esta tagueado sea de verdad la niña.
        foreach (Transform t in todos)
        {
            if (!t.CompareTag("Player")) continue;

            if (t.GetComponent<PlayerController>() == null && t.GetComponentInParent<PlayerController>() == null)
            {
                errores.Add(escena.name + ": el objeto '" + t.name + "' esta tagueado como 'Player' pero no es la niña " +
                            "(no tiene PlayerController). Ponelo en Untagged.");
            }
        }
    }

    // ---------------- Auxiliares ----------------

    // Las escenas "Zona ..." que estan en el Build Settings.
    private static List<string> RutasDeZonas()
    {
        List<string> rutas = new List<string>();

        foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
        {
            if (!s.enabled) continue;

            string nombre = System.IO.Path.GetFileNameWithoutExtension(s.path);
            if (nombre.StartsWith("Zona")) rutas.Add(s.path);
        }

        if (rutas.Count == 0)
        {
            Debug.LogWarning("[Puertas] No encontre ninguna escena que se llame 'Zona ...' en el Build Settings.");
        }

        return rutas;
    }

    // Todos los objetos de la escena abierta cuyo nombre empieza con PUERTA (incluye los apagados).
    private static List<Transform> ObjetosDePuerta()
    {
        List<Transform> lista = new List<Transform>();

        Transform[] todos = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Transform t in todos)
        {
            if (t.name.TrimStart().ToUpperInvariant().StartsWith(PREFIJO)) lista.Add(t);
        }

        return lista;
    }

    // "PUERTA Z3 ABAJO 3 --> Z6 ARRIBA 3 (VERTICAL)"  ->  id: "Z3 ABAJO 3", destino: "Z6 ARRIBA 3"
    public static bool ParsearNombre(string nombre, out string id, out string idDestino)
    {
        id = "";
        idDestino = "";

        if (string.IsNullOrEmpty(nombre)) return false;

        string limpio = SinParentesis(nombre).Trim();
        if (!limpio.ToUpperInvariant().StartsWith(PREFIJO)) return false;

        limpio = limpio.Substring(PREFIJO.Length);

        // Aceptamos "-->" y "->".
        int corte = limpio.IndexOf("-->");
        int largoFlecha = 3;
        if (corte < 0)
        {
            corte = limpio.IndexOf("->");
            largoFlecha = 2;
        }
        if (corte < 0) return false;

        id = Normalizar(limpio.Substring(0, corte));
        idDestino = Normalizar(limpio.Substring(corte + largoFlecha));

        return id.Length > 0 && idDestino.Length > 0;
    }

    // "Z6 ARRIBA 3" -> "Zona 6"
    public static string EscenaDeId(string idDestino)
    {
        string t = idDestino.Trim();
        if (t.Length < 2) return "";
        if (t[0] != 'Z' && t[0] != 'z') return "";

        string numero = "";
        for (int i = 1; i < t.Length && char.IsDigit(t[i]); i++)
        {
            numero += t[i];
        }

        return numero.Length > 0 ? "Zona " + numero : "";
    }

    private static string SinParentesis(string texto)
    {
        StringBuilder sb = new StringBuilder();
        int nivel = 0;

        foreach (char c in texto)
        {
            if (c == '(') nivel++;
            else if (c == ')') { if (nivel > 0) nivel--; }
            else if (nivel == 0) sb.Append(c);
        }

        return sb.ToString();
    }

    // Saca espacios de mas: "Z6   ARRIBA  1" -> "Z6 ARRIBA 1"
    private static string Normalizar(string texto)
    {
        string[] partes = texto.Trim().Split(' ');
        StringBuilder sb = new StringBuilder();

        foreach (string p in partes)
        {
            if (p.Length == 0) continue;
            if (sb.Length > 0) sb.Append(' ');
            sb.Append(p);
        }

        return sb.ToString();
    }

    // Crea el asset de ajustes si todavia no existe, asi se puede tocar desde el Inspector.
    private static void CrearAjustesSiFaltan()
    {
        const string carpeta = "Assets/Resources";
        const string ruta = carpeta + "/AjustesTransicion.asset";

        if (AssetDatabase.LoadAssetAtPath<AjustesTransicion>(ruta) != null) return;

        if (!AssetDatabase.IsValidFolder(carpeta))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        AjustesTransicion ajustes = ScriptableObject.CreateInstance<AjustesTransicion>();
        AssetDatabase.CreateAsset(ajustes, ruta);
        AssetDatabase.SaveAssets();

        Debug.Log("[Puertas] Cree " + ruta + ". Seleccionalo en el Project para ajustar los tiempos de la transicion.");
    }
}
#endif
