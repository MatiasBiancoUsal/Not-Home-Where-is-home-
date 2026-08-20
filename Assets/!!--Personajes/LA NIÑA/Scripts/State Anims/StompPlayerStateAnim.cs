using UnityEngine;

public class StompPlayerStateAnim : StatesAnimsAbstract
{
    public StompPlayerStateAnim(Animator animPlayer)
    {
        ActiveAnimation("stateAnim", 15, ref animPlayer);
    }
}
