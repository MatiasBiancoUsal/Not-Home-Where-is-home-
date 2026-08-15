using UnityEngine;

public class RunPlayerStateAnim : StatesAnimsAbstract
{
    public RunPlayerStateAnim(Animator animPlayer)
    {
        ActiveAnimation("stateAnim", 2, ref animPlayer);
    }
}
