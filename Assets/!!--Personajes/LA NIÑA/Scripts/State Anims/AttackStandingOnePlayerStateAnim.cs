using UnityEngine;

public class AttackStandingOnePlayerStateAnim : StatesAnimsAbstract
{
    public AttackStandingOnePlayerStateAnim(Animator animPlayer)
    {
        ActiveAnimation("stateAnim", 10, ref animPlayer);
    }
}
