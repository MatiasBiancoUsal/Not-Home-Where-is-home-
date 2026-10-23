#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEngine;

// ============================================================
//  REVISAR LOS BLOQUEOS TEMPORALES (solo editor)
//
//  Mira todos los objetos de la escena abierta que tengan el script BloqueoTemporal
//  y avisa si les falta algo para funcionar:
//    - sin cinematica asignada  -> nunca se abren
//    - sin collider             -> no tapan el paso (son solo un dibujo)
//    - apagados                 -> su Awake no corre
//    - ya abiertos en el progreso guardado -> no van a aparecer en la partida
//
//  Menu:  Not Home > Bloqueos > Revisar los bloqueos de esta escena
// ============================================================
public static class MenuBloqueos
{
    [MenuItem("Not Home/Bloqueos/Revisar los bloqueos de esta escena", false, 90)]
    private static void Revisar()
    {
        BloqueoTemporal[] bloqueos = Object.FindObjectsByType<BloqueoTemporal>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (bloqueos.Length == 0)
        {
            Debug.LogWarning("[Bloqueos] En esta escena no hay NINGUN objeto con el script BloqueoTemporal. " +
                             "Si hay cosas que tienen que desaparecer despues de la cinematica, hay que agregarselo.");
            return;
        }

        StringBuilder reporte = new StringBuilder();
        reporte.AppendLine("[Bloqueos] Encontre " + bloqueos.Length + " bloqueo(s) en la escena:");

        int problemas = 0;

        foreach (BloqueoTemporal b in bloqueos)
        {
            if (b == null) continue;

            string nombre = b.gameObject.name;
            reporte.AppendLine("");
            reporte.AppendLine("   • " + nombre);

            // ¿Esta prendido? Si esta apagado, su Awake no corre y nunca se engancha.
            if (!b.gameObject.activeInHierarchy)
            {
                reporte.AppendLine("       PROBLEMA: esta APAGADO (o su padre lo esta). Prendelo.");
                problemas++;
            }

            // ¿Tiene la cinematica puesta?
            if (b.cinematica == null)
            {
                reporte.AppendLine("       PROBLEMA: le falta la CINEMATICA. Arrastrasela al campo 'Cinematica'.");
                problemas++;
            }
            else if (!b.cinematica.gameObject.scene.IsValid())
            {
                reporte.AppendLine("       PROBLEMA: la cinematica asignada no esta en esta escena.");
                problemas++;
            }

            // ¿Tapa el paso de verdad? (un collider solido, no trigger)
            int solidos = 0, triggers = 0;
            foreach (Collider2D c in b.GetComponentsInChildren<Collider2D>(true))
            {
                if (c == null) continue;
                if (c.isTrigger) triggers++; else solidos++;
            }

            if (solidos == 0)
            {
                reporte.AppendLine("       OJO: no tiene ningun collider solido, asi que NO tapa el paso " +
                                   (triggers > 0 ? "(solo tiene triggers)." : "(es solo un dibujo)."));
                problemas++;
            }

            // ¿Se va a ver? (para el desvanecido)
            if (b.GetComponentsInChildren<SpriteRenderer>(true).Length == 0)
            {
                reporte.AppendLine("       OJO: no tiene SpriteRenderer, asi que no se va a ver el desvanecido " +
                                   "(igual desaparece de golpe).");
            }

            // ¿El progreso guardado dice que ya se abrio?
            string clave = string.IsNullOrEmpty(b.claveGuardado)
                ? "Bloqueo@" + b.gameObject.scene.name + "@" + nombre
                : b.claveGuardado;

            if (b.recordarQueYaSeAbrio && ProgresoJuego.YaMostrado(clave))
            {
                reporte.AppendLine("       OJO: el progreso guardado dice que YA SE ABRIO, asi que al darle Play " +
                                   "no va a aparecer. Usa 'Not Home > Borrar progreso guardado' para probarlo de nuevo.");
                problemas++;
            }
        }

        reporte.AppendLine("");
        reporte.AppendLine(problemas == 0
            ? "   Todo en orden: estos bloqueos deberian desaparecer al terminar la cinematica."
            : "   Hay " + problemas + " cosa(s) para revisar (mira arriba).");

        // Siempre como mensaje normal: si fuera advertencia y el filtro amarillo de la
        // consola estuviera apagado, el reporte no se veria.
        Debug.Log(reporte.ToString());
    }

    [MenuItem("Not Home/Bloqueos/Seleccionar los bloqueos de esta escena", false, 91)]
    private static void Seleccionar()
    {
        BloqueoTemporal[] bloqueos = Object.FindObjectsByType<BloqueoTemporal>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        GameObject[] objetos = new GameObject[bloqueos.Length];
        for (int i = 0; i < bloqueos.Length; i++) objetos[i] = bloqueos[i].gameObject;

        Selection.objects = objetos;
        Debug.Log("[Bloqueos] Seleccione " + objetos.Length + " objeto(s) con BloqueoTemporal.");
    }
}
#endif
