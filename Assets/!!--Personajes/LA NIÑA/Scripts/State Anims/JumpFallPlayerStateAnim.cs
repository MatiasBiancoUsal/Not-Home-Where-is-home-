using UnityEngine;

public class JumpFallPlayerStateAnim : StatesAnimsAbstract
{
    public JumpFallPlayerStateAnim(Animator animPlayer)
    {
        ActiveAnimation("stateAnim", 4, ref animPlayer);
    }
}
