using UnityEngine;

public class JumpStartPlayerStateAnim : StatesAnimsAbstract
{
    public JumpStartPlayerStateAnim(Animator animPlayer)
    {
        ActiveAnimation("stateAnim", 3, ref animPlayer);
    }
}
