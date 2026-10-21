using UnityEngine;

public class EnemyBall : MonoBehaviour
{
    public float speed = 5f;
    public float lifeTime = 5f;

    [Header("Paredes")]
    [Tooltip("Layers que FRENAN la bala (paredes, piso, etc.). Es lo principal y no depende de tags. Si queda vacio, usa la layer 'Ground'.")]
    public LayerMask wallLayers;
    [Tooltip("Fallback opcional por TAG, para paredes viejas que todavia usan el tag 'Wall'. Vacio = no se usa.")]
    public string wallTag = "Wall";

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // fallback: si no se configuro en el inspector, frenamos con la layer Ground.
        if (wallLayers.value == 0) wallLayers = LayerMask.GetMask("Ground");
    }

    public void SetDirection(Vector2 direction)
    {
        rb.linearVelocity = direction.normalized * speed;
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // La bala se frena y desaparece al tocar una pared (no la traspasa), asi el player puede
        // resguardarse detras de las paredes. La deteccion es por LAYER (principal, independiente
        // de los tags que ahora se usan para Dash/Stomp/SuperJump) y, como fallback, por el tag
        // viejo "Wall" (para las paredes que todavia lo usan).
        bool enLayerPared = (wallLayers.value & (1 << other.gameObject.layer)) != 0;
        bool tieneTagPared = !string.IsNullOrEmpty(wallTag) && other.CompareTag(wallTag);

        if (enLayerPared || tieneTagPared)
        {
            rb.linearVelocity = Vector2.zero;
            Destroy(gameObject);
        }
    }
}
