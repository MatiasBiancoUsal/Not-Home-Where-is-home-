using System.Collections;
using UnityEngine;

// ============================================================
//  UTILIDADES DEL BOSS FINAL
//  Ayudantes que comparten el boss, sus ataques y los mini bichos:
//  encontrar a la niña, pegarle, crear cuadrados de prueba, sacudir la camara...
//  No va en ningun objeto: lo usan los otros scripts solos.
// ============================================================
public static class UtilBoss
{
    private static Sprite cuadrado;
    private static PlayerController jugador;
    private static Collider2D colliderJugador;

    // Con Reload Domain desactivado los static se arrastran entre sesiones de Play.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarStatics()
    {
        cuadrado = null;
        jugador = null;
        colliderJugador = null;
    }

    // ---------- La niña ----------

    public static PlayerController Jugador()
    {
        if (jugador == null)
        {
            jugador = Object.FindFirstObjectByType<PlayerController>();
            colliderJugador = jugador != null ? jugador.GetComponent<Collider2D>() : null;
        }
        return jugador;
    }

    public static Collider2D ColliderJugador()
    {
        Jugador();
        return colliderJugador;
    }

    // El centro del cuerpo de la niña (no los pies).
    public static Vector2 PosicionJugador()
    {
        PlayerController pc = Jugador();
        if (pc == null) return Vector2.zero;

        return colliderJugador != null ? (Vector2)colliderJugador.bounds.center : (Vector2)pc.transform.position;
    }

    // ¿Este collider esta tocando a la niña? Funciona sin importar los layers ni si es trigger,
    // asi no hay que configurar la matriz de colisiones para nada del boss.
    public static bool TocaAlJugador(Collider2D propio)
    {
        Collider2D c = ColliderJugador();
        if (c == null || propio == null) return false;
        if (!c.enabled || !propio.enabled || !c.gameObject.activeInHierarchy) return false;

        ColliderDistance2D d = propio.Distance(c);
        return d.isValid && d.distance <= 0f;
    }

    // Le pega a la niña igual que un HurtBox: descuenta vida y la empuja.
    // Devuelve TRUE si el golpe entro (no lo freno el escudo ni la invulnerabilidad).
    public static bool PegarAlJugador(int danio, Vector2 origen, float empuje, float duracionEmpuje = 0.15f)
    {
        PlayerController pc = Jugador();
        if (pc == null || danio <= 0) return false;

        Damageable d = pc.GetComponent<Damageable>();
        if (d == null) return false;

        bool entro = d.ApplyDamage(danio, origen, empuje);

        if (entro && pc.movement != null && pc.gameObject.activeInHierarchy)
        {
            pc.movement.onKnockBack = true;
            pc.StartCoroutine(SoltarEmpuje(pc.movement, duracionEmpuje));
        }

        return entro;
    }

    private static IEnumerator SoltarEmpuje(PlayerMovement m, float segundos)
    {
        yield return new WaitForSeconds(segundos);
        if (m != null) m.onKnockBack = false;
    }

    // ---------- Layers ----------

    // El mismo layer de piso que usa la niña para saber si esta parada.
    public static LayerMask MascaraDePiso()
    {
        PlayerController pc = Jugador();
        if (pc != null && pc.jump != null) return pc.jump.groundMask;
        return LayerMask.GetMask("Default");
    }

    // El layer al que le pega el ataque de la niña (el "Enemy Layer" de PlayerAttacks).
    // Asi los bichos y el boss se pueden golpear sin tener que acordarse de ponerles el layer.
    public static int CapaDeEnemigos()
    {
        PlayerAttacks a = Object.FindFirstObjectByType<PlayerAttacks>();
        if (a == null) return -1;

        int mascara = a.enemyLayer.value;
        for (int i = 0; i < 32; i++)
        {
            if ((mascara & (1 << i)) != 0) return i;
        }
        return -1;
    }

    // ---------- Efectos ----------

    public static void Sacudir(float fuerza, float duracion)
    {
        if (CameraShaker.Instance != null) CameraShaker.Instance.Sacudir(fuerza, duracion);
    }

    public static void Sonar(AudioClip clip, float volumen, Vector3 posicion)
    {
        if (clip == null) return;

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(clip, volumen);
        else AudioSource.PlayClipAtPoint(clip, posicion, volumen);
    }

    // ---------- Cuadrados de prueba ----------

    // Un cuadrado blanco de 1x1 unidad, generado por codigo. Es lo que se ve mientras
    // no haya dibujos: despues se reemplaza arrastrando los sprites en el Inspector.
    public static Sprite Cuadrado()
    {
        if (cuadrado != null) return cuadrado;

        Texture2D tex = new Texture2D(4, 4);
        Color[] pixeles = new Color[16];
        for (int i = 0; i < pixeles.Length; i++) pixeles[i] = Color.white;
        tex.SetPixels(pixeles);
        tex.filterMode = FilterMode.Point;
        tex.Apply();

        cuadrado = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        return cuadrado;
    }

    // Arma un objeto de prueba (cuadrado de color con collider). Lo devuelve APAGADO para
    // que el que lo pide le agregue sus scripts y lo configure antes de que corra su Awake.
    public static GameObject CrearCuadrado(string nombre, Color color, Vector2 tamanio, int ordenDeDibujo)
    {
        GameObject go = new GameObject(nombre);
        go.SetActive(false);
        go.transform.localScale = new Vector3(tamanio.x, tamanio.y, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Cuadrado();
        sr.color = color;
        sr.sortingOrder = ordenDeDibujo;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;

        return go;
    }
}
