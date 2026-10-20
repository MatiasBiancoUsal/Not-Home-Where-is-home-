using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

// ============================================================
//  DIAGNOSTICO: EN QUE FASE SE VA EL TIEMPO AL ARRANCAR
//
//  Herramienta temporal para encontrar por que tarda tanto en arrancar el Play.
//  No hay que ponerlo en ningun objeto: corre solo.
//
//  Marca cuatro momentos y dice cuanto paso entre cada uno:
//    1. BeforeSceneLoad  -> Unity ya cargo el codigo, todavia no la escena.
//    2. AfterSceneLoad   -> la escena cargo y corrieron TODOS los Awake.
//    3. Primer Update    -> corrieron todos los Start y se dibujo el primer frame.
//
//  Si el tiempo grande esta entre 1 y 2, el problema es la escena (Awakes, fisica,
//  tilemaps). Si esta ANTES del 1, el problema es de Unity o de los paquetes
//  (servicios, assets), y no de la escena.
//
//  Cuando encontremos el problema, este archivo se borra.
// ============================================================
public static class DiagnosticoFasesDelArranque
{
    private static readonly Stopwatch reloj = new Stopwatch();
    private static long enBeforeScene;
    private static long enAfterScene;
    private static bool yaInformo;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reiniciar()
    {
        reloj.Restart();
        enBeforeScene = 0;
        enAfterScene = 0;
        yaInformo = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AntesDeLaEscena()
    {
        enBeforeScene = reloj.ElapsedMilliseconds;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void DespuesDeLaEscena()
    {
        enAfterScene = reloj.ElapsedMilliseconds;

        // Un objeto invisible que vive un frame, solo para medir hasta el primer Update.
        GameObject go = new GameObject("~diagnostico");
        go.hideFlags = HideFlags.HideAndDontSave;
        go.AddComponent<MarcaDelPrimerFrame>();
    }

    internal static void InformarPrimerFrame()
    {
        if (yaInformo) return;
        yaInformo = true;

        long primerFrame = reloj.ElapsedMilliseconds;

        Debug.Log(
            "[Diagnostico] FASES DEL ARRANQUE (en milisegundos)\n" +
            "   1) Codigo listo -> antes de cargar la escena: " + enBeforeScene + " ms\n" +
            "   2) Cargar la escena + todos los Awake:        " + (enAfterScene - enBeforeScene) + " ms\n" +
            "   3) Los Start + el primer frame dibujado:      " + (primerFrame - enAfterScene) + " ms\n" +
            "   ----------------------------------------------\n" +
            "   Desde que arranco el codigo hasta jugar:      " + primerFrame + " ms\n\n" +
            "   Si estos numeros son CHICOS pero igual tardaste mucho mirando la pantalla,\n" +
            "   el tiempo se va ANTES: en Unity preparando el Play, no en tu juego.");
    }

    private class MarcaDelPrimerFrame : MonoBehaviour
    {
        private void Update()
        {
            InformarPrimerFrame();
            Destroy(gameObject);
        }
    }
}
