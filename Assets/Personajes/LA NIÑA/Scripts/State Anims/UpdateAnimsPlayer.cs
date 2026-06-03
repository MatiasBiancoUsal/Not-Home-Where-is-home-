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
        // actualizacion animaciones de salto
        if (!playerController.jump.IsGrounded) // si el jugador está tocando el suelo
        {
            //estamos en el aire
            if (playerController.rb.linearVelocity.y > 0.1)
            {
                //subiendo
                animationManager.SetState(new JumpStartPlayerStateAnim(playerController.animPlayer));
            }
            else if (playerController.rb.linearVelocity.y < -0.1)
            {
                //cayendo
                animationManager.SetState(new JumpFallPlayerStateAnim(playerController.animPlayer));
            }

            return; // si el jugador no está tocando el suelo, no se actualizan las animaciones de movimiento
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
