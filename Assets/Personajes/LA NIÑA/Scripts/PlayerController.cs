using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    // Componentes
    [HideInInspector] public Rigidbody2D rb;
    public Animator animPlayer;
    public Controles controles;

    // Variables
    public float speed;

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
        jump.CheckGround(); // Salto

    }

    private void Update()
    {
        updateAnimsPlayer.UpdateAnimations(); // Animaciones
    }

    private void OnEnable()
    {
        controles.Enable();

        controles.Player.Jump.performed += OnJump;
    }

    private void OnDisable()
    {
        controles.Player.Jump.performed -= OnJump;

        controles.Disable();
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        jump.OnUpdate();
    }
}