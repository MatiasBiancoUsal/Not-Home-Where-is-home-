using UnityEngine;

public class DashPlayerStateAnim : StatesAnimsAbstract
{
    public DashPlayerStateAnim(Animator animPlayer)
    {
        ActiveAnimation("stateAnim", 6, ref animPlayer);
    }
}
