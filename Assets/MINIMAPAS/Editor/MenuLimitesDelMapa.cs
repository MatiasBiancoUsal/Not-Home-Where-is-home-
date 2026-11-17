#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// ============================================================
//  LIMITES DEL MAPA, ZONA POR ZONA
//
//  El minimapa necesita saber de donde a donde va la zona para poder ubicar el
//  marcador de la niña. Esos limites estan en el prefab del minimapa, asi que todas
//  las zonas que no los pisen con los suyos terminan usando los de la Zona 1: el
//  marcador queda fuera del dibujo y el mapa "no anda".
//
//  Este comando recorre las 6 zonas, mide cuanto ocupa el escenario de cada una y le
//  escribe SUS limites al minimapa.
// ============================================================
public static class MenuLimitesDelMapa
{
    private const string CARPETA = "Assets/-------ESCENAS/";
    private static readonly string[] ZONAS = { "Zona 1", "Zona 2", "Zona 3", "Zona 4", "Zona 5", "Zona 6" };

    [MenuItem("Not Home/Minimapas/Calcular los limites del mapa en todas las zonas", false, 20)]
    private static void CalcularEnTodas()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.Log("[Minimapa] Cancelado: primero guardá la escena que tenés abierta.");
            return;
        }

        string abierta = SceneManager.GetActiveScene().path;
        StringBuilder reporte = new StringBuilder("[Minimapa] limites por zona\n");

        foreach (string nombre in ZONAS)
        {
            string ruta = CARPETA + nombre + ".unity";
            if (!System.IO.File.Exists(ruta))
            {
                reporte.AppendLine(nombre + ": no existe la escena");
                continue;
            }

            Scene escena = EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);

            MinimapUI mapa = Object.FindFirstObjectByType<MinimapUI>(FindObjectsInactive.Include);
            if (mapa == null)
            {
                reporte.AppendLine(nombre + ": no tiene minimapa");
                continue;
            }

            if (!MedirElEscenario(out Vector2 min, out Vector2 max))
            {
                reporte.AppendLine(nombre + ": no encontre colliders de escenario para medir");
                continue;
            }

            // Los campos son privados: se escriben por SerializedObject, que ademas deja
            // el cambio guardado como override del prefab.
            SerializedObject so = new SerializedObject(mapa);
            so.FindProperty("useExactWorldBounds").boolValue = true;
            so.FindProperty("exactWorldMin").vector2Value = min;
            so.FindProperty("exactWorldMax").vector2Value = max;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(mapa);
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);

            reporte.AppendLine(nombre + ": de (" + min.x.ToString("F0") + ", " + min.y.ToString("F0") +
                               ") a (" + max.x.ToString("F0") + ", " + max.y.ToString("F0") + ")");
        }

        if (!string.IsNullOrEmpty(abierta)) EditorSceneManager.OpenScene(abierta, OpenSceneMode.Single);

        reporte.AppendLine("Listo. Probá el mapa en cada zona: el marcador tiene que caer donde está la niña.");
        Debug.Log(reporte.ToString());
    }

    // Mide la zona juntando todos los colliders SOLIDOS del escenario. Deja afuera los
    // triggers (puertas, checkpoints), al player y a lo que se mueva: esos no son el mapa.
    private static bool MedirElEscenario(out Vector2 min, out Vector2 max)
    {
        min = Vector2.zero;
        max = Vector2.zero;
        bool encontreAlguno = false;
        Bounds total = new Bounds();

        foreach (Collider2D col in Object.FindObjectsByType<Collider2D>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (col == null || !col.enabled || col.isTrigger) continue;
            if (col.GetComponentInParent<PlayerController>() != null) continue;

            Rigidbody2D rb = col.attachedRigidbody;
            if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic) continue;

            if (!encontreAlguno)
            {
                total = col.bounds;
                encontreAlguno = true;
            }
            else
            {
                total.Encapsulate(col.bounds);
            }
        }

        if (!encontreAlguno) return false;

        min = total.min;
        max = total.max;
        return true;
    }
}
#endif
