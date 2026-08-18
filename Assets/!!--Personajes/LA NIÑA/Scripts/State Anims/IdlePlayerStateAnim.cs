using UnityEngine;

public class IdlePlayerStateAnim : StatesAnimsAbstract
{
    public IdlePlayerStateAnim(Animator animPlayer)
    {
        ActiveAnimation("stateAnim", 1, ref animPlayer);
    }
}
