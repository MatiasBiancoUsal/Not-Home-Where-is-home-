using UnityEngine;

public class SuperJumpPlayerStateAnim : StatesAnimsAbstract
{
    public SuperJumpPlayerStateAnim(Animator animPlayer)
    {
        ActiveAnimation("stateAnim", 19, ref animPlayer);
    }
}
