#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// ============================================================
//  CONTAR LOS PUNTOS DE CADA ZONA (solo editor)
//
//  Recorre las 6 zonas, cuenta TODAS las monedas (tambien las que estan apagadas)
//  y escribe en la consola cuantos puntos hay en cada una y en todo el juego.
//
//  Sirve para poner el precio de las habilidades: cada vez que se agregan o sacan
//  monedas, se vuelve a correr y los numeros quedan actualizados.
//
//  Menu:  Not Home > Puntos > Contar los puntos de todas las zonas
// ============================================================
public static class MenuContarPuntos
{
    [MenuItem("Not Home/Puntos/Contar los puntos de todas las zonas", false, 60)]
    private static void Contar()
    {
        // Si hay cambios sin guardar, primero preguntamos: vamos a abrir otras escenas.
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string escenaOriginal = EditorSceneManager.GetActiveScene().path;
        StringBuilder reporte = new StringBuilder();
        int totalDelJuego = 0;

        System.Collections.Generic.List<TotalesDePuntos.TotalDeZona> lista =
            new System.Collections.Generic.List<TotalesDePuntos.TotalDeZona>();

        reporte.AppendLine("[Puntos] Puntos que se pueden juntar en cada zona:");

        try
        {
            foreach (EditorBuildSettingsScene entrada in EditorBuildSettings.scenes)
            {
                if (!entrada.enabled) continue;

                string nombre = System.IO.Path.GetFileNameWithoutExtension(entrada.path);
                if (!nombre.StartsWith("Zona")) continue;

                Scene escena = EditorSceneManager.OpenScene(entrada.path, OpenSceneMode.Single);

                int puntos = 0, monedas = 0;
                Coleccionable[] todas = Object.FindObjectsByType<Coleccionable>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);

                foreach (Coleccionable m in todas)
                {
                    if (m == null || m.gameObject.scene != escena) continue;
                    puntos += Mathf.Max(0, m.puntos);
                    monedas++;
                }

                totalDelJuego += puntos;
                lista.Add(new TotalesDePuntos.TotalDeZona { escena = nombre, puntos = puntos });
                reporte.AppendLine("   " + nombre + ": " + puntos + " puntos (" + monedas + " monedas)");
            }
        }
        finally
        {
            if (!string.IsNullOrEmpty(escenaOriginal))
            {
                EditorSceneManager.OpenScene(escenaOriginal, OpenSceneMode.Single);
            }
        }

        GuardarEnElAsset(lista.ToArray());

        reporte.AppendLine("   TOTAL DEL JUEGO: " + totalDelJuego + " puntos");
        reporte.AppendLine("   (guardado en Assets/Resources/TotalesDePuntos, que es lo que muestra el marcador)");
        Debug.Log(reporte.ToString());
    }

    // El marcador del juego lee este asset para saber el total del juego sin tener que
    // visitar todas las zonas. Se crea la primera vez y despues se va actualizando.
    private static void GuardarEnElAsset(TotalesDePuntos.TotalDeZona[] zonas)
    {
        const string CARPETA = "Assets/Resources";
        const string RUTA = CARPETA + "/TotalesDePuntos.asset";

        if (!AssetDatabase.IsValidFolder(CARPETA))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        TotalesDePuntos asset = AssetDatabase.LoadAssetAtPath<TotalesDePuntos>(RUTA);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<TotalesDePuntos>();
            AssetDatabase.CreateAsset(asset, RUTA);
        }

        asset.zonas = zonas;
        asset.ultimoConteo = System.DateTime.Now.ToString("dd/MM/yyyy HH:mm");

        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
    }
}
#endif
