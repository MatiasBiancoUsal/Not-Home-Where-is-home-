using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.Services.Analytics;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UnityConsent;

// ============================================================
//  ANALYTICS DE NOT HOME (Unity Cloud / Unity Analytics)
//
//  No hay que ponerlo en ninguna escena: arranca SOLO al abrir el juego
//  (RuntimeInitializeOnLoadMethod), inicializa los servicios de Unity,
//  da el consentimiento y manda los eventos.
//
//  Eventos que manda (TODOS tienen que estar creados en el Dashboard de
//  Unity Cloud > Analytics > Event Manager, con los mismos parametros,
//  si no Unity los descarta):
//
//   EntrarZona            nombreZona, numeroZona, puntosGlobales
//   Muerte                nombreZona, causaMuerte, muertesEnZona, segundosEnZona
//   DesbloquearHabilidad  nombreHabilidad, costoHabilidad, puntosRestantes, nombreZona
//   PuntosInsuficientes   nombreHabilidad, costoHabilidad, puntosGlobales, nombreZona
//   PartidaIniciada       tipoInicio, nombreZona
//   JuegoTerminado        segundosSesion, muertesSesion
//
//  Los eventos de "cuantos jugadores nuevos / sesiones / tiempo jugado" ya los
//  manda Unity solo (eventos estandar), no hace falta programarlos.
// ============================================================
public static class AnalyticsJuego
{
    // ¿Mandar los eventos TAMBIEN mientras probamos en el editor?
    //
    // Normalmente NO: asi las pruebas del equipo no ensucian los numeros de los
    // jugadores reales. Se prende un ratito solo para comprobar que los eventos
    // llegan al dashboard antes de hacer la build.
    //
    // No se toca aca: se prende y se apaga desde el menu
    //    Not Home > Analytics > Mandar eventos desde el editor (PRUEBA)
    // que ademas te muestra un tilde cuando esta prendido, para que no te lo olvides.
    //
    // En la build no existe: ahi los eventos se mandan siempre.
    public const string PREF_DESDE_EDITOR = "NotHome.Analytics.DesdeElEditor";

    private static bool EnviarDesdeElEditor
    {
        get
        {
#if UNITY_EDITOR
            return UnityEditor.EditorPrefs.GetBool(PREF_DESDE_EDITOR, false);
#else
            return false;
#endif
        }
    }

    // Muestra en la Consola cada evento que se manda ("[Analytics] Muerte ...").
    private static readonly bool MOSTRAR_EN_CONSOLA = true;

    private static bool listo;
    private static readonly List<KeyValuePair<string, CustomEvent>> pendientes = new List<KeyValuePair<string, CustomEvent>>();

    // Lo ultimo que le pego a la niña: si muere, esa fue la causa.
    private static string ultimaCausa;

    private static string zonaActual;
    private static float inicioZona;
    private static int muertesEnZona;
    private static int muertesSesion;

    // El proyecto tiene Reload Domain apagado: los static no se reinician solos al dar Play.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarStatics()
    {
        listo = false;
        pendientes.Clear();
        ultimaCausa = null;
        zonaActual = null;
        inicioZona = 0f;
        muertesEnZona = 0;
        muertesSesion = 0;
        SceneManager.sceneLoaded -= AlCargarEscena;
        Application.quitting -= AlCerrarElJuego;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Arrancar()
    {
        SceneManager.sceneLoaded += AlCargarEscena;
        Inicializar();
    }

    private static async void Inicializar()
    {
        if (Application.isEditor && !EnviarDesdeElEditor) return;

        try
        {
            // Consentimiento del jugador para recolectar datos (lo que vimos en clase).
            // Analytics SI, publicidad NO.
            EndUserConsent.SetConsentState(new ConsentState
            {
                AnalyticsIntent = ConsentStatus.Granted,
                AdsIntent = ConsentStatus.Denied
            });

            await UnityServices.InitializeAsync();
            listo = true;

            // Lo que paso mientras se inicializaba (por ejemplo entrar a la primera zona).
            foreach (var e in pendientes) Grabar(e.Key, e.Value);
            pendientes.Clear();

            Application.quitting += AlCerrarElJuego;
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[Analytics] No se pudo inicializar Unity Services: " + ex.Message);
        }
    }

    // Al cerrar (o al apretar Stop en el editor) Unity apaga Analytics y manda solo lo
    // pendiente. Desde aca ya no hay que tocar AnalyticsService: tiraria
    // "ServicesInitializationException".
    private static void AlCerrarElJuego()
    {
        listo = false;
    }

    // ---------------- EVENTOS ----------------

    // Se llama solo al cargar una escena. Solo cuenta si es una zona DISTINTA a la
    // anterior: morir recarga la misma zona y eso no es "entrar".
    private static void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        if (modo != LoadSceneMode.Single) return;

        int numero = NumeroDeZona(escena.name);
        if (numero <= 0)
        {
            // Menu, creditos, etc. Al volver a una zona desde aca, vuelve a contar.
            zonaActual = null;
            return;
        }

        if (escena.name == zonaActual) return;

        zonaActual = escena.name;
        inicioZona = Time.realtimeSinceStartup;
        muertesEnZona = 0;

        Enviar("EntrarZona", new CustomEvent("EntrarZona")
        {
            { "nombreZona", escena.name },
            { "numeroZona", numero },
            { "puntosGlobales", PuntosGlobales() }
        });
    }

    // Lo llaman las cosas que lastiman a la niña (HurtBox, pared sombra, boss...) justo
    // antes de pegarle. Si el golpe la mata, esta es la causa que se manda en "Muerte".
    public static void RegistrarGolpe(string causa)
    {
        ultimaCausa = causa;
    }

    // Lo llama PlayerDeath cuando la vida llega a 0.
    public static void Muerte()
    {
        muertesEnZona++;
        muertesSesion++;

        Enviar("Muerte", new CustomEvent("Muerte")
        {
            { "nombreZona", ZonaActiva() },
            { "causaMuerte", string.IsNullOrEmpty(ultimaCausa) ? "Desconocida" : ultimaCausa },
            { "muertesEnZona", muertesEnZona },
            { "segundosEnZona", Mathf.RoundToInt(Time.realtimeSinceStartup - inicioZona) }
        });

        ultimaCausa = null;
    }

    public static void DesbloquearHabilidad(string habilidad, int costo, int puntosRestantes)
    {
        Enviar("DesbloquearHabilidad", new CustomEvent("DesbloquearHabilidad")
        {
            { "nombreHabilidad", habilidad },
            { "costoHabilidad", costo },
            { "puntosRestantes", puntosRestantes },
            { "nombreZona", ZonaActiva() }
        });
    }

    // Toco la flor pero no le alcanzaban los puntos: sirve para saber si los precios
    // de las habilidades estan muy altos.
    public static void PuntosInsuficientes(string habilidad, int costo, int puntosGlobales)
    {
        Enviar("PuntosInsuficientes", new CustomEvent("PuntosInsuficientes")
        {
            { "nombreHabilidad", habilidad },
            { "costoHabilidad", costo },
            { "puntosGlobales", puntosGlobales },
            { "nombreZona", ZonaActiva() }
        });
    }

    // tipo: "nueva" o "continuar".
    public static void PartidaIniciada(string tipo, string zona)
    {
        Enviar("PartidaIniciada", new CustomEvent("PartidaIniciada")
        {
            { "tipoInicio", tipo },
            { "nombreZona", zona }
        });
    }

    // Boss final derrotado.
    public static void JuegoTerminado()
    {
        Enviar("JuegoTerminado", new CustomEvent("JuegoTerminado")
        {
            { "segundosSesion", Mathf.RoundToInt(Time.realtimeSinceStartup) },
            { "muertesSesion", muertesSesion }
        });

        // Este es el ultimo evento de la partida y mucha gente cierra el juego apenas
        // ve los creditos. Lo subimos en el acto para no perderlo.
        if (listo)
        {
            try { AnalyticsService.Instance.Flush(); }
            catch (Exception ex) { Debug.LogWarning("[Analytics] No se pudo subir el final: " + ex.Message); }
        }
    }

    // ---------------- AYUDAS ----------------

    // Saca un nombre "lindo" de causa de muerte a partir del objeto que pego.
    // Recorre el objeto y sus padres buscando palabras conocidas, asi todas las
    // copias ("Pinchos largos (12)", "Pinchos cortos_0") cuentan como "Pinchos".
    public static string CausaDesdeObjeto(GameObject go)
    {
        for (Transform t = go != null ? go.transform : null; t != null; t = t.parent)
        {
            string n = t.name.ToLowerInvariant().Replace(" ", "").Replace("_", "");

            if (n.Contains("pincho")) return "Pinchos";
            if (n.Contains("bloqueaplastante") || n.Contains("aplast")) return "BloqueAplastante";
            if (n.Contains("laser")) return "Laser";
            if (n.Contains("paredsombra")) return "ParedSombra";
            if (n.Contains("ball") || n.Contains("bola")) return "BolaEnemigo";
            if (n.Contains("enemywall")) return "EnemigoPared";
            if (n.Contains("moco")) return "EnemigoMoco";
            if (n.Contains("cat1")) return "EnemigoCat1";
            if (n.Contains("cat2")) return "EnemigoCat2";
            if (n.Contains("boss")) return "Boss";
        }

        // No reconocido: el nombre del objeto sin "(Clone)" ni " (3)".
        return go == null ? "Desconocida" : Regex.Replace(go.name, @"\s*\(.*?\)", "").Trim();
    }

    private static void Enviar(string nombre, CustomEvent evento)
    {
        // Se escribe el evento CON SUS PARAMETROS, asi se puede verificar en la Consola
        // que los datos salen bien aunque todavia no se hayan subido.
        if (MOSTRAR_EN_CONSOLA) Debug.Log("[Analytics] " + Describir(nombre, evento));

        if (Application.isEditor && !EnviarDesdeElEditor) return;

        if (!listo)
        {
            pendientes.Add(new KeyValuePair<string, CustomEvent>(nombre, evento));
            return;
        }

        Grabar(nombre, evento);
    }

    // Arma el texto "Evento Muerte. nombreZona: Zona 3, causaMuerte: Pinchos, ..."
    private static string Describir(string nombre, CustomEvent evento)
    {
        string texto = "Evento " + nombre + ".";

        foreach (KeyValuePair<string, object> p in evento)
        {
            texto += " " + p.Key + ": " + p.Value + ",";
        }

        return texto.TrimEnd(',');
    }

    private static void Grabar(string nombre, CustomEvent evento)
    {
        try
        {
            AnalyticsService.Instance.RecordEvent(evento);

            // Unity junta los eventos y los sube cada tanto (puede tardar un minuto o
            // mas). Flush() los manda YA.
            //
            // En el juego de verdad NO se usa: mandar de a uno gasta bateria y datos
            // al pedo. Pero mientras probamos desde el editor si, asi vemos aparecer el
            // evento en el dashboard en segundos en vez de quedarnos esperando sin
            // saber si llego o si esta mal escrito.
            if (Application.isEditor) AnalyticsService.Instance.Flush();
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[Analytics] No se pudo mandar " + nombre + ": " + ex.Message);
        }
    }

    private static string ZonaActiva()
    {
        return SceneManager.GetActiveScene().name;
    }

    private static int PuntosGlobales()
    {
        return ScoreManager.Instance != null ? ScoreManager.Instance.PuntajeGlobal : 0;
    }

    // "Zona 3" -> 3. Cualquier otra escena -> 0.
    private static int NumeroDeZona(string nombreEscena)
    {
        if (!nombreEscena.StartsWith("Zona ")) return 0;
        return int.TryParse(nombreEscena.Substring(5), out int n) ? n : 0;
    }
}
