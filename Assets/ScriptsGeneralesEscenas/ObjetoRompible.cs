using UnityEngine;

// ============================================================
//  OBJETO ROMPIBLE POR HABILIDAD
//  Va EN el objeto rompible (que tiene un Collider2D). Se rompe solo si el PLAYER lo
//  toca con la habilidad que coincide con el TAG del objeto, y desde la direccion correcta:
//    - Tag "Dash"      -> rompe si el player esta dasheando (cualquier direccion).
//    - Tag "Stomp"     -> rompe si el player esta stompeando y le cae ENCIMA (desde arriba).
//    - Tag "SuperJump" -> rompe si el player viene en super salto y le pega de ABAJO hacia arriba.
//  Funciona con el collider en trigger o solido (escucha las dos colisiones).
// ============================================================
[RequireComponent(typeof(Collider2D))]
public class ObjetoRompible : MonoBehaviour
{
    [Tooltip("Efecto opcional (particulas/fragmentos) que se instancia al romperse. Vacio = solo desaparece.")]
    public GameObject efectoAlRomper;

    private Collider2D col;
    private bool roto;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryBreak(collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryBreak(other);
    }

    private void TryBreak(Collider2D otherCol)
    {
        // Solo el player rompe estos objetos.
        PlayerController player = otherCol.GetComponentInParent<PlayerController>();
        if (player == null) return;

        if (CumpleHabilidad(player, otherCol))
        {
            Romper();
        }
    }

    // Segun el TAG de ESTE objeto, chequea la habilidad correcta y la direccion del golpe.
    private bool CumpleHabilidad(PlayerController player, Collider2D playerCol)
    {
        if (CompareTag("Dash"))
        {
            return player.dash.IsDash; // dasheando, en cualquier direccion
        }

        if (CompareTag("Stomp"))
        {
            // stompeando (o recien impactado) y el player por ENCIMA del objeto.
            // Usamos IsStompImpacting porque el chequeo de piso corta el stomp un instante antes
            // de que se dispare esta colision (sino, los objetos en layer Ground no rompian).
            return player.stomp.IsStompImpacting && playerCol.bounds.center.y > col.bounds.center.y;
        }

        if (CompareTag("SuperJump"))
        {
            // en super salto y pegandole de ABAJO hacia arriba: el player esta por DEBAJO.
            return player.superJump.IsSuperJumping && playerCol.bounds.center.y < col.bounds.center.y;
        }

        return false; // el objeto no tiene un tag de habilidad conocido
    }

    private void Romper()
    {
        if (roto) return; // evita romperse dos veces (varias colisiones en el mismo frame)
        roto = true;

        if (efectoAlRomper != null)
        {
            Instantiate(efectoAlRomper, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}
