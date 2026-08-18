using UnityEngine;

public class SuperJumpLoopPlayerStateAnim : StatesAnimsAbstract
{
    public SuperJumpLoopPlayerStateAnim(Animator animPlayer)
    {
        ActiveAnimation("stateAnim", 17, ref animPlayer);
    }
}
