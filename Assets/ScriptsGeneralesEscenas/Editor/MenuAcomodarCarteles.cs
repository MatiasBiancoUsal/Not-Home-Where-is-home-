#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// ============================================================
//  COPIAR EL DISEÑO DE UN CARTEL A TODAS LAS ZONAS (solo editor)
//
//  Acomodas UNA flor a mano, como te guste (posiciones, tamaños, tipografias,
//  colores, sonidos del tipeo, los textos comunes) y despues corres esto: todas
//  las demas flores y los carteles de indicaciones del juego quedan igualitos.
//
//  Asi no hay que repetir los mismos numeros seis veces ni quedan carteles
//  distintos entre si.
//
//  LA FLOR MODELO es la de abajo. Si preferis ajustar otra, cambia estas dos
//  lineas y listo.
//
//  NO se copia: la habilidad que entrega cada flor, su costo, ni las
//  explicaciones de cada habilidad (esas son propias de cada una).
//
//  Menu:  Not Home > Carteles > Copiar el diseño de la flor modelo a todas las zonas
// ============================================================
public static class MenuAcomodarCarteles
{
    // De donde se copia. Ajusta ESTA flor a mano y despues corre el comando.
    private const string ZONA_MODELO = "Zona 3";
    private const string FLOR_MODELO = "FlorDash";

    private const string RUTA_PREFAB = "Assets/Conseguir habilidad/Flor.prefab";

    // La descripcion es el texto mas LARGO del cartel (el titulo son dos palabras).
    // Si su caja queda mas angosta que la del titulo, los renglones se parten en
    // lugares feos ("...para desbloquear / DASH?") y el bloque se ve apretado.
    // Con esto la descripcion usa el mismo ancho que el titulo.
    //
    // Ponelo en false si alguna vez querés la descripcion mas angosta a proposito.
    private const bool DESCRIPCION_TAN_ANCHA_COMO_EL_TITULO = true;

    // Los textos que son iguales en todas las flores. Los de cada habilidad no estan
    // aca a proposito: esos los escribe cada flor por su cuenta.
    private static readonly string[] TEXTOS_COMUNES =
    {
        "tituloDeLaPregunta",
        "textoDeLaPregunta",
        "tituloSinPuntos",
        "textoParaCerrar"
    };

    [MenuItem("Not Home/Carteles/Copiar el diseño de la flor modelo a todas las zonas", false, 40)]
    private static void Copiar()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string escenaOriginal = EditorSceneManager.GetActiveScene().path;
        StringBuilder reporte = new StringBuilder();

        EstiloCartelHabilidad modelo = new EstiloCartelHabilidad();
        string[] textos = new string[TEXTOS_COMUNES.Length];
        int copiados = 0;

        try
        {
            // ---- 1. Leer la flor modelo ----
            if (!LeerElModelo(modelo, textos, reporte))
            {
                Debug.LogWarning(reporte.ToString());
                return;
            }

            reporte.AppendLine("[Carteles] Diseño copiado de " + ZONA_MODELO + " > " + FLOR_MODELO + ":");
            reporte.AppendLine("   titulo a " + modelo.tamanoTitulo.ToString("0.#") +
                               ", descripcion a " + modelo.tamanoDescripcion.ToString("0.#") +
                               ", texto centrado en x=" + modelo.posicionTitulo.x.ToString("0.#") +
                               ", caja de la descripcion de " + modelo.cajaDescripcion.x.ToString("0.#"));
            reporte.AppendLine("");

            // ---- 2. El prefab, para que las flores nuevas ya nazcan iguales ----
            copiados += CopiarAlPrefab(modelo, textos, reporte);

            // ---- 3. Todas las zonas ----
            foreach (EditorBuildSettingsScene entrada in EditorBuildSettings.scenes)
            {
                if (!entrada.enabled) continue;

                string nombreEscena = System.IO.Path.GetFileNameWithoutExtension(entrada.path);
                if (!nombreEscena.StartsWith("Zona")) continue;

                Scene escena = EditorSceneManager.OpenScene(entrada.path, OpenSceneMode.Single);
                int enEstaZona = 0;

                foreach (ActivarFlor flor in Object.FindObjectsByType<ActivarFlor>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (AplicarAFlor(flor, modelo, textos, reporte, escena.name)) enEstaZona++;
                }

                foreach (CartelIndicaciones cartel in Object.FindObjectsByType<CartelIndicaciones>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (AplicarACartel(cartel, modelo, reporte, escena.name)) enEstaZona++;
                }

                if (enEstaZona > 0)
                {
                    EditorSceneManager.MarkSceneDirty(escena);
                    EditorSceneManager.SaveScene(escena);
                    copiados += enEstaZona;
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
        reporte.AppendLine("   Listo: " + copiados + " cartel(es) quedaron iguales al modelo.");

        AssetDatabase.SaveAssets();
        Debug.Log(reporte.ToString());
    }

    // Abre la zona modelo y se guarda una COPIA de su estilo y de sus textos comunes.
    // Tiene que ser una copia de verdad: en cuanto abramos otra escena, la flor modelo
    // deja de existir y se llevaria el estilo con ella.
    private static bool LeerElModelo(EstiloCartelHabilidad destino, string[] textos, StringBuilder reporte)
    {
        string ruta = RutaDeLaEscena(ZONA_MODELO);
        if (string.IsNullOrEmpty(ruta))
        {
            reporte.AppendLine("[Carteles] No encontre la escena '" + ZONA_MODELO + "'. " +
                               "Revisa el nombre arriba de MenuAcomodarCarteles.cs.");
            return false;
        }

        EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);

        ActivarFlor modelo = null;
        foreach (ActivarFlor flor in Object.FindObjectsByType<ActivarFlor>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (flor == null) continue;
            if (modelo == null) modelo = flor;                       // por las dudas
            if (flor.gameObject.name == FLOR_MODELO) { modelo = flor; break; }
        }

        if (modelo == null)
        {
            reporte.AppendLine("[Carteles] En '" + ZONA_MODELO + "' no hay ninguna flor. " +
                               "No se copio nada.");
            return false;
        }

        destino.CopiarDesde(modelo.EstiloDelCartel);

        // Se corrige aca, sobre la copia: asi TODOS los carteles salen con el ancho
        // bueno, incluida la flor modelo cuando el recorrido llegue a su zona.
        if (DESCRIPCION_TAN_ANCHA_COMO_EL_TITULO && destino.cajaTitulo.x > destino.cajaDescripcion.x)
        {
            destino.cajaDescripcion = new Vector2(destino.cajaTitulo.x, destino.cajaDescripcion.y);
        }

        SerializedObject so = new SerializedObject(modelo);
        for (int i = 0; i < TEXTOS_COMUNES.Length; i++)
        {
            SerializedProperty p = so.FindProperty(TEXTOS_COMUNES[i]);
            textos[i] = p != null && p.propertyType == SerializedPropertyType.String ? p.stringValue : null;
        }

        return true;
    }

    private static int CopiarAlPrefab(EstiloCartelHabilidad modelo, string[] textos, StringBuilder reporte)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RUTA_PREFAB);
        if (prefab == null)
        {
            reporte.AppendLine("   (no encontre " + RUTA_PREFAB + ", salteado)");
            return 0;
        }

        ActivarFlor flor = prefab.GetComponent<ActivarFlor>();
        if (flor == null) return 0;

        if (!AplicarAFlor(flor, modelo, textos, reporte, "Flor.prefab")) return 0;

        PrefabUtility.SavePrefabAsset(prefab);
        return 1;
    }

    private static bool AplicarAFlor(ActivarFlor flor, EstiloCartelHabilidad modelo, string[] textos,
                                     StringBuilder reporte, string donde)
    {
        if (flor == null) return false;

        flor.EstiloDelCartel.CopiarDesde(modelo);

        SerializedObject so = new SerializedObject(flor);
        for (int i = 0; i < TEXTOS_COMUNES.Length; i++)
        {
            if (textos[i] == null) continue;

            SerializedProperty p = so.FindProperty(TEXTOS_COMUNES[i]);
            if (p != null && p.propertyType == SerializedPropertyType.String) p.stringValue = textos[i];
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        Anotar(flor);
        reporte.AppendLine("   " + donde + " > " + flor.gameObject.name);
        return true;
    }

    private static bool AplicarACartel(CartelIndicaciones cartel, EstiloCartelHabilidad modelo,
                                       StringBuilder reporte, string donde)
    {
        if (cartel == null) return false;

        if (cartel.estiloCartel == null) cartel.estiloCartel = new EstiloCartelHabilidad();
        cartel.estiloCartel.CopiarDesde(modelo);

        Anotar(cartel);
        reporte.AppendLine("   " + donde + " > " + cartel.gameObject.name);
        return true;
    }

    // Tocamos el objeto directamente (es lo unico que puede copiar el estilo entero de
    // una), asi que hay que avisarle a Unity: que lo guarde, y que si es una instancia
    // de prefab anote los cambios como overrides suyos.
    private static void Anotar(Component componente)
    {
        if (PrefabUtility.IsPartOfPrefabInstance(componente))
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(componente);
        }

        EditorUtility.SetDirty(componente);
    }

    private static string RutaDeLaEscena(string nombre)
    {
        foreach (EditorBuildSettingsScene entrada in EditorBuildSettings.scenes)
        {
            if (System.IO.Path.GetFileNameWithoutExtension(entrada.path) == nombre) return entrada.path;
        }
        return null;
    }
}
#endif
