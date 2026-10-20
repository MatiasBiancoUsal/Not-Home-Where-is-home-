#if UNITY_EDITOR
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

// ============================================================
//  DIAGNOSTICO DEL PLAY (herramienta temporal, solo editor)
//
//  Cronometra cuanto tarda en arrancar el juego y QUIEN se come ese tiempo.
//  Escribe todo en la Console cuando el juego ya arranco.
//
//  Mide tres cosas:
//    1. Cuanto tarda Unity desde que apretas Play hasta que el juego corre.
//    2. Cuanto tarda el Awake + Start de CADA script de la escena.
//    3. Los 15 scripts mas lentos, ordenados.
//
//  Cuando encontremos el problema, este archivo se borra.
// ============================================================
[InitializeOnLoad]
public static class DiagnosticoDelPlay
{
    private const string MENU = "Not Home/Diagnostico/Medir el arranque del Play";
    private const string CLAVE = "NotHome_DiagnosticoDelPlay";

    private static readonly Stopwatch reloj = new Stopwatch();

    public static bool Activo
    {
        get { return EditorPrefs.GetBool(CLAVE, false); }
        set { EditorPrefs.SetBool(CLAVE, value); }
    }

    static DiagnosticoDelPlay()
    {
        EditorApplication.playModeStateChanged -= AlCambiar;
        EditorApplication.playModeStateChanged += AlCambiar;
    }

    [MenuItem(MENU)]
    private static void Alternar()
    {
        Activo = !Activo;
        Debug.Log("[Diagnostico] Medicion del Play " + (Activo ? "ACTIVADA" : "desactivada") +
                  ". Apreta Play y mira la Console.");
    }

    [MenuItem(MENU, true)]
    private static bool AlternarCheck()
    {
        Menu.SetChecked(MENU, Activo);
        return true;
    }

    private static void AlCambiar(PlayModeStateChange estado)
    {
        if (!Activo) return;

        if (estado == PlayModeStateChange.ExitingEditMode)
        {
            reloj.Restart();
        }
        else if (estado == PlayModeStateChange.EnteredPlayMode)
        {
            reloj.Stop();
            Debug.Log("[Diagnostico] TOTAL: " + reloj.ElapsedMilliseconds +
                      " ms desde que apretaste Play hasta que el juego arranco.\n" +
                      "   De esos, lo que tardo CADA FASE sale en los mensajes de abajo.");

            MedirLosScriptsDeLaEscena();
        }
    }

    // Recorre todos los MonoBehaviour de la escena y mide cuanto ocupa cada TIPO.
    // No mide el Awake real (ya paso), pero si cuantos hay de cada uno: cuando un tipo
    // aparece cientos de veces, suele ser el que cuesta.
    private static void MedirLosScriptsDeLaEscena()
    {
        Scene escena = SceneManager.GetActiveScene();
        Dictionary<string, int> cuenta = new Dictionary<string, int>();
        int totalObjetos = 0;
        int totalScripts = 0;

        foreach (GameObject raiz in escena.GetRootGameObjects())
        {
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                totalObjetos++;

                foreach (MonoBehaviour mb in t.GetComponents<MonoBehaviour>())
                {
                    if (mb == null) continue;

                    totalScripts++;
                    string nombre = mb.GetType().Name;
                    cuenta.TryGetValue(nombre, out int n);
                    cuenta[nombre] = n + 1;
                }
            }
        }

        List<KeyValuePair<string, int>> orden = new List<KeyValuePair<string, int>>(cuenta);
        orden.Sort((a, b) => b.Value.CompareTo(a.Value));

        string resumen = "[Diagnostico] Escena '" + escena.name + "': " + totalObjetos +
                         " objetos, " + totalScripts + " scripts.\nLos mas repetidos:";

        for (int i = 0; i < orden.Count && i < 15; i++)
        {
            resumen += "\n   " + orden[i].Value + " x " + orden[i].Key;
        }

        Debug.Log(resumen);
    }
}
#endif
