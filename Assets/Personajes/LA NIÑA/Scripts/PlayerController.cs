using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    // Componentes
    [HideInInspector] public Rigidbody2D rb;
    public Animator animPlayer;
    public Controles controles;

    [Header("Variables Movimiento")]
    public float speed;

    [Header("Variables de salto")]
    public float normalGravity = 1.5f;
    public float fallGravity = 4f;

    // MECANICAS
    [HideInInspector] public UpdateAnimsPlayer updateAnimsPlayer;
    [HideInInspector] public PlayerMovement movement;
    [HideInInspector] public PlayerJump jump;

    private void Awake()
    {
        // Componentes
        rb = GetComponent<Rigidbody2D>();
        animPlayer = GetComponent<Animator>();
        controles = new Controles();

        // Conectar mecanicas
        updateAnimsPlayer = GetComponent<UpdateAnimsPlayer>();
        movement = GetComponent<PlayerMovement>();
        jump = GetComponent<PlayerJump>();
    }

    private void FixedUpdate()
    {
        movement.Move(); //Movimiento
        jump.OnUpdate(); // Salto

    }

    private void Update()
    {
        updateAnimsPlayer.UpdateAnimations(); // Animaciones
    }

    private void OnEnable()
    {
        controles.Enable();

        controles.Player.Jump.performed += OnJump;
        controles.Player.Jump.canceled += OnJumpRelease;
    }

    private void OnDisable()
    {
        controles.Player.Jump.performed -= OnJump;
        controles.Player.Jump.canceled -= OnJumpRelease;

        controles.Disable();
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        jump.JumpHold();
    }

    private void OnJumpRelease(InputAction.CallbackContext context)
    {
        jump.JumpRelease();
    }
}