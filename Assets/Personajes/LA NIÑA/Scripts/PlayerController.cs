using System;
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
    [HideInInspector] public PlayerDoubleJump doubleJump;
    [HideInInspector] public PlayerDash dash;

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
        doubleJump = GetComponent<PlayerDoubleJump>();
        dash = GetComponent<PlayerDash>();
    }

    private void FixedUpdate()
    {
        movement.Move(); //Movimiento
        jump.OnUpdate(); // Salto
        doubleJump.OnUpdate(); // Doble salto
        dash.OnUpdate(); // Dash

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

        controles.Player.Dash.performed += OnDash;
    }


    private void OnDisable()
    {
        controles.Player.Jump.performed -= OnJump;
        controles.Player.Jump.canceled -= OnJumpRelease;
        controles.Player.Dash.performed -= OnDash;

        controles.Disable();
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        // El controller decide que tipo de salto hacer:
        if (jump.CanGroundJump())             // si hay un salto desde el piso o coyote disponible
        {
            jump.JumpHold();                  // salto normal
        }
        else if (!doubleJump.TryDoubleJump()) // si no, intentamos el doble salto (en el aire)
        {
            jump.BufferJump();                // si tampoco se pudo, guardamos el buffer del salto
        }
    }

    private void OnJumpRelease(InputAction.CallbackContext context)
    {
        jump.JumpRelease();
    }

    private void OnDash(InputAction.CallbackContext context)
    {
        dash.DashHold();
    }
}