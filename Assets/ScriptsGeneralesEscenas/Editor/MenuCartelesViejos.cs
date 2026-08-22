#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// ============================================================
//  QUITAR LOS CARTELES DE ZONA VIEJOS (solo editor)
//
//  Contexto: hubo DOS sistemas de nombre de zona conviviendo.
//    - El de sprites (ZoneNameUI + prefab ZoneNamePanel): se dispara en el Start()
//      de CADA escena, asi que aparecia siempre, encimado con el otro.
//    - El de TransicionZonas: aparece en las transiciones entre zonas y en la intro.
//
//  Este comando saca el ZoneNamePanel de las 6 zonas, PERO NO BORRA NADA del proyecto:
//  el prefab, el script y los PNG siguen estando en Assets. Si algun dia se quiere volver
//  atras, alcanza con arrastrar el prefab de nuevo a las escenas.
// ============================================================
public static class MenuCartelesViejos
{
    [MenuItem("Not Home/Quitar carteles de zona viejos (ZoneNamePanel)", false, 40)]
    private static void Quitar()
    {
        if (!EditorUtility.DisplayDialog(
                "Quitar carteles de zona viejos",
                "Voy a sacar el objeto 'ZoneNamePanel' de las 6 zonas.\n\n" +
                "NO se borra nada del proyecto: el prefab, el script ZoneNameUI y los PNG de " +
                "CartelesZonas quedan donde estan. Solo dejan de estar puestos en las escenas.\n\n" +
                "¿Seguimos?",
                "Dale", "Cancelar"))
        {
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string escenaOriginal = EditorSceneManager.GetActiveScene().path;
        int sacados = 0;

        try
        {
            foreach (EditorBuildSettingsScene entrada in EditorBuildSettings.scenes)
            {
                if (!entrada.enabled) continue;

                string nombre = System.IO.Path.GetFileNameWithoutExtension(entrada.path);
                if (!nombre.StartsWith("Zona")) continue;

                Scene escena = EditorSceneManager.OpenScene(entrada.path, OpenSceneMode.Single);
                int enEstaEscena = 0;

                ZoneNameUI[] carteles = Object.FindObjectsByType<ZoneNameUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);

                foreach (ZoneNameUI cartel in carteles)
                {
                    if (cartel == null) continue;

                    GameObject aBorrar = RaizDelCartel(cartel.gameObject);
                    if (aBorrar == null) continue;

                    // RED DE SEGURIDAD: si lo que ibamos a borrar contiene el HUD del juego,
                    // NO se toca. Preferimos dejar el cartel viejo antes que borrar los
                    // puntos o los corazones por un error de jerarquia.
                    if (aBorrar.GetComponentInChildren<ScoreUI>(true) != null ||
                        aBorrar.GetComponentInChildren<HeartUI>(true) != null)
                    {
                        Debug.LogError("[Carteles] En " + escena.name + " el cartel esta adentro de '" + aBorrar.name +
                                       "', que tambien tiene el HUD. NO lo toco: sacalo a mano desde la Hierarchy.", aBorrar);
                        continue;
                    }

                    string queSaco = aBorrar.name;

                    try
                    {
                        Object.DestroyImmediate(aBorrar);
                        enEstaEscena++;
                        sacados++;
                        Debug.Log("[Carteles] " + escena.name + ": saque '" + queSaco + "'.");
                    }
                    catch (System.Exception e)
                    {
                        // Puede pasar si Unity no deja reestructurar el prefab. En ese caso
                        // al menos lo apagamos, asi deja de aparecer en el juego.
                        aBorrar.SetActive(false);
                        Debug.LogWarning("[Carteles] " + escena.name + ": no pude borrar '" + queSaco +
                                         "', lo deje DESACTIVADO. (" + e.Message + ")", aBorrar);
                        enEstaEscena++;
                    }
                }

                if (enEstaEscena > 0)
                {
                    EditorSceneManager.MarkSceneDirty(escena);
                    EditorSceneManager.SaveScene(escena);
                }
                else
                {
                    Debug.Log("[Carteles] " + escena.name + ": no habia ningun cartel viejo.");
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

        Debug.Log("[Carteles] LISTO: " + sacados + " cartel(es) viejo(s) fuera de las escenas. " +
                  "El prefab, el script y los PNG siguen en el proyecto por si se quieren usar despues.");
    }

    // El objeto que hay que sacar es la RAIZ del ZoneNamePanel, no el hijo que tiene el script.
    private static GameObject RaizDelCartel(GameObject desde)
    {
        if (!PrefabUtility.IsPartOfPrefabInstance(desde)) return desde;

        // "Nearest" y no "Outermost": si estuviera anidado adentro del CANVATODO,
        // el outermost seria el CANVATODO entero y borrariamos todo el HUD.
        GameObject raiz = PrefabUtility.GetNearestPrefabInstanceRoot(desde);
        return raiz != null ? raiz : desde;
    }
}
#endif
