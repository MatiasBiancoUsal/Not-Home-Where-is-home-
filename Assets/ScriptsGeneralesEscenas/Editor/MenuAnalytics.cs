#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// ============================================================
//  INTERRUPTOR DE ANALYTICS PARA PROBAR (solo editor)
//
//  Normalmente, cuando le damos Play en Unity los eventos NO se mandan a Unity
//  Cloud: solo se escriben en la Consola ("[Analytics] Muerte"). Asi nuestras
//  pruebas no ensucian los numeros de los jugadores de verdad.
//
//  Pero antes de la build hay que comprobar UNA VEZ que los eventos realmente
//  llegan al dashboard. Para eso se prende esto, se juega un rato, se mira el
//  dashboard... y se vuelve a apagar.
//
//  El menu queda TILDADO mientras esta prendido, justamente para que no se nos
//  olvide apagarlo. La build final no mira esta opcion: ahi siempre manda.
//
//  Menu:  Not Home > Analytics > Mandar eventos desde el editor (PRUEBA)
// ============================================================
public static class MenuAnalytics
{
    private const string MENU = "Not Home/Analytics/Mandar eventos desde el editor (PRUEBA)";

    [MenuItem(MENU, false, 80)]
    private static void Alternar()
    {
        bool prendido = !EditorPrefs.GetBool(AnalyticsJuego.PREF_DESDE_EDITOR, false);
        EditorPrefs.SetBool(AnalyticsJuego.PREF_DESDE_EDITOR, prendido);

        if (prendido)
        {
            Debug.LogWarning(
                "[Analytics] PRENDIDO: desde ahora, lo que juegues en el editor SE MANDA a Unity Cloud.\n" +
                "   Jugá un rato (entrá a una zona, mori, tocá una flor sin puntos) y esperá un minuto.\n" +
                "   Despues mirá el dashboard y APAGALO de nuevo desde el mismo menu, asi las pruebas " +
                "del equipo no ensucian los datos de los jugadores.");
        }
        else
        {
            Debug.Log("[Analytics] APAGADO: lo que juegues en el editor ya no se manda a Unity Cloud " +
                      "(los mensajes de la Consola siguen apareciendo igual). La build SI manda siempre.");
        }
    }

    // El tilde del menu.
    [MenuItem(MENU, true)]
    private static bool EstaPrendido()
    {
        Menu.SetChecked(MENU, EditorPrefs.GetBool(AnalyticsJuego.PREF_DESDE_EDITOR, false));
        return true;
    }
}
#endif
