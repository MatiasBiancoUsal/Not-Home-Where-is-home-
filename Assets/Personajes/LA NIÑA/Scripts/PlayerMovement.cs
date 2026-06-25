using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private PlayerController playerController;

    [Header("Movement")]
    private bool isFacingRight = true; // variable booleana para verificar si el player esta mirando a la derecha
    private bool isMoving; // verifica si el player se esta moviendo o no

    [HideInInspector] public bool onKnockBack;

    // Getters
    public bool IsMoving // getter de la variable de isMoving que se actualiza en el metodo de Move para saber si el player se esta moviendo o no
    {
        get { return isMoving; }
    }

    public bool IsFacingRight // getter de la variable de isFacingRight que se actualiza en el metodo de Move para saber si el player esta mirando a la derecha o no
    {
        get { return isFacingRight; }
    }

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    
    public void Move() // controla el movimiento del player
    {

        // si ocurre un knock back return
        if (onKnockBack)
        {
            return;
        }

        // mientras corre el envion del salto de pared, no pisamos la velocidad horizontal
        if (playerController.climb.IsWallJumpLocked)
        {
            return;
        }

        // vector donde se almacena el valor del movimiento del player que se recibe desde el playerController
        Vector2 move = playerController.controles.Player.Move.ReadValue<Vector2>();

        // aplicamos la velocidad en x al rigidbody del player controller
        playerController.rb.linearVelocity = new Vector2(move.x * playerController.speed, playerController.rb.linearVelocity.y);

        // dentro del bool isMoving, guardamos si el player se mueve o no
        isMoving = move.x != 0;

        // llamado a la mecanica de FLIP
        if (move.x > 0 && !isFacingRight)
        {
            Flip();
        }
        else if (move.x < 0 && isFacingRight)
        {
            Flip();
        }
    }

    private void Flip() // controla la direccion del player
    {
        isFacingRight = !isFacingRight;

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (isFacingRight ? 1f : -1f);
        transform.localScale = scale;
    }

    // Fuerza hacia donde mira el player (1 = derecha, -1 = izquierda). Lo usa el salto de pared.
    public void SetFacing(float dir)
    {
        if (dir > 0f && !isFacingRight) Flip();
        else if (dir < 0f && isFacingRight) Flip();
    }

}