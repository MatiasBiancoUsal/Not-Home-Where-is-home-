using UnityEngine;

public class ClimbWalkPlayerStateAnim : StatesAnimsAbstract
{
    public ClimbWalkPlayerStateAnim(Animator animPlayer)
    {
        ActiveAnimation("stateAnim", 8, ref animPlayer);
    }
}
