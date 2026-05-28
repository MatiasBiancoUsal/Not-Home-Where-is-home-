using UnityEngine;

public class UpdateAnimsPlayer : MonoBehaviour
{
    private PlayerController playerController;
    private AnimationManager animationManager;
    private PlayerMovement playerMovement;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
        animationManager = new AnimationManager();
        playerMovement = GetComponent<PlayerMovement>();
    }

    public void UpdateAnimations()
    {
        if (playerMovement.IsMoving) // si el jugador se esta moviendo
        {
            animationManager.SetState(new RunPlayerStateAnim(playerController.animPlayer));
        }
        else // si el jugador no se esta moviendo
        {
            animationManager.SetState(new IdlePlayerStateAnim(playerController.animPlayer));
        }
    }
}
