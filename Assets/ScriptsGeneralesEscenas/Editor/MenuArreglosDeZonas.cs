#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// ============================================================
//  ARREGLOS QUE SE APLICAN A TODAS LAS ZONAS DE UNA
//
//  Cada comando abre las 6 zonas, hace su arreglo, guarda y pasa a la siguiente.
//  Escriben en la Consola lo que tocaron, zona por zona.
//
//  IMPORTANTE: guardá tu escena antes de correr cualquiera de estos, porque abren
//  otras escenas y la que tengas abierta se cierra.
// ============================================================
public static class MenuArreglosDeZonas
{
    private const string CARPETA = "Assets/-------ESCENAS/";
    private static readonly string[] ZONAS = { "Zona 1", "Zona 2", "Zona 3", "Zona 4", "Zona 5", "Zona 6" };

    // ============================================================
    //  1. CAMARA: poder mirar hacia abajo con S
    // ============================================================

    [MenuItem("Not Home/Arreglos/Agregar 'mirar hacia abajo' a las camaras", false, 10)]
    private static void AgregarMirarAbajo()
    {
        RecorrerZonas("mirar hacia abajo", (escena, reporte) =>
        {
            int puestos = 0;

            foreach (CinemachinePositionComposer composer in
                     Object.FindObjectsByType<CinemachinePositionComposer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (composer.GetComponent<CamaraMirarAbajo>() != null) continue;

                Undo.AddComponent<CamaraMirarAbajo>(composer.gameObject);
                puestos++;
            }

            reporte.AppendLine(puestos == 0
                ? "   ya estaba puesto"
                : "   agregado en " + puestos + " camara(s)");

            return puestos > 0;
        });
    }

    // ============================================================
    //  2. CINEMATICAS: hacerlas mas rapidas
    // ============================================================

    [MenuItem("Not Home/Arreglos/Acelerar las cinematicas (x0.6)", false, 11)]
    private static void AcelerarCinematicas()
    {
        const float FACTOR = 0.6f;

        RecorrerZonas("cinematicas mas rapidas", (escena, reporte) =>
        {
            bool cambio = false;

            foreach (CinematicaFrames cine in
                     Object.FindObjectsByType<CinematicaFrames>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Undo.RecordObject(cine, "Acelerar cinematica");

                if (cine.frames != null)
                {
                    foreach (FrameCinematica f in cine.frames)
                    {
                        if (f == null) continue;
                        f.duracion = Redondear(f.duracion * FACTOR);
                        f.fadeAlSiguiente = Redondear(f.fadeAlSiguiente * FACTOR);
                    }
                }

                cine.esperaConElJuegoCongelado = Redondear(cine.esperaConElJuegoCongelado * FACTOR);
                cine.fadeAlNegro = Redondear(cine.fadeAlNegro * FACTOR);
                cine.esperaEnNegro = Redondear(cine.esperaEnNegro * FACTOR);
                cine.fadeAparecerPrimerFrame = Redondear(cine.fadeAparecerPrimerFrame * FACTOR);
                cine.esperaAntesDeSalir = Redondear(cine.esperaAntesDeSalir * FACTOR);
                cine.fadeUltimoFrameANegro = Redondear(cine.fadeUltimoFrameANegro * FACTOR);
                cine.fadeVolverAlJuego = Redondear(cine.fadeVolverAlJuego * FACTOR);

                EditorUtility.SetDirty(cine);
                reporte.AppendLine("   " + cine.name + ": acelerada");
                cambio = true;
            }

            if (!cambio) reporte.AppendLine("   sin cinematicas");
            return cambio;
        });
    }

    // Deja numeros lindos (0.42 en vez de 0.4199999) y nunca baja de un minimo usable.
    private static float Redondear(float valor)
    {
        return Mathf.Max(0.05f, Mathf.Round(valor * 100f) / 100f);
    }

    // ============================================================
    //  3. BLOQUES APLASTANTES: collider y velocidad de caida
    // ============================================================

    [MenuItem("Not Home/Arreglos/Revisar los bloques aplastantes", false, 12)]
    private static void RevisarBloques()
    {
        const float VELOCIDAD_SUGERIDA = 1.6f; // gravedad del bloque: mas bajo = cae mas lento

        RecorrerZonas("bloques aplastantes", (escena, reporte) =>
        {
            bool cambio = false;
            int revisados = 0;

            foreach (BloqueAplastante bloque in
                     Object.FindObjectsByType<BloqueAplastante>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                revisados++;

                // a) Que tenga un collider SOLIDO. Sin el, la niña lo atraviesa y despues
                //    tampoco puede pararse encima cuando ya cayo.
                bool tieneSolido = false;
                foreach (Collider2D col in bloque.GetComponents<Collider2D>())
                {
                    if (!col.isTrigger) { tieneSolido = true; break; }
                }

                if (!tieneSolido)
                {
                    SpriteRenderer sr = bloque.GetComponentInChildren<SpriteRenderer>();
                    BoxCollider2D nuevo = Undo.AddComponent<BoxCollider2D>(bloque.gameObject);

                    if (sr != null)
                    {
                        // El tamaño del dibujo, pasado a coordenadas locales del bloque.
                        Vector3 escala = bloque.transform.lossyScale;
                        Vector2 tam = sr.bounds.size;
                        nuevo.size = new Vector2(
                            escala.x != 0f ? tam.x / Mathf.Abs(escala.x) : tam.x,
                            escala.y != 0f ? tam.y / Mathf.Abs(escala.y) : tam.y);
                        nuevo.offset = bloque.transform.InverseTransformPoint(sr.bounds.center);
                    }

                    reporte.AppendLine("   " + bloque.name + ": LE FALTABA EL COLLIDER, se lo agregue");
                    cambio = true;
                }

                // b) Que no caiga como un piano.
                if (bloque.velocidadCaida > VELOCIDAD_SUGERIDA)
                {
                    Undo.RecordObject(bloque, "Velocidad del bloque");
                    reporte.AppendLine("   " + bloque.name + ": caia a " + bloque.velocidadCaida +
                                       ", lo baje a " + VELOCIDAD_SUGERIDA);
                    bloque.velocidadCaida = VELOCIDAD_SUGERIDA;
                    EditorUtility.SetDirty(bloque);
                    cambio = true;
                }

                // c) Aviso si esta apoyado en el piso: si lo esta, no va a caer nunca.
                Collider2D propio = bloque.GetComponent<Collider2D>();
                if (propio != null)
                {
                    Vector2 justoAbajo = (Vector2)propio.bounds.center - new Vector2(0f, propio.bounds.extents.y + 0.05f);
                    Collider2D piso = Physics2D.OverlapPoint(justoAbajo, 1 << 3); // layer Ground
                    if (piso != null)
                    {
                        reporte.AppendLine("   " + bloque.name + ": OJO, esta apoyado sobre '" +
                                           piso.name + "'. Asi no va a caer: subilo un poco.");
                    }
                }
            }

            if (revisados == 0) reporte.AppendLine("   sin bloques");
            return cambio;
        });
    }

    // ============================================================
    //  Motor: abre cada zona, corre el arreglo y guarda si hubo cambios
    // ============================================================

    private static void RecorrerZonas(string queEstamosHaciendo, System.Func<Scene, StringBuilder, bool> arreglo)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.Log("[Arreglos] Cancelado: primero guardá la escena que tenés abierta.");
            return;
        }

        string abierta = SceneManager.GetActiveScene().path;
        StringBuilder reporte = new StringBuilder();
        reporte.AppendLine("[Arreglos] " + queEstamosHaciendo);

        List<string> tocadas = new List<string>();

        foreach (string nombre in ZONAS)
        {
            string ruta = CARPETA + nombre + ".unity";
            if (!System.IO.File.Exists(ruta))
            {
                reporte.AppendLine(nombre + ": no existe la escena");
                continue;
            }

            Scene escena = EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);
            reporte.AppendLine(nombre + ":");

            bool cambio = arreglo(escena, reporte);

            if (cambio)
            {
                EditorSceneManager.MarkSceneDirty(escena);
                EditorSceneManager.SaveScene(escena);
                tocadas.Add(nombre);
            }
        }

        if (!string.IsNullOrEmpty(abierta)) EditorSceneManager.OpenScene(abierta, OpenSceneMode.Single);

        reporte.AppendLine(tocadas.Count == 0
            ? "No hubo que cambiar nada."
            : "Zonas guardadas: " + string.Join(", ", tocadas));

        Debug.Log(reporte.ToString());
    }
}
#endif
