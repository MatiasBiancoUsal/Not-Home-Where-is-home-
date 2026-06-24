using UnityEngine;

public class SuperJumpExpirePlayerStateAnim : StatesAnimsAbstract
{
    public SuperJumpExpirePlayerStateAnim(Animator animPlayer)
    {
        ActiveAnimation("stateAnim", 18, ref animPlayer);
    }
}
