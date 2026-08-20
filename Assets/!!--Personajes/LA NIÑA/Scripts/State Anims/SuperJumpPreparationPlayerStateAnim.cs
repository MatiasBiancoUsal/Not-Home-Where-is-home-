using UnityEngine;

public class SuperJumpPreparationPlayerStateAnim : StatesAnimsAbstract
{
    public SuperJumpPreparationPlayerStateAnim(Animator animPlayer)
    {
        ActiveAnimation("stateAnim", 16, ref animPlayer);
    }
}
