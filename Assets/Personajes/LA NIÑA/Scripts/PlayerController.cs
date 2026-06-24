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
    [HideInInspector] public PlayerSuperJump superJump;
    [HideInInspector] public PlayerDash dash;
    [HideInInspector] public PlayerClimb climb;
    [HideInInspector] public PlayerAttacks attacks;
    [HideInInspector] public PlayerStomp stomp;

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
        superJump = GetComponent<PlayerSuperJump>();
        dash = GetComponent<PlayerDash>();
        climb = GetComponent<PlayerClimb>();
        attacks = GetComponent<PlayerAttacks>();
        stomp = GetComponent<PlayerStomp>();
    }

    private void FixedUpdate()
    {
        jump.CheckGround(); // mantiene actualizado si esta en el piso (lo usa tambien el climb)

        climb.OnUpdate(); // Trepar
        if (climb.IsClimbing) return; // mientras trepa, se pausan las demas mecanicas

        movement.Move(); //Movimiento
        superJump.OnUpdate(); // Super salto (corre despues del movimiento: lo frena mientras carga)
        jump.OnUpdate(); // Salto
        doubleJump.OnUpdate(); // Doble salto
        dash.OnUpdate(); // Dash
        attacks.OnUpdate(); // Ataques
        stomp.OnUpdate(); // Stomp

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

        controles.Player.Stomp.performed += OnStomp;

        controles.Player.Attack.performed += OnAttack;
    }


    private void OnDisable()
    {
        controles.Player.Jump.performed -= OnJump;
        controles.Player.Jump.canceled -= OnJumpRelease;

        controles.Player.Dash.performed -= OnDash;

        controles.Player.Stomp.performed -= OnStomp;

        controles.Player.Attack.performed -= OnAttack;

        controles.Disable();
    }



    // Metodos Inputs
    private void OnJump(InputAction.CallbackContext context)
    {
        // Si estamos trepando, ESPACIO nos despega saltando de la pared (y queda el doble salto).
        if (climb.IsClimbing)
        {
            climb.JumpOffWall();
            return;
        }

        // Si estamos cargando el super salto: con carga suficiente lo EJECUTA; si fue muy rapido
        // (carga insuficiente), TryExecute devuelve false y caemos a un salto NORMAL aca abajo.
        if (superJump.IsPreparing && superJump.TryExecute())
        {
            return;
        }

        // Si venimos en pleno super salto, el ESPACIO en el aire es un DOBLE salto (con su animacion),
        // sin que el coyote del piso lo convierta en un salto normal.
        if (superJump.IsSuperJumping)
        {
            doubleJump.TryDoubleJump();
            return;
        }

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
        if (climb.IsClimbing) return; // trepando no aplica el release del salto
        jump.JumpRelease();
    }

    private void OnDash(InputAction.CallbackContext context)
    {
        if (climb.IsClimbing) return; // no se puede dashear mientras trepa
        dash.DashHold();
    }

    private void OnStomp(InputAction.CallbackContext context)
    {
        stomp.StompHold();
    }

    private void OnAttack(InputAction.CallbackContext context)
    {
        attacks.AttackHold();
    }

}