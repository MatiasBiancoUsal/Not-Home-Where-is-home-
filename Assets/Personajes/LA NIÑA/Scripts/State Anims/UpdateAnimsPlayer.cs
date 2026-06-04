using UnityEngine;

public class UpdateAnimsPlayer : MonoBehaviour
{
    private PlayerController playerController;
    private AnimationManager animationManager;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
        animationManager = new AnimationManager();
    }

    public void UpdateAnimations()
    {
        // animacion de DASH: tiene prioridad sobre todo lo demas, por eso va primero
        if (playerController.dash.IsDash)
        {
            animationManager.SetState(new DashPlayerStateAnim(playerController.animPlayer));
            return; // evita otras animaciones
        }

        // actualizacion animaciones de salto
        if (!playerController.jump.IsGrounded) // si el jugador est� tocando el suelo
        {
            //estamos en el aire
            if (playerController.rb.linearVelocity.y > 0.1)
            {
                //subiendo
                if (playerController.doubleJump.IsDoubleJumping) // si venimos de un doble salto
                {
                    animationManager.SetState(new DoubleJumpPlayerStateAnim(playerController.animPlayer));
                }
                else
                {
                    animationManager.SetState(new JumpStartPlayerStateAnim(playerController.animPlayer));
                }
            }
            else if (playerController.rb.linearVelocity.y < -0.1)
            {
                //cayendo
                animationManager.SetState(new JumpFallPlayerStateAnim(playerController.animPlayer));
            }

            return; // evita otras animaciones
        }

        // actualizacion animaciones de movimiento
        if (playerController.movement.IsMoving) // si el jugador se esta moviendo
        {
            animationManager.SetState(new RunPlayerStateAnim(playerController.animPlayer));
        }
        else // si el jugador no se esta moviendo
        {
            animationManager.SetState(new IdlePlayerStateAnim(playerController.animPlayer));
        }
    }
}
