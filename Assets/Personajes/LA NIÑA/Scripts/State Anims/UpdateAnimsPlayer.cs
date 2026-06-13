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
        // Actualizar animaciones de ataque
        if (playerController.attacks.IsAttack)
        {
            if (playerController.attacks.AttackDirection == Vector2.up)
            {
                animationManager.SetState(new AttackUpPlayerStateAnims(playerController.animPlayer));
            }
            else if (playerController.attacks.AttackDirection == Vector2.down)
            {
                animationManager.SetState(new AttackDownPlayerStateAnims(playerController.animPlayer));
            }
            else
            {
                switch (playerController.attacks.ComboStep)
                {
                    case 1:  animationManager.SetState(new AttackStandingTwoPlayerStateAnim(playerController.animPlayer));   break;
                    case 2:  animationManager.SetState(new AttackStandingThreePlayerStateAnim(playerController.animPlayer)); break;
                    default: animationManager.SetState(new AttackStandingOnePlayerStateAnim(playerController.animPlayer));   break;
                }
            }
            return; // evita otras animaciones
        }

        // animacion de TREPAR: maxima prioridad
        if (playerController.climb.IsClimbing)
        {
            if (playerController.climb.IsClimbWalking) // subiendo o bajando
            {
                animationManager.SetState(new ClimbWalkPlayerStateAnim(playerController.animPlayer));
            }
            else // pegado quieto
            {
                animationManager.SetState(new ClimbIdlePlayerStateAnim(playerController.animPlayer));
            }
            return;
        }

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
