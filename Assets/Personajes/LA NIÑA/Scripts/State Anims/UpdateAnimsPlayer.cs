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

        // FLOTE post-dash: aunque todavia no cae (velocity.y = 0), mostramos la pose de CAIDA
        // asi no queda congelada en el dash y se ve mejor cuando flota antes de caer.
        if (playerController.dash.IsFloating)
        {
            animationManager.SetState(new JumpFallPlayerStateAnim(playerController.animPlayer));
            return;
        }

        // SUPER SALTO: animacion de SALIDA cuando se vencio el tiempo (caduca). Si el player
        // se pone a caminar, la cortamos (no la dejamos pisar al run).
        if (playerController.superJump.IsExpiring && !playerController.movement.IsMoving)
        {
            animationManager.SetState(new SuperJumpExpirePlayerStateAnim(playerController.animPlayer));
            return;
        }

        // SUPER SALTO: preparacion. Arranca con el INICIO (una vez) y al toque pasa al LOOP de espera.
        if (playerController.superJump.IsPreparing)
        {
            if (playerController.superJump.IsWaitingLoop)
            {
                animationManager.SetState(new SuperJumpLoopPlayerStateAnim(playerController.animPlayer));
            }
            else
            {
                animationManager.SetState(new SuperJumpPreparationPlayerStateAnim(playerController.animPlayer));
            }
            return;
        }

        // STOMP: pisoton en el aire. Mientras dura (impulso arriba + caida fuerte) mostramos
        // su animacion, por encima del salto/caida normal.
        if (playerController.stomp.IsStomping)
        {
            animationManager.SetState(new StompPlayerStateAnim(playerController.animPlayer));
            return;
        }

        // actualizacion animaciones de salto
        if (!playerController.jump.IsGrounded) // si el jugador est� tocando el suelo
        {
            //estamos en el aire
            if (playerController.rb.linearVelocity.y > 0.1)
            {
                //subiendo
                if (playerController.doubleJump.IsDoubleJumping) // doble salto primero (puede venir DESPUES de un super salto)
                {
                    animationManager.SetState(new DoubleJumpPlayerStateAnim(playerController.animPlayer));
                }
                else if (playerController.superJump.IsSuperJumping) // si venimos de un SUPER salto
                {
                    animationManager.SetState(new SuperJumpPlayerStateAnim(playerController.animPlayer));
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
