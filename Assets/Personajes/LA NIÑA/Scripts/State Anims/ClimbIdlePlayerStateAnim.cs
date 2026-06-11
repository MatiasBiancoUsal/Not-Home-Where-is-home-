using UnityEngine;

public class ClimbIdlePlayerStateAnim : StatesAnimsAbstract
{
    public ClimbIdlePlayerStateAnim(Animator animPlayer)
    {
        ActiveAnimation("stateAnim", 7, ref animPlayer);
    }
}
