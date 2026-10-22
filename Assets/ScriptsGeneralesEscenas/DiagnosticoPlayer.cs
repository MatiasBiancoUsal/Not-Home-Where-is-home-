using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

// ============================================================
//  DIAGNOSTICO DE LA NIÑA (solo para probar en el editor)
//
//  Apretando F9 escribe en la consola TODO lo que puede estar trabandola:
//  si tiene el control, si su cuerpo tiene gravedad, si quedo trepando, si el juego
//  esta congelado, y que hay debajo de sus pies.
//
//  No hay que ponerlo en ninguna escena: se crea solo al arrancar el juego, y
//  SOLO dentro de Unity (en el juego final no existe).
// ============================================================
public class DiagnosticoPlayer : MonoBehaviour
{
    public static Key tecla = Key.F9;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Crear()
    {
        if (!Application.isEditor && !Debug.isDebugBuild) return;

        GameObject go = new GameObject("DiagnosticoPlayer");
        go.AddComponent<DiagnosticoPlayer>();
        DontDestroyOnLoad(go);
    }

    [Tooltip("Si la niña queda quieta en el aire mas de estos segundos, el reporte sale solo.")]
    public static float segundosParaAvisarSola = 1.2f;

    private float quietaEnElAire;
    private float proximoAvisoAutomatico;

    private void Update()
    {
        Keyboard k = Keyboard.current;
        if (k != null && k[tecla].wasPressedThisFrame)
        {
            Debug.Log(Reporte());
            return;
        }

        VigilarSiSeTrabo();
    }

    private Vector3 posicionAnterior;
    private float tiempoEmpujando;

    // Se da cuenta sola de DOS cosas:
    //   1. Quedo quieta en el aire (algo invisible la sostiene).
    //   2. Estas apretando para moverte y NO se mueve (algo invisible la frena).
    private void VigilarSiSeTrabo()
    {
        PlayerController pc = Object.FindFirstObjectByType<PlayerController>();

        if (pc == null || !pc.enabled || Time.timeScale <= 0f ||
            CinematicaFrames.EnCurso || TransicionZonas.EnCurso)
        {
            quietaEnElAire = 0f;
            tiempoEmpujando = 0f;
            return;
        }

        // ---- 1) Flotando ----
        bool flotando = pc.jump != null && !pc.jump.IsGrounded &&
                        pc.rb != null && pc.rb.linearVelocity.sqrMagnitude < 0.02f;

        quietaEnElAire = flotando ? quietaEnElAire + Time.unscaledDeltaTime : 0f;

        // ---- 2) Apretando para moverse sin avanzar ----
        float inputX = 0f;
        if (pc.controles != null) inputX = pc.controles.Player.Move.ReadValue<Vector2>().x;

        float avance = Mathf.Abs(pc.transform.position.x - posicionAnterior.x);
        posicionAnterior = pc.transform.position;

        bool empujandoSinAvanzar = Mathf.Abs(inputX) > 0.3f && avance < 0.004f;
        tiempoEmpujando = empujandoSinAvanzar ? tiempoEmpujando + Time.unscaledDeltaTime : 0f;

        string motivo = null;
        if (quietaEnElAire >= segundosParaAvisarSola)
            motivo = "quieta en el aire " + quietaEnElAire.ToString("0.0") + "s";
        else if (tiempoEmpujando >= 0.8f)
            motivo = "apretando para moverse " + tiempoEmpujando.ToString("0.0") + "s y NO avanza";

        if (motivo == null || Time.unscaledTime < proximoAvisoAutomatico) return;

        proximoAvisoAutomatico = Time.unscaledTime + 8f;   // que no inunde la consola
        ultimoInputX = inputX;

        Debug.LogWarning("[Diagnostico niña] SE TRABO: " + motivo + ".\n" + Reporte());
    }

    private float ultimoInputX;

    private string Reporte()
    {
        StringBuilder s = new StringBuilder();
        s.AppendLine("[Diagnostico niña] ---------------------------------");
        s.AppendLine("   Time.timeScale = " + Time.timeScale + (Time.timeScale == 0f ? "   <-- EL JUEGO ESTA CONGELADO" : ""));
        s.AppendLine("   Cinematica en curso = " + CinematicaFrames.EnCurso);
        s.AppendLine("   Transicion de zona en curso = " + TransicionZonas.EnCurso);
        s.AppendLine("   Escondida (boss) = " + EscondersePlayer.Escondida);

        PlayerController pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc == null)
        {
            s.AppendLine("   NO ENCONTRE A LA NIÑA en la escena.");
            return s.ToString();
        }

        s.AppendLine("   Posicion = " + pc.transform.position);
        s.AppendLine("   PlayerController activo = " + pc.enabled + (pc.enabled ? "" : "   <-- NO TIENE EL CONTROL"));

        Rigidbody2D rb = pc.rb;
        if (rb != null)
        {
            s.AppendLine("   Cuerpo: tipo = " + rb.bodyType + (rb.bodyType != RigidbodyType2D.Dynamic ? "   <-- NO LE AFECTA LA GRAVEDAD" : ""));
            s.AppendLine("           gravedad = " + rb.gravityScale + (Mathf.Approximately(rb.gravityScale, 0f) ? "   <-- GRAVEDAD EN CERO" : ""));
            s.AppendLine("           simulado = " + rb.simulated);
            s.AppendLine("           velocidad = " + rb.linearVelocity);
            s.AppendLine("           congelado = " + rb.constraints);
        }

        if (pc.climb != null)
        {
            s.AppendLine("   Trepando = " + pc.climb.IsClimbing + (pc.climb.IsClimbing ? "   <-- QUEDO AGARRADA A UNA PARED" : ""));
        }
        if (pc.jump != null)
        {
            s.AppendLine("   En el piso = " + pc.jump.IsGrounded);
        }
        if (pc.movement != null)
        {
            s.AppendLine("   Empujada (knockback) = " + pc.movement.onKnockBack);
        }

        Collider2D col = pc.GetComponent<Collider2D>();
        s.AppendLine("   Su collider esta " + (col == null ? "AUSENTE" : (col.enabled ? "prendido" : "APAGADO   <-- atraviesa todo")));

        // Que hay alrededor (lo que la puede estar sosteniendo o trabando).
        Collider2D[] cerca = Physics2D.OverlapCircleAll(pc.transform.position, 3f);
        s.AppendLine("   Objetos a menos de 3 unidades:");
        int n = 0;
        foreach (Collider2D c in cerca)
        {
            if (c == null || c.gameObject == pc.gameObject) continue;
            s.AppendLine("      - " + c.gameObject.name +
                         "  (layer " + LayerMask.LayerToName(c.gameObject.layer) +
                         ", tag " + c.tag + (c.isTrigger ? ", trigger" : "") + ")");
            if (++n >= 12) { s.AppendLine("      ... y mas"); break; }
        }
        if (n == 0) s.AppendLine("      (nada: esta en el aire)");

        QueLaSostiene(s, pc);
        BuscarPosicionesRotas(s);

        return s.ToString();
    }

    // Lo mas importante cuando queda flotando: QUE toca su cuerpo, y por que el juego
    // no lo considera "piso".
    private void QueLaSostiene(StringBuilder s, PlayerController pc)
    {
        Collider2D propio = pc.GetComponent<Collider2D>();
        if (propio == null) return;

        s.AppendLine("   --- Que esta tocando su cuerpo ---");

        Collider2D[] todos = Physics2D.OverlapBoxAll(propio.bounds.center,
                                                     (Vector2)propio.bounds.size + new Vector2(0.15f, 0.15f), 0f);
        int n = 0;
        LayerMask piso = pc.jump != null ? pc.jump.groundMask : 0;

        foreach (Collider2D c in todos)
        {
            if (c == null || c.gameObject == pc.gameObject) continue;
            if (c.transform.IsChildOf(pc.transform)) continue;

            bool esPiso = (piso.value & (1 << c.gameObject.layer)) != 0;
            ColliderDistance2D d = propio.Distance(c);

            s.AppendLine("      " + NombreCompleto(c.transform) +
                         "  (layer " + LayerMask.LayerToName(c.gameObject.layer) +
                         (c.isTrigger ? ", trigger" : "") +
                         (esPiso ? ", CUENTA como piso" : ", NO cuenta como piso") +
                         ", distancia " + d.distance.ToString("0.00") + ")");
            n++;
        }

        if (n == 0) s.AppendLine("      (nada la toca)");

        // Barrido alrededor: busca lo que le tapa el paso en las 4 direcciones, aunque
        // todavia no lo este tocando. Asi aparece el objeto invisible que la frena.
        s.AppendLine("   --- Que hay alrededor (barrido de 1 unidad) ---");

        Vector2[] dirs = { Vector2.right, Vector2.left, Vector2.up, Vector2.down };
        string[] nombres = { "DERECHA", "IZQUIERDA", "ARRIBA", "ABAJO" };

        for (int i = 0; i < 4; i++)
        {
            RaycastHit2D[] golpes = Physics2D.BoxCastAll(propio.bounds.center, propio.bounds.size * 0.9f,
                                                         0f, dirs[i], 1f);
            string encontrado = "";
            foreach (RaycastHit2D h in golpes)
            {
                if (h.collider == null || h.collider.gameObject == pc.gameObject) continue;
                if (h.collider.transform.IsChildOf(pc.transform)) continue;

                encontrado += "\n         " + NombreCompleto(h.collider.transform) +
                              "  (layer " + LayerMask.LayerToName(h.collider.gameObject.layer) +
                              (h.collider.isTrigger ? ", trigger" : ", SOLIDO") +
                              ", a " + h.distance.ToString("0.00") + ")";
            }

            s.AppendLine("      " + nombres[i] + ":" + (encontrado == "" ? " libre" : encontrado));
        }

        if (Mathf.Abs(ultimoInputX) > 0.3f)
        {
            s.AppendLine("   Estabas apretando hacia " + (ultimoInputX > 0 ? "la DERECHA" : "la IZQUIERDA") +
                         ": mira esa direccion en la lista de arriba.");
        }

        // El mismo chequeo que usa el juego para decidir si esta en el piso.
        if (pc.jump != null)
        {
            RaycastHit2D hit = Physics2D.CircleCast(pc.transform.position, pc.jump.groundRadius,
                                                    Vector2.down, pc.jump.groundCheckDistance, pc.jump.groundMask);
            s.AppendLine("   El chequeo de piso del juego " +
                         (hit.collider != null ? "SI encuentra: " + hit.collider.name
                                               : "NO encuentra nada (por eso cree que esta en el aire)"));
        }
    }

    // Busca objetos con posicion/tamaño "NaN" (invalido). Son los que llenan la consola con
    // "Assertion failed on expression: 'IsFinite(distanceForSort)'" en cada cuadro.
    private void BuscarPosicionesRotas(StringBuilder s)
    {
        s.AppendLine("   --- Objetos con posicion invalida (NaN) ---");

        int rotos = 0;

        Camera cam = Camera.main;
        if (cam != null && !EsFinito(cam.transform.position))
        {
            s.AppendLine("      LA CAMARA tiene posicion invalida: " + cam.transform.position);
            rotos++;
        }

        foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (r == null) continue;

            bool malaPos = !EsFinito(r.transform.position);
            bool malaEscala = !EsFinito(r.transform.lossyScale);
            bool malosBordes = !EsFinito(r.bounds.center) || !EsFinito(r.bounds.size);

            if (!malaPos && !malaEscala && !malosBordes) continue;

            s.AppendLine("      " + NombreCompleto(r.transform) +
                         (malaPos ? "  [posicion]" : "") +
                         (malaEscala ? "  [escala]" : "") +
                         (malosBordes ? "  [tamaño]" : ""));

            if (++rotos >= 10) { s.AppendLine("      ... y mas"); break; }
        }

        if (rotos == 0) s.AppendLine("      (ninguno: todo bien por este lado)");
    }

    private static bool EsFinito(Vector3 v)
    {
        return !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z) &&
               !float.IsInfinity(v.x) && !float.IsInfinity(v.y) && !float.IsInfinity(v.z);
    }

    // El camino completo del objeto, para encontrarlo facil en la Hierarchy.
    private static string NombreCompleto(Transform t)
    {
        string ruta = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            ruta = t.name + " > " + ruta;
        }
        return ruta;
    }
}
