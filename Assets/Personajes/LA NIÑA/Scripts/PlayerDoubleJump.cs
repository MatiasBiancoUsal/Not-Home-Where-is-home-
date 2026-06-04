using UnityEngine;

public class PlayerDoubleJump : MonoBehaviour
{
    private PlayerController playerController;

    [Header("Variables Doble Salto")]
    public bool canDoubleJump = true;   // A FUTURO: arranca desactivado y se desbloquea como mejora. Por ahora lo dejamos en true.
    public float doubleJumpForce;        // fuerza (velocidad) del salto en el aire. Se setea en el Inspector.
    public int maxAirJumps = 1;          // cuantos saltos extra en el aire (1 = doble salto, 2 = triple, etc.)

    private int airJumpsLeft;            // cuantos saltos de aire quedan disponibles ahora mismo
    private bool isDoubleJumping;        // si estamos en pleno doble salto (lo usa la animacion)

    //Getters
    public bool IsDoubleJumping
    {
        get { return isDoubleJumping; }
    }

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        airJumpsLeft = maxAirJumps;
    }

    public void OnUpdate()
    {
        // cuando tocamos el piso, recargamos los saltos de aire y reseteamos el estado
        if (playerController.jump.IsGrounded)
        {
            airJumpsLeft = maxAirJumps;
            isDoubleJumping = false;
        }
    }

    // intenta hacer un salto en el aire. Devuelve true si lo hizo, false si no pudo.
    public bool TryDoubleJump()
    {
        // si la habilidad esta bloqueada, no hacemos nada (para el sistema de mejoras a futuro)
        if (!canDoubleJump)
        {
            return false;
        }

        // si ya no quedan saltos en el aire, no hacemos nada
        if (airJumpsLeft <= 0)
        {
            return false;
        }

        // realizamos el salto en el aire
        airJumpsLeft--;
        playerController.rb.linearVelocity = new Vector2(playerController.rb.linearVelocity.x, doubleJumpForce);
        playerController.rb.gravityScale = playerController.normalGravity;

        isDoubleJumping = true; // para que se reproduzca la animacion de doble salto

        return true; // avisamos que SI se hizo el doble salto
    }
}
