#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// ============================================================
//  ARREGLAR LOS COLLIDERS DE LOS TILEMAPS (solo editor)
//
//  EL PROBLEMA: el Tilemap Collider 2D solo hace UNA CAJITA POR TILE. Entre
//  cajita y cajita queda una junta microscopica, el capsule de la niña se
//  engancha ahi, rebota un pixel, el chequeo de piso la ve un frame en el aire
//  y se dispara la animacion de salto. Resultado: camina dando brincos.
//
//  LA SOLUCION: un Composite Collider 2D suelda todas esas cajitas en UN SOLO
//  contorno continuo. Se van las juntas y camina liso. Necesita ademas un
//  Rigidbody 2D en Static (si queda en Dynamic, el escenario se cae al dar Play).
//
//  Esto recorre las 6 zonas y lo deja configurado igual en todas.
//
//  SOLO toca tilemaps que YA tienen un Tilemap Collider 2D y estan en la capa
//  Ground: a los de decoracion no les agrega nada (si no, quedarian paredes
//  invisibles en el aire).
//
//  Menu:  Not Home > Tilemaps > Arreglar los colliders (que camine liso)
// ============================================================
public static class MenuTilemaps
{
    // Un pelin de "engorde" de cada tile para que no queden pelos de luz entre
    // uno y otro al soldarlos. Es el valor que ya tenian Zona 2 y Zona 3.
    private const float ENGORDE = 0.01f;

    private const int CAPA_PISO = 3;   // Ground

    [MenuItem("Not Home/Tilemaps/Arreglar los colliders (que camine liso)", false, 30)]
    private static void Arreglar()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string escenaOriginal = EditorSceneManager.GetActiveScene().path;
        StringBuilder reporte = new StringBuilder();
        int arreglados = 0;

        reporte.AppendLine("[Tilemaps] Colliders de piso:");

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

                int enEstaZona = 0;
                bool encontreAlguno = false;

                foreach (TilemapCollider2D col in Object.FindObjectsByType<TilemapCollider2D>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (col == null || col.gameObject.layer != CAPA_PISO) continue;

                    encontreAlguno = true;
                    if (ArreglarUno(col, reporte)) enEstaZona++;
                }

                if (!encontreAlguno)
                {
                    reporte.AppendLine("      (no hay tilemaps de piso en esta zona)");
                }

                if (enEstaZona > 0)
                {
                    EditorSceneManager.MarkSceneDirty(escena);
                    EditorSceneManager.SaveScene(escena);
                    arreglados += enEstaZona;
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
        reporte.AppendLine(arreglados == 0
            ? "   Ya estaba todo bien, no hubo que tocar nada."
            : "   Listo: " + arreglados + " tilemap(s) arreglados y guardados.");

        Debug.Log(reporte.ToString());
    }

    private static bool ArreglarUno(TilemapCollider2D col, StringBuilder reporte)
    {
        GameObject go = col.gameObject;
        StringBuilder queHice = new StringBuilder();

        // 1) El Rigidbody 2D. El Composite lo necesita si o si, y va en Static:
        //    en Dynamic se caeria todo el escenario al dar Play.
        Rigidbody2D cuerpo = go.GetComponent<Rigidbody2D>();
        if (cuerpo == null)
        {
            cuerpo = Undo.AddComponent<Rigidbody2D>(go);
            queHice.Append(" + Rigidbody 2D");
        }
        if (cuerpo.bodyType != RigidbodyType2D.Static)
        {
            Undo.RecordObject(cuerpo, "Rigidbody a Static");
            cuerpo.bodyType = RigidbodyType2D.Static;
            queHice.Append(" + Body Type a Static");
        }

        // 2) El Composite Collider 2D, que es el que suelda las cajitas.
        CompositeCollider2D composite = go.GetComponent<CompositeCollider2D>();
        if (composite == null)
        {
            composite = Undo.AddComponent<CompositeCollider2D>(go);
            queHice.Append(" + Composite Collider 2D");
        }
        if (composite.geometryType != CompositeCollider2D.GeometryType.Polygons)
        {
            // Outlines deja el piso HUECO y la niña se puede meter adentro.
            Undo.RecordObject(composite, "Composite a Polygons");
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            queHice.Append(" + Geometry Type a Polygons");
        }

        // 3) Y avisarle al Tilemap Collider que le pase sus cajitas al Composite.
        //    Sin esto, el Composite esta pero no hace nada (le paso a la Zona 6).
        if (col.compositeOperation != Collider2D.CompositeOperation.Merge)
        {
            Undo.RecordObject(col, "Tilemap Collider a Merge");
            col.compositeOperation = Collider2D.CompositeOperation.Merge;
            queHice.Append(" + Composite Operation a Merge");
        }
        if (!Mathf.Approximately(col.extrusionFactor, ENGORDE))
        {
            Undo.RecordObject(col, "Extrusion Factor");
            col.extrusionFactor = ENGORDE;
            queHice.Append(" + Extrusion Factor a " + ENGORDE);
        }

        if (queHice.Length == 0)
        {
            reporte.AppendLine("      • " + go.name + ": ya estaba bien");
            return false;
        }

        EditorUtility.SetDirty(go);
        reporte.AppendLine("      • " + go.name + ":" + queHice);
        return true;
    }
}
#endif
