using UnityEngine;

public class AttackStandingTwoPlayerStateAnim : StatesAnimsAbstract
{
public AttackStandingTwoPlayerStateAnim(Animator animPlayer)
    {
        ActiveAnimation("stateAnim", 11, ref animPlayer);
    }
}
