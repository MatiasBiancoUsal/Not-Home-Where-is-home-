using UnityEngine;

public class DoubleJumpPlayerStateAnim : StatesAnimsAbstract
{
    public DoubleJumpPlayerStateAnim(Animator animPlayer)
    {
        ActiveAnimation("stateAnim", 5, ref animPlayer);
    }
}
