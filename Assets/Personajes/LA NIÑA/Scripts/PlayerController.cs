using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Componentes
    [HideInInspector] public Rigidbody2D rb;
    public Animator animPlayer;
    public Controles controles;

    // Variables
    public float speed;

    // MECANICAS
    public UpdateAnimsPlayer updateAnimsPlayer;
    [HideInInspector] public PlayerMovement movement;

    private void Awake()
    {
        // Componentes
        rb = GetComponent<Rigidbody2D>();
        animPlayer = GetComponent<Animator>();
        controles = new Controles();

        // Conectar mecanicas
        updateAnimsPlayer = GetComponent<UpdateAnimsPlayer>();  
        movement = GetComponent<PlayerMovement>();
    }

    private void FixedUpdate()
    {
        movement.Move(); //Movimiento
        // Salto

    }

    private void Update()
    {
        updateAnimsPlayer.UpdateAnimations(); // Animaciones
    }

    private void OnEnable()
    {
        controles.Enable();
    }

    private void OnDisable()
    {
        controles.Disable();
    }
}