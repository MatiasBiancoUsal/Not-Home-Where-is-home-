using UnityEngine;

// ============================================================
//  CHECKPOINT
//  Cuando la niña lo toca, pasa a ser el lugar donde reaparece si muere.
//
//  Se agrega SOLO a los objetos que se llaman "CHECKPOINT ..." (ver PuntoDeReaparicion).
//  Tambien se puede poner a mano en cualquier objeto.
//
//  Ya no bloquea el paso: sus colliders pasan a ser trigger y se mueve al layer
//  "Ignore Raycast". Antes eran bloques solidos (y alguno con el tag Wall), y la niña
//  se quedaba trabada contra ellos o se pegaba para trepar.
// ============================================================
public class Checkpoint : MonoBehaviour
{
    [Tooltip("Sonido al activarlo (opcional). Hay uno en Assets/Sonidos/Player/HintsStarsLite/Checkpoint 2.")]
    public AudioClip sonidoAlActivar;
    [Range(0f, 1f)] public float volumen = 0.7f;
    [Tooltip("Distancia maxima para buscar el piso debajo del checkpoint.")]
    public float distanciaAlPiso = 15f;

    private void Awake()
    {
        Collider2D[] colliders = GetComponents<Collider2D>();
        if (colliders.Length == 0)
        {
            colliders = new Collider2D[] { gameObject.AddComponent<BoxCollider2D>() };
        }

        foreach (Collider2D c in colliders)
        {
            c.isTrigger = true;
        }

        // Si quedara en el layer del piso, la niña "pisaria" el trigger y podria saltar en el aire.
        int ignorar = LayerMask.NameToLayer("Ignore Raycast");
        gameObject.layer = ignorar >= 0 ? ignorar : 2;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController pc = other.GetComponentInParent<PlayerController>();
        if (pc == null) return;

        Vector3 donde = LugarDeReaparicion(pc);
        string escena = gameObject.scene.name;

        if (PuntoDeReaparicion.EsElActual(escena, donde)) return;

        PuntoDeReaparicion.Guardar(escena, donde);

        if (sonidoAlActivar != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(sonidoAlActivar, volumen);
        }
    }

    // Parada en el piso, debajo del checkpoint. Si no hay piso, donde esta la niña ahora.
    private Vector3 LugarDeReaparicion(PlayerController pc)
    {
        Collider2D propio = GetComponent<Collider2D>();
        Vector2 origen = propio != null ? (Vector2)propio.bounds.center : (Vector2)transform.position;
        return PuntoDeReaparicion.EnElPiso(origen, pc, distanciaAlPiso);
    }
}
